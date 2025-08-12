using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Menus;
using StardewValley.Extensions;
using StardewValley.Tools;
using StardewValley.Monsters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.RageBait;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;

  static ModConfig Config = null!;

  const string RageBaitQualifiedItemId = "(O)selph.RageBaitCP_RageBait";
  const string IridiumLobsterQualifiedItemId = "(O)selph.RageBaitCP_IridiumLobster";

  public override void Entry(IModHelper helper) {
    Config = helper.ReadConfig<ModConfig>();
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.Input.ButtonPressed += OnButtonPressed;
    helper.Events.Display.MenuChanged += OnMenuChanged;
    helper.Events.Display.RenderingActiveMenu += OnRenderingActiveMenu;
    helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;

    // Iridium crab lmao
    helper.Events.GameLoop.DayStarted += OnDayStarted;
    helper.Events.Player.InventoryChanged += OnInventoryChanged;
  }

  [Flags]
  enum RageBaitMode {
    SPEED,
    REVERSE,
    HARD,
    RESIZE,
    CHALLENGE,
    DECOY,
    DRAIN,
    CLICK,
    NOFISH,
    NOBAR,
    DVD,
    SEGMENT,
  }
  static RageBaitMode[] allRageBaitModes = (RageBaitMode[])Enum.GetValues(typeof(RageBaitMode));

  class RageBaitStateClass {
    // Static fields
    public HashSet<RageBaitMode> rageBaitModes = new();
    // For RESIZE mode
    public bool isBobberBarShrinking;
    public int originalBobberBarHeight;
    // For DECOY mode
    public DecoyBobber? decoyBobber1;
    public DecoyBobber? decoyBobber2;
    public bool shouldDrawFish;
    // For DRAIN mode
    public int ticksSinceDrain;
    // For NOFISH and NOBAR
    public float originalPosition;
    // For CLICK mode
    public int ticksSinceClick;
    // For DVD mode
    public int menuX;
    public int menuY;
    public int dvdTrajectoryX;
    public int dvdTrajectoryY;
    // For SEGMENT mode
    public float lockedBarPosition;
  }
  static readonly PerScreen<RageBaitStateClass?> rageBaitState = new();
  static RageBaitStateClass? RageBaitState {
    get => rageBaitState.Value;
    set => rageBaitState.Value = value;
  }

  static IReflectedField<int>? fioSonarMode;

  void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    // get Generic Mod Config Menu's API (if it's installed)
    var configMenu = Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
    if (configMenu is not null) {
      configMenu.Register(
          mod: this.ModManifest,
          reset: () => Config = new ModConfig(),
          save: () => {
            Helper.WriteConfig(Config);
          });

      configMenu.AddNumberOption(
          mod: this.ModManifest,
          name: () => Helper.Translation.Get("Config.maxModifiers.name"),
          tooltip: () => Helper.Translation.Get("Config.maxModifiers.description"),
          getValue: () => Config.MaxModifiers,
          setValue: value => { Config.MaxModifiers = value; },
          min: 1,
          max: 10
      );

      configMenu.AddBoolOption(
          mod: this.ModManifest,
          name: () => Helper.Translation.Get("Config.showModifiers.name"),
          tooltip: () => Helper.Translation.Get("Config.showModifiers.description"),
          getValue: () => Config.ShowModifiers,
          setValue: value => { Config.ShowModifiers = value; }
      );

    }
    try {
      if (Helper.ModRegistry.IsLoaded("barteke22.FishingInfoOverlays")) {
        var overlayType = AccessTools.TypeByName("StardewMods.Overlay");
        if (overlayType is not null) {
          fioSonarMode = Helper.Reflection.GetField<int>(overlayType, "sonarMode");
        }
      }
    }
    catch (Exception ex) {
      StaticMonitor.Log($"Error reading into Fishing Info Overlay's config: {ex.ToString()}. Please report this to Rage Bait.", LogLevel.Warn);
    }
  }

  static void OnMenuChanged(object? sender, MenuChangedEventArgs e) {
    if (e.NewMenu is not BobberBar bobberBar) {
      RageBaitState = null;
      return;
    }
    var rod = Game1.player.CurrentTool as FishingRod;
    if (rod is not null
        && rod.GetBait()?.QualifiedItemId == RageBaitQualifiedItemId) {
      RageBaitState = new();
      RageBaitState.isBobberBarShrinking = true;
      RageBaitState.originalBobberBarHeight = bobberBar.bobberBarHeight;
      bobberBar.challengeBaitFishes = 4;
      RageBaitState.ticksSinceDrain = 0;
      RageBaitState.shouldDrawFish =
        (fioSonarMode?.GetValue() ?? -1) > 1
        || ((fioSonarMode?.GetValue() ?? -1) >= 0 && rod.GetTackleQualifiedItemIDs().Contains("(O)SonarBobber"));

      RageBaitState.rageBaitModes = ((RageBaitMode[])Enum.GetValues(typeof(RageBaitMode))).OrderBy(v => Game1.random.Next()).Take(Config.MaxModifiers).ToHashSet();
      foreach (var mode in RageBaitState.rageBaitModes) {
        switch (mode) {
          case RageBaitMode.REVERSE:
            bobberBar.bobberBarHeight = RageBaitState.rageBaitModes.Contains(RageBaitMode.SEGMENT)
              ? 190 : 568 / 2;
            bobberBar.bobberPosition = 60;
            break;
          case RageBaitMode.HARD:
            bobberBar.difficulty *= 2;
            break;
          case RageBaitMode.SPEED:
            bobberBar.distanceFromCatchPenaltyModifier *= 2;
            break;
          case RageBaitMode.DECOY:
            RageBaitState.decoyBobber1 = new DecoyBobber(284, bobberBar.difficulty, bobberBar.motionType);
            RageBaitState.decoyBobber2 = new DecoyBobber(60, bobberBar.difficulty, bobberBar.motionType);
            break;
          case RageBaitMode.NOFISH:
          case RageBaitMode.NOBAR:
            bobberBar.distanceFromCatchPenaltyModifier /= 1.5f;
            break;
          case RageBaitMode.DVD:
            RageBaitState.menuX = bobberBar.xPositionOnScreen;
            RageBaitState.menuY = bobberBar.yPositionOnScreen;
            RageBaitState.dvdTrajectoryX = Game1.random.Next(5, 10);
            RageBaitState.dvdTrajectoryY = Game1.random.Next(2, 5);
            if (Game1.random.NextBool()) RageBaitState.dvdTrajectoryX *= -1;
            if (Game1.random.NextBool()) RageBaitState.dvdTrajectoryY *= -1;
            break;
          case RageBaitMode.SEGMENT:
            RageBaitState.lockedBarPosition = bobberBar.bobberBarPos;
            break;
        }
      }
      if (Config.ShowModifiers) {
        int i = 0;
        foreach (var mode in RageBaitState.rageBaitModes) {
          i++;
          var msgKey = mode.ToString();
          if (msgKey == "SEGMENT" && Game1.options.gamepadControls) msgKey = "SEGMENT_Gamepad";
          Game1.addHUDMessage(
              new HUDMessage(ModEntry.Helper.Translation.Get(
                  "RageBaitMode",
                  new {
                    num = i,
                    mode = ModEntry.Helper.Translation.Get(msgKey ?? "CHALLENGE")
                  })) {
                noIcon = true,
              });
        }
      }
    }
  }

  static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e) {
    if (Game1.activeClickableMenu is not BobberBar bobberBar
        || bobberBar.fadeIn
        || bobberBar.fadeOut
        || RageBaitState is null) {
      return;
    }
    foreach (var mode in RageBaitState.rageBaitModes) {
      switch (mode) {
        case RageBaitMode.REVERSE:
          if (bobberBar.bobberInBar) {
            bobberBar.distanceFromCatching -= 0.005f * bobberBar.distanceFromCatchPenaltyModifier;
          } else {
            bobberBar.distanceFromCatching += 0.005f;
          }
          break;
        case RageBaitMode.RESIZE:
          if (RageBaitState.isBobberBarShrinking) {
            bobberBar.bobberBarHeight -= RageBaitState.originalBobberBarHeight / 120;
            bobberBar.bobberBarPos += RageBaitState.originalBobberBarHeight / 120;
            if (bobberBar.bobberBarHeight <= RageBaitState.originalBobberBarHeight / 3)
              RageBaitState.isBobberBarShrinking = false;
          } else {
            bobberBar.bobberBarHeight += RageBaitState.originalBobberBarHeight / 120;
            bobberBar.bobberBarPos -= RageBaitState.originalBobberBarHeight / 120;
            if (bobberBar.bobberBarHeight >= RageBaitState.originalBobberBarHeight * 1.5)
              RageBaitState.isBobberBarShrinking = true;
          }
          break;
        case RageBaitMode.SPEED:
          if (BobberInBarForReverse(bobberBar)) {
            bobberBar.distanceFromCatching += 0.002f;
          }
          bobberBar.bobberBarPos += bobberBar.bobberBarSpeed;
          if (bobberBar.bobberBarPos + bobberBar.bobberBarHeight > 568f) {
            bobberBar.bobberBarPos = 568 - bobberBar.bobberBarHeight;
          }
          bobberBar.bobberPosition += bobberBar.bobberSpeed + bobberBar.floaterSinkerAcceleration;
          break;
        case RageBaitMode.DECOY:
          RageBaitState.decoyBobber1?.update();
          RageBaitState.decoyBobber2?.update();
          break;
        case RageBaitMode.DRAIN:
          if (!BobberInBarForReverse(bobberBar)) {
            RageBaitState.ticksSinceDrain += 1;
            if (RageBaitState.ticksSinceDrain >= 12) {
              RageBaitState.ticksSinceDrain = 0;
              Game1.player.Stamina -= Game1.player.MaxStamina / 100;
              Game1.player.health -= Game1.player.maxHealth / 100;
            }
          }
          break;
        case RageBaitMode.CLICK:
          RageBaitState.ticksSinceClick += 1;
          if (BobberInBarForReverse(bobberBar) && RageBaitState.ticksSinceClick >= 20) {
            bobberBar.distanceFromCatching -= RageBaitState.rageBaitModes.Contains(RageBaitMode.SPEED) ? 0.004f : 0.002f;
          }
          break;
        case RageBaitMode.SEGMENT:
          bobberBar.bobberBarPos = RageBaitState.lockedBarPosition;
          break;
      }
    }
    if (!RageBaitState.rageBaitModes.Contains(RageBaitMode.CHALLENGE)) {
      bobberBar.challengeBaitFishes = 4;
    }
  }

  static void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e) {
    if (Game1.activeClickableMenu is not BobberBar bobberBar
        || bobberBar.fadeIn
        || bobberBar.fadeOut
        || RageBaitState is null) {
      return;
    }
    if (RageBaitState.rageBaitModes.Contains(RageBaitMode.NOFISH)) {
      RageBaitState.originalPosition = bobberBar.bobberPosition;
      bobberBar.bobberPosition = -10000;
    }
    if (RageBaitState.rageBaitModes.Contains(RageBaitMode.NOBAR)) {
      RageBaitState.originalPosition = bobberBar.bobberBarPos;
      bobberBar.bobberBarPos = -10000;
    }
    if (RageBaitState.rageBaitModes.Contains(RageBaitMode.DVD)) {
      RageBaitState.menuX += RageBaitState.dvdTrajectoryX;
      RageBaitState.menuY += RageBaitState.dvdTrajectoryY;
      if (RageBaitState.menuX < 0
          || RageBaitState.menuX + 96 > Game1.viewport.Width) {
        RageBaitState.dvdTrajectoryX *= -1;
      }
      if (RageBaitState.menuY < 0
          || RageBaitState.menuY + 636 > Game1.viewport.Height) {
        RageBaitState.dvdTrajectoryY *= -1;
      }
      bobberBar.xPositionOnScreen = RageBaitState.menuX;
      bobberBar.yPositionOnScreen = RageBaitState.menuY;
    }
  }

  static void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e) {
    if (Game1.activeClickableMenu is not BobberBar bobberBar
        || bobberBar.fadeIn
        || bobberBar.fadeOut
        || RageBaitState is null) {
      return;
    }
    if (RageBaitState.rageBaitModes.Contains(RageBaitMode.DECOY)
       && RageBaitState.decoyBobber1 is not null
       && RageBaitState.decoyBobber2 is not null) {
      if (RageBaitState.shouldDrawFish) {
        var fishTextureData = ItemRegistry.GetDataOrErrorItem(bobberBar.whichFish);
        var texture = fishTextureData.GetTexture();
        var sourceRect = fishTextureData.GetSourceRect();
        e.SpriteBatch.Draw(texture, new Vector2(bobberBar.xPositionOnScreen + 64 + 18, (float)(bobberBar.yPositionOnScreen + 12 + 24) + RageBaitState.decoyBobber1.bobberPosition) + bobberBar.fishShake + bobberBar.everythingShake, sourceRect, Color.White, 0f, new Vector2(9.5f, 9f), 3f, SpriteEffects.FlipHorizontally, 1f);
        e.SpriteBatch.Draw(texture, new Vector2(bobberBar.xPositionOnScreen + 64 + 18, (float)(bobberBar.yPositionOnScreen + 12 + 24) + RageBaitState.decoyBobber2.bobberPosition) + bobberBar.fishShake + bobberBar.everythingShake, sourceRect, Color.White, 0f, new Vector2(9.5f, 9f), 3f, SpriteEffects.FlipHorizontally, 1f);
        // Draw again to ensure the "real" bobber is on top
        e.SpriteBatch.Draw(texture, new Vector2(bobberBar.xPositionOnScreen + 64 + 18, (float)(bobberBar.yPositionOnScreen + 12 + 24) + bobberBar.bobberPosition) + bobberBar.fishShake + bobberBar.everythingShake, sourceRect, Color.White, 0f, new Vector2(9.5f, 9f), 3f, SpriteEffects.FlipHorizontally, 1f);
      } else {
        e.SpriteBatch.Draw(Game1.mouseCursors, new Vector2(bobberBar.xPositionOnScreen + 64 + 18, (float)(bobberBar.yPositionOnScreen + 12 + 24) + RageBaitState.decoyBobber1.bobberPosition) + bobberBar.fishShake + bobberBar.everythingShake, new Microsoft.Xna.Framework.Rectangle(614 + (bobberBar.bossFish ? 20 : 0), 1840, 20, 20), Color.White, 0f, new Vector2(10f, 10f), 2f, SpriteEffects.None, 0.88f);
        e.SpriteBatch.Draw(Game1.mouseCursors, new Vector2(bobberBar.xPositionOnScreen + 64 + 18, (float)(bobberBar.yPositionOnScreen + 12 + 24) + RageBaitState.decoyBobber2.bobberPosition) + bobberBar.fishShake + bobberBar.everythingShake, new Microsoft.Xna.Framework.Rectangle(614 + (bobberBar.bossFish ? 20 : 0), 1840, 20, 20), Color.White, 0f, new Vector2(10f, 10f), 2f, SpriteEffects.None, 0.88f);
        // Draw again to ensure the "real" bobber is on top
        e.SpriteBatch.Draw(Game1.mouseCursors, new Vector2(bobberBar.xPositionOnScreen + 64 + 18, (float)(bobberBar.yPositionOnScreen + 12 + 24) + bobberBar.bobberPosition) + bobberBar.fishShake + bobberBar.everythingShake, new Microsoft.Xna.Framework.Rectangle(614 + (bobberBar.bossFish ? 20 : 0), 1840, 20, 20), Color.White, 0f, new Vector2(10f, 10f), 2f, SpriteEffects.None, 0.88f);
      }
    }
    if (RageBaitState.rageBaitModes.Contains(RageBaitMode.NOFISH)) {
      bobberBar.bobberPosition = RageBaitState.originalPosition;
    }
    if (RageBaitState.rageBaitModes.Contains(RageBaitMode.NOBAR)) {
      bobberBar.bobberBarPos = RageBaitState.originalPosition;
    }
  }
  static void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
    if (Game1.activeClickableMenu is not BobberBar bobberBar
        || bobberBar.fadeIn
        || bobberBar.fadeOut
        || RageBaitState is null) {
      return;
    }
    if (e.Button.IsUseToolButton()) {
      RageBaitState.ticksSinceClick = 0;
      if (RageBaitState.rageBaitModes.Contains(RageBaitMode.SEGMENT)) {
        bobberBar.bobberBarPos -= bobberBar.bobberBarHeight * 0.75f;
        if (bobberBar.bobberBarPos < 0) {
          bobberBar.bobberBarPos = 0;
        }
        RageBaitState.lockedBarPosition = bobberBar.bobberBarPos;
      }
    }
    if (e.Button.IsActionButton()) {
      RageBaitState.ticksSinceClick = 0;
      if (RageBaitState.rageBaitModes.Contains(RageBaitMode.SEGMENT)) {
        bobberBar.bobberBarPos += bobberBar.bobberBarHeight * 0.75f;
        if (bobberBar.bobberBarPos + bobberBar.bobberBarHeight > 568) {
          bobberBar.bobberBarPos = 568 - bobberBar.bobberBarHeight;
        }
        RageBaitState.lockedBarPosition = bobberBar.bobberBarPos;
      }
    }
  }

  static bool BobberInBarForReverse(BobberBar bar) {
    if (RageBaitState?.rageBaitModes.Contains(RageBaitMode.REVERSE) ?? false) {
      return !bar.bobberInBar;
    } else {
      return bar.bobberInBar;
    }
  }

  static void OnDayStarted(object? sender, DayStartedEventArgs e) {
    Utility.ForEachLocation(location => {
      foreach (var obj in location.objects.Values) {
        if (obj is CrabPot crabPot
            && crabPot.heldObject.Value is not null
            && crabPot.bait.Value?.QualifiedItemId == RageBaitQualifiedItemId) {
          //            && !crabPot.bait.Value.modData.ContainsKey($"{ModEntry.UniqueId}_AlreadyChecked")) {
          //          crabPot.bait.Value.modData.Add($"{ModEntry.UniqueId}_AlreadyChecked", "");
          if (Game1.random.NextBool(0.005)) {
            crabPot.heldObject.Value = ItemRegistry.Create<SObject>(IridiumLobsterQualifiedItemId);
          }
          crabPot.heldObject.Value.Quality = SObject.bestQuality;
          crabPot.heldObject.Value.Stack = 2;
        }
      }
      return true;
    });
  }

  static void OnInventoryChanged(object? sender, InventoryChangedEventArgs e) {
    int crabCount = 0;
    bool stoleMoney = false;
    foreach (var item in e.Added) {
      if (item.QualifiedItemId == IridiumLobsterQualifiedItemId) {
        crabCount += item.Stack;
        for (int i = 0; i < item.Stack; i++) {
          var iridiumLobster = new RockCrab(e.Player.Tile * 64f + new Vector2(Game1.random.Next(-10, 10), Game1.random.Next(-10, 10)), "Lava Crab") {
            Speed = 10,
          };
          iridiumLobster.shellGone.Value = true;
          iridiumLobster.waiter = false;
          iridiumLobster.moveTowardPlayer(-1);
          iridiumLobster.objectsToDrop.Clear();
          if (e.Player.Money > 2000) {
            e.Player.Money -= 2000;
            stoleMoney = true;
            for (var j = 0; j < 8; j++) {
              iridiumLobster.objectsToDrop.Add("GoldCoin");
            }
          }
          iridiumLobster.objectsToDrop.Add("337");
          //Vector2 pos = Utility.recursiveFindOpenTileForCharacter(iridiumLobster, e.Player.currentLocation, iridiumLobster.Tile + new Vector2(0f, 1f), 20, allowOffMap: false);
          //iridiumLobster.setTileLocation(pos);
          e.Player.currentLocation.characters.Add(iridiumLobster);
        }
        e.Player.removeItemFromInventory(item);
      }
    }
    if (crabCount > 0) {
      Game1.addHUDMessage(
          new HUDMessage(ModEntry.Helper.Translation.Get(
              (stoleMoney ? "IridiumLobster" : "IridiumLobsterNoMoney") +
              (crabCount == 1 ? "" : "Plural"))) {
            noIcon = true,
          });
    }
  }
}

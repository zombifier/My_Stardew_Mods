using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.GameData.Shops;
using StardewValley.BellsAndWhistles;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.CustomBuilders;

static class GeodeBreakers {
  public static void RegisterCustomTriggers() {
    GameLocation.RegisterTileAction($"{ModEntry.UniqueId}_OpenGeodeBreaker", OpenGeodeBreaker);
  }

  public static void RegisterEvents(IModHelper helper) {
    helper.Events.Display.MenuChanged += OnMenuChanged;
  }

  public static void ApplyPatches(Harmony harmony) {
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(GeodeMenu), nameof(GeodeMenu.performHoverAction)),
        postfix: new HarmonyMethod(typeof(GeodeBreakers), nameof(GeodeMenu_performHoverAction_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(GeodeMenu), nameof(GeodeMenu.receiveLeftClick)),
        postfix: new HarmonyMethod(typeof(GeodeBreakers), nameof(GeodeMenu_receiveLeftClick_Postfix)));
  }

  static readonly PerScreen<NPC?> geodeBreaker = new();

  static void OnMenuChanged(object? sender, MenuChangedEventArgs e) {
    if (e.OldMenu is GeodeMenu) {
      geodeBreaker.Value = null;
    }
    if (e.NewMenu is GeodeMenu geodeMenu && geodeBreaker.Value is not null) {
      // Pick a texture that exists
      string[] possibleTextureNames = [
        $"{geodeBreaker.Value.Sprite.textureName.Value}_GeodeBreaker",
        $"Characters/{NPC.getTextureNameForCharacter(geodeBreaker.Value.Name)}_GeodeBreaker",
        $"{geodeBreaker.Value.Sprite.textureName.Value}",
        "Characters/Clint",
      ];
      var textureName = possibleTextureNames.FirstOrDefault(Game1.content.DoesAssetExist<Texture2D>);
      if (String.IsNullOrEmpty(textureName)) {
        ModEntry.StaticMonitor.Log("ERROR: No valid texture for geode breaker found? This should not happen.", LogLevel.Error);
        return;
      }
      geodeMenu.clint = new AnimatedSprite(
          textureName,
          8,
          32,
          48);
    }
  }

  public static bool OpenGeodeBreaker(GameLocation location, string[] args, Farmer farmer, Point point) {
    if (!Utils.TileActionCommon(location, args, farmer, point, out var npcId, out var _)) {
      return false;
    }
    var geodeCracker = Game1.getCharacterFromName(npcId);
    if (geodeCracker is null) {
      ModEntry.StaticMonitor.Log($"Error when opening geode cracker menu: {npcId} not found?", LogLevel.Warn);
      return false;
    }
    geodeBreaker.Value = geodeCracker;
    Game1.activeClickableMenu = new GeodeMenu();
    return true;
  }

  static void GeodeMenu_performHoverAction_Postfix(GeodeMenu __instance) {
    if (__instance.alertTimer > 0 || geodeBreaker.Value is null) return;
    if (Game1.player.Money < 25) {
      __instance.descriptionText = Game1.content.LoadStringReturnNullIfNotFound($"Strings/UI:GeodeMenu_Description_NotEnoughMoney_{geodeBreaker.Value.Name}") ?? __instance.descriptionText;
    } else {
      __instance.descriptionText = Game1.content.LoadStringReturnNullIfNotFound($"Strings/UI:GeodeMenu_Description_{geodeBreaker.Value.Name}") ?? __instance.descriptionText;
    }
  }

  static void GeodeMenu_receiveLeftClick_Postfix(GeodeMenu __instance) {
    if (geodeBreaker.Value is null) return;
    if (__instance.wiggleWordsTimer == 500 && __instance.alertTimer == 1500) {
      __instance.descriptionText = Game1.content.LoadStringReturnNullIfNotFound($"Strings/UI:GeodeMenu_InventoryFull_{geodeBreaker.Value}") ?? __instance.descriptionText;
    }
  }
}

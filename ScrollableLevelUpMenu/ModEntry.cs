using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Pathfinding;
using StardewValley.GameData.Locations;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using ModNameTooltip;
using Selph.StardewMods.Common;

namespace Selph.StardewMods.ScrollableLevelUpMenu;

internal sealed class ModEntry : Mod {
  static IMonitor StaticMonitor = null!;
  static IModHelper StaticHelper = null!;
  static IModNameAPI? modNameApi;
  public override void Entry(IModHelper helper) {
    WarpPathfindingCache.IgnoreLocationNames.Remove("Backwoods");
    StaticMonitor = Monitor;
    StaticHelper = helper;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.Display.MenuChanged += OnMenuChanged;
    helper.Events.Input.ButtonPressed += OnButtonPressed;
    helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
    var harmony = new Harmony(this.ModManifest.UniqueID);

    harmony.Patch(
        original: AccessTools.DeclaredConstructor(typeof(LevelUpMenu), [typeof(int), typeof(int)]),
        postfix: new HarmonyMethod(AccessTools.Method(typeof(ModEntry),
            nameof(LevelUpMenu_Constructor_Postfix))));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(LevelUpMenu), nameof(LevelUpMenu.draw)),
        transpiler: new HarmonyMethod(AccessTools.Method(typeof(ModEntry),
          nameof(LevelUpMenu_draw_Transpiler))));
  }

  const int MAX_HEIGHT = 64 * 12;

  static void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    try {
      modNameApi = StaticHelper.ModRegistry.GetApi<IModNameAPI>("mushymato.ModNameTooltip");
    }
    catch (Exception exception) {
      StaticMonitor.Log($"Error registering the ModName API: {exception.ToString()}", LogLevel.Warn);
    }
  }

  // Shrink the level up menu's height to a max size
  static void LevelUpMenu_Constructor_Postfix(LevelUpMenu __instance, int skill, int level) {
    __instance.height = Math.Min(__instance.height, MAX_HEIGHT + 256 - 16);
  }

  static void OnMenuChanged(object? sender, MenuChangedEventArgs e) {
    if (e.NewMenu is LevelUpMenu) {
      isModifiedDraw.Value = false;
      currentScroll.Value = 0;
      maxScroll.Value = 0;
      isScrollingDown.Value = true;
      maxScrollLinger.Value = 0;
    }
  }
  static PerScreen<bool> isModifiedDraw = new();
  static PerScreen<int> currentScroll = new();
  static PerScreen<int> maxScroll = new();
  static PerScreen<bool> isScrollingDown = new();
  static PerScreen<int> maxScrollLinger = new();
  static IEnumerable<CodeInstruction> LevelUpMenu_draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
    // Find the variable that stores the line draw offset
    CodeMatcher matcher = new(instructions, generator);
    matcher
      .MatchStartForward(
          new CodeMatch(OpCodes.Ldarg_0),
          new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.yPositionOnScreen))),
          new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(IClickableMenu), nameof(IClickableMenu.spaceToClearTopBorder))),
          new CodeMatch(OpCodes.Add),
          new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)80)
          )
      .MatchStartForward(
          new CodeMatch(static inst => inst.IsStloc())
          )
      .ThrowIfNotMatch($"Could not find entry point 0 for {nameof(LevelUpMenu_draw_Transpiler)}");
    var loadRecipePositionVar = matcher.Instruction.StToLd().LdToLda();
    // Insert after the variable, prep it and the spritebatch
    matcher
      //.MatchStartForward(
      //    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LevelUpMenu), nameof(LevelUpMenu.newCraftingRecipes)))
      //    )
      //.MatchStartForward(
      //    new CodeMatch(static inst => inst.IsStloc())
      //    )
      //.ThrowIfNotMatch($"Could not find entry point 1 for {nameof(LevelUpMenu_draw_Transpiler)}")
      .Advance(1)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Ldarg_1),
          loadRecipePositionVar,
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(MaybePrepRecipePositionAndSpriteBatch)))
          );

    // Draw mod the item originates from
    matcher
      .MatchStartForward(
          new CodeMatch(static inst => inst.IsLdloc()),
          new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CraftingRecipe), nameof(CraftingRecipe.isCookingRecipe)))
          )
      .ThrowIfNotMatch($"Could not find entry point 1 for {nameof(LevelUpMenu_draw_Transpiler)}");
    var loadRecipeVar = matcher.Instruction;
    matcher
      .MatchStartForward(
          new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(CraftingRecipe), nameof(CraftingRecipe.drawMenuView))))
      .ThrowIfNotMatch($"Could not find entry point 1.5 for {nameof(LevelUpMenu_draw_Transpiler)}")
      .Advance(1)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Ldarg_1),
          loadRecipeVar,
          loadRecipePositionVar,
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(MaybeDrawModInfo)))
          );

    // Reset spritebatch after drawing the info and recipes
    matcher
      .MatchStartForward(new CodeMatch(OpCodes.Endfinally)) // recipe draw
      .Advance(1)
      .ThrowIfNotMatch($"Could not find entry point 2 for {nameof(LevelUpMenu_draw_Transpiler)}");
    var labels = matcher.Instruction.ExtractLabels();
    matcher
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
          new CodeInstruction(OpCodes.Ldarg_1),
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(MaybeResetSpriteBatch))));
    return matcher.InstructionEnumeration();
  }

  static void MaybePrepRecipePositionAndSpriteBatch(LevelUpMenu menu, SpriteBatch b, ref int recipePosition) {
    try {
      isModifiedDraw.Value = false;
      maxScroll.Value = 0;
      foreach (var extraInfo in menu.extraInfoForLevel) {
        maxScroll.Value += 48;
      }
      foreach (var recipe in menu.newCraftingRecipes) {
        maxScroll.Value += (recipe.bigCraftable ? 128 : 64) + 8;
        if (modNameApi?.TryGetModName(recipe, out var modName) is true && modName.ModInfo is not null) {
          maxScroll.Value += 4;
        }
      }
      if (maxScroll.Value >= MAX_HEIGHT) {
        isModifiedDraw.Value = true;
        // Set limited draw rectangle
        b.End();
        Rectangle scissorRectangle = new(menu.xPositionOnScreen, recipePosition, menu.width, MAX_HEIGHT);
        b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, Utility.ScissorEnabled);
        b.GraphicsDevice.ScissorRectangle = scissorRectangle;
        // Set draw offset
        recipePosition -= currentScroll.Value;
        if (maxScrollLinger.Value < 60) {
          maxScrollLinger.Value++;
        } else {
          if (isScrollingDown.Value) {
            currentScroll.Value++;
            if (currentScroll.Value >= maxScroll.Value - MAX_HEIGHT) {
              isScrollingDown.Value = false;
              maxScrollLinger.Value = 0;
            }
          } else {
            currentScroll.Value--;
            if (currentScroll.Value <= 0) {
              isScrollingDown.Value = true;
              maxScrollLinger.Value = 0;
            }
          }
        }
      }
    }
    catch (Exception e) {
      StaticMonitor.Log($"Error when drawing level up menu: {e}", LogLevel.Error);
    }
  }

  static void MaybeDrawModInfo(LevelUpMenu menu, SpriteBatch b, CraftingRecipe recipe, ref int position) {
    try {
      if (modNameApi?.TryGetModName(recipe, out var modName) is true && modName.ModInfo is not null) {
        var text2 = modName.ModName;
        b.DrawString(Game1.smallFont, text2,
            new Vector2(
              (menu.xPositionOnScreen + menu.width / 2) - Game1.smallFont.MeasureString(text2).X / 2f - 64f,
              position + (recipe.bigCraftable ? 38 : 12) + 28), modName.ModNameColor);
        position += 4;
      }
    }
    catch (Exception e) {
      StaticMonitor.Log($"Error when drawing level up menu: {e}", LogLevel.Error);
    }
  }

  static void MaybeResetSpriteBatch(LevelUpMenu menu, SpriteBatch b) {
    try {
      if (isModifiedDraw.Value) {
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
      }
    }
    catch (Exception e) {
      StaticMonitor.Log($"Error when drawing level up menu: {e}", LogLevel.Error);
    }
  }

  static void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
    if (Game1.activeClickableMenu is not LevelUpMenu menu
        || !isModifiedDraw.Value) return;
    if (e.Button == SButton.RightThumbstickDown || e.Button == SButton.RightThumbstickUp) {
      maxScrollLinger.Value = -120;
      isScrollingDown.Value = true;
      currentScroll.Value += e.Button == SButton.RightThumbstickDown ? 20 : -20;
      currentScroll.Value = Utility.Clamp(currentScroll.Value, 0, maxScroll.Value - MAX_HEIGHT);
      if (currentScroll.Value >= maxScroll.Value - MAX_HEIGHT) {
        isScrollingDown.Value = false;
      }
      if (currentScroll.Value <= 0) {
        isScrollingDown.Value = true;
      }
    }
  }

  static void OnMouseWheelScrolled(object? sender, MouseWheelScrolledEventArgs e) {
    if (Game1.activeClickableMenu is not LevelUpMenu menu
        || !isModifiedDraw.Value) return;
    maxScrollLinger.Value = -120;
    isScrollingDown.Value = true;
    currentScroll.Value += e.Delta < 0 ? 20 : -20;
    currentScroll.Value = Utility.Clamp(currentScroll.Value, 0, maxScroll.Value - MAX_HEIGHT);
    if (currentScroll.Value >= maxScroll.Value - MAX_HEIGHT) {
      isScrollingDown.Value = false;
    }
    if (currentScroll.Value <= 0) {
      isScrollingDown.Value = true;
    }
  }
}

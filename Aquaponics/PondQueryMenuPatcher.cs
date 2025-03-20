using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Extensions;
using StardewValley.Buildings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LeFauxMods.Common.Integrations.CustomBush;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Aquaponics;

static class PondQueryMenuPatcher {
  public static void ApplyPatches(Harmony harmony) {
    // Adding a new button to pond menu to clear crops
    harmony.Patch(
        original: AccessTools.DeclaredConstructor(typeof(PondQueryMenu),
          new[] {typeof(FishPond)}),
        postfix: new HarmonyMethod(typeof(PondQueryMenuPatcher),
          nameof(PondQueryMenuPatcher.PondQueryMenu_Constructor_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(PondQueryMenu),
          nameof(PondQueryMenu.performHoverAction)),
        postfix: new HarmonyMethod(typeof(PondQueryMenuPatcher),
          nameof(PondQueryMenuPatcher.PondQueryMenu_performHoverAction_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(PondQueryMenu),
          nameof(PondQueryMenu.draw)),
        prefix: new HarmonyMethod(typeof(PondQueryMenuPatcher),
          nameof(PondQueryMenuPatcher.PondQueryMenu_draw_Prefix)),
        postfix: new HarmonyMethod(typeof(PondQueryMenuPatcher),
          nameof(PondQueryMenuPatcher.PondQueryMenu_draw_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(PondQueryMenu),
          nameof(PondQueryMenu.receiveLeftClick)),
        postfix: new HarmonyMethod(typeof(PondQueryMenuPatcher),
          nameof(PondQueryMenuPatcher.PondQueryMenu_receiveLeftClick_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(IClickableMenu),
          nameof(IClickableMenu.populateClickableComponentList)),
        postfix: new HarmonyMethod(typeof(PondQueryMenuPatcher),
          nameof(PondQueryMenuPatcher.IClickableMenu_populateClickableComponentList_Postfix)));
  }

  static ClickableTextureComponent? removeCropsButton;
  static bool confirmingCropRemoval = false;

  static void PondQueryMenu_Constructor_Postfix(PondQueryMenu __instance, FishPond fish_pond) {
    if (!ModEntry.IsAquaponicsPond(fish_pond, out var _) ||
        !FishPondCropManager.TryGetOnePot(fish_pond, out var _)) {
      confirmingCropRemoval = false;
      removeCropsButton = null;
      return;
    }
    removeCropsButton = new ClickableTextureComponent(
        new Rectangle(__instance.xPositionOnScreen + PondQueryMenu.width + 4, __instance.yPositionOnScreen + PondQueryMenu.height - 256 - IClickableMenu.borderWidth, 64, 64),
        Game1.mouseCursors,
        new Rectangle(48, 384, 16, 16),
        4f)
    {
      myID = 999,
      downNeighborID = -99998
    };
    __instance.emptyButton.upNeighborID = ClickableComponent.SNAP_AUTOMATIC;
    if (Game1.options.SnappyMenus) {
      __instance.allClickableComponents.Add(removeCropsButton);
    }
    confirmingCropRemoval = false;
  }

	static void PondQueryMenu_performHoverAction_Postfix(PondQueryMenu __instance, int x, int y, ref string ___hoverText) {
    if (removeCropsButton is not null) {
      if (removeCropsButton.containsPoint(x, y)) {
        removeCropsButton.scale = Math.Min(4.1f, removeCropsButton.scale + 0.05f);
        ___hoverText = ModEntry.Helper.Translation.Get("PondQueryMenu.removeCrops");
      }
      else {
        removeCropsButton.scale = Math.Max(4f, removeCropsButton.scale - 0.05f);
      }
    }
  }

  // Draw in the prefix so it gets darkened by the "clear fish" overlay properly
  static void PondQueryMenu_draw_Prefix(PondQueryMenu __instance, SpriteBatch b, Rectangle ____confirmationBoxRectangle, string ____confirmationText, bool ___confirmingEmpty) {
    if (___confirmingEmpty) removeCropsButton?.draw(b);
  }

  static void PondQueryMenu_draw_Postfix(PondQueryMenu __instance, SpriteBatch b, Rectangle ____confirmationBoxRectangle, string ____confirmationText, bool ___confirmingEmpty) {
    if (!___confirmingEmpty) removeCropsButton?.draw(b);
    if (confirmingCropRemoval) {
      if (!Game1.options.showClearBackgrounds) {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
      }
      int num10 = 16;
      ____confirmationBoxRectangle.Width += num10;
      ____confirmationBoxRectangle.Height += num10;
      ____confirmationBoxRectangle.X -= num10 / 2;
      ____confirmationBoxRectangle.Y -= num10 / 2;
      Game1.DrawBox(____confirmationBoxRectangle.X, ____confirmationBoxRectangle.Y, ____confirmationBoxRectangle.Width, ____confirmationBoxRectangle.Height);
      ____confirmationBoxRectangle.Width -= num10;
      ____confirmationBoxRectangle.Height -= num10;
      ____confirmationBoxRectangle.X += num10 / 2;
      ____confirmationBoxRectangle.Y += num10 / 2;
      b.DrawString(Game1.smallFont, ____confirmationText, new Vector2(____confirmationBoxRectangle.X, ____confirmationBoxRectangle.Y), Game1.textColor);
      __instance.yesButton.draw(b);
      __instance.noButton.draw(b);
    }
    // Draw mouse one more time so the new UI doesn't draw on top of it
    __instance.drawMouse(b);
  }

  static void PondQueryMenu_receiveLeftClick_Postfix(PondQueryMenu __instance, int x, int y, bool playSound, ref Rectangle ____confirmationBoxRectangle, ref string ____confirmationText, FishPond ____pond, ref string? ___hoverText) {
    if (Game1.globalFade) return;
    if (confirmingCropRemoval) {
      if (__instance.yesButton.containsPoint(x, y)) {
        confirmingCropRemoval = false;
        Game1.playSound("fishSlap");
        __instance.exitThisMenu();
        var items = FishPondCropManager.RemoveAllCrops(____pond);
        Game1.player.addItemsByMenuIfNecessary(items);
      }
      else if (__instance.noButton.containsPoint(x, y)) {
        confirmingCropRemoval = false;
        Game1.playSound("smallSelect");
        if (Game1.options.SnappyMenus) {
          __instance.currentlySnappedComponent = __instance.getComponentWithID(103);
          __instance.snapCursorToCurrentSnappedComponent();
        }
      }
      return;
    }
    if (removeCropsButton?.containsPoint(x, y) ?? false) {
      ____confirmationBoxRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 400, 100);
      ____confirmationBoxRectangle.X = Game1.uiViewport.Width / 2 - ____confirmationBoxRectangle.Width / 2;
      ____confirmationText = ModEntry.Helper.Translation.Get("PondQueryMenu.removeCropsConfirmation");
      ____confirmationText = Game1.parseText(____confirmationText, Game1.smallFont, ____confirmationBoxRectangle.Width);
      Vector2 vector = Game1.smallFont.MeasureString(____confirmationText);
      ____confirmationBoxRectangle.Height = (int)vector.Y;
      ____confirmationBoxRectangle.Y = Game1.uiViewport.Height / 2 - ____confirmationBoxRectangle.Height / 2;
      __instance.yesButton = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(Game1.uiViewport.Width / 2 - 64 - 4, ____confirmationBoxRectangle.Bottom + 32, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f)
      {
        myID = 111,
             rightNeighborID = 105
      };
      __instance.noButton = new ClickableTextureComponent(new Microsoft.Xna.Framework.Rectangle(Game1.uiViewport.Width / 2 + 4, ____confirmationBoxRectangle.Bottom + 32, 64, 64), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47), 1f)
      {
        myID = 105,
             leftNeighborID = 111
      };
      Game1.playSound("smallSelect");
      if (Game1.options.SnappyMenus) {
        __instance.populateClickableComponentList();
        __instance.currentlySnappedComponent = __instance.noButton;
        __instance.snapCursorToCurrentSnappedComponent();
      }
      ___hoverText = null;
      confirmingCropRemoval = true;
    }
  }

  static void IClickableMenu_populateClickableComponentList_Postfix(IClickableMenu __instance) {
    if (__instance is not PondQueryMenu) return;
    if (removeCropsButton is not null && Game1.options.SnappyMenus) {
      __instance.allClickableComponents.Add(removeCropsButton);
    }
  }
}

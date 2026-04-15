using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;
using StardewValley.ItemTypeDefinitions;
using HarmonyLib;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ExtraMachineConfig;

sealed class SmokedItemHarmonyPatcher {
  internal static string SmokedItemTag = "smoked_item";
  internal static string SmokedItemNoDarkenTag = "smoked_item_no_darken";
  internal static string SteamedItemTag = "steamed_item";
  internal static string DrawPreserveSpriteTag = "draw_preserve_sprite";
  internal static string DrawPrismaticLayerTag = "draw_prismatic_layer";
  public static string PrismaticExtraColor = "PRISMATIC";

  public static void ApplyPatches(Harmony harmony) {
    harmony.Patch(
        original: AccessTools.Method(typeof(StardewValley.Object),
          nameof(StardewValley.Object.drawInMenu),
          new Type[] {
          typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float),
          typeof(StackDrawType), typeof(Color), typeof(bool) }),
        prefix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(Object_drawInMenu_prefix)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(Object_drawInMenu_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(StardewValley.Object),
          nameof(StardewValley.Object.drawWhenHeld)),
        prefix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(Object_drawWhenHeld_prefix)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(Object_drawWhenHeld_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(ColoredObject),
          nameof(ColoredObject.drawInMenu),
          new Type[] {
          typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float),
          typeof(StackDrawType), typeof(Color), typeof(bool) }),
        prefix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(ColoredObject_drawInMenu_prefix)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(ColoredObject_drawInMenu_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(ColoredObject),
          nameof(ColoredObject.drawWhenHeld)),
        prefix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(Object_drawWhenHeld_prefix)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(Object_drawWhenHeld_postfix)));
  }

  private static bool isSmokedItem(Item item, out bool darkenSprite) {
    bool smokedItem = ItemContextTagManager.DoesTagMatch(SmokedItemTag, item.GetContextTags());
    bool smokedItemNoDarken = ItemContextTagManager.DoesTagMatch(SmokedItemNoDarkenTag, item.GetContextTags());
    darkenSprite = smokedItem;
    return smokedItem || smokedItemNoDarken;
  }

  private static bool isSteamedItem(Item item) {
    return ItemContextTagManager.DoesTagMatch(SteamedItemTag, item.GetContextTags());
  }

  private static bool isDrawPreserveSpriteItem(StardewValley.Object item) {
    return ItemContextTagManager.DoesTagMatch(DrawPreserveSpriteTag, item.GetContextTags()) &&
      item.preservedParentSheetIndex.Value != null && item.preservedParentSheetIndex.Value != "-1";
  }

  private static bool isDrawPrismaticItem(Item item) {
    return ItemContextTagManager.DoesTagMatch(DrawPrismaticLayerTag, item.GetContextTags());
  }

  private static bool Object_drawInMenu_prefix(StardewValley.Object __instance, out ParsedItemData? __state, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow) {
    return drawInMenu(__instance, out __state, spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
  }

  private static bool ColoredObject_drawInMenu_prefix(ColoredObject __instance, out ParsedItemData? __state, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color colorOverride, bool drawShadow) {
    var item = Utils.GetActualItemForHolder(__instance);
    if (item is not null) {
      item.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, colorOverride, drawShadow);
      __state = null;
      return false;
    }
    return drawInMenu(__instance, out __state, spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, colorOverride, drawShadow);
  }

  private static bool Object_drawWhenHeld_prefix(StardewValley.Object __instance, out ParsedItemData? __state, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f) {
    return drawWhenHeld(__instance, out __state, spriteBatch, objectPosition, f);
  }

  private static void Object_drawInMenu_postfix(StardewValley.Object __instance, ParsedItemData? __state, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow) {
    bool shouldRedrawMenuIcons = false;
    if (isSmokedItem(__instance, out bool darkenSprite)) {
      shouldRedrawMenuIcons = darkenSprite;
      drawSmoke(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && color.A < byte.MaxValue) ? ((float)(int)color.A / 255f) : transparency, __state, darkenSprite);
    }
    if (isSteamedItem(__instance)) {
      drawSteam(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && color.A < byte.MaxValue) ? ((float)(int)color.A / 255f) : transparency);
    }
    if (isDrawPrismaticItem(__instance)) {
      shouldRedrawMenuIcons = true;
      drawOrSetPrismaticOverlay(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && color.A < byte.MaxValue) ? ((float)(int)color.A / 255f) : transparency, __state, darkenSprite || __instance.QualifiedItemId == "(O)SmokedFish");
    }
    if (shouldRedrawMenuIcons) {
      // draw the icons again because the extra colors will overlap them
      __instance.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth + 3E-05f, drawStackNumber, color);
    }
  }

  private static void ColoredObject_drawInMenu_postfix(ColoredObject __instance, ParsedItemData? __state, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color colorOverride, bool drawShadow) {
    bool shouldRedrawMenuIcons = false;
    if (isSmokedItem(__instance, out bool darkenSprite)) {
      shouldRedrawMenuIcons = darkenSprite;
      drawSmoke(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && colorOverride.A < byte.MaxValue) ? ((float)(int)colorOverride.A / 255f) : transparency, __state, darkenSprite);
    }
    if (isSteamedItem(__instance)) {
      drawSteam(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && colorOverride.A < byte.MaxValue) ? ((float)(int)colorOverride.A / 255f) : transparency);
    }
    if (isDrawPrismaticItem(__instance)) {
      shouldRedrawMenuIcons = __instance.QualifiedItemId == "(O)SmokedFish";
      drawOrSetPrismaticOverlay(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && colorOverride.A < byte.MaxValue) ? ((float)(int)colorOverride.A / 255f) : transparency, __state, darkenSprite || __instance.QualifiedItemId == "(O)SmokedFish");
    }
    shouldRedrawMenuIcons = drawExtraColors(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && colorOverride.A < byte.MaxValue) ? ((float)(int)colorOverride.A / 255f) : transparency, __state) || shouldRedrawMenuIcons;
    if (shouldRedrawMenuIcons) {
      // draw the icons again because the extra colors will overlap them
      __instance.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth + 3E-05f, drawStackNumber, colorOverride);
    }
  }

  private static void Object_drawWhenHeld_postfix(StardewValley.Object __instance, ParsedItemData? __state, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f) {
    float layerDepth = Math.Max(0f, (float)(f.StandingPixel.Y + 4) / 10000f);
    if (isSmokedItem(__instance, out bool darkenSprite)) {
      drawSmoke(__instance, spriteBatch, objectPosition, 1f, layerDepth, 1f, __state, darkenSprite);
    }
    if (isSteamedItem(__instance)) {
      drawSteam(__instance, spriteBatch, objectPosition, 1f, layerDepth, 1f);
    }
    if (isDrawPrismaticItem(__instance)) {
      drawOrSetPrismaticOverlay(__instance, spriteBatch, objectPosition, 1f, layerDepth, 1f, __state, darkenSprite || __instance.QualifiedItemId == "(O)SmokedFish");
    }
    if (__instance is ColoredObject) {
      _ = drawExtraColors(__instance, spriteBatch, objectPosition, 1f, layerDepth, 1f, __state);
    }
  }

  // Draw the preserve sprite if available.
  // Assumes that the item has a valid flavor.
  private static bool drawInMenu(StardewValley.Object item, out ParsedItemData? parsedItemData, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color colorOverride, bool drawShadow) {
    parsedItemData = null;
    if (isDrawPreserveSpriteItem(item)) {
      item.AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
      if (drawShadow) {
        spriteBatch.Draw(Game1.shadowTexture, location + new Vector2(32f, 48f), Game1.shadowTexture.Bounds, colorOverride * 0.5f, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f, SpriteEffects.None, layerDepth - 0.0001f);
      }
      parsedItemData = ItemRegistry.GetData(item.preservedParentSheetIndex.Value);
      drawSprite(parsedItemData, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && colorOverride.A < byte.MaxValue) ? ((float)(int)colorOverride.A / 255f) : transparency);
      item.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth + 3E-05f, drawStackNumber, colorOverride);
      return false;
    }
    return true;
  }

  private static bool drawWhenHeld(StardewValley.Object item, out ParsedItemData? parsedItemData, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f) {
    parsedItemData = null;
    if (isDrawPreserveSpriteItem(item)) {
      parsedItemData = ItemRegistry.GetData(item.preservedParentSheetIndex.Value);
      float layerDepth = Math.Max(0f, (float)(f.StandingPixel.Y + 3) / 10000f);
      drawSprite(parsedItemData, spriteBatch, objectPosition, 1f, layerDepth);
      return false;
    }
    return true;
  }

  private static void drawSprite(ParsedItemData? parsedItemData, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float layerDepth, float transparency = 1f) {
    if (parsedItemData is null) return;
    Vector2 vector = new Vector2(8f, 8f);
    float num = 4f * scaleSize;
    string textureName = parsedItemData.TextureName;
    int spriteIndex = parsedItemData.SpriteIndex;
    Texture2D texture2D = Game1.content.Load<Texture2D>(textureName);
    spriteBatch.Draw(texture2D, location + new Vector2(32f, 32f) * scaleSize, Game1.getSourceRectForStandardTileSheet(texture2D, spriteIndex, 16, 16), Color.White * transparency, 0f, vector * scaleSize, num, SpriteEffects.None, Math.Min(1f, layerDepth + 1E-05f));
  }

  private static void drawSmoke(Item item, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float layerDepth, float transparency = 1f, ParsedItemData? parsedItemData = null, bool darkenSprite = true) {
    Vector2 vector = new Vector2(8f, 8f);
    float num = 4f * scaleSize;
    int num2 = 700 + ((int)item.sellToStorePrice() + 17) * 7777 % 200;

    ParsedItemData dataOrErrorItem = parsedItemData ?? ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
    Texture2D texture2D = dataOrErrorItem.GetTexture();
    int spriteIndex = dataOrErrorItem.SpriteIndex;

    // Draw dark overlay
    if (darkenSprite) {
      spriteBatch.Draw(dataOrErrorItem.GetTexture(), location + new Vector2(32f, 32f) * scaleSize, Game1.getSourceRectForStandardTileSheet(texture2D, spriteIndex, 16, 16), new Color(80, 30, 10) * 0.6f * transparency, 0f, vector * scaleSize, num, SpriteEffects.None, Math.Min(1f, layerDepth + 1.5E-05f));
    }
    // Draw smoke effects
    spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(32f, 32f) * scaleSize + new Vector2(0f, (float)((0.0 - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Color(80, 80, 80) * transparency * 0.53f * (1f - (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000.0) / 2000f), (float)((0.0 - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000.0) * 0.001f, vector * scaleSize, num / 2f, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f));
    spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(24f, 40f) * scaleSize + new Vector2(0f, (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num2)) % 2000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Color(80, 80, 80) * transparency * 0.53f * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num2) % 2000.0) / 2000f), (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num2)) % 2000.0) * 0.001f, vector * scaleSize, num / 2f, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f));
    spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(48f, 21f) * scaleSize + new Vector2(0f, (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num2 * 2))) % 2000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Color(80, 80, 80) * transparency * 0.53f * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num2 * 2)) % 2000.0) / 2000f), (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num2 * 2))) % 2000.0) * 0.001f, vector * scaleSize, num / 2f, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f));
  }

  private static void drawSteam(Item item, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float layerDepth, float transparency = 1f) {
    Vector2 vector = new Vector2(8f, 8f);
    float num = 4f * scaleSize;
    int num2 = 700 + ((int)item.sellToStorePrice() + 17) * 7777 % 200;

    // Draw steam effects
    spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(32f, 32f) * scaleSize + new Vector2(0f, (float)((0.0 - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), Color.White * transparency * 0.53f * (1f - (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000.0) / 2000f), (float)((0.0 - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000.0) * 0.001f, vector * scaleSize, num / 2f, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f));
    spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(24f, 40f) * scaleSize + new Vector2(0f, (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num2)) % 2000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), Color.White * transparency * 0.53f * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num2) % 2000.0) / 2000f), (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num2)) % 2000.0) * 0.001f, vector * scaleSize, num / 2f, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f));
    spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(48f, 21f) * scaleSize + new Vector2(0f, (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num2 * 2))) % 2000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), Color.White * transparency * 0.53f * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num2 * 2)) % 2000.0) / 2000f), (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num2 * 2))) % 2000.0) * 0.001f, vector * scaleSize, num / 2f, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f));
  }

  private static void drawOrSetPrismaticOverlay(Item item, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float layerDepth, float transparency = 1f, ParsedItemData? parsedItemData = null, bool darkenSprite = false) {
    // For uncolored objects, draw the overlay
    // For colored objects, simply set its color (this technically should be done in the prefix but eh)
    if (item is ColoredObject coloredObject && item.QualifiedItemId != "(O)SmokedFish") {
      coloredObject.color.Value = Utility.GetPrismaticColor();
      return;
    }
    Vector2 vector = new Vector2(8f, 8f);
    float num = 4f * scaleSize;
    ParsedItemData dataOrErrorItem;
    if (item.QualifiedItemId == "(O)SmokedFish" && item is SObject obj) {
      dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(obj.preservedParentSheetIndex.Value);
    } else {
      dataOrErrorItem = parsedItemData ?? ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
    }
    Texture2D texture2D = dataOrErrorItem.GetTexture();
    int spriteIndex = dataOrErrorItem.SpriteIndex;
    spriteIndex++;
    spriteBatch.Draw(dataOrErrorItem.GetTexture(), location + new Vector2(32f, 32f) * scaleSize, Game1.getSourceRectForStandardTileSheet(texture2D, spriteIndex, 16, 16), Utility.GetPrismaticColor() * transparency * (darkenSprite ? 0.6f : 1f), 0f, vector * scaleSize, num, SpriteEffects.None, Math.Min(1f, layerDepth + 1.5E-05f));
  }


  // This also handles prismatic colors
  // Returns true if extra colors are indeed drawn and we need to redraw menu icons
  private static bool drawExtraColors(Item item, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float layerDepth, float transparency = 1f, ParsedItemData? parsedItemData = null) {
    Vector2 vector = new Vector2(8f, 8f);
    float num = 4f * scaleSize;
    ParsedItemData dataOrErrorItem = parsedItemData ?? ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
    Texture2D texture2D = dataOrErrorItem.GetTexture();
    int spriteIndex = dataOrErrorItem.SpriteIndex;

    int i = 1;
    while (true) {
      if (item.modData.TryGetValue($"{MachineHarmonyPatcher.ExtraColorKeyPrefix}.{i}", out var val)) {
        Color color = (val == PrismaticExtraColor ? Utility.GetPrismaticColor(i) : Utils.stringToColor(val)) ?? Color.White;
        // The first color mask is for the base color, so we add i to 1
        Rectangle sourceRect = dataOrErrorItem.GetSourceRect(i + 1, item.ParentSheetIndex);
        spriteBatch.Draw(texture2D, location + new Vector2(32f, 32f) * scaleSize, sourceRect, color * transparency, 0f, vector * scaleSize, num, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f + i * 1E-07F));
        i++;
      } else {
        break;
      }
    }
    return i > 1;
  }
}

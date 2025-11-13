using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;
using StardewValley.ItemTypeDefinitions;
using HarmonyLib;

namespace Selph.StardewMods.ExtraMachineConfig;

sealed class SmokedItemHarmonyPatcher {
  internal static string SmokedItemTag = "smoked_item";
  internal static string SteamedItemTag = "steamed_item";
  internal static string DrawPreserveSpriteTag = "draw_preserve_sprite";

  public static void ApplyPatches(Harmony harmony) {
    harmony.Patch(
        original: AccessTools.Method(typeof(StardewValley.Object),
          nameof(StardewValley.Object.drawInMenu),
          new Type[] {
          typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float),
          typeof(StackDrawType), typeof(Color), typeof(bool) }),
        prefix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.Object_drawInMenu_prefix)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.Object_drawInMenu_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(StardewValley.Object),
          nameof(StardewValley.Object.drawWhenHeld)),
        prefix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.Object_drawWhenHeld_prefix)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.Object_drawWhenHeld_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(ColoredObject),
          nameof(ColoredObject.drawInMenu),
          new Type[] {
          typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float),
          typeof(StackDrawType), typeof(Color), typeof(bool) }),
        prefix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.ColoredObject_drawInMenu_prefix)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.ColoredObject_drawInMenu_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(ColoredObject),
          nameof(ColoredObject.drawWhenHeld)),
        prefix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.Object_drawWhenHeld_prefix)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.Object_drawWhenHeld_postfix)));
  }

  private static bool isSmokedItem(Item item) {
    return ItemContextTagManager.DoesTagMatch(SmokedItemTag, item.GetContextTags());
  }

  private static bool isSteamedItem(Item item) {
    return ItemContextTagManager.DoesTagMatch(SteamedItemTag, item.GetContextTags());
  }

  private static bool isDrawPreserveSpriteItem(StardewValley.Object item) {
    return ItemContextTagManager.DoesTagMatch(DrawPreserveSpriteTag, item.GetContextTags()) &&
      item.preservedParentSheetIndex.Value != null && item.preservedParentSheetIndex.Value != "-1";
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
    if (isSmokedItem(__instance)) {
      drawSmoke(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && color.A < byte.MaxValue) ? ((float)(int)color.A / 255f) : transparency, __state);
    }
    if (isSteamedItem(__instance)) {
      drawSteam(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && color.A < byte.MaxValue) ? ((float)(int)color.A / 255f) : transparency);
    }
  }

  private static void ColoredObject_drawInMenu_postfix(ColoredObject __instance, ParsedItemData? __state, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color colorOverride, bool drawShadow) {
    if (isSmokedItem(__instance)) {
      drawSmoke(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && colorOverride.A < byte.MaxValue) ? ((float)(int)colorOverride.A / 255f) : transparency, __state);
    }
    if (isSteamedItem(__instance)) {
      drawSteam(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && colorOverride.A < byte.MaxValue) ? ((float)(int)colorOverride.A / 255f) : transparency);
    }
    drawExtraColors(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && colorOverride.A < byte.MaxValue) ? ((float)(int)colorOverride.A / 255f) : transparency, __state, drawStackNumber, colorOverride);
  }

  private static void Object_drawWhenHeld_postfix(StardewValley.Object __instance, ParsedItemData? __state, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f) {
    float layerDepth = Math.Max(0f, (float)(f.StandingPixel.Y + 4) / 10000f);
    if (isSmokedItem(__instance)) {
      drawSmoke(__instance, spriteBatch, objectPosition, 1f, layerDepth, 1f, __state);
    }
    if (isSteamedItem(__instance)) {
      drawSteam(__instance, spriteBatch, objectPosition, 1f, layerDepth, 1f);
    }
    if (__instance is ColoredObject) {
      drawExtraColors(__instance, spriteBatch, objectPosition, 1f, layerDepth, 1f, __state);
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

  private static void drawSmoke(Item item, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float layerDepth, float transparency = 1f, ParsedItemData? parsedItemData = null) {
    Vector2 vector = new Vector2(8f, 8f);
    float num = 4f * scaleSize;
    int num2 = 700 + ((int)item.sellToStorePrice() + 17) * 7777 % 200;

    ParsedItemData dataOrErrorItem = parsedItemData ?? ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
    Texture2D texture2D = dataOrErrorItem.GetTexture();
    int spriteIndex = dataOrErrorItem.SpriteIndex;

    // Draw dark overlay
    spriteBatch.Draw(dataOrErrorItem.GetTexture(), location + new Vector2(32f, 32f) * scaleSize, Game1.getSourceRectForStandardTileSheet(texture2D, spriteIndex, 16, 16), new Color(80, 30, 10) * 0.6f * transparency, 0f, vector * scaleSize, num, SpriteEffects.None, Math.Min(1f, layerDepth + 1.5E-05f));
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

  private static void drawExtraColors(Item item, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float layerDepth, float transparency = 1f, ParsedItemData? parsedItemData = null, StackDrawType? drawStackNumber = null, Color? colorOverride = null) {
    Vector2 vector = new Vector2(8f, 8f);
    float num = 4f * scaleSize;
    ParsedItemData dataOrErrorItem = parsedItemData ?? ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
    Texture2D texture2D = dataOrErrorItem.GetTexture();
    int spriteIndex = dataOrErrorItem.SpriteIndex;

    int i = 1;
    while (true) {
      if (item.modData.TryGetValue($"{MachineHarmonyPatcher.ExtraColorKeyPrefix}.{i}", out var val)) {
        Color color = Utils.stringToColor(val) ?? Color.White;
        // The first color mask is for the base color, so we add i to 1
        Microsoft.Xna.Framework.Rectangle sourceRect = dataOrErrorItem.GetSourceRect(i + 1, item.ParentSheetIndex);
        spriteBatch.Draw(texture2D, location + new Vector2(32f, 32f) * scaleSize, sourceRect, color * transparency, 0f, vector * scaleSize, num, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f + i * 2E-06F));
        i++;
      } else {
        break;
      }
    }
    if (i > 1 && drawStackNumber is not null && colorOverride is not null) {
      // draw the icons again because the extra colors will overlap them
      item.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth + 3E-05f, drawStackNumber.Value, colorOverride.Value);
    }
  }
}

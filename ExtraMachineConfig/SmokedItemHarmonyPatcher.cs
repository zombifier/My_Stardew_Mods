using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Inventories;
using StardewValley.GameData.Machines;
using StardewValley.GameData.BigCraftables;
using StardewValley.TokenizableStrings;
using HarmonyLib;
using System.Collections.Generic;

namespace ExtraMachineConfig; 

class SmokedItemHarmonyPatcher {
  internal static string SmokedItemTag = "smoked_item";

  public static void ApplyPatches(Harmony harmony) {
    harmony.Patch(
        original: AccessTools.Method(typeof(StardewValley.Object),
          nameof(StardewValley.Object.drawInMenu),
          new Type[] {
          typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float),
          typeof(StackDrawType), typeof(Color), typeof(bool) }),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.Object_drawInMenu_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(StardewValley.Object),
          nameof(StardewValley.Object.drawWhenHeld)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.Object_drawWhenHeld_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(ColoredObject),
          nameof(ColoredObject.drawInMenu),
          new Type[] {
          typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float),
          typeof(StackDrawType), typeof(Color), typeof(bool) }),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.ColoredObject_drawInMenu_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(ColoredObject),
          nameof(ColoredObject.drawWhenHeld)),
        postfix: new HarmonyMethod(typeof(SmokedItemHarmonyPatcher), nameof(SmokedItemHarmonyPatcher.ColoredObject_drawWhenHeld_postfix)));
  }

  private static void Object_drawInMenu_postfix(StardewValley.Object __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow) {
    if (ItemContextTagManager.DoesTagMatch(SmokedItemTag, __instance.GetContextTags()))
      drawSmoke(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && color.A < byte.MaxValue) ? ((float)(int)color.A / 255f) : transparency);
  }

	private static void Object_drawWhenHeld_postfix(StardewValley.Object __instance, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f) {
    if (ItemContextTagManager.DoesTagMatch(SmokedItemTag, __instance.GetContextTags())) {
			float layerDepth = Math.Max(0f, (float)(f.StandingPixel.Y + 4) / 10000f);
      // base Object.drawWhenHeld draw the object at layerDepth + 3E-05f
      drawSmoke(__instance, spriteBatch, objectPosition, 1f, layerDepth);
    }
  }

  private static void ColoredObject_drawInMenu_postfix(ColoredObject __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color colorOverride, bool drawShadow) {
    if (ItemContextTagManager.DoesTagMatch(SmokedItemTag, __instance.GetContextTags()))
      drawSmoke(__instance, spriteBatch, location, scaleSize, layerDepth,
          (transparency == 1f && colorOverride.A < byte.MaxValue) ? ((float)(int)colorOverride.A / 255f) : transparency);
  }

	private static void ColoredObject_drawWhenHeld_postfix(ColoredObject __instance, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f) {
    if (ItemContextTagManager.DoesTagMatch(SmokedItemTag, __instance.GetContextTags())) {
			float layerDepth = Math.Max(0f, (float)(f.StandingPixel.Y + 4) / 10000f);
      // base Object.drawWhenHeld draw the object at layerDepth + 3E-05f
      drawSmoke(__instance, spriteBatch, objectPosition, 1f, layerDepth);
    }
  }

	private static void drawSmoke(Item item, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float layerDepth, float transparency = 1f) {
			Vector2 vector = new Vector2(8f, 8f);
			float num = 4f * scaleSize;
			int num2 = 700 + ((int)item.sellToStorePrice() + 17) * 7777 % 200;
			spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(32f, 32f) * scaleSize + new Vector2(0f, (float)((0.0 - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Color(80, 80, 80) * transparency * 0.53f * (1f - (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000.0) / 2000f), (float)((0.0 - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000.0) * 0.001f, vector * scaleSize, num / 2f, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f));
			spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(24f, 40f) * scaleSize + new Vector2(0f, (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num2)) % 2000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Color(80, 80, 80) * transparency * 0.53f * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num2) % 2000.0) / 2000f), (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)num2)) % 2000.0) * 0.001f, vector * scaleSize, num / 2f, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f));
			spriteBatch.Draw(Game1.mouseCursors, location + new Vector2(48f, 21f) * scaleSize + new Vector2(0f, (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num2 * 2))) % 2000.0) * 0.03f), new Microsoft.Xna.Framework.Rectangle(372, 1956, 10, 10), new Color(80, 80, 80) * transparency * 0.53f * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num2 * 2)) % 2000.0) / 2000f), (float)((0.0 - (Game1.currentGameTime.TotalGameTime.TotalMilliseconds + (double)(num2 * 2))) % 2000.0) * 0.001f, vector * scaleSize, num / 2f, SpriteEffects.None, Math.Min(1f, layerDepth + 2E-05f));
		}
}

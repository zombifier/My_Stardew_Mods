using System;
using StardewModdingAPI;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Enchantments;
using StardewValley.Tools;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Selph.StardewMods.CrackerExtractor;

using SObject = StardewValley.Object;

public class HarmonyPatcher {
  static FarmAnimal? animal;
  static FishPond? fishPond;

  public static void ApplyPatches(Harmony harmony) {
    // Patch tools interactions
    harmony.Patch(
        original: AccessTools.Method(typeof(Tool), nameof(Tool.canThisBeAttached), new Type[] { typeof(SObject) }),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(HarmonyPatcher.Tool_canThisBeAttached_prefix))));
    harmony.Patch(
        original: AccessTools.Method(typeof(Tool), nameof(Tool.beginUsing)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(HarmonyPatcher.Tool_beginUsing_prefix))));
    harmony.Patch(
        original: AccessTools.Method(typeof(Tool), nameof(Tool.endUsing)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(HarmonyPatcher.Tool_endUsing_prefix))));
    harmony.Patch(
        original: AccessTools.Method(typeof(Tool), nameof(Tool.DoFunction)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(HarmonyPatcher.Tool_DoFunction_prefix))));
    harmony.Patch(
        original: AccessTools.Method(typeof(Tool), nameof(Tool.CanAddEnchantment)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(HarmonyPatcher.Tool_CanAddEnchantment_prefix))));
    harmony.Patch(
        original: AccessTools.Method(typeof(Game1), nameof(Game1.drawTool), new Type[] {typeof(Farmer), typeof(int)}),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(HarmonyPatcher.Game1_drawTool_prefix))));
  }

  static bool isCrackerExtractor(Tool tool) {
    return tool.QualifiedItemId == "(T)selph.CrackerExtractorCP.CrackerExtractor";
  }

  static bool Game1_drawTool_prefix(Farmer f, int currentToolIndex) {
    if (!isCrackerExtractor(f.CurrentTool)) {
      return true;  
    }
    f.CurrentTool.draw(Game1.spriteBatch);
    return false;
  }

	static bool Tool_canThisBeAttached_prefix(Tool __instance, ref bool __result, SObject o) {
    if (!isCrackerExtractor(__instance)) {
      return true;  
    }
    __result = o is null || o.QualifiedItemId == "(O)74";
    return false;
  }

	static bool Tool_CanAddEnchantment_prefix(Tool __instance, ref bool __result, BaseEnchantment enchantment) {
    if (!isCrackerExtractor(__instance)) {
      return true;  
    }
    __result = false;
    return false;
  }

  static bool Tool_beginUsing_prefix(Tool __instance, ref bool __result, GameLocation location, int x, int y, Farmer who) {
    if (!isCrackerExtractor(__instance)) {
      return true;  
    }
    __result = true;
    if (__instance.attachments[0] is null) {
      if (who == Game1.player) {
        Game1.showRedMessage(ModEntry.Helper.Translation.Get("CrackerExtractor.noShard"));
      }
    } else {
      x = (int)who.GetToolLocation().X;
      y = (int)who.GetToolLocation().Y;
      animal = Utility.GetBestHarvestableFarmAnimal(toolRect: new Microsoft.Xna.Framework.Rectangle(x - 32, y - 32, 64, 64), animals: location.animals.Values, tool: __instance);
      fishPond = location.getBuildingAt(who.GetToolLocation() / 64f) as FishPond;
      if (animal is not null) {
        if (!animal.hasEatenAnimalCracker.Value) {
          if (who == Game1.player) {
            Game1.showRedMessage(ModEntry.Helper.Translation.Get("CrackerExtractor.animalNoCracker"));
          }
          animal = null;
        } else {
          animal.doEmote(8);
          animal.pauseTimer = 1500;
        }
      } else if (fishPond is not null) {
        if (!fishPond.goldenAnimalCracker.Value) {
          if (who == Game1.player) {
            Game1.showRedMessage(ModEntry.Helper.Translation.Get("CrackerExtractor.fishPondNoCracker"));
          }
          fishPond = null;
        }
      } else {
        Game1.showRedMessage(ModEntry.Helper.Translation.Get("CrackerExtractor.noTarget"));
      }
    }
    // Commence extraction
    DelayedAction.playSoundAfterDelay("fishingRodBend", 300);
    DelayedAction.playSoundAfterDelay("fishingRodBend", 1200);
    who.Halt();
    int currentFrame = who.FarmerSprite.CurrentFrame;
    who.FarmerSprite.animateOnce(287 + who.FacingDirection, 50f, 4);
    who.FarmerSprite.oldFrame = currentFrame;
    who.UsingTool = true;
    who.CanMove = false;
    return false;
  }

  static bool Tool_DoFunction_prefix(Tool __instance, GameLocation location, int x, int y, int power, Farmer who) {
    if (!isCrackerExtractor(__instance)) {
      return true;  
    }
    if (animal is not null) {
      animal.hasEatenAnimalCracker.Value = false;
    } else if (fishPond is not null) {
      fishPond.goldenAnimalCracker.Value = false;
    } else {
      finish(who);
      return false;
    }
    __instance.attachments[0].Stack -= 1;
    if (__instance.attachments[0].Stack == 0) {
      __instance.attachments[0] = null;
    }
    who.addItemByMenuIfNecessary(ItemRegistry.Create("(O)GoldenAnimalCracker"));
    finish(who);
    return false;
  }

  static bool Tool_endUsing_prefix(Tool __instance, GameLocation location, Farmer who) {
    if (!isCrackerExtractor(__instance)) {
      return true;  
    }
    __instance.swingTicker++;
    who.stopJittering();
    who.canReleaseTool = false;
    if (Game1.isAnyGamePadButtonBeingPressed() || !who.IsLocalPlayer)
    {
      who.lastClick = who.GetToolLocation();
    }
    return false;
  }

  static void finish(Farmer who) {
    animal = null;
    fishPond = null;
    who.CanMove = true;
    who.completelyStopAnimatingOrDoingAction();
    who.UsingTool = false;
    who.canReleaseTool = true;
  }
}

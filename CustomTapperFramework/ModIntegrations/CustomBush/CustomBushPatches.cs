using System;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Objects;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace Selph.StardewMods.MachineTerrainFramework;

using SObject = StardewValley.Object;

public class CustomBushPatcher {
  public static void ApplyPatches(Harmony harmony) {
    var CustomBushModPatchesType = AccessTools.TypeByName("StardewMods.CustomBush.Framework.Services.ModPatches");

    harmony.Patch(
        original: AccessTools.Method(CustomBushModPatchesType,
          "IndoorPot_performObjectDropInAction_postfix"),
        prefix: new HarmonyMethod(typeof(CustomBushPatcher),
          nameof(CustomBushPatcher.IndoorPot_performObjectDropInAction_Prefix)));
  }

  // Disallow custom bushes in water planters (for now)
  static bool IndoorPot_performObjectDropInAction_Prefix(IndoorPot __0, ref bool __1, Item dropInItem, bool probe) {
    if (!probe &&
        WaterIndoorPotUtils.isWaterPlanter(__0) &&
        dropInItem is SObject obj &&
        obj.IsTeaSapling()) {
      __1 = false;
      return false;
    }
    return true;
  }
}

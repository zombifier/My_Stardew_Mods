using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Inventories;
using StardewValley.GameData.Machines;
using StardewValley.GameData.BigCraftables;
using StardewValley.TokenizableStrings;
using HarmonyLib;
using System.Collections.Generic;

namespace ExtraMachineConfig {

  internal sealed class ModEntry : Mod {
    internal new static IModHelper Helper { get; set; }
    internal static IMonitor Mmonitor { get; set; }

    public override void Entry(IModHelper helper) {
      Helper = helper;
      Mmonitor = this.Monitor;

      var harmony = new Harmony(this.ModManifest.UniqueID);

      harmony.Patch(
          original: AccessTools.Method(typeof(StardewValley.MachineDataUtility),
            nameof(StardewValley.MachineDataUtility.GetOutputItem)),
          postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.GetOutputItemPostfix)));
    }

    private static void GetOutputItemPostfix(ref Item __result, StardewValley.Object machine, MachineItemOutput outputData, Item inputItem, Farmer who, bool probe, ref int? overrideMinutesUntilReady) {
      if (__result == null) {
        return;
      }
      if (outputData.PreserveId == "INHERIT" && inputItem is StardewValley.Object inputObject && __result is StardewValley.Object resultObject) {
        resultObject.preservedParentSheetIndex.Value = inputObject.preservedParentSheetIndex.Value;
      }
    }
  }
}

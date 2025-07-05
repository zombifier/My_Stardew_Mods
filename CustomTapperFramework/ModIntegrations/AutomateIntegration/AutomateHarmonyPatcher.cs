using System;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.TerrainFeatures;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace Selph.StardewMods.MachineTerrainFramework;

using SObject = StardewValley.Object;

public class AutomatePatcher {
  public static void ApplyPatches(Harmony harmony) {
    var dataBasedMachineType = AccessTools.TypeByName("Pathoschild.Stardew.Automate.Framework.Machines.DataBasedObjectMachine");
    var tapperMachineType = AccessTools.TypeByName("Pathoschild.Stardew.Automate.Framework.Machines.Objects.TapperMachine");

    if (dataBasedMachineType is not null) {
      harmony.Patch(
          original: AccessTools.Method(dataBasedMachineType, "OnOutputCollected"),
          postfix: new HarmonyMethod(typeof(AutomatePatcher),
            nameof(AutomatePatcher.DataBasedMachine_OnOutputCollected_Postfix)));
    } else {
      ModEntry.StaticMonitor.Log("Cannot find Automate's machine type to patch.", LogLevel.Info);
    }

    if (tapperMachineType is not null) {
      harmony.Patch(
          original: AccessTools.Method(tapperMachineType, "Reset"),
          postfix: new HarmonyMethod(typeof(AutomatePatcher),
            nameof(AutomatePatcher.TapperMachine_Reset_Postfix)));
    } else {
      ModEntry.StaticMonitor.Log("Cannot find Automate's tapper type to patch. This is harmless.", LogLevel.Info);
    }
  }

  static void DataBasedMachine_OnOutputCollected_Postfix(object __instance, Item item) {
    try {
      var machine = ModEntry.Helper.Reflection.GetProperty<SObject>(__instance, "Machine").GetValue();
      if (machine.IsTapper()) {
        Utils.UpdateTapperProduct(machine);
      }
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log(e.Message, LogLevel.Error);
    }
  }

  // Needed for non-vanilla tappers on trees
  static void TapperMachine_Reset_Postfix(object __instance, Item item) {
    try {
      var machine = ModEntry.Helper.Reflection.GetProperty<SObject>(__instance, "Machine").GetValue();
      if (machine.IsTapper()) {
        Utils.UpdateTapperProduct(machine);
      }
      // apply OutputCollected rule
      MachineData? machineData = machine.GetMachineData();
      if (MachineDataUtility.TryGetMachineOutputRule(machine, machineData, MachineOutputTrigger.OutputCollected, item.getOne(), null, machine.Location, out MachineOutputRule outputCollectedRule, out _, out _, out _))
        machine.OutputMachine(machineData, outputCollectedRule, machine.lastInputItem.Value, null, machine.Location, false);
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log(e.Message, LogLevel.Error);
    }
  }
}

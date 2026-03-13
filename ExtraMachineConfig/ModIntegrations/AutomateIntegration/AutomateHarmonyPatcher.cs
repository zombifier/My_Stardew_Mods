using System;
using StardewValley;
using StardewValley.Objects;
using HarmonyLib;
using StardewModdingAPI;

namespace Selph.StardewMods.ExtraMachineConfig;

using SObject = StardewValley.Object;

// Contains patches to make the multiple output object feature work properly with Automate.
public class AutomatePatcher {
  public static void ApplyPatches(Harmony harmony) {
    var dataBasedMachineType = AccessTools.TypeByName("Pathoschild.Stardew.Automate.Framework.Machines.DataBasedObjectMachine");
    var crabPotMachineType = AccessTools.TypeByName("Pathoschild.Stardew.Automate.Framework.Machines.Objects.CrabPotMachine");

    harmony.Patch(
        original: AccessTools.DeclaredMethod(dataBasedMachineType, "GetOutput"),
        postfix: new HarmonyMethod(typeof(AutomatePatcher),
          nameof(AutomatePatcher.DataBasedMachine_GetOutput_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(crabPotMachineType, "GetOutput"),
        postfix: new HarmonyMethod(typeof(AutomatePatcher),
          nameof(AutomatePatcher.CrabPotMachine_GetOutput_Postfix)));

    // Technically with this the above patches are not needed (outside of Crab Pots, which call getOne() if the crab book triggers and wiping the chest)
    // other than the convenience of not spilling extra items as debris on a full output chest
    var trackedItemType = AccessTools.TypeByName("Pathoschild.Stardew.Automate.TrackedItem");

    harmony.Patch(
        original: AccessTools.Method(trackedItemType, "Take"),
        postfix: new HarmonyMethod(typeof(AutomatePatcher),
          nameof(AutomatePatcher.TrackedItem_Take_Postfix)));
  }

  static void DataBasedMachine_GetOutput_Postfix(object __instance, ref object __result) {
    GetOutput_Postfix(__instance, ref __result, "OnOutputCollected");
  }

  static void CrabPotMachine_GetOutput_Postfix(object __instance, ref object __result) {
    GetOutput_Postfix(__instance, ref __result, "Reset");
  }

  static void GetOutput_Postfix(object __instance, ref object __result, string emptyFunc) {
    try {
      var machine = ModEntry.Helper.Reflection.GetProperty<SObject>(__instance, "Machine").GetValue();
      if (machine.heldObject.Value.heldObject.Value is Chest chest &&
          chest.Items.Count > 0) {
        foreach (var item in chest.Items) {
          if (item is not null) {
            __result = ModEntry.Helper.Reflection.GetMethod(__instance, "GetTracked")
              .Invoke<object>(item, (object trackedStacks, Item _) => {
                try {
                  bool empty = ModEntry.Helper.Reflection.GetProperty<int>(trackedStacks, "Count").GetValue() <= 0;
                  if (empty) {
                    chest.Items.Remove(item);
                    if (chest.Items.Count == 0) {
                      machine.heldObject.Value.heldObject.Value = null;
                      if (machine.heldObject.Value.QualifiedItemId == MachineHarmonyPatcher.HolderQualifiedId) {
                        var item = machine.heldObject.Value;
                        //machine.heldObject.Value = null;
                        //machine.readyForHarvest.Value = false;
                        //machine.showNextIndex.Value = false;
                        //machine.ResetParentSheetIndex();
                        ModEntry.Helper.Reflection.GetMethod(__instance, emptyFunc).Invoke(trackedStacks, item);
                      }
                    }
                  }
                }
                catch (Exception e) {
                  ModEntry.StaticMonitor.Log(e.Message, LogLevel.Error);
                }
              }, null);
            return;
          }
        }
      }
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log(e.Message, LogLevel.Error);
    }
  }

  // Transition the chest over once; this is a quick fix for machines not handled by the patch above
  static void TrackedItem_Take_Postfix(object __instance, Item? __result, int count) {
    try {
      if (__result is not SObject resultObj) return;
      var original = ModEntry.Helper.Reflection.GetField<Item>(__instance, "Item").GetValue();
      if (original is not SObject origObj) return;
      if (origObj.heldObject.Value is Chest) {
        resultObj.heldObject.Value = origObj.heldObject.Value;
        origObj.heldObject.Value = null;
      }
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log(e.Message, LogLevel.Error);
    }
  }
}

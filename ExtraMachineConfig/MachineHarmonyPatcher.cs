using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Inventories;
using StardewValley.GameData.Machines;
using HarmonyLib;
using System.Collections.Generic;

namespace Selph.StardewMods.ExtraMachineConfig; 

using SObject = StardewValley.Object;

sealed class MachineHarmonyPatcher {

  // Keys for the CustomData map
  internal static Regex RequirementIdKeyRegex =
    new Regex(@$"{ModEntry.UniqueId}\.RequirementId\.(\d+)");
  internal static Regex RequirementTagsKeyRegex =
    new Regex(@$"{ModEntry.UniqueId}\.RequirementTags\.(\d+)");
  internal static string RequirementCountKeyPrefix = $"{ModEntry.UniqueId}.RequirementCount";
  internal static string RequirementAddPriceMultiplierKeyPrefix = $"{ModEntry.UniqueId}.RequirementAddPriceMultiplier";
  internal static string RequirementInvalidMsgKey = $"{ModEntry.UniqueId}.RequirementInvalidMsg";
  internal static string InheritPreserveIdKey = $"{ModEntry.UniqueId}.InheritPreserveId";
  internal static string CopyColorKey = $"{ModEntry.UniqueId}.CopyColor";
  internal static string RequiredCountMaxKey = $"{ModEntry.UniqueId}.RequiredCountMax";
  internal static string ExtraOutputIdsKey = $"{ModEntry.UniqueId}.ExtraOutputIds";
  internal static string OverrideInputItemIdKey = $"{ModEntry.UniqueId}.OverrideInputItemId";
  internal static string UnflavoredDisplayNameOverrideKey = $"{ModEntry.UniqueId}.UnflavoredDisplayNameOverride";

  // ModData keys
  internal static string ExtraContextTagsKey = $"{ModEntry.UniqueId}.ExtraContextTags";
  internal static string ExtraPreserveIdKeyPrefix = $"{ModEntry.UniqueId}.ExtraPreserveId";
  internal static string ExtraColorKeyPrefix = $"{ModEntry.UniqueId}.ExtraColor";

  // ModData value regexes
  internal static Regex DropInIdRegex = new Regex(@"DROP_IN_ID_(\d+)");
  internal static Regex DropInPreserveRegex = new Regex(@"DROP_IN_PRESERVE_(\d+)");
  internal static Regex InputExtraIdRegex = new Regex(@"INPUT_EXTRA_ID_(\d+)");
  
  // Display name macros
  internal static string ExtraPreservedDisplayNamePrefix = "%EXTRA_PRESERVED_DISPLAY_NAME";

  internal static bool enableGetOutputItemSideEffect = false;

  public static void ApplyPatches(Harmony harmony) {
    harmony.Patch(
        original: AccessTools.Method(
          typeof(StardewValley.MachineDataUtility),
          nameof(StardewValley.MachineDataUtility.GetOutputData),
          new Type[] { typeof(List<MachineItemOutput>), typeof(bool), typeof(Item),
          typeof(Farmer), typeof(GameLocation) }),
        prefix: new HarmonyMethod(typeof(MachineHarmonyPatcher), nameof(MachineHarmonyPatcher.MachineDataUtility_GetOutputData_prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(StardewValley.MachineDataUtility),
          nameof(StardewValley.MachineDataUtility.GetOutputItem)),
        prefix: new HarmonyMethod(typeof(MachineHarmonyPatcher), nameof(MachineHarmonyPatcher.MachineDataUtility_GetOutputItem_prefix)),
        postfix: new HarmonyMethod(typeof(MachineHarmonyPatcher), nameof(MachineHarmonyPatcher.MachineDataUtility_GetOutputItem_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.PlaceInMachine)),
        prefix: new HarmonyMethod(typeof(MachineHarmonyPatcher), nameof(MachineHarmonyPatcher.SObject_PlaceInMachine_Prefix)),
        postfix: new HarmonyMethod(typeof(MachineHarmonyPatcher), nameof(MachineHarmonyPatcher.SObject_PlaceInMachine_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Item),
          "_PopulateContextTags"),
        postfix: new HarmonyMethod(typeof(MachineHarmonyPatcher), nameof(MachineHarmonyPatcher.Item_GetContextTags_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Farmer),
          nameof(Farmer.OnItemReceived)),
        postfix: new HarmonyMethod(typeof(MachineHarmonyPatcher), nameof(MachineHarmonyPatcher.Farmer_OnItemReceived_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Chest),
          nameof(Chest.addItem)),
        postfix: new HarmonyMethod(typeof(MachineHarmonyPatcher), nameof(MachineHarmonyPatcher.Chest_addItem_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject), "loadDisplayName"),
        postfix: new HarmonyMethod(typeof(MachineHarmonyPatcher), nameof(MachineHarmonyPatcher.SObject_loadDisplayName_postfix)));
  }

  // This patch:
  // * Checks for additional fuel requirements specified in the output rule's custom data, and
  // removes rules that cannot be satisfied
  private static void MachineDataUtility_GetOutputData_prefix(ref List<MachineItemOutput> outputs,
      bool useFirstValidOutput, Item inputItem, Farmer who,
      GameLocation location) {
    if (outputs == null || outputs.Count < 0) {
      return;
    }
    string? invalidMessage = null;
    IInventory inventory = SObject.autoLoadFrom ?? who.Items;
    List<MachineItemOutput> newOutputs = new List<MachineItemOutput>();
    foreach (MachineItemOutput output in outputs) {
      if (output.CustomData == null) {
        newOutputs.Add(output);
        continue;
      }
      bool valid = true;
      var extraRequirements = ModEntry.ModApi.GetExtraRequirements(output);
      foreach (var entry in extraRequirements) {
        if (Game1.player.getItemCountInList(inventory, entry.Item1) < entry.Item2) {
          valid = false;
        }
      }
      var extraTagsRequirements = ModEntry.ModApi.GetExtraTagsRequirements(output);
      foreach (var entry in extraTagsRequirements) {
        if (Utils.getItemCountInListByTags(inventory, entry.Item1) < entry.Item2) {
          valid = false;
        }
      }
      if (valid) {
        newOutputs.Add(output);
      } else {
        if (output.CustomData.TryGetValue(RequirementInvalidMsgKey, out var msg)) {
          invalidMessage ??= msg;
        }
      }
    }
    outputs = newOutputs;
    if (outputs.Count == 0 && invalidMessage != null && who.IsLocalPlayer &&
        SObject.autoLoadFrom == null) {
      Game1.showRedMessage(invalidMessage);
    }
  }

  // This patch:
  // * Generates a replacement input item if that is specified
  private static void MachineDataUtility_GetOutputItem_prefix(SObject machine,
      MachineItemOutput outputData, ref Item? inputItem,
      Farmer who, bool probe) {
    if (outputData?.CustomData?.TryGetValue(OverrideInputItemIdKey, out var overrideInput) ?? false) {
      string overrideInputId = overrideInput == "NEARBY_FLOWER_QUALIFIED_ID" ?
        ItemRegistry.QualifyItemId(MachineDataUtility.GetNearbyFlowerItemId(machine) ?? null) :
        overrideInput;
      if (overrideInputId != null) {
        ItemQueryContext context = new ItemQueryContext(machine.Location, who, Game1.random);
  			inputItem = ItemQueryResolver.TryResolveRandomItem(overrideInputId, context);
      } else {
        inputItem = null;
      }
    }
  }


  // This patch:
  // * Checks for additional fuel requirements specified in the output rule's custom data, and
  // removes them from inventory
  // * Checks if preserve ID is set to inherit the input item's preserve ID, and applies it
  // * Checks if a colored item should be created and apply the changes
  // * Check if more input items should be consumed
  // * Produces extra outputs and put them in a chest saved in the output item's heldObject
  // * Applies the display name override if the output is unflavored
  // * Saves extra flavor and color info if specified
  private static void MachineDataUtility_GetOutputItem_postfix(ref Item __result, SObject machine,
      MachineItemOutput outputData, Item inputItem,
      Farmer who, bool probe,
      ref int? overrideMinutesUntilReady) {
    if (__result == null || outputData == null) {
      return;
    }
    
    var resultObject = __result as SObject;

    // Generate the extra output items and save them in a chest saved in the output item's heldObject.
    var extraOutputs = ModEntry.ModApi.GetExtraOutputs(outputData);
    if (extraOutputs.Count > 0 && resultObject != null) {
      var chest = new Chest();
      resultObject.heldObject.Value = chest;
      GameStateQueryContext context = new GameStateQueryContext(machine.Location, who, resultObject, inputItem, Game1.random);
      ItemQueryContext itemContext = new ItemQueryContext(machine.Location, who, Game1.random);
      foreach (var extraOutputData in extraOutputs) {
        if (!GameStateQuery.CheckConditions(extraOutputData.Condition, context)) {
          continue;
        }
        var item = MachineDataUtility.GetOutputItem(machine, extraOutputData, inputItem, who, false, out var _);
        if (item != null) {
          chest.addItem(item);
        }
      }
    }

    IInventory inventory = SObject.autoLoadFrom ?? who.Items;
    // Inherit preserve ID
    if ((outputData.PreserveId == "INHERIT" ||
          (outputData.CustomData != null &&
           outputData.CustomData.ContainsKey(InheritPreserveIdKey))) &&
        inputItem is SObject inputObject &&
        inputObject.preservedParentSheetIndex.Value != "-1" &&
        resultObject != null) {
      resultObject.preservedParentSheetIndex.Value = inputObject.preservedParentSheetIndex.Value;
    }

    // Override display name if unflavored
    if (outputData.CustomData != null &&
        outputData.CustomData.TryGetValue(UnflavoredDisplayNameOverrideKey, out var unflavoredDislayNameOverride) &&
        resultObject != null &&
        (resultObject.preservedParentSheetIndex.Value == null ||
         resultObject.preservedParentSheetIndex.Value == "-1")) {
      resultObject.displayNameFormat = unflavoredDislayNameOverride;
    }

    if (inputItem is null) return;
    if (outputData.CustomData == null) {
      return;
    }

    // Remove extra fuel (and add their prices if specified)
    var extraRequirements = Utils.GetExtraRequirementsImpl(outputData, /*isContextTag=*/false);
    IDictionary<string, Item> usedFuels = new Dictionary<string, Item>();
    foreach (var entry in extraRequirements) {
      var item = Utils.RemoveItemFromInventoryById(inventory, entry.itemId, entry.count, !enableGetOutputItemSideEffect);
      if (item != null) {
        usedFuels[entry.fuelEntryId] = item;
        if (entry.priceMultiplier > 0 && resultObject is not null) {
          resultObject.Price += (int)(((item as SObject)?.Price ?? 0) * entry.priceMultiplier);
        }
      }
    }
    var extraTagsRequirements = Utils.GetExtraRequirementsImpl(outputData, /*isContextTag=*/true);
    foreach (var entry in extraTagsRequirements) {
      var item = Utils.RemoveItemFromInventoryByTags(inventory, entry.itemId, entry.count, !enableGetOutputItemSideEffect);
      if (item != null) {
        usedFuels[entry.fuelEntryId] = item;
        if (entry.priceMultiplier > 0 && resultObject is not null) {
          resultObject.Price += (int)(((item as SObject)?.Price ?? 0) * entry.priceMultiplier);
        }
      }
    }

    int i = 1;
    // Record the extra fuels' ID/preserve ID if needed
    while (true) {
      string? val = Utils.getPreserveId(__result, i);
      if (val is not null) {
        // Fuel id -> flavor
        var dropInIdMatch = DropInIdRegex.Match(val);
        if (dropInIdMatch.Success) {
          string idToCheck = dropInIdMatch.Groups[1].Value;
          if (usedFuels.TryGetValue(idToCheck, out var fuelItem)) {
            __result.modData[$"{ExtraPreserveIdKeyPrefix}.{i}"] = fuelItem.ItemId;
            if (resultObject is not null) {
              resultObject.Name = resultObject.Name.Replace($"PRESERVE_ID_{idToCheck}", fuelItem.ItemId);
            }
          }
        }
        // Fuel flavor -> flavor
        var dropIdPreserveMatch = DropInPreserveRegex.Match(val);
        if (dropIdPreserveMatch.Success) {
          string idToCheck = dropIdPreserveMatch.Groups[1].Value;
          if (usedFuels.TryGetValue(idToCheck, out var fuelItem)) {
            __result.modData[$"{ExtraPreserveIdKeyPrefix}.{i}"] = (fuelItem as SObject)?.preservedParentSheetIndex.Value ?? "";
            if (resultObject is not null) {
              resultObject.Name = resultObject.Name.Replace($"PRESERVE_ID_{idToCheck}", (fuelItem as SObject)?.preservedParentSheetIndex.Value ?? "");
            }
          }
        }
        // Input item's extra flavors -> flavor
        var inputExtraPreserveIdMatch = InputExtraIdRegex.Match(val);
        if (inputExtraPreserveIdMatch.Success) {
          string idToCheck = inputExtraPreserveIdMatch.Groups[1].Value;
          if (inputItem.modData.TryGetValue($"{ExtraPreserveIdKeyPrefix}.{idToCheck}", out var preserveId)) {
            __result.modData[$"{ExtraPreserveIdKeyPrefix}.{i}"] = preserveId;
            if (resultObject is not null) {
              resultObject.Name = resultObject.Name.Replace($"PRESERVE_ID_{idToCheck}", preserveId);
            }
          }
        }
        // TODO: Fuel's extra flavors??? This is super niche I sincerely hope no mod authors plan on using this lmao
        i++;
      } else {
        break;
      }
    }
    // Record the extra fuels' color if needed
    i = 1;
    while (true) {
      if (__result.modData.TryGetValue($"{ExtraColorKeyPrefix}.{i}", out var val)) {
        var dropInIdMatch = DropInIdRegex.Match(val);
        if (dropInIdMatch.Success) {
          string idToCheck = dropInIdMatch.Groups[1].Value;
          if (usedFuels.TryGetValue(idToCheck, out var fuelItem)) {
            __result.modData[$"{ExtraColorKeyPrefix}.{i}"] = Utils.colorToString(TailoringMenu.GetDyeColor(fuelItem) ?? Color.White);
          }
        }
        var dropIdPreserveMatch = DropInPreserveRegex.Match(val);
        if (dropIdPreserveMatch.Success) {
          string idToCheck = dropIdPreserveMatch.Groups[1].Value;
          if (usedFuels.TryGetValue(idToCheck, out var fuelItem)) {
            var preservedIdItem = ItemRegistry.Create((fuelItem as SObject)?.preservedParentSheetIndex.Value);
            __result.modData[$"{ExtraColorKeyPrefix}.{i}"] = Utils.colorToString(TailoringMenu.GetDyeColor(preservedIdItem) ?? Color.White);
          }
        }
        var inputExtraPreserveIdMatch = InputExtraIdRegex.Match(val);
        if (inputExtraPreserveIdMatch.Success) {
          string idToCheck = inputExtraPreserveIdMatch.Groups[1].Value;
          if (inputItem.modData.TryGetValue($"{ExtraColorKeyPrefix}.{idToCheck}", out var color)) {
            __result.modData[$"{ExtraColorKeyPrefix}.{i}"] = color;
          }
        }
        i++;
      } else {
        break;
      }
    }

    // Color the item
    if ((outputData.CustomData.ContainsKey(CopyColorKey)) &&
        resultObject is not null) {
      StardewValley.Objects.ColoredObject newColoredObject;
      if (__result is StardewValley.Objects.ColoredObject coloredObject) {
        newColoredObject = coloredObject;
      } else {
        newColoredObject = new StardewValley.Objects.ColoredObject(
            __result.ItemId,
            __result.Stack,
            Color.White
            );
        ModEntry.Helper.Reflection.GetMethod(newColoredObject, "GetOneCopyFrom").Invoke(__result);
        newColoredObject.Stack = __result.Stack;
        newColoredObject.heldObject.Value = resultObject.heldObject.Value;
      }
      var color = TailoringMenu.GetDyeColor(inputItem);
      if (color != null) {
        newColoredObject.color.Value = (Color)color;
        __result = newColoredObject;
        resultObject = __result as SObject;
      }
    }
    // Consume extra input items and replace output stack count
    if (enableGetOutputItemSideEffect) {
      if (outputData.CustomData.ContainsKey(RequiredCountMaxKey) &&
          Int32.TryParse(outputData.CustomData[RequiredCountMaxKey], out int requiredCountMax) &&
          MachineDataUtility.TryGetMachineOutputRule(machine, machine.GetMachineData(), MachineOutputTrigger.ItemPlacedInMachine, inputItem, who, machine.Location, out var _, out var triggerRule, out var _, out var _)) {
        int requiredCountMin = triggerRule.RequiredCount;
        if (requiredCountMax < requiredCountMin) {
          ModEntry.StaticMonitor.Log($"Warning: RequiredCountMax value ({requiredCountMax}) smaller than TriggerCount ({requiredCountMin}) for rule {outputData.Id}, ignoring.", LogLevel.Warn);
        } else {
          int baseOutputStack = Math.Min(inputItem.Stack, requiredCountMax);
          __result.Stack = (int)Utility.ApplyQuantityModifiers(baseOutputStack, outputData.StackModifiers, outputData.StackModifierMode, machine.Location, who, __result, inputItem);
          SObject.ConsumeInventoryItem(who, inputItem, baseOutputStack - requiredCountMin);
        }
      }
    }
  }

  private static void Item_GetContextTags_postfix(Item __instance, ref HashSet<string> tags) {
    if (__instance.modData.TryGetValue(ExtraContextTagsKey, out string contextTags)) {
      tags.UnionWith(contextTags.Split(","));
    }
    // Some extra helper tags
    //if (__instance is SObject obj && obj.preservedParentSheetIndex.Value is null) {
    //  __result.Add("no_preserve_parent_sheet_index");
    //}
    int i = 1;
    while (true) {
      var extraPreserveId = Utils.getPreserveId(__instance, i);
      if (extraPreserveId is not null) {
        tags.Add($"extra_preserve_sheet_index_{i}_{ItemContextTagManager.SanitizeContextTag(extraPreserveId)}");
        i++;
      } else {
        break;
      }
    }
  }

  private static void Farmer_OnItemReceived_postfix(Farmer __instance, Item item, int countAdded, Item mergedIntoStack, bool hideHudNotification = false) {
    if (item is SObject obj && obj.heldObject.Value is Chest chest) {
      __instance.addItemsByMenuIfNecessary(new List<Item>(chest.Items));
      obj.heldObject.Value = null;
    }
  }

  public static void Chest_addItem_postfix(Chest __instance, Item __result, Item item) {
    if (__result != null || item is not SObject {heldObject.Value: Chest chest} obj) return;
    foreach (var extraItem in chest.Items) {
      var leftoverItem = __instance.addItem(extraItem);
      if (leftoverItem != null) {
        Game1.createItemDebris(leftoverItem, __instance.TileLocation * 64f, -1, __instance.Location);
      }
    }
    obj.heldObject.Value = null;
  }

  // These two patches ensure GetOutputItem only performs side effects (ie removing items from
  // inventory for additional fuels) if it's called within the context of PlaceInMachine,
  // allowing GetOutputItem to be callable anywhere without ill effects.
  public static void SObject_PlaceInMachine_Prefix(MachineData machineData, Item inputItem, bool probe, Farmer who, bool showMessages = true, bool playSounds = true) {
    enableGetOutputItemSideEffect = true;
  }

	public static void SObject_PlaceInMachine_Postfix(MachineData machineData, Item inputItem, bool probe, Farmer who, bool showMessages = true, bool playSounds = true) {
    enableGetOutputItemSideEffect = false;
  }

  public static void SObject_loadDisplayName_postfix(ref string __result, SObject __instance) {
    int i = 1;
    while (true) {
      var val = Utils.getPreserveId(__instance, i);
      if (val is not null) {
        string preserveDisplayName = (ItemRegistry.GetData("(O)" + val)?.DisplayName ?? "");
        __result = __result.Replace($"{ExtraPreservedDisplayNamePrefix}_{i}", preserveDisplayName);
        i++;
      } else {
        break;
      }
    }
  }
}

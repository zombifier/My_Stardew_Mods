using Microsoft.Xna.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Triggers;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Delegates;
using StardewValley.Internal;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.MachineTerrainFramework;

static class CropExtensionHandler {
  static public void Register(Harmony harmony, IModHelper helper) {
    harmony.Patch(
        original: AccessTools.Method(typeof(HoeDirt),
          nameof(HoeDirt.GetFertilizerSpeedBoost)),
        postfix: new HarmonyMethod(typeof(CropExtensionHandler),
          nameof(HoeDirt_GetFertilizerSpeedBoost_Postfix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(HoeDirt),
          nameof(HoeDirt.plant)),
        postfix: new HarmonyMethod(typeof(CropExtensionHandler),
          nameof(HoeDirt_plant_Postfix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(HoeDirt),
          nameof(HoeDirt.destroyCrop)),
        prefix: new HarmonyMethod(typeof(CropExtensionHandler),
          nameof(HoeDirt_destroyCrop_Prefix)),
        postfix: new HarmonyMethod(typeof(CropExtensionHandler),
          nameof(HoeDirt_destroyCrop_Postfix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(Crop),
          nameof(Crop.harvest)),
        postfix: new HarmonyMethod(typeof(CropExtensionHandler),
          nameof(Crop_harvest_Postfix)));
    // Patch that allow negative speed maluses to work
    try {
      harmony.Patch(
          original: AccessTools.Method(typeof(HoeDirt),
            nameof(HoeDirt.applySpeedIncreases)),
          transpiler: new HarmonyMethod(typeof(CropExtensionHandler),
            nameof(HoeDirt_applySpeedIncreases_Transpiler)));
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log($"Error patching HoeDirt.applySpeedIncreases; negative crop grow speed maluses won't work. Error detail: {e.ToString()}", LogLevel.Warn);
    }

    ModEntry.ModApi.CropHarvested += OnCropHarvested;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
  }

  static bool GetCropDataFor(Crop? crop, out CropExtensionData? data, out CropExtensionData? defaultData) {
    var cropId = crop?.netSeedIndex.Value ?? crop?.whichForageCrop.Value ?? "Empty";
    if (cropId is not null && ModEntry.cropExtensionDataAssetHandler.data.TryGetValue(cropId, out data)) {
    } else {
      data = null;
    }
    if (ModEntry.cropExtensionDataAssetHandler.data.TryGetValue("Default", out defaultData)) {
    }
    if (cropId == "Empty") defaultData = null;
    return data is not null || defaultData is not null;
  }

  // A version of the game's ApplyQuantityModifiers that pass in the special GSQ context object
  static float ApplyQuantityModifiers(float value, IList<QuantityModifier>? modifiers, QuantityModifier.QuantityModifierMode mode, GameStateQueryContext gsqContext) {
    if (modifiers == null || !modifiers.Any()) {
      return value;
    }
    float? num = null;
    foreach (QuantityModifier modifier in modifiers) {
      float amount = modifier.Amount;
      List<float> randomAmount = modifier.RandomAmount;
      if (randomAmount != null && randomAmount.Any()) {
        amount = gsqContext.Random.ChooseFrom(modifier.RandomAmount);
      }
      if (!GameStateQuery.CheckConditions(modifier.Condition, gsqContext)) {
        continue;
      }
      switch (mode) {
        case QuantityModifier.QuantityModifierMode.Minimum: {
            float num3 = QuantityModifier.Apply(value, modifier.Modification, amount);
            if (!num.HasValue || num3 < num) {
              num = num3;
            }
            break;
          }
        case QuantityModifier.QuantityModifierMode.Maximum: {
            float num2 = QuantityModifier.Apply(value, modifier.Modification, amount);
            if (!num.HasValue || num2 > num) {
              num = num2;
            }
            break;
          }
        default:
          num = QuantityModifier.Apply(num.GetValueOrDefault(value), modifier.Modification, amount);
          break;
      }
    }
    return num.GetValueOrDefault(value);
  }

  // Apply grow speed increases
  static void HoeDirt_GetFertilizerSpeedBoost_Postfix(HoeDirt __instance, ref float __result) {
    if (__instance.crop is null
        || !GetCropDataFor(__instance.crop, out var data, out var defaultData)) {
      return;
    }
    if ((data?.GrowSpeedModifiers ?? defaultData?.GrowSpeedModifiers) is { } growSpeedModifiers) {
      var growSpeedModifierMode = data?.GrowSpeedModifiers is not null ? data.GrowSpeedModifierMode : defaultData?.GrowSpeedModifierMode ?? QuantityModifier.QuantityModifierMode.Stack;
      __result = ApplyQuantityModifiers(__result, growSpeedModifiers, growSpeedModifierMode, GetGsqContext(__instance.crop, Game1.player));
    }
  }

  static void OnCropHarvested(ICropHarvestedEvent e) {
    var context = GetGsqContext(e.crop, Game1.player);
    var iqContext = GetIqContext(e.crop);
    if (GetCropDataFor(e.crop, out var data, out var defaultData)) {
      if (!e.isExtraDrops && ((data?.CropQualityModifiers ?? defaultData?.CropQualityModifiers) is { } cropQualityModifiers)) {
        var cropQualityModifierMode = data?.CropQualityModifiers is not null ? data.CropQualityModifierMode : defaultData?.CropQualityModifierMode ?? QuantityModifier.QuantityModifierMode.Stack;
        e.produce.Quality = (int)ApplyQuantityModifiers(e.produce.Quality, cropQualityModifiers, cropQualityModifierMode, context);
        e.produce.FixQuality();
      }
      if (!e.isExtraDrops && ((data?.CropQuantityModifiers ?? defaultData?.CropQuantityModifiers) is { } cropQuantityModifiers)) {
        var cropQuantityModifierMode = data?.CropQuantityModifiers is not null ? data.CropQuantityModifierMode : defaultData?.CropQuantityModifierMode ?? QuantityModifier.QuantityModifierMode.Stack;
        e.count = (int)ApplyQuantityModifiers(e.count, cropQuantityModifiers, cropQuantityModifierMode, context);
      }
      if ((data?.MainDropOverride ?? defaultData?.MainDropOverride) is { } mainDropOverride) {
        foreach (var entry in mainDropOverride) {
          if (GameStateQuery.CheckConditions(entry.Condition, context)) {
            var newItem = ItemQueryResolver.TryResolveRandomItem(entry, iqContext);
            if (entry.CopyColor && e.produce is ColoredObject coloredProduce && ColoredObject.TrySetColor(newItem, coloredProduce.color.Value, out var newColoredItem)) {
              newItem = newColoredItem;
            }
            if (!e.isExtraDrops && entry.OverrideStack) e.count = newItem.Stack;
            newItem.Stack = 1;
            if (!e.isExtraDrops && entry.OverrideQuality) newItem.Quality = e.produce.Quality;
            e.produce = newItem;
            break;
          }
        }
      }
      if (!e.isExtraDrops && (data?.ExtraDrops ?? defaultData?.ExtraDrops) is { } extraDrops) {
        foreach (var entry in extraDrops) {
          if (GameStateQuery.CheckConditions(entry.Condition, context)) {
            var item = ItemQueryResolver.TryResolveRandomItem(entry, iqContext);
            if (entry.CopyColor && e.produce is ColoredObject coloredProduce && ColoredObject.TrySetColor(item, coloredProduce.color.Value, out var newColoredItem)) {
              item = newColoredItem;
            }
            for (int i = 0; i < item.Stack; i++) {
              if (e.junimo is null) {
                Game1.createItemDebris(item.getOne(), new Vector2(e.crop.tilePosition.X * 64 + 32, e.crop.tilePosition.Y * 64 + 32), -1);
              } else {
                e.junimo.tryToAddItemToHut(item.getOne());
              }
            }
          }
        }
      }
    }
  }

  static void Crop_harvest_Postfix(Crop __instance, int xTile, int yTile, HoeDirt soil, JunimoHarvester? junimoHarvester = null, bool isForcedScytheHarvest = false) {
    if (GetCropDataFor(__instance, out var data, out var defaultData)
        && __instance.RegrowsAfterHarvest()
        && (data?.RegrowSpeedModifiers ?? defaultData?.RegrowSpeedModifiers) is { } regrowSpeedModifiers) {
      var context = GetGsqContext(__instance, Game1.player);
      var regrowSpeedModifierMode = data?.RegrowSpeedModifiers is not null ? data.RegrowSpeedModifierMode : defaultData?.RegrowSpeedModifierMode ?? QuantityModifier.QuantityModifierMode.Stack;
      var regrowSpeed = ApplyQuantityModifiers(0, regrowSpeedModifiers, regrowSpeedModifierMode, context);
      __instance.dayOfCurrentPhase.Value = Math.Max(1, (int)(__instance.dayOfCurrentPhase.Value / Math.Max(1.0f + regrowSpeed, 0.1f)));
    }
  }

  static void HoeDirt_plant_Postfix(HoeDirt __instance, bool __result, string itemId, Farmer who, bool isFertilizer) {
    if (isFertilizer || !__result || __instance.crop is null
        || !GetCropDataFor(__instance.crop, out var data, out var defaultData)
        || (data?.PlantTriggers ?? defaultData?.PlantTriggers) is not { } plantTriggers) {
      return;
    }
    foreach (var action in plantTriggers) {
      if (!TriggerActionManager.TryRunAction(
            action
            .Replace("TILE_X", __instance.Tile.X.ToString())
            .Replace("TILE_Y", __instance.Tile.Y.ToString())
            .Replace("LOCATION_NAME", __instance.Location.Name),
            "Manual",
            [__instance, who, __instance.crop],
            out var error, out var exception)) {
        ModEntry.StaticMonitor.Log($"Error running trigger action {action}: {error}", LogLevel.Error);
        ModEntry.StaticMonitor.Log($"Exception running trigger action {action}: {exception.ToString()}");
      }
    }
  }

  // Need a transpiler to be compat with WOL
  static IEnumerable<CodeInstruction> HoeDirt_applySpeedIncreases_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Find the days to remove/add var
    matcher
      .MatchEndForward(
        new CodeMatch(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Math), nameof(Math.Ceiling), new[] { typeof(float) })),
        new CodeMatch(OpCodes.Conv_I4),
        new CodeMatch(OpCodes.Stloc_S))
      .ThrowIfNotMatch($"Could not find entry point for {nameof(HoeDirt_applySpeedIncreases_Transpiler)}");
    var daysToRemoveVar = matcher.Operand;
    // Insert MaybeAddDays(hoeDirt, daysToRemove) at the end
    matcher
      .End();
    var labels = matcher.Labels;
    matcher
      .RemoveInstruction()
      .InsertAndAdvance(
        new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
        new CodeInstruction(OpCodes.Ldloc_S, daysToRemoveVar),
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CropExtensionHandler), nameof(CropExtensionHandler.MaybeAddDays))),
        new CodeInstruction(OpCodes.Ret)
        );
    //foreach (var i in matcher.InstructionEnumeration()) {
    //  ModEntry.StaticMonitor.Log($"{i.opcode} {i.operand}", LogLevel.Alert);
    //}
    return matcher.InstructionEnumeration();
  }

  static void MaybeAddDays(HoeDirt dirt, int daysToRemove) {
    // shouldn't need to check for dirt.crop null since this should only be called after the check,
    // but eh, mods can do whatevs, and if we crash here we crash to desktop
    if (dirt.crop is null || daysToRemove >= 0) return;
    int tries = 0;
    while (daysToRemove < 0 && tries < 3) {
      for (int j = 0; j < dirt.crop.phaseDays.Count; j++) {
        if (dirt.crop.phaseDays[j] != 99999) {
          dirt.crop.phaseDays[j]++;
          daysToRemove++;
        }
        if (daysToRemove >= 0) {
          break;
        }
      }
      tries++;
    }
  }

  static void HoeDirt_destroyCrop_Prefix(HoeDirt __instance, bool showAnimation, ref Crop? __state) {
    __state = __instance.crop;
  }

  static void HoeDirt_destroyCrop_Postfix(HoeDirt __instance, bool showAnimation, Crop? __state) {
    if (__state is null
        || !GetCropDataFor(__state, out var data, out var defaultData)
        || (data?.DestroyedTriggers ?? defaultData?.DestroyedTriggers) is not { } destroyedTriggers) {
      return;
    }
    foreach (var action in destroyedTriggers) {
      if (!TriggerActionManager.TryRunAction(
            action
            .Replace("TILE_X", __instance.Tile.X.ToString())
            .Replace("TILE_Y", __instance.Tile.Y.ToString())
            .Replace("LOCATION_NAME", __instance.Location.Name),
            "Manual",
            [__instance, Game1.player, __state],
            out var error, out var exception)) {
        ModEntry.StaticMonitor.Log($"Error running trigger action {action}: {error}", LogLevel.Error);
        ModEntry.StaticMonitor.Log($"Exception running trigger action {action}: {exception.ToString()}");
      }
    }
  }

  public static GameStateQueryContext GetGsqContext(Crop crop, Farmer? who) {
    who ??= Game1.player;
    Dictionary<string, object> customFields = new();
    customFields["Tile"] = crop.tilePosition;
    customFields["Crop"] = crop;
    customFields["Dirt"] = crop.Dirt;
    GameStateQueryContext context = new(
        crop.currentLocation,
        who,
        ItemRegistry.Create(crop.GetData()?.HarvestItemId ?? "0"),
        ItemRegistry.Create(crop.netSeedIndex.Value ?? "0"),
        Game1.random,
        customFields: customFields);
    return context;
  }

  static ItemQueryContext GetIqContext(Crop crop) {
    ItemQueryContext context = new(crop.currentLocation, Game1.player, Game1.random, "Machine Terrain Framework crop override");
    return context;
  }

  static void OnDayStarted(object? sender, DayStartedEventArgs e) {
    if (!Context.IsMainPlayer) return;
    Utility.ForEachLocation(location => {
      foreach (TerrainFeature t in location.terrainFeatures.Values) {
        if (t is HoeDirt hoeDirt) {
          RunDayStartingTriggers(hoeDirt);
        }
      }
      foreach (SObject o in location.objects.Values) {
        if (o is IndoorPot pot) {
          RunDayStartingTriggers(pot.hoeDirt.Value);
        }
      }
      return true;
    });
  }

  static void RunDayStartingTriggers(HoeDirt hoeDirt) {
    if (!GetCropDataFor(hoeDirt.crop, out var data, out var defaultData)
        || (data?.DayStartTriggers ?? defaultData?.DayStartTriggers) is not { } dayEndTriggers) {
      return;
    }
    // Don't run for fresh seeds (the ones likely spread by PlantCrop)
    if (hoeDirt.crop is not null && hoeDirt.crop.currentPhase.Value == 0 && hoeDirt.crop.dayOfCurrentPhase.Value == 0) {
      return;
    }
    foreach (var action in dayEndTriggers) {
      if (!TriggerActionManager.TryRunAction(
            action
            .Replace("TILE_X", hoeDirt.Tile.X.ToString())
            .Replace("TILE_Y", hoeDirt.Tile.Y.ToString())
            .Replace("LOCATION_NAME", hoeDirt.Location.Name),
            "Manual",
            [hoeDirt, Game1.player, hoeDirt.crop],
            out var error, out var exception)) {
        ModEntry.StaticMonitor.Log($"Error running trigger action {action}: {error}", LogLevel.Error);
        ModEntry.StaticMonitor.Log($"Exception running trigger action {action}: {exception?.ToString()}");
      }
    }
  }
}

using StardewModdingAPI;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using System;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Delegates;
using StardewValley.Triggers;

namespace Selph.StardewMods.MachineTerrainFramework;

static class CropExtensionDelegates {
  public static void Register() {
    GameStateQuery.Register($"{ModEntry.UniqueId}_NEARBY_CROPS", NEARBY_CROPS);
    GameStateQuery.Register($"{ModEntry.UniqueId}_IS_FULLY_GROWN", IS_FULLY_GROWN);
    TriggerActionManager.RegisterAction($"{ModEntry.UniqueId}_ResetGrowDays", ResetGrowDays);
    TriggerActionManager.RegisterAction($"{ModEntry.UniqueId}_KillCrop", KillCrop);
    TriggerActionManager.RegisterAction($"{ModEntry.UniqueId}_DestroyCrop", DestroyCrop);
    TriggerActionManager.RegisterAction($"{ModEntry.UniqueId}_TransformCrop", TransformCrop);
    TriggerActionManager.RegisterAction($"{ModEntry.UniqueId}_PlantCrop", PlantCrop);
    TriggerActionManager.RegisterAction($"{ModEntry.UniqueId}_IfCrop", IfCrop);
  }

  static bool NEARBY_CROPS(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGetInt(query, 1, out var radius, out var error, "int radius")
        || !ArgUtility.TryGetOptional(query, 2, out var subGsq, out error, null, true, "string subGsq")
        || !ArgUtility.TryGetOptionalInt(query, 3, out var desiredCount, out error, 1, "int count")
        || !ArgUtility.TryGetOptionalBool(query, 4, out var fullGrownOnly, out error, false, "bool acceptsPots")
        || !ArgUtility.TryGetOptionalBool(query, 5, out var acceptsPots, out error, false, "bool fullGrownOnly")) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    if (context.CustomFields?.TryGetValue("Tile", out var obj) is null or false
        || obj is not Vector2 tile) {
      return GameStateQuery.Helpers.ErrorResult(query, "Error: Called outside of crop extension data?");
    }
    // Aquaponics hack - ignore out of bounds crops
    if (tile.X < 0 || tile.Y < 0) return true;

    var count = 0;
    for (var x = -radius; x <= radius; x++) {
      for (var y = -radius; y <= radius; y++) {
        // Don't check the current crop
        if (x == 0 && y == 0) continue;
        if (context.Location.terrainFeatures.TryGetValue(tile + new Vector2(x, y), out var t)
            && t is HoeDirt dirt
            && dirt.crop is not null
            && (string.IsNullOrWhiteSpace(subGsq) || GameStateQuery.CheckConditions(subGsq, CropExtensionHandler.GetGsqContext(dirt.crop, context.Player)))
            && (!fullGrownOnly || dirt.crop.fullyGrown.Value is true)) {
          count++;
          if (count >= desiredCount) return true;
        }
        // Accepts water planters as natural grown
        if (context.Location.objects.TryGetValue(tile + new Vector2(x, y), out var o)
          && (acceptsPots || o.QualifiedItemId == WaterIndoorPotUtils.WaterPlanterQualifiedItemId)
          && o is IndoorPot pot
          && pot.hoeDirt.Value.crop is not null
          && (string.IsNullOrWhiteSpace(subGsq) || GameStateQuery.CheckConditions(subGsq, CropExtensionHandler.GetGsqContext(pot.hoeDirt.Value.crop, context.Player)))
          && (!fullGrownOnly || pot.hoeDirt.Value.crop?.fullyGrown.Value is true)) {
          count++;
          if (count >= desiredCount) return true;
        }
      }
    }
    return false;
  }

  static bool IS_IN_POT(string[] query, GameStateQueryContext context) {
    if (context.CustomFields?.TryGetValue("Tile", out var obj) is null or false
        || obj is not Vector2 tile
        || context.CustomFields?.TryGetValue("Dirt", out var obj2) is null or false
        || obj2 is not HoeDirt dirt
        ) {
      return GameStateQuery.Helpers.ErrorResult(query, "Error: Called outside of crop extension data?");
    }
    return dirt.Pot is not null && dirt.Pot.QualifiedItemId != WaterIndoorPotUtils.WaterPlanterQualifiedItemId;
  }

  static bool IS_FULLY_GROWN(string[] query, GameStateQueryContext context) {
    if (context.CustomFields?.TryGetValue("Tile", out var obj) is null or false
        || obj is not Vector2 tile
        || context.CustomFields?.TryGetValue("Dirt", out var obj2) is null or false
        || obj2 is not HoeDirt dirt
        ) {
      return GameStateQuery.Helpers.ErrorResult(query, "Error: Called outside of crop extension data?");
    }
    return dirt.crop?.fullyGrown.Value is true;
  }

  //static bool IS_CROP(string[] query, GameStateQueryContext context) {
  //  if (!ArgUtility.TryGet(query, 1, out var cropId, out var error, false, "string cropId")) {
  //    return GameStateQuery.Helpers.ErrorResult(query, error);
  //  }
  //  if (context.CustomFields?.TryGetValue("Crop", out var obj) is null or false
  //      || obj is not Crop crop) {
  //    return GameStateQuery.Helpers.ErrorResult(query, "Error: Called outside of crop extension data?");
  //  }
  //  return crop.netSeedIndex.Value == cropId;
  //}

  // Trigger action starts!
  // This handles the common part of the trigger actions - retrieving the arguments from the command
  // and the context, iterates over every hoedirt/crop in the target area, and runs the provided
  // action for them.
  // Some actions may have extra arguments before the shared ones; startIndex allows space for them.
  static bool RunTriggerOnCropsCommon(
      string[] args,
      TriggerActionContext context,
      int startIndex,
      Action<HoeDirt, Farmer> action,
      out string? error) {
    error = null;
    if (!ArgUtility.TryGetOptionalInt(args, startIndex + 1, out var radius, out error, 1, "int radius")
        || !ArgUtility.TryGetOptionalBool(args, startIndex + 2, out var excludeMainCrop, out error, true, "bool excludeMainCrop")
        || !ArgUtility.TryGetOptional(args, startIndex + 3, out var gsq, out error, null, true, "string gsq")
         ) {
      return false;
    }
    if (context.TriggerArgs.Length < 3
        || context.TriggerArgs[0] is not HoeDirt hoeDirt
        || context.TriggerArgs[1] is not Farmer who
        || context.TriggerArgs[2] is not Crop crop) {
      error = "Error running trigger action - ran outside of crop extension data?";
      return false;
    }
    // Aquaponics hack - ignore out of bounds crops
    if (hoeDirt.Tile.X < 0 || hoeDirt.Tile.Y < 0) return true;

    for (var x = -radius; x <= radius; x++) {
      for (var y = -radius; y <= radius; y++) {
        if (excludeMainCrop && x == 0 && y == 0) {
          continue;
        }
        if (hoeDirt.Location.terrainFeatures.TryGetValue(hoeDirt.Tile + new Vector2(x, y), out var t)
            && t is HoeDirt hd
            && hd.crop is not null
            && (String.IsNullOrWhiteSpace(gsq) || GameStateQuery.CheckConditions(gsq, CropExtensionHandler.GetGsqContext(hd.crop, who)))
            ) {
          action(hd, who);
        }
        if (hoeDirt.Location.objects.TryGetValue(hoeDirt.Tile + new Vector2(x, y), out var o)
            && o is IndoorPot pot
            && pot.hoeDirt.Value.crop is not null
            && (String.IsNullOrWhiteSpace(gsq) || GameStateQuery.CheckConditions(gsq, CropExtensionHandler.GetGsqContext(pot.hoeDirt.Value.crop, who)))
           ) {
          action(pot.hoeDirt.Value, who);
        }
      }
    }
    return true;
  }

  static void RunActionForRadius(Vector2 sourceTile, int radius, Action<Vector2> action) {
    for (var x = -radius; x <= +radius; x++) {
      for (var y = -radius; y <= +radius; y++) {
        action(sourceTile + new Vector2(x, y));
      }
    }
  }

  static bool ResetGrowDays(string[] args, TriggerActionContext context, out string? error) {
    return RunTriggerOnCropsCommon(args, context, 0, (hoeDirt, who) => {
      hoeDirt.crop?.ResetPhaseDays();
      hoeDirt.applySpeedIncreases(who);
    }, out error);
  }

  static bool KillCrop(string[] args, TriggerActionContext context, out string? error) {
    return RunTriggerOnCropsCommon(args, context, 0, (hoeDirt, who) => {
      hoeDirt.crop.Kill();
    }, out error);
  }

  static bool DestroyCrop(string[] args, TriggerActionContext context, out string? error) {
    return RunTriggerOnCropsCommon(args, context, 0, (hoeDirt, who) => {
      hoeDirt.destroyCrop(false);
    }, out error);
  }

  static bool TransformCrop(string[] args, TriggerActionContext context, out string? error) {
    if (!ArgUtility.TryGet(args, 1, out var newCropId, out error, false, "string newCropId")) {
      return false;
    }
    return RunTriggerOnCropsCommon(args, context, 1, (hoeDirt, who) => {
      var newCrop = new Crop(newCropId, (int)hoeDirt.Tile.X, (int)hoeDirt.Tile.Y, hoeDirt.Location);
      var oldCrop = hoeDirt.crop;
      hoeDirt.destroyCrop(false);
      hoeDirt.plant(newCropId, who, false);
      if (oldCrop is not null && Game1.cropData.ContainsKey(newCropId)) {
        newCrop.currentPhase.Value = Math.Min(oldCrop.currentPhase.Value, oldCrop.phaseDays.Count - 1);
      }
    }, out error);
  }

  static bool PlantCrop(string[] args, TriggerActionContext context, out string? error) {
    if (!ArgUtility.TryGet(args, 1, out var cropId, out error, false, "string CropId")
        || !ArgUtility.TryGetInt(args, 2, out var radius, out error, "int radius")
        || !ArgUtility.TryGetOptionalFloat(args, 3, out var chance, out error, 1, "float chance")
        || !ArgUtility.TryGetOptionalInt(args, 4, out var maxCount, out error, Int32.MaxValue, "int maxCount")
        ) {
      return false;
    }
    if (context.TriggerArgs.Length < 2
        || context.TriggerArgs[0] is not HoeDirt hoeDirt
        || context.TriggerArgs[1] is not Farmer who) {
      error = "Error running trigger action - ran outside of crop extension data?";
      return false;
    }
    // Aquaponics hack - ignore out of bounds crops
    if (hoeDirt.Tile.X < 0 || hoeDirt.Tile.Y < 0) return true;

    var count = 0;
    for (var x = hoeDirt.Tile.X - radius; x <= hoeDirt.Tile.X + radius; x++) {
      for (var y = hoeDirt.Tile.Y - radius; y <= hoeDirt.Tile.Y + radius; y++) {
        if (hoeDirt.Location.terrainFeatures.TryGetValue(new(x, y), out var t)
            && t is HoeDirt hd
            && hd.crop is null
            && (chance >= 1.0 || Game1.random.NextBool(chance))) {
          hd.plant(cropId, who, false);
          count++;
          if (count >= maxCount) return true;
        }
      }
    }
    return true;
  }

  static bool IfCrop(string[] args, TriggerActionContext context, out string? error) {
    if (context.TriggerArgs.Length < 3
        || context.TriggerArgs[0] is not HoeDirt hoeDirt
        || context.TriggerArgs[1] is not Farmer who
        || context.TriggerArgs[2] is not Crop crop) {
      error = "Error running trigger action - ran outside of crop extension data?";
      return false;
    }
    int startTrueIndex = -1;
    for (int i = 1; i < args.Length; i++) {
      if (args[i] == "##") {
        startTrueIndex = i + 1;
        break;
      }
    }
    if (startTrueIndex == -1 || startTrueIndex == args.Length) {
      return InvalidFormatError(out error);
    }
    int startFalseIndex = -1;
    for (int j = startTrueIndex + 1; j < args.Length; j++) {
      if (args[j] == "##") {
        startFalseIndex = j + 1;
        break;
      }
    }
    if (startFalseIndex == args.Length - 1) {
      return InvalidFormatError(out error);
    }
    Exception exception;
    if (GameStateQuery.CheckConditions(ArgUtility.UnsplitQuoteAware(args, ' ', 1, startTrueIndex - 1 - 1), CropExtensionHandler.GetGsqContext(crop, who))) {
      int maxCount = ((startFalseIndex > -1) ? (startFalseIndex - startTrueIndex - 1) : int.MaxValue);
      string action = ArgUtility.UnsplitQuoteAware(args, ' ', startTrueIndex, maxCount);
      if (!TriggerActionManager.TryRunAction(
            action
            .Replace("TILE_X", hoeDirt.Tile.X.ToString())
            .Replace("TILE_Y", hoeDirt.Tile.Y.ToString())
            .Replace("LOCATION_NAME", hoeDirt.Location.Name),
            "Manual",
            context.TriggerArgs,
            out error,
            out exception)) {
        error = "failed applying if-true action '" + action + "': " + error;
        return false;
      }
    } else if (startFalseIndex > -1) {
      string action2 = ArgUtility.UnsplitQuoteAware(args, ' ', startFalseIndex);
      if (!TriggerActionManager.TryRunAction(
            action2
            .Replace("TILE_X", hoeDirt.Tile.X.ToString())
            .Replace("TILE_Y", hoeDirt.Tile.Y.ToString())
            .Replace("LOCATION_NAME", hoeDirt.Location.Name),
            "Manual",
            context.TriggerArgs,
            out error,
            out exception)) {
        error = "failed applying if-false action '" + action2 + "': " + error;
        return false;
      }
    }
    error = null;
    return true;
    static bool InvalidFormatError(out string outError) {
      outError = "invalid format: expected a string in the form 'If <game state query> ## <do if true>' or 'If <game state query> ## <do if true> ## <do if false>'";
      return false;
    }
  }
}

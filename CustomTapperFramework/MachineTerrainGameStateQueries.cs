using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.TerrainFeatures;
using System;

namespace Selph.StardewMods.MachineTerrainFramework;

using Helpers = GameStateQuery.Helpers;

enum TerrainFeatures {
  Tree,
  FruitTree,
  GiantCrop,
  Unknown,
}

public static class MachineTerrainGameStateQueries {
  public static bool MACHINE_TILE_HAS_TERRAIN_FEATURE(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGetEnum<TerrainFeatures>(query, 1, out var featureEnumCondition, out var error) ||
        !ArgUtility.TryGetOptional(query, 2, out var featureIdCondition, out error)) {
      return Helpers.ErrorResult(query, error);
    }
    if (context.CustomFields == null ||
        !context.CustomFields.TryGetValue("Tile", out object? tileObj) ||
        tileObj is not Vector2 tile) {
      return Helpers.ErrorResult(query, "No tile found - called outside TerrainCondition?");
    }
    if (Utils.GetFeatureAt(context.Location, tile, out var feature, out var unused)) {
      var featureEnum = feature switch {
        Tree => TerrainFeatures.Tree,
        FruitTree => TerrainFeatures.FruitTree,
        GiantCrop => TerrainFeatures.GiantCrop,
        _ => TerrainFeatures.Unknown,
      };
      if (featureEnum != featureEnumCondition) {
        return false;
      }
      if (featureIdCondition != null) {
        string? featureId = Utils.GetFeatureId(feature);
        return featureIdCondition == featureId;
      }
      return true;
    }
    return false;
  }
}

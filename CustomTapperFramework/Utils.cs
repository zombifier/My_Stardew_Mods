using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;


namespace Selph.StardewMods.MachineTerrainFramework;

using SObject = StardewValley.Object;

public static class Utils {
  public static bool GetFeatureAt(GameLocation location, Vector2 pos, out TerrainFeature feature, out Vector2 centerPos) {
    centerPos = pos;
    if (location.terrainFeatures.TryGetValue(pos, out feature) &&
        (feature is Tree || feature is FruitTree)) {
      centerPos = pos;
      return true;
    }
    foreach (var resourceClump in location.resourceClumps) {
      if (resourceClump.occupiesTile((int)pos.X, (int)pos.Y)) {
        centerPos = GetTapperLocationForClump(resourceClump);
        feature = resourceClump;
        return true;
      }
    }
    return false;
  }

  public static IList<ExtendedTapItemData> GetOutputRulesForPlacedTapper(SObject tapper, out TerrainFeature feature, string ruleId = null) {
    if (GetFeatureAt(tapper.Location, tapper.TileLocation, out feature, out var centerPos)) {
      return GetOutputRules(tapper, feature, out var _unused, ruleId);
    }
    return null;
  }

  // Legacy Tapper API:
  // Get the modded output rules for this tapper.
  // NOTE: If this function returns null, its consumers should not update the tapper.
  // If a list, then touch it.
  public static IList<ExtendedTapItemData> GetOutputRules(SObject tapper, TerrainFeature feature, out bool disallowBaseTapperRules, string ruleId = null) {
    disallowBaseTapperRules = false;
    if (ModEntry.assetHandler.data.TryGetValue(tapper.QualifiedItemId, out var data)) {
      disallowBaseTapperRules = !data.AlsoUseBaseGameRules;
      IList<ExtendedTapItemData> outputRules = feature switch {
        Tree tree => tree.growthStage.Value >= 5 && !tree.stump.Value ?
          data.TreeOutputRules : null,
        FruitTree fruitTree => fruitTree.growthStage.Value >= 4 && !fruitTree.stump.Value ?
          data.FruitTreeOutputRules : null,
        GiantCrop giantCrop => data.GiantCropOutputRules,
          _ => null,
      };
      if (outputRules == null) return null;
      string sourceId = GetFeatureId(feature);
      IEnumerable<ExtendedTapItemData> filteredOutputRules;
      if (ruleId != null) {
        filteredOutputRules = from outputRule in outputRules
          where outputRule.Id == ruleId
          select outputRule;
      } else {
        filteredOutputRules = from outputRule in outputRules
        where outputRule.SourceId == null || outputRule.SourceId == sourceId
        select outputRule;
      }
      return filteredOutputRules.ToList();
    }
    return null;
  }

  // Copied from base game's function
  // Update this tapper with produce from the base game, if any.
  public static void UpdateTapperProduct(SObject tapper) {
    if (tapper == null) {
      return;
    }
    var data = GetOutputRulesForPlacedTapper(tapper, out var feature);
    var farmer = feature switch {
      Tree tree => Game1.GetPlayer(tree.lastPlayerToHit.Value),
      FruitTree fruitTree => Game1.GetPlayer(fruitTree.lastPlayerToHit.Value),
      _ => null,
    };
    if (data != null) {
      float timeMultiplier = 1f;
      foreach (string contextTag in tapper.GetContextTags()) {
        if (contextTag.StartsWith("tapper_multiplier_") && float.TryParse(contextTag.Substring("tapper_multiplier_".Length), out var result)) {
          timeMultiplier = 1f / result;
          break;
        }
      }
      string previousItemId = ((tapper.lastInputItem?.Value?.QualifiedItemId != null) ? ItemRegistry.QualifyItemId(tapper.lastInputItem.Value.QualifiedItemId) : null);
      foreach (ExtendedTapItemData tapItem in data) {

        Random a = new Random();
        // Check if chance applies
        if (!Game1.random.NextBool(tapItem.Chance)) {
          continue;
        }

        // Allow game state and item queries to use an input item.
        // For trees, this will be their seeds.
        // For fruit trees and giant crops, this will be their first produce defined in the list.
        Item inputItem = GetFeatureItem(feature, farmer);
        if (!GameStateQuery.CheckConditions(tapItem.Condition, tapper.Location, farmer, targetItem: null, inputItem: inputItem)) {
          continue;
        }

        // Check if previousItemId matches
        if (tapItem.PreviousItemId != null) {
          bool flag = false;
          foreach (string item2 in tapItem.PreviousItemId) {
            flag = (string.IsNullOrEmpty(item2) ? (previousItemId == null) : string.Equals(previousItemId, ItemRegistry.QualifyItemId(item2), StringComparison.OrdinalIgnoreCase));
            if (flag) {
              break;
            }
          }
          if (!flag) {
            continue;
          }
        }

        // Check if product is in season
        if (tapItem.Season.HasValue && !tapper.Location.SeedsIgnoreSeasonsHere() && tapItem.Season != tapper.Location.GetSeason()) {
          continue;
        }

        Item item = ItemQueryResolver.TryResolveRandomItem(tapItem, new ItemQueryContext(tapper.Location, farmer, null, "MachineTerrainFramework custom tapper '" + tapper.QualifiedItemId + "' > output rules"),
            avoidRepeat: false, null, (string id) =>
            id.Replace("DROP_IN_ID", inputItem?.QualifiedItemId ?? "0")
            .Replace("NEARBY_FLOWER_ID", MachineDataUtility.GetNearbyFlowerItemId(tapper) ?? "-1"));
        if (item != null && item is SObject @object) {
          int num = (int)Utility.ApplyQuantityModifiers(tapItem.DaysUntilReady, tapItem.DaysUntilReadyModifiers, tapItem.DaysUntilReadyModifierMode, tapper.Location, Game1.player);
          var output = @object;
          var minutesUntilReady = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay, (int)Math.Max(1.0, Math.Floor((float)num * timeMultiplier)));
          tapper.heldObject.Value = output;
          tapper.MinutesUntilReady = minutesUntilReady;
          tapper.lastOutputRuleId.Value = tapItem.Id;
          tapper.showNextIndex.Value = false;
          break;
        }
      }
    }
  }

  public static void Shake(TerrainFeature feature, Vector2 tile) {
    if (feature is Tree tree) {
      tree.shake(tile, false);
    }
    if (feature is FruitTree fruitTree) {
      fruitTree.shake(tile, false);
    }
    if (feature is GiantCrop giantCrop) {
      // Shake the crop
      giantCrop.shakeTimer = 100f;
      giantCrop.NeedsUpdate = true;
    }
  }

  public static Vector2 GetTapperLocationForClump(ResourceClump resourceClump) {
    var centerPos = resourceClump.Tile;
    centerPos.X = (int)centerPos.X + (int)resourceClump.width.Value / 2;
    centerPos.Y = (int)centerPos.Y + (int)resourceClump.height.Value - 1;
    return centerPos;
  }

  public static bool IsCrabPot(Item item) {
    return item.HasContextTag("custom_crab_pot_item");
  }

  public static bool IsCustomTreeTappers(Item item) {
    return item.HasContextTag("custom_wild_tree_tapper_item");
  }

  public static bool DisallowWildTreePlacement(Item item) {
    return item.HasContextTag("disallow_wild_tree_placement");
  }

  public static bool IsFruitTreeTapper(Item item) {
    return item.HasContextTag("custom_fruit_tree_tapper_item");
  }

  public static bool IsGiantCropTapper(Item item) {
    return item.HasContextTag("custom_giant_crop_tapper_item");
  }

  // Return true if this item is a modded tapper and it is placeable on the terrain feature in question.
  // If isVanillaTapper is true, then it is a vanilla tapper
  public static bool IsModdedTapperPlaceableAt(SObject obj, GameLocation location, Vector2 tileLocation, out bool isVanillaTapper, out TerrainFeature feature, out Vector2 centerPos) {
    isVanillaTapper = true;
    if (!Utils.GetFeatureAt(location, tileLocation, out feature, out centerPos) || location.objects.ContainsKey(centerPos)) {
      return false;
    }

    // Context tags based
    switch (feature) {
      case Tree:
        if (DisallowWildTreePlacement(obj)) return false;
        if (IsCustomTreeTappers(obj)) {
          isVanillaTapper = false;
          return true;
        }
        break;
      case FruitTree:
        if (IsFruitTreeTapper(obj)) return true;
        break;
      case GiantCrop:
        if (IsGiantCropTapper(obj)) return true;
        break;
    }

    // Legacy API
    if (Utils.GetOutputRules(obj, feature, out bool disallowBaseTapperRules) is var outputRules &&
        outputRules != null) {
      return true;
    }
    isVanillaTapper = !disallowBaseTapperRules;
    return false;
  } 

  public static string? GetFeatureId(TerrainFeature feature) {
    return feature switch {
      Tree tree => tree.treeType.Value,
      FruitTree fruitTree => fruitTree.treeId.Value,
      GiantCrop giantCrop => giantCrop.Id,
      _ => null,
    };
  }

  public static Item? GetFeatureItem(TerrainFeature feature, Farmer player) {
    switch (feature) {
      case Tree tree:
        return ItemRegistry.Create(tree.GetData().SeedItemId);
      case FruitTree fruitTree:
        if (fruitTree.GetData().Fruit.Count == 0) {
          return null;
        }
        return ItemQueryResolver.TryResolveRandomItem(fruitTree.GetData().Fruit[0], new ItemQueryContext(feature.Location, player, null, "MachineTerrainFramework terrain feature output"));
      case GiantCrop giantCrop:
        if (giantCrop.GetData().HarvestItems.Count == 0) {
          return null;
        }
        return ItemQueryResolver.TryResolveRandomItem(giantCrop.GetData().HarvestItems[0], new ItemQueryContext(feature.Location, player, null, "MachineTerrainFramework terrain feature output"));
      default:
        return null;
    }
  }

	public static Item? OutputTapper(SObject machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady) {
    if (GetFeatureAt(machine.Location, machine.TileLocation, out var feature, out var _) && feature is Tree tree) {
      overrideMinutesUntilReady = 0;
      try {
        Random r = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, 73137.0, (double)tree.Tile.X * 9.0, (double)tree.Tile.Y * 13.0);
        object[] args = [tree.GetData()?.TapItems, machine.lastInputItem.Value?.ItemId, r, (float)1.0, null, null];
        ModEntry.Helper.Reflection
          .GetMethod(tree, "TryGetTapperOutput")
          .Invoke<bool>(args);
        overrideMinutesUntilReady = Convert.ToInt32(args[5]);
        ModEntry.StaticMonitor.Log($"Found tapper output: {(args[4] as Item)?.QualifiedItemId ?? "null"} ready in {Convert.ToInt32(args[5])}");
        return args[4] as Item;
      } catch (Exception e) { 
        ModEntry.StaticMonitor.Log($"Error getting tapper output: {e.Message}", LogLevel.Warn);
      }
    }
    else {
      ModEntry.StaticMonitor.Log($"Error getting tapper output: can't find tree underneath machine at {machine.Location.DisplayName} coords {machine.TileLocation.X} {machine.TileLocation.Y}", LogLevel.Warn);
    }
    overrideMinutesUntilReady = null;
    return null;

  }
}

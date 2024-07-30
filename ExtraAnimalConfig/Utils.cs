using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.FarmAnimals;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ExtraAnimalConfig;

public sealed class SiloUtils {
  static string SiloCapacityKeyPrefix = $"{ModEntry.UniqueId}.SiloCapacity.";
  static string FeedCountKeyPrefix = $"{ModEntry.UniqueId}.FeedCount.";

  public static int GetFeedCapacityFor(GameLocation location, string itemId) {
    int totalCapacity = 0;
    foreach (Building building in location.buildings) {
      if ((building.GetData()?.CustomFields?.TryGetValue(SiloCapacityKeyPrefix + itemId, out var capacityStr) ?? false) &&
          int.TryParse(capacityStr, out var capacity)) {
        totalCapacity += capacity;
      }
    }
    return totalCapacity;
  }

  public static Dictionary<string, int> GetFeedCapacityFor(GameLocation location, string[] itemIds) {
    Dictionary<string, int> result = new();
    foreach (var itemId in itemIds) {
      result.Add(itemId, GetFeedCapacityFor(location, itemId));
    }
    return result;
  }

  public static List<string> GetFeedForThisBuilding(Building? building) {
    List<string> result = new();
    if (building is null) {
      return result;
    }
    if (building.hayCapacity.Value > 0) {
      result.Add("(O)178");
    }
    if (building.GetData()?.CustomFields is not null) {
      foreach (var entry in building.GetData()?.CustomFields!) {
        var index = entry.Key.IndexOf(SiloCapacityKeyPrefix);
        if (index >= 0) {
          result.Add(entry.Key.Remove(index, SiloCapacityKeyPrefix.Length));
        }
      }
    }
    return result;
  }

  public static int GetFeedCountFor(GameLocation location, string itemId) {
    if (location.modData.TryGetValue(FeedCountKeyPrefix + itemId, out var countStr) &&
        int.TryParse(countStr, out var count) &&
        count > 0) {
      return count;
    }
    return 0;
  }

  // Every function assumes itemId is qualified and valid object ID
  public static SObject? GetFeedFromAnySilo(string itemId, int itemCount = 1) {
    SObject? feedObj = null;
    Utility.ForEachLocation((GameLocation location) => {
        var totalCount = GetFeedCountFor(location, itemId);
        var count = Math.Min(totalCount, itemCount);
        if (count > 0) {
          totalCount -= count;
          location.modData[FeedCountKeyPrefix + itemId] = totalCount.ToString();
          feedObj = ItemRegistry.Create<SObject>(itemId, count);
          return true;
        }
        return false;
    });
    return feedObj;
  }

  // Saves the feed to the current location.
  // Returns the number of feed that can't be stored
  public static int StoreFeedInAnySilo(string itemId, int count, bool probe = false) {
    Utility.ForEachLocation((GameLocation location) => {
      var currentCount = GetFeedCountFor(location, itemId);
      var newCount = Math.Min(currentCount + count, GetFeedCapacityFor(location, itemId));
      if (!probe) {
        location.modData[FeedCountKeyPrefix + itemId] = newCount.ToString();
      }
      count -= newCount - currentCount;
      if (count <= 0) {
        return true;
      }
      return false;
    });
    return count;
  }
}

public sealed class AnimalUtils {
  static string CustomTroughTileProperty = $"{ModEntry.UniqueId}.CustomTrough";
  public static string BuildingFeedOverrideIdKey = $"{ModEntry.UniqueId}.BuildingFeedOverrideId";

  // Whether this animal only eats modded food and not hay/grass
  // Returns false if they do eat grass outside, or don't need to eat at all
  public static bool AnimalOnlyEatsModdedFood(FarmAnimal animal) {
    return (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
        animalExtensionData.FeedItemId != null &&
        (animal.GetAnimalData()?.GrassEatAmount ?? 0) <= 0);
  }

  public static bool AnimalIsOutsideForager(FarmAnimal animal) {
    return (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
        animalExtensionData.OutsideForager);
  }

  // Returns true if:
  // * This is a custom trough which accepts this item
  // * This is a vanilla trough whose building has an override to accept this item
  public static bool CanThisTileAcceptThisItem(AnimalHouse animalHouse, int x, int y, string qualifiedItemId) {
    return qualifiedItemId == GetCustomFeedForTile(animalHouse, x, y);
  }

  public static string? GetCustomFeedForTile(AnimalHouse animalHouse, int x, int y, bool excludeOverride = false) {
    string? qualifiedItemId = animalHouse.doesTileHaveProperty(x, y, CustomTroughTileProperty, "Back");
    if (!excludeOverride &&
        qualifiedItemId is null &&
        animalHouse.doesTileHaveProperty(x, y, "Trough", "Back") != null &&
         GetBuildingFeedOverride(animalHouse, out var buildingFeedOverride)) {
      qualifiedItemId = buildingFeedOverride;
    }
    return qualifiedItemId;
  }

  public static string? GetCustomFeedThisAnimalCanEat(FarmAnimal animal, GameLocation animalHouse) {
    string? qualifiedItemId = null;
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData)) {
      qualifiedItemId = animalExtensionData.FeedItemId;
    }
    if (qualifiedItemId is null &&
         GetBuildingFeedOverride(animalHouse, out var buildingFeedOverride)) {
      qualifiedItemId = buildingFeedOverride;
    }
    return qualifiedItemId;
  }

  public static bool GetBuildingFeedOverride(GameLocation animalHouse, out string? itemId) {
    itemId = null;
    if (animalHouse.GetContainingBuilding()?.GetData()?.CustomFields?.TryGetValue(BuildingFeedOverrideIdKey, out var value) ?? false) {
      itemId = value;
      return true;
    }
    return false;
  }

  public static bool BuildingHasFeedOverride(GameLocation animalHouse) {
    return GetBuildingFeedOverride(animalHouse, out var _);
  }
}

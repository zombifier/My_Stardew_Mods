using StardewValley;
using StardewValley.Internal;
using StardewValley.GameData.FarmAnimals;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.GameData.Buildings;
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
      if (building.daysOfConstructionLeft.Value <= 0 &&
          (building.GetData()?.CustomFields?.TryGetValue(SiloCapacityKeyPrefix + itemId, out var capacityStr) ?? false) &&
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
          return false;
        }
        return true;
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
        return false;
      }
      return true;
    });
    return count;
  }

  public static bool ScytheHasGatherer(Tool tool) {
    ScytheToolEnchantments.IScytheToolEnchantmentsApi? api =
      ModEntry.Helper.ModRegistry.GetApi<ScytheToolEnchantments.IScytheToolEnchantmentsApi>("mushymato.ScytheToolEnchantments");
    if (api is not null) {
      return api.HasGathererEnchantment(tool);
    }
    return false;
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

  public static Item? GetGoldenAnimalCracker(FarmAnimal animal) {
    return animal.hasEatenAnimalCracker.Value ? ItemRegistry.Create("(O)GoldenAnimalCracker") : null;
  }
}

public sealed class ExtraProduceUtils {
  // Animal modData keys
  static string ProduceDaysSinceLastLayKeyPrefix = $"${ModEntry.UniqueId}.ProduceDaysSinceLastLay";
  static string CurrentProduceIdKeyPrefix = $"${ModEntry.UniqueId}.CurrentProduceId";

  public static void DecrementProduceDays(FarmAnimal animal) {
    foreach (var key in animal.modData.Keys) {
      if (key.StartsWith(ProduceDaysSinceLastLayKeyPrefix) && Int32.TryParse(animal.modData[key], out var days)) {
        animal.modData[key] = (days + 1).ToString();
      }
    }
  }
  
  public static bool GetHarvestMethodOverride(FarmAnimal animal, string? produceId, out string? harvestMethod) {
    harvestMethod = null;
    if (produceId != null && animal.type?.Value != null &&
        ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
        animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(produceId) ?? produceId, out var animalProduceExtensionData) &&
        animalProduceExtensionData.HarvestTool != null) {
      harvestMethod = animalProduceExtensionData.HarvestTool;
      return true;
    }
    return false;
  }

  public static bool IsHarvestMethod(FarmAnimal animal, string? produceId, FarmAnimalHarvestType harvestMethod, string? tool = null) {
    if (GetHarvestMethodOverride(animal,  produceId, out var harvestMethodOverride)) {
      var moddedHarvestToolString = harvestMethod switch {
        FarmAnimalHarvestType.DropOvernight => "DropOvernight",
        FarmAnimalHarvestType.DigUp => "DigUp",
        FarmAnimalHarvestType.HarvestWithTool => tool ?? "Milk Pail",
        _ => "None",
      };
      return harvestMethodOverride == moddedHarvestToolString;
    }
    var vanillaTool = animal.GetAnimalData()?.HarvestTool;
    return animal.GetHarvestType() == harvestMethod && (tool is null || vanillaTool == tool);
  }

  // Returns whether the animal's current produce is hardcoded to drop instead of harvested by tool
  public static bool DoNotDropCurrentProduce(FarmAnimal animal, string? produceId) {
    return !IsHarvestMethod(animal, produceId, FarmAnimalHarvestType.DropOvernight);
  }

  public static void DropOrAddToGrabber(FarmAnimal animal, string? produceId, out bool drop, out bool addToGrabber) {
    drop = animal.GetHarvestType() == FarmAnimalHarvestType.DropOvernight;
    addToGrabber = animal.GetHarvestType() != FarmAnimalHarvestType.DigUp;
    if (GetHarvestMethodOverride(animal, produceId, out var harvestMethod)) {
      drop = harvestMethod == "DropOvernight";
      addToGrabber = harvestMethod != "DigUp";
    }
  }

  public static bool IsDebris(FarmAnimal animal, string? produceId) {
    if (GetHarvestMethodOverride(animal, produceId, out var harvestMethod)) {
      return harvestMethod == "Debris";
    }
    return false;
  }

  public static SObject CreateProduce(string produceId, FarmAnimal animal) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
        animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(produceId) ?? produceId, out var animalProduceExtensionData) &&
        animalProduceExtensionData.ItemQuery != null) {
      // DO NOT SUBMIT WITHOUT FIXING THIS FOR 1.6.9
      //var context = new ItemQueryContext(animal.home?.GetIndoors(), Game1.GetPlayer(animal.ownerID.Value), Game1.random);
      var context = new ItemQueryContext(animal.home?.GetIndoors(), Game1.getFarmer(animal.ownerID.Value), Game1.random);
      var item = ItemQueryResolver.TryResolveRandomItem(animalProduceExtensionData.ItemQuery, context);
      if (item is SObject obj) {
        return obj;
      }
    }
    // Vanilla fallback
    return ItemRegistry.Create<SObject>(produceId);
  }

  // Queue the additional produces into the animal's 'queue'. Or drop them/add them to
  // autograbber.
  // If the animal's produce is null then pop the queue and put it in the currentProduce field as well.
  // To be run in the postfix of the day update.
  public static void QueueExtraProduceIds(FarmAnimal animal, GameLocation location) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
        animalExtensionData.ExtraProduceSpawnList is not null) {
      // Global conditions - don't produce if the animal's not fed or if it's juvenile
      if (animal.moodMessage.Value == 4 || animal.isBaby()) {
        return;
      }
      foreach (var slotData in animalExtensionData.ExtraProduceSpawnList) {
        // Per slot conditions
        if (slotData.SyncWithMainProduce && animal.daysSinceLastLay.Value > 0) {
          continue;
        }
        if (animal.modData.TryGetValue($"{ProduceDaysSinceLastLayKeyPrefix}.{slotData.Id}", out var daysSinceLastLayStr) &&
            Int32.TryParse(daysSinceLastLayStr, out var daysSinceLastLay) &&
            daysSinceLastLay < slotData.DaysToProduce) {
          continue;
        }
        foreach (var produceData in slotData.ProduceItemIds ?? []) {
          // Per produce conditions
          if (animal.friendshipTowardFarmer.Value < produceData.MinimumFriendship ||
              produceData.Condition != null && !GameStateQuery.CheckConditions(produceData.Condition, location, null, null, AnimalUtils.GetGoldenAnimalCracker(animal))) {
            continue;
          }

          // Create the produce object. Fallback to clay if not found
          var produceId = produceData.ItemId ?? "330";
          animal.modData[$"{ProduceDaysSinceLastLayKeyPrefix}.{slotData.Id}"] = "0";
          animal.modData[$"{CurrentProduceIdKeyPrefix}.{slotData.Id}"] = produceId;
          break;
        }
      }

      List<string> keysToRemove = new();
      // Now process the produce - add them to grabber, drop them, or set them as the animal's main produce if it's null
      foreach (var key in animal.modData.Keys) {
        if (key.StartsWith(CurrentProduceIdKeyPrefix)) {
          var produceId = animal.modData[key];
          var produce = CreateProduce(produceId, animal);
          produce.CanBeSetDown = false;
          produce.Quality = animal.produceQuality.Value;
          if (animal.hasEatenAnimalCracker.Value) {
            produce.Stack = 2;
          }

          DropOrAddToGrabber(animal, produceId, out bool drop, out bool addToGrabber);
          bool addedToGrabber = false;
          if (addToGrabber) {
            foreach (SObject machine in location.objects.Values) {
              if (machine.QualifiedItemId == "(BC)165" && machine.heldObject.Value is Chest chest && chest.addItem(produce) == null) {
                machine.showNextIndex.Value = true;
                addedToGrabber = true;
                keysToRemove.Add(key);
                break;
              }
            }
          }
          if (!addedToGrabber) {
            if (drop) {
              produce.Stack = 1;
              Utility.spawnObjectAround(animal.Tile, produce, location);
              if (animal.hasEatenAnimalCracker.Value) {
                SObject o = (SObject)produce.getOne();
                Utility.spawnObjectAround(animal.Tile, o, location);
              }
              keysToRemove.Add(key);
            } else if (animal.currentProduce.Value is null) {
              animal.currentProduce.Value = produceId;
              keysToRemove.Add(key);
            }
          }
        }
      }

      foreach (var key in keysToRemove) {
        animal.modData.Remove(key);
      }
    }
  }

  public static void PopQueueAndReplaceProduce(FarmAnimal animal) {
    if (animal.currentProduce.Value is not null) {
      return;
    }
    foreach (var key in animal.modData.Keys) {
      if (key.StartsWith(CurrentProduceIdKeyPrefix)) {
        animal.currentProduce.Value = animal.modData[key];
        animal.ReloadTextureIfNeeded();
        animal.modData.Remove(key);
        return;
      }
    }
  }

  public static void ReplaceCurrentProduceWithMatching(FarmAnimal animal, FarmAnimalHarvestType harvestMethod, string? tool = null) {
    if (IsHarvestMethod(animal, animal.currentProduce.Value, harvestMethod, tool)) {
      return;
    }
    var currentProduce = animal.currentProduce.Value;
    foreach (var key in animal.modData.Keys) {
      if (key.StartsWith(CurrentProduceIdKeyPrefix) &&
          IsHarvestMethod(animal, animal.modData[key], harvestMethod, tool)) {
          animal.currentProduce.Value = animal.modData[key];
          animal.modData[key] = currentProduce;
          return;
      }
    }
  }

  public static void DropDebrisOnDayStart(FarmAnimal animal) {
    List<string> debrisToDrop = new();
    if (animal.currentProduce.Value is not null && ExtraProduceUtils.IsDebris(animal, animal.currentProduce.Value)) {
      debrisToDrop.Add(animal.currentProduce.Value);
      animal.currentProduce.Value = null;
    }
    foreach (var key in animal.modData.Keys) {
      if (key.StartsWith(ExtraProduceUtils.CurrentProduceIdKeyPrefix) &&
          IsDebris(animal, animal.modData[key])) {
        debrisToDrop.Add(animal.modData[key]);
        animal.modData.Remove(key);
      }
    }
    foreach (var produceId in debrisToDrop) {
      var produce = CreateProduce(produceId, animal);
      produce.Stack = 1;
      produce.Quality = animal.produceQuality.Value;
      var debris = new Debris(-2, 1, animal.Tile * 64f, animal.Tile * 64f, 0.1f) {
        item = produce
      };
      debris.Chunks[0].bounces = 3;
      animal.currentLocation.debris.Add(debris);
      if (animal.hasEatenAnimalCracker.Value) {
        SObject o = (SObject)produce.getOne();
        o.Quality = animal.produceQuality.Value;
        var debris2 = new Debris(-2, 1, animal.Tile * 64f, animal.Tile * 64f, 0.1f) {
          item = o
        };
        debris2.Chunks[0].bounces = 3;
        animal.currentLocation.debris.Add(debris2);
      }
    }
  }
}

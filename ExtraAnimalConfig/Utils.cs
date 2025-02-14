using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Extensions;
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

public static class SiloUtils {
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

public static class AnimalUtils {
  static string CustomTroughTileProperty = $"{ModEntry.UniqueId}.CustomTrough";
  public static string BuildingFeedOverrideIdKey = $"{ModEntry.UniqueId}.BuildingFeedOverrideId";

  // Whether this animal only eats modded food and not hay/grass
  // Returns false if they do eat grass outside, or don't need to eat at all
  public static bool AnimalOnlyEatsModdedFood(FarmAnimal animal) {
    return (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
        animalExtensionData.FeedItemId != null &&
        (animal.GetAnimalData()?.GrassEatAmount ?? 0) <= 0);
  }

  public static bool AnimalIsOutsideForager(FarmAnimal animal) {
    return (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
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
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData)) {
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
    if (animalHouse.ParentBuilding?.GetData()?.CustomFields?.TryGetValue(BuildingFeedOverrideIdKey, out var value) ?? false) {
      itemId = value;
      return true;
    }
    return false;
  }

  public static bool BuildingHasFeedOverride(GameLocation animalHouse) {
    return GetBuildingFeedOverride(animalHouse, out var _);
  }

  public static string AnimalAge = $"{ModEntry.UniqueId}.Age";
  public static string AnimalFriendship = $"{ModEntry.UniqueId}.Friendship";

  public static Item? GetGoldenAnimalCracker(FarmAnimal animal) {
    var item = animal.hasEatenAnimalCracker.Value ?
      ItemRegistry.Create("(O)GoldenAnimalCracker") :
      ItemRegistry.Create("(O)Weeds");
    item.modData[AnimalAge] = animal.age.Value.ToString();
    item.modData[AnimalFriendship] = animal.friendshipTowardFarmer.Value.ToString();
    return item;
  }

  static Dictionary<long, double> BeginAttackTimeDict = new();
  static Dictionary<long, double> LastAttackTimeDict = new();
  static Dictionary<long, long> CurrentVictimDict = new();
  static Dictionary<long, double> LastPathfindingTimeDict = new();

  static double GetLastAttackTime(FarmAnimal animal) {
    if (!LastAttackTimeDict.ContainsKey(animal.myID.Value)) {
      LastAttackTimeDict[animal.myID.Value] = -1;
    }
    return LastAttackTimeDict[animal.myID.Value];
  }

  static double GetLastPathfindingTime(FarmAnimal animal, GameTime time) {
    if (!LastPathfindingTimeDict.ContainsKey(animal.myID.Value)) {
      LastPathfindingTimeDict[animal.myID.Value] = time.TotalGameTime.TotalMilliseconds;
    }
    return LastPathfindingTimeDict[animal.myID.Value];
  }

  static double GetBeginAttackTime(FarmAnimal animal, GameTime time) {
    if (!BeginAttackTimeDict.ContainsKey(animal.myID.Value)) {
      BeginAttackTimeDict[animal.myID.Value] = time.TotalGameTime.TotalMilliseconds;
    }
    return BeginAttackTimeDict[animal.myID.Value];
  }

  static Farmer? GetVictim(FarmAnimal animal, int attackRange) {
    if (CurrentVictimDict.TryGetValue(animal.myID.Value, out var farmerId)) {
      var farmer = Game1.GetPlayer(farmerId);
      if (farmer?.currentLocation != animal.currentLocation) {
        CurrentVictimDict.Remove(animal.myID.Value);
        return null;
      }
    }
    IList<Farmer> victimList = [];
    foreach (var potentialVictim in animal.currentLocation.farmers) {
      // Mercifully, animals will not attack farmers trying to harvest from them with a tool
      if (!animal.CanGetProduceWithTool(potentialVictim.CurrentTool) &&
          FarmAnimal.GetFollowRange(animal, attackRange).Contains(potentialVictim.StandingPixel)) {
        victimList.Add(potentialVictim);
      }
    }
    if (victimList.Count > 0) {
      var victim = Game1.random.ChooseFrom(victimList);
      CurrentVictimDict[animal.myID.Value] = victim.UniqueMultiplayerID;
      return victim;
    }
    return null;
  }

  public static void ClearDicts() {
    BeginAttackTimeDict.Clear();
    LastAttackTimeDict.Clear();
    CurrentVictimDict.Clear();
    LastPathfindingTimeDict.Clear();
  }

  // If attack animal
  // * Find target
  // * If target already intersects, damage
  // * Otherwise path to target
  public static void AnimalAttack(FarmAnimal animal, GameTime time, ref bool result) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
        animalExtensionData.IsAttackAnimal &&
        time.TotalGameTime.TotalMilliseconds - GetLastAttackTime(animal) > animalExtensionData.AttackIntervalMs) {
      var victim = GetVictim(animal, animalExtensionData.AttackRange);
      if (victim is not null) {
        if (FarmAnimal.GetFollowRange(animal, 1).Intersects(victim.GetBoundingBox())) {
          // RAWR!
          if (animalExtensionData.AttackDamage > 0) {
            animal.doEmote(12);
            victim.takeDamage(animalExtensionData.AttackDamage, false, null);
          }
          CurrentVictimDict.Remove(animal.myID.Value);
          LastAttackTimeDict[animal.myID.Value] = time.TotalGameTime.TotalMilliseconds;
          BeginAttackTimeDict.Remove(animal.myID.Value);
          animal.controller = null;
          result = true;
        } else if (time.TotalGameTime.TotalMilliseconds - GetBeginAttackTime(animal, time) > animalExtensionData.AttackMaxChaseTimeMs) {
          // Got bored, stop chasing
          animal.doEmote(8);
          animal.controller = null;
          CurrentVictimDict.Remove(animal.myID.Value);
          LastAttackTimeDict[animal.myID.Value] = time.TotalGameTime.TotalMilliseconds;
          BeginAttackTimeDict.Remove(animal.myID.Value);
//        } else if (FarmAnimal.NumPathfindingThisTick < FarmAnimal.MaxPathfindingPerTick) {
        } else if (time.TotalGameTime.TotalMilliseconds - GetLastPathfindingTime(animal, time) > 1000 &&
            FarmAnimal.NumPathfindingThisTick < FarmAnimal.MaxPathfindingPerTick) {
          // Keep chasing!
          animal.controller = new PathFindController(animal, animal.currentLocation, victim.TilePoint, Game1.random.Next(4));
          LastPathfindingTimeDict[animal.myID.Value] = time.TotalGameTime.TotalMilliseconds;
          FarmAnimal.NumPathfindingThisTick += 1;
          result = false;
        }
        return;
      } else {
        // No victim or victim became null, clean up
        CurrentVictimDict.Remove(animal.myID.Value);
        BeginAttackTimeDict.Remove(animal.myID.Value);
      }
    }
  }

  static string BuildingInhabitantsIgnoreRainKey = $"${ModEntry.UniqueId}.InhabitantsIgnoreRain";
  static string BuildingInhabitantsIgnoreWinterKey = $"${ModEntry.UniqueId}.InhabitantsIgnoreWinter";

  // the below functions false if the animal ignore rain/winter
  public static bool AnimalAffectedByRain(GameLocation location, FarmAnimal animal) {
    if ((ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
          animalExtensionData.IgnoreRain) ||
      (animal.home?.GetData()?.CustomFields?.ContainsKey(BuildingInhabitantsIgnoreRainKey) ?? false)) {
      return false;
    }
    return location.IsRainingHere();
  }

  public static bool AnimalAffectedByWinter(GameLocation location, FarmAnimal animal) {
    if ((ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
          animalExtensionData.IgnoreWinter) ||
        (animal.home?.GetData()?.CustomFields?.ContainsKey(BuildingInhabitantsIgnoreWinterKey) ?? false)) {
      return false;
    }
    return location.IsWinterHere();
  }
}

public static class ExtraProduceUtils {
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
    if (produceId != null &&
        ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
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

  public static string CachedProduceQualityKey = $"${ModEntry.UniqueId}.CachedProduceQuality";

  public static SObject CreateProduce(string produceId, FarmAnimal animal) {
    // Restore cached quality if set
    if (animal.modData.TryGetValue(CachedProduceQualityKey, out var cachedProduceQualityStr) &&
        Int32.TryParse(cachedProduceQualityStr, out var cachedProduceQuality)) {
      animal.produceQuality.Value = cachedProduceQuality;
      animal.modData.Remove(CachedProduceQualityKey);
    }
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
        animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(produceId) ?? produceId, out var animalProduceExtensionData) &&
        animalProduceExtensionData.ItemQuery != null) {
      var context = new ItemQueryContext(animal.home?.GetIndoors(), Game1.GetPlayer(animal.ownerID.Value), Game1.random, "ExtraAnimalConfig animal " + animal.type.Value + " producing");
      var item = ItemQueryResolver.TryResolveRandomItem(animalProduceExtensionData.ItemQuery, context);
      if (item is SObject obj) {
        if (animalProduceExtensionData.IgnoreAnimalQuality) {
          animal.modData[CachedProduceQualityKey] = animal.produceQuality.ToString();
          animal.produceQuality.Value = obj.Quality;
        }
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
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
        animalExtensionData.ExtraProduceSpawnList is not null &&
        animalExtensionData.ExtraProduceSpawnList.Count > 0) {
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
      animal.homeInterior?.debris.Add(debris);
      if (animal.hasEatenAnimalCracker.Value) {
        SObject o = (SObject)produce.getOne();
        o.Quality = animal.produceQuality.Value;
        var debris2 = new Debris(-2, 1, animal.Tile * 64f, animal.Tile * 64f, 0.1f) {
          item = o
        };
        debris2.Chunks[0].bounces = 3;
        animal.homeInterior?.debris.Add(debris2);
      }
    }
  }
}

public static class LightUtils {
  // light source ID
  static string LightSourceIdPrefix = $"${ModEntry.UniqueId}.AnimalLightSourceId_";
  // Animal modData keys
  static string LightSourceIdKey = $"${ModEntry.UniqueId}.AnimalLightSourceId";

  public static void AddLight(FarmAnimal animal, GameLocation location) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
        animalExtensionData.GlowColor is not null) {
      if (!animal.modData.ContainsKey(LightSourceIdKey)) {
        animal.modData[LightSourceIdKey] = LightSourceIdPrefix + animal.myID.Value;
      }
      var color = Utility.StringToColor(animalExtensionData.GlowColor) ?? Color.White;
      if (!location.hasLightSource(animal.modData[LightSourceIdKey])) {
        location.sharedLights.AddLight(new LightSource(
              animal.modData[LightSourceIdKey],
              4,
              //            new Vector2(animal.Position.X + animal.Sprite.getWidth() / 2, animal.Position.Y + animal.Sprite.getHeight() / 2),
              new Vector2(animal.StandingPixel.X, animal.StandingPixel.Y),
              animalExtensionData.GlowRadius,
              new Color(256 - color.R, 256 - color.G, 256 - color.B),
              LightSource.LightContext.None,
              0L));
      }
    }
  }

  public static void UpdateLight(FarmAnimal animal, GameLocation location) {
    if (animal.modData.TryGetValue(LightSourceIdKey, out var lightSourceId)) {
      location.repositionLightSource(lightSourceId,
          new Vector2(animal.StandingPixel.X, animal.StandingPixel.Y));
    }
  }

  public static void RemoveLight(FarmAnimal animal, GameLocation location) {
    if (animal.modData.TryGetValue(LightSourceIdKey, out var lightSourceId) &&
        location.hasLightSource(lightSourceId)) {
      location.removeLightSource(lightSourceId);
    }
  }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Extensions;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.GameData.Objects;
using Microsoft.Xna.Framework;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Aquaponics;

static class FishPondCropManager {
  static ConditionalWeakTable<FishPond, PondHarvester> pondToHarvesterDict = new();
  
  public static string AquaponicsHoeDirt = $"{ModEntry.UniqueId}_IsAquaponics";
  public static string CropsChestName = $"{ModEntry.UniqueId}_CropsChest";
  public static string OutputChestName = $"{ModEntry.UniqueId}_OutputChest";
  public static string AlreadyDoubled = $"{ModEntry.UniqueId}_AlreadyDoubled";

  public static string PotsXCoordKey = $"{ModEntry.UniqueId}_XPotCoords";

  public static Chest? GetOldCropsChestForMigration(FishPond pond) {
    return pond.GetBuildingChest(CropsChestName);
  }

  public static Chest? GetFishPondOutputChest(FishPond pond) {
    return pond.GetBuildingChest(OutputChestName);
  }


  public const int MAX_SEED_COUNT = 20;
  const int MAX_TRIES = 100;

  public static List<IndoorPot>? GetFishPondIndoorPots(FishPond pond, bool addIfNotFound = false) {
    var location = pond.GetParentLocation() ?? Game1.getFarm();
    // No pots, initialize!
    if (!pond.modData.ContainsKey(PotsXCoordKey)) {
      int tries = 0;
      while (tries < MAX_TRIES) {
        int x = Game1.random.Next(int.MinValue, -9999);
        bool free = true;
        for (int y = 0; y < MAX_SEED_COUNT; y++) {
          if (location.objects.ContainsKey(new(x, y))) {
            free = false;
            break;
          }
        }
        if (free) {
          for (int y = 0; y < MAX_SEED_COUNT; y++) {
            var newPot = new IndoorPot(new (x, y));
            newPot.Water();
            newPot.hoeDirt.Value.modData[AquaponicsHoeDirt] = "";
            newPot.hoeDirt.Value.modData["selph.CustomTapperFramework.IsAmphibious"] = "";
            location.objects.Add(new (x, y), newPot);
          }
          pond.modData[PotsXCoordKey] = x.ToString();
          break;
        }
        tries++;
      }
    }

    if (pond.modData.TryGetValue(PotsXCoordKey, out var potsXCoordStr) &&
        Int64.TryParse(potsXCoordStr, out var xCoord)) {
      var result = new List<IndoorPot>();
      for (int y = 0; y < ModEntry.Config.SeedCount; y++) {
        if (location.objects.TryGetValue(new Vector2(xCoord, y), out var obj) &&
            obj is IndoorPot pot) {
          result.Add(pot);
        } else if (addIfNotFound) {
          var newPot = new IndoorPot(new (xCoord, y));
          newPot.Water();
          newPot.hoeDirt.Value.modData[AquaponicsHoeDirt] = "";
          newPot.hoeDirt.Value.modData["selph.CustomTapperFramework.IsAmphibious"] = "";
          location.objects.Add(new (xCoord, y), newPot);
          result.Add(newPot);
        } else {
          ModEntry.StaticMonitor.Log($"Pot not found at {xCoord} {y}. If this is an MP game, it might be because of lag; otherwise if this is filling your logs please report.", LogLevel.Info);
        }
      }
      return result;
    } else {
      ModEntry.StaticMonitor.Log($"ERROR: Pond has no x coords set???", LogLevel.Error);
    }
    return null;
  }

  public static void ClearFishPondIndoorPots(FishPond pond) {
    if (pond.modData.TryGetValue(PotsXCoordKey, out var potsXCoordStr) &&
        Int64.TryParse(potsXCoordStr, out var x)) {
      var location = pond.GetParentLocation() ?? Game1.getFarm();
      var result = new List<IndoorPot>();
      for (int y = 0; y < MAX_SEED_COUNT; y++) {
        location.objects.Remove(new(x, y));
      }
    }
    pond.modData.Remove(PotsXCoordKey);
  }

  // Get one pot if it has crops or bushes
  public static bool TryGetOnePot(FishPond pond, [NotNullWhen(true)] out IndoorPot? pot) {
    var pots = GetFishPondIndoorPots(pond);
    if (pots is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No pots found?", LogLevel.Error);
      pot = null;
      return false;
    }
    if (pots.Count > 0 && pots[0] is IndoorPot p &&
        (p.hoeDirt.Value.crop is not null || p.bush.Value is not null || p.heldObject.Value is not null)) {
      pot = p;
      return true;
    }
    pot = null;
    return false;
  }

  public static List<Item> RemoveAllCrops(FishPond pond) {
    var saplings = new List<Item>();
    var pots = GetFishPondIndoorPots(pond);
    if (pots is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No pots found?", LogLevel.Error);
      return saplings;
    }
    string? bushItemId = null;
    int bushCount = 0;
    foreach (var pot in pots) {
      if (pot.bush.Value is not null) {
        if (bushItemId is not null &&
            (ModEntry.cbApi?.TryGetBush(pot.bush.Value, out var _, out var id) ?? false)) {
          bushItemId = id;
        } else {
          bushItemId ??= "(O)251";
        }
        bushCount++;
        pot.bush.Value = null;
      }
      pot.hoeDirt.Value.crop = null;
    }
    if (bushItemId is not null) {
      saplings.Add(ItemRegistry.Create(bushItemId, bushCount));
    }
    return saplings;
  }

  public static bool PlantCrops(FishPond pond, SObject seed, Farmer who, bool showMessage = false) {
    // Only allow seeds
    if (seed.Category != -74) return false;
    if (TryGetOnePot(pond, out var _)) {
      if (showMessage) {
        Game1.showRedMessage(ModEntry.Helper.Translation.Get("Plant.alreadyPlanted"));
      }
      return false;
    }
    if (seed.Stack < ModEntry.Config.SeedCount) {
      if (showMessage) {
        Game1.showRedMessage(ModEntry.Helper.Translation.Get("Plant.notEnoughSeeds", new {num = ModEntry.Config.SeedCount.ToString()}));
      }
      return false;
    }
    // Ban mixed seeds, including modded ones from IE
    if (seed.QualifiedItemId == "(O)770" || seed.QualifiedItemId == "(O)MixedFlowerSeeds" ||
        ModEntry.ieApi?.GetCustomSeeds(seed.ItemId, true, true) is not null ||
        (ModEntry.ieApi is not null &&
         ItemRegistry.GetDataOrErrorItem(seed.QualifiedItemId).RawData is ObjectData objectData &&
         (objectData.CustomFields?.ContainsKey("mistyspring.ItemExtensions/MixedSeeds") ?? false)
        )) {
      if (showMessage) {
        Game1.showRedMessage(ModEntry.Helper.Translation.Get("Plant.noWildSeeds"));
      }
      return false;
    }
    var pots = GetFishPondIndoorPots(pond, true);
    if (pots is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No pots found?", LogLevel.Error);
      return false;
    }
    var location = pond.GetParentLocation();
    int plantedCount = 0;
    foreach (var pot in pots) {
      if (!pot.performObjectDropInAction(seed, false, who)) {
        if (plantedCount > 0) {
          ModEntry.StaticMonitor.Log($"IMPOSSIBLE: Only some pots were planted (seed ID {seed.QualifiedItemId})?", LogLevel.Error);
        }
        return false;
      }
      plantedCount++;
    }
	  location.playSound("dropItemInWater");
    seed.Stack -= ModEntry.Config.SeedCount;
    ModEntry.Helper.Multiplayer.SendMessage(pond.id.Value.ToString(), $"{ModEntry.UniqueId}_SyncCrops", modIDs: new[] { ModEntry.UniqueId });
    return true;
  }

  // harvest crops and add output to the chest
  public static bool HarvestCrops(FishPond pond, Farmer? who, out int farmingExp, out int foragingExp) {
    farmingExp = 0;
    foragingExp = 0;
    who ??= Game1.player;
    var pots = GetFishPondIndoorPots(pond);
    var outputChest = GetFishPondOutputChest(pond);
    if (pots is null || outputChest is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No chest found?", LogLevel.Error);
      return false;
    }
    // this is only for the crop quality rng
    foreach (var pot in pots) {
      if (pot.heldObject.Value is not null) {
        var forageHarvest = pot.heldObject.Value;
        forageHarvest.Quality = pot.Location.GetHarvestSpawnedObjectQuality(who, true, new(pond.tileX.Value, pond.tileY.Value));
        outputChest.addItem(forageHarvest);
        pot.heldObject.Value = null;
        //pot.Location.OnHarvestedForage(who, forageHarvest);
  	    foragingExp += 2;
        farmingExp += 3;
      } else if (pot.bush.Value is not null &&
          pot.bush.Value.readyForHarvest()) {
        var harvestItem =
          (ModEntry.cbApi?.TryGetShakeOffItem(pot.bush.Value, out var bushHarvest) ?? false) ? bushHarvest : ItemRegistry.Create("(O)815");
        pot.bush.Value.tileSheetOffset.Value = 0;
        pot.bush.Value.setUpSourceRect();
        if (Game1.random.NextBool(0.5)) {
          harvestItem.Stack *= 2;
          outputChest.addItem(harvestItem);
        }
      } else if (pot.hoeDirt.Value.readyForHarvest()) {
        var harvester = GetHarvesterFor(pond);
        if (pot.hoeDirt.Value.crop.harvest(
            (int)pot.TileLocation.X, (int)pot.TileLocation.Y,
            pot.hoeDirt.Value, GetHarvesterFor(pond))) {
          pot.hoeDirt.Value.destroyCrop(false);
        }
        farmingExp += harvester.farmingExp;
      }
    }
    if (outputChest.isEmpty()) {
      return false;
    }
    Utility.consolidateStacks(outputChest.Items);
    outputChest.Items.RemoveEmptySlots();
    return true;
  }

  public static void AddOutputChestToInventory(FishPond pond, Farmer who) {
    var outputChest = GetFishPondOutputChest(pond);
    if (outputChest is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No chest found?", LogLevel.Error);
      return;
    }
    Utility.CollectSingleItemOrShowChestMenu(outputChest);
    who.currentLocation.playSound("leafrustle");
    return;
  }

  public static PondHarvester GetHarvesterFor(FishPond pond) {
    if (pondToHarvesterDict.TryGetValue(pond, out var harvester)) {
      return harvester;
    } else {
      var newHarvester = new PondHarvester(pond);
      pondToHarvesterDict.Add(pond, newHarvester);
      return newHarvester;
    }
  }

  public static void DayStarted(FishPond pond) {
    var pots = GetFishPondIndoorPots(pond);
    var location = pond.GetParentLocation();
    if (pots is null || pots.Count == 0) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No chest found?", LogLevel.Error);
      return;
    }
    bool hasCrops = (pots.Count > 0 && pots[0] is IndoorPot p &&
        (p.hoeDirt.Value.crop is not null || p.bush.Value is not null || p.heldObject.Value is not null));
    // Fish spawns 25% faster
    if (hasCrops && Game1.random.NextBool(0.25)) {
      pond.daysSinceSpawn.Value += 1;
    }
    // Output has 50% chance to be doubled
    if (hasCrops &&
        pond.output.Value is not null &&
        !pond.output.Value.modData.ContainsKey(AlreadyDoubled) &&
        Game1.random.NextBool(0.50)) {
      pond.output.Value.Stack = (int)(pond.output.Value.Stack * (pond.goldenAnimalCracker.Value ? 1.5 : 2));
      pond.output.Value.modData[AlreadyDoubled] = "";
    }
    bool shouldSpeedUpBushes = hasCrops && Game1.random.NextBool(0.25);
    // Water pots automatically and speed up bushes growth by 25%
    foreach (var pot in pots) {
      pot.Water();
      // Destroy dead crops automatically
      if (pot.hoeDirt.Value.crop?.dead.Value ?? false) {
          pot.hoeDirt.Value.destroyCrop(false);
      }
      if (shouldSpeedUpBushes && pot.bush.Value is not null &&
          pot.bush.Value.datePlanted.Value > -999) {
        pot.bush.Value.datePlanted.Value -= 1;
      }
    }
  }

  public static void SaveLoaded() {
    Utility.ForEachBuilding(building => {
      if (ModEntry.IsAquaponicsPond(building, out var pond) &&
          !pond.modData.ContainsKey(PotsXCoordKey)) {
        var location = pond.GetParentLocation() ?? Game1.getFarm();
        var chest = FishPondCropManager.GetOldCropsChestForMigration(pond);
        if (chest is null) {
          ModEntry.StaticMonitor.Log($"No old crops chest found? This is harmless, probably.", LogLevel.Warn);
          return true;
        }
        // migrate only if we have enough pots to fill the slots, otherwise just clear and return
        if (chest.Items.Count < ModEntry.Config.SeedCount) {
          chest.Items.Clear();
          return true;
        }

        int tries = 0;
        int? x = null;
        while (tries < MAX_TRIES) {
          x = Game1.random.Next(int.MinValue, -9999);
          bool free = true;
          for (int y = 0; y < ModEntry.Config.SeedCount; y++) {
            if (location.objects.ContainsKey(new(x.Value, y))) {
              free = false;
              break;
            }
          }
          if (free) {
            pond.modData[PotsXCoordKey] = x.ToString();
            break;
          }
          x = null;
          tries++;
        }
        if (x is not null) {
          int y = 0;
          foreach (var item in chest.Items) {
            if (item is IndoorPot oldPot) {
              location.objects.Add(new(x.Value, y), oldPot);
            }
            y++;
          }
        }
        chest.Items.Clear();
      }
      return true;
    });
  }
}

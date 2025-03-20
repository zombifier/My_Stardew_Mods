using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.GameData.Objects;
using Microsoft.Xna.Framework;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Aquaponics;

static class FishPondCropManager {
  static Dictionary<Guid, PondHarvester> pondToHarvesterDict = new();
  
  public static string AquaponicsHoeDirt = $"{ModEntry.UniqueId}_IsAquaponics";
  public static string CropsChestName = $"{ModEntry.UniqueId}_CropsChest";
  public static string OutputChestName = $"{ModEntry.UniqueId}_OutputChest";

  public static Chest? GetFishPondOutputChest(FishPond pond) {
    return pond.GetBuildingChest(OutputChestName);
  }

  public static Chest? GetFishPondCropsChest(FishPond pond) {
    return pond.GetBuildingChest(CropsChestName);
  }

  const int MAX_TRIES = 10;

  // In the unlikely, unlikely event the shadow tile we wanted was taken, pick a new tile
  static Vector2? GetUnoccupiedDummyTile(GameLocation location) {
    for (int i = 0; i < MAX_TRIES; i++) {
      var coord = new Vector2(Game1.random.Next(-99999, -9999), 0);
      if (!location.objects.ContainsKey(coord)) {
        return coord;
      }
    }
    ModEntry.StaticMonitor.Log("ERROR: Unable to find tile to place shadow pot for forage crops", LogLevel.Error);
    return null;
  }

  // Get one pot if it has crops or bushes
  public static bool TryGetOnePot(FishPond pond, [NotNullWhen(true)] out IndoorPot? pot) {
    var chest = GetFishPondCropsChest(pond);
    if (chest is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No chest found?", LogLevel.Error);
      pot = null;
      return false;
    }
    if (chest.Items.Count > 0 && chest.Items[0] is IndoorPot p &&
        (p.hoeDirt.Value.crop is not null || p.bush.Value is not null || p.heldObject.Value is not null)) {
      pot = p;
      return true;
    }
    pot = null;
    return false;
  }

  public static List<Item> RemoveAllCrops(FishPond pond) {
    var saplings = new List<Item>();
    var chest = GetFishPondCropsChest(pond);
    if (chest is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No chest found?", LogLevel.Error);
      return saplings;
    }
    string? bushItemId = null;
    int bushCount = 0;
    foreach (var item in chest.Items) {
      if (item is IndoorPot pot && pot.bush.Value is not null) {
        if (bushItemId is not null &&
            (ModEntry.cbApi?.TryGetBush(pot.bush.Value, out var _, out var id) ?? false)) {
          bushItemId = id;
        } else {
          bushItemId ??= "(O)251";
        }
        bushCount++;
        pot.bush.Value = null;
      }
    }
    chest.Items.Clear();
    if (bushItemId is not null) {
      saplings.Add(ItemRegistry.Create(bushItemId, bushCount));
    }
    return saplings;
  }

  public static bool PlantCrops(FishPond pond, SObject seed, Farmer who, bool showMessage = false) {
    // Only allow seeds
    // Ban all bushes for now since their day update logic does not work
    // Well, tea bushes work, but banning all bushes just for consistency
    if (seed.Category != -74 || seed.IsTeaSapling()) return false;
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
    var chest = GetFishPondCropsChest(pond);
    if (chest is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No chest found?", LogLevel.Error);
      return false;
    }
    var location = pond.GetParentLocation();
    chest.Items.Clear();
    // Initialize if chest empty
    //if (chest.Items.Count < ModEntry.Config.SeedCount) {
    //  for (int i = chest.Items.Count; i < ModEntry.Config.SeedCount; i++) {
    for (int i = 0; i < ModEntry.Config.SeedCount; i++) {
      var tile = new Vector2(-9999-pond.tileX.Value-i-1, 0);
      var newPot = new IndoorPot(tile);
      // Just in case so they don't stack, but this shouldn't happen
      newPot.Name += "_" + i;
      // We do this again because otherwise the hoe dirt's crops won't be set,
      // and its draw logic doesn't work without it set (updateDrawMath does not run if tile is 0,0)
      // Usually, a real pot will have its tile set by GameLocation.OnObjectAdded
      newPot.TileLocation = tile;
      newPot.Location = location;
      newPot.Water();
      newPot.hoeDirt.Value.modData[AquaponicsHoeDirt] = "";
      newPot.hoeDirt.Value.modData["selph.CustomTapperFramework.IsAmphibious"] = "";
      chest.Items.Add(newPot);
    }
    //}
    //chest.Items.RemoveEmptySlots();
    int plantedCount = 0;
    foreach (var item in chest.Items) {
      if (item is IndoorPot pot) {
        if (!pot.performObjectDropInAction(seed, false, who)) {
          if (plantedCount > 0) {
            ModEntry.StaticMonitor.Log($"IMPOSSIBLE: Only some pots were planted (seed ID {seed.QualifiedItemId})?", LogLevel.Error);
          }
          return false;
        }
        plantedCount++;
      } else {
        ModEntry.StaticMonitor.Log("IMPOSSIBLE (PLANT CROPS): Non-pots in fish pond input?", LogLevel.Error);
      }
    }
	  location.playSound("dropItemInWater");
    seed.Stack -= ModEntry.Config.SeedCount;
    return true;
  }

  // harvest crops and add output to the chest
  public static bool HarvestCrops(FishPond pond, Farmer? who, out int farmingExp, out int foragingExp) {
    farmingExp = 0;
    foragingExp = 0;
    who ??= Game1.player;
    var cropsChest = GetFishPondCropsChest(pond);
    var outputChest = GetFishPondOutputChest(pond);
    if (cropsChest is null || outputChest is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No chest found?", LogLevel.Error);
      return false;
    }
    // this is only for the crop quality rng
    int i = 0;
    foreach (var item in cropsChest.Items) {
      if (item is IndoorPot pot) {
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
              pond.tileX.Value + i, pond.tileY.Value + i,
              pot.hoeDirt.Value, GetHarvesterFor(pond))) {
            pot.hoeDirt.Value.destroyCrop(false);
          }
          farmingExp += harvester.farmingExp;
        } else {
          return false;
        }
      } else {
        ModEntry.StaticMonitor.Log("IMPOSSIBLE (HARVEST): Non-pots in fish pond input?", LogLevel.Error);
      }
      i++;
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
    var id = pond.id.Value;
    if (!pondToHarvesterDict.ContainsKey(id)) {
      pondToHarvesterDict[id] = new PondHarvester(pond);
    }
    return pondToHarvesterDict[id];
  }

  public static void DayUpdateHoeDirt(FishPond pond) {
    var cropsChest = GetFishPondCropsChest(pond);
    var location = pond.GetParentLocation();
    if (cropsChest is null) {
      ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No chest found?", LogLevel.Error);
      return;
    }
    bool shouldSpeedUpBushes = Game1.random.NextBool(0.25);
    foreach (var item in cropsChest.Items) {
      if (item is IndoorPot pot) {
        bool isForageCrop =
          pot.hoeDirt.Value.crop is not null &&
          (pot.hoeDirt.Value.crop.isWildSeedCrop() || pot.hoeDirt.Value.crop.replaceWithObjectOnFullGrown is not null);
        // Forage crops work by *actually* finding the pot from Game1.objects using its tile coords and put it in heldObject.
        // In order for it to work we need to temporarily place the pot in the world and remove it afterwards.
        if (isForageCrop) {
          if (pot.Location.objects.ContainsKey(pot.TileLocation)) {
            pot.TileLocation = GetUnoccupiedDummyTile(pot.Location) ?? pot.TileLocation;
          }
          pot.Location.objects[pot.TileLocation] = pot;
        }
        pot.DayUpdate();
        pot.Water();
        if (isForageCrop) {
          pot.Location.objects.Remove(pot.TileLocation);
        }
        // Destroy crop on season start automatically
        if (pot.hoeDirt.Value.crop is not null) {
          var crop = pot.hoeDirt.Value.crop;
          if (crop.dead.Value) {
            pot.hoeDirt.Value.destroyCrop(false);
          } else {
            crop.sourceRect = crop.getSourceRect(0);
          }
        }
        if (shouldSpeedUpBushes && pot.bush.Value is not null &&
            pot.bush.Value.datePlanted.Value > -999) {
          pot.bush.Value.datePlanted.Value -= 1;
        }
      } else {
        ModEntry.StaticMonitor.Log("IMPOSSIBLE (UPDATE): Non-pots in fish pond input?", LogLevel.Error);
      }
    }
  }

  public static void SaveLoaded() {
    Utility.ForEachBuilding(building => {
      if (ModEntry.IsAquaponicsPond(building, out var pond)) {
        var location = pond.GetParentLocation() ?? Game1.getFarm();
        var chest = GetFishPondCropsChest(pond);
        if (chest is null) {
          ModEntry.StaticMonitor.Log($"IMPOSSIBLE: No chest found?", LogLevel.Error);
          return true;
        }
        int i = 0;
        foreach (var item in chest.Items) {
          if (item is IndoorPot pot) {
            pot.Location = location;
            var tile = new Vector2(-9999-pond.tileX.Value-i-1, 0);
            pot.TileLocation = tile; 
            if (pot.bush.Value is not null) {
              pot.bush.Value.setUpSourceRect();
            }
            i++;
          }
        }
      }
      return true;
    });
  }
}

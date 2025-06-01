using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Aquaponics;

public interface IAquaponicsApi {
  // Whether this hoe dirt is one inside the aquaponics pond.
  public bool IsAquaponicsHoeDirt(HoeDirt hoeDirt);
  // Get the chest containing the output of this aquaponics pond.
  public Chest? GetFishPondOutputChest(FishPond pond);
  // Get the chest containing a list of indoor pot items that hold this pond's crops.
  [Obsolete("This mod has moved away from a chest of pots")]
  public Chest? GetFishPondCropsChest(FishPond pond);
  // Get a list of all indoor pots associated with this fish pond.
  public List<IndoorPot>? GetFishPondIndoorPots(FishPond pond);
  // Try to plant the seed. If planted successfully, will subtract "Config.SeedCount" from the seed item's stack count.
  public bool PlantCrops(FishPond pond, SObject seed, Farmer who, bool showMessage = false);
  // Trigger harvest from this pond if possible, and deposits the items into the building's output chest.
  // It is up the caller to collect the items from the output chest afterwards.
  // Also returns the amount of farming and foraging exp calculated (follows the game formula). it is also up to the caller to award it to the farmer.
  public bool HarvestCrops(FishPond pond, Farmer? who, out int farmingExp, out int foragingExp);
  // Remove all crops/bushes from this aquaponics pond. Returns a list of bush items if bushes were removed.
  public List<Item> RemoveAllCrops(FishPond pond);
}

public class AquaponicsApi : IAquaponicsApi {
  public bool IsAquaponicsHoeDirt(HoeDirt hoeDirt) {
    return hoeDirt.modData.ContainsKey(FishPondCropManager.AquaponicsHoeDirt);
  }

  public Chest? GetFishPondOutputChest(FishPond pond) {
    return FishPondCropManager.GetFishPondOutputChest(pond);
  }

  [Obsolete("This mod has moved away from a chest of pots")]
  public Chest? GetFishPondCropsChest(FishPond pond) {
    return FishPondCropManager.GetOldCropsChestForMigration(pond);
  }

  public List<IndoorPot>? GetFishPondIndoorPots(FishPond pond) {
    return FishPondCropManager.GetFishPondIndoorPots(pond);
  }

  public bool PlantCrops(FishPond pond, SObject seed, Farmer who, bool showMessage = false) {
    return FishPondCropManager.PlantCrops(pond, seed, who, showMessage);
  }

  public bool HarvestCrops(FishPond pond, Farmer? who, out int farmingExp, out int foragingExp) {
    return FishPondCropManager.HarvestCrops(pond, who, out farmingExp, out foragingExp);
  }

  public List<Item> RemoveAllCrops(FishPond pond) {
    return FishPondCropManager.RemoveAllCrops(pond);
  }
}

using System;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Buildings;
using StardewModdingAPI;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Aquaponics;

class PondHarvester : JunimoHarvester {
  FishPond pond;
  public int farmingExp;
  public PondHarvester(FishPond pond) {
    this.pond = pond;
    // these fields are required 
    this.currentLocation = pond.GetParentLocation();
    this.position.Value = new(-1, -1);
  }

  public override void tryToAddItemToHut(Item i) {
    ModEntry.StaticMonitor.Log($"Harvesting {i.QualifiedItemId}", LogLevel.Info);
    FishPondCropManager.GetFishPondOutputChest(this.pond)?.Items.Add(i);
    int price = 0;
    if (i is SObject obj) {
      price = obj.Price;
    }
    float experience = (float)(16.0 * Math.Log(0.018 * (double)price + 1.0, Math.E));
    this.farmingExp = (int)experience;
  }
}

using System;
using StardewValley;
using StardewValley.Characters;

namespace Selph.StardewMods.MachineTerrainFramework;

public interface IMachineTerrainFrameworkApi {
  // Events that will fire when crop is harvested.
  // IMPORTANT NOTES:
  // * Does not fire for forage crops, only "regular" crops. This only applies for spring onions and gingers;
  //   seasonal seeds are already transformed into objects on day start.
  // * Does not fire for sunflower seeds/hay from wheat/extra SC drops.
  // * For crops that drop multiple stacks like potatoes or cranberries, this event will be called for every
  //   drops. You should take the `isExtraDrops` field into account.
  public event Action<ICropHarvestedEvent>? CropHarvested;
}

public interface ICropHarvestedEvent {
  public Crop crop { get; }
  // You can modify these fields to change what gets returned.
  public Item produce { get; set; }
  public int count { get; set; }
  // Whether this event is for the extra drops (e.g. extra potatoes) from count > 1 that don't benefit from quality
  // instead of the main drop. If you're modifying count, it's highly recommended you only do it when isExtraDrops is false,
  // lest you risk infinite recursion.
  public bool isExtraDrops { get; }
  public JunimoHarvester? junimo { get; }
  public bool isForcedScytheHarvest { get; }
}

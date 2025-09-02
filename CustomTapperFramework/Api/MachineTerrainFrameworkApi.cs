using System;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace Selph.StardewMods.MachineTerrainFramework;

public class MachineTerrainFrameworkApi : IMachineTerrainFrameworkApi {
  public event Action<ICropHarvestedEvent>? CropHarvested;

  internal void RunCropHarvestedEvents(Crop crop, ref Item produce, ref int count, bool isExtraDrops, JunimoHarvester? junimo, bool isForcedScytheHarvest) {
    if (CropHarvested is null) return;
    var ev = new CropHarvestedEvent(crop, produce, count, isExtraDrops, junimo, isForcedScytheHarvest);
    foreach (Action<ICropHarvestedEvent> del in CropHarvested.GetInvocationList()) {
      try {
        del.Invoke(ev);
      }
      catch (Exception e) {
        ModEntry.StaticMonitor.Log("Error processing CropHarvestedEvent: " + e.ToString(), LogLevel.Error);
      }
    }
    produce = ev.produce ?? produce;
    count = Math.Max(1, ev.count);
    return;
  }
}

public class CropHarvestedEvent : ICropHarvestedEvent {
  public Crop crop { get; }
  public Item produce { get; set; }
  public int count { get; set; }
  public bool isExtraDrops { get; }
  public JunimoHarvester? junimo { get; }
  public bool isForcedScytheHarvest { get; }

  internal CropHarvestedEvent(Crop crop, Item produce, int count, bool isExtraDrops, JunimoHarvester? junimo, bool isForcedScytheHarvest) {
    this.crop = crop;
    this.produce = produce;
    this.count = count;
    this.isExtraDrops = isExtraDrops;
    this.junimo = junimo;
    this.isForcedScytheHarvest = isForcedScytheHarvest;
  }
}

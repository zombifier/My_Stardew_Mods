using System.Collections;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.GameData.BigCraftables;
using Microsoft.Xna.Framework.Graphics;

using SObject = StardewValley.Object;

namespace ExtraMachineConfig;

public class ExtraOutputAssetHandler {
  private string dataPath = "selph.ExtraMachineConfig/ExtraOutputs";
  public Dictionary<string, MachineItemOutput> data {get; private set;}

  public void OnAssetRequested(object sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath)) {
      var dict = new Dictionary<string, MachineItemOutput>();
      e.LoadFrom(() => dict, AssetLoadPriority.Low);
    }
  }

  public void OnAssetReady(object sender, AssetReadyEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath)) {
      this.data = Game1.content.Load<Dictionary<string, MachineItemOutput>>(this.dataPath);
      ModEntry.StaticMonitor.Log("Loaded extra machine output data with " + data.Count + " entries.", LogLevel.Info);
    }
  }

  public void OnGameLaunched(object sender, GameLaunchedEventArgs e) {
    this.data = Game1.content.Load<Dictionary<string, MachineItemOutput>>(this.dataPath);
    ModEntry.StaticMonitor.Log("Loaded extra machine output data with " + data.Count + " entries.", LogLevel.Info);
  }

  public void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e) {
    foreach (var name in e.NamesWithoutLocale) {
      if (name.IsEquivalentTo(this.dataPath)) {
        this.data = Game1.content.Load<Dictionary<string, MachineItemOutput>>(this.dataPath);
        ModEntry.StaticMonitor.Log("Reloaded extra machine output data with " + data.Count + " entries.", LogLevel.Info);
      }
    }
  }

}

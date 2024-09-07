using System.Collections;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Common;

public abstract class DictAssetHandler<AssetType> {
  private string dataPath;
  private IMonitor monitor;

  private Dictionary<string, AssetType>? privateData = null;
  public Dictionary<string, AssetType> data {
    get {
      if (privateData == null) {
        privateData = Game1.content.Load<Dictionary<string, AssetType>>(this.dataPath);
        monitor.Log($"Loaded asset {dataPath} with {data.Count} entries.");
      }
      return privateData!;
    }
  }

  public DictAssetHandler(string dataPath, IMonitor monitor) {
    this.dataPath = dataPath;
    this.monitor = monitor;
  }

  public void RegisterEvents(IModHelper helper) {
    helper.Events.Content.AssetRequested += this.OnAssetRequested;
    helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;
  }

  public void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath)) {
      var dict = new Dictionary<string, AssetType>();
      e.LoadFrom(() => dict, AssetLoadPriority.Low);
    }
  }

  public void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e) {
    foreach (var name in e.NamesWithoutLocale) {
      if (name.IsEquivalentTo(this.dataPath)) {
        monitor.Log($"Asset {dataPath} invalidated, reloading.");
        this.privateData = null;
      }
    }
  }
}

using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Pathoschild.Stardew.Automate;

using SObject = StardewValley.Object;

namespace CustomTapperFramework;

public class AssetHandler {
  private string dataPath;
  public Dictionary<string, TapperModel> data {get; private set;}

  public AssetHandler() {
    // "selph.CustomTapperFramework/Data"
    dataPath = $"{ModEntry.UniqueId}/Data";
  }

  public void OnAssetRequested(object sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath)) {
      e.LoadFrom(() => new Dictionary<string, TapperModel>(), AssetLoadPriority.Low);
    }
  }

  public void OnAssetReady(object sender, AssetReadyEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath)) {
      this.data = Game1.content.Load<Dictionary<string, TapperModel>>(this.dataPath);
      ModEntry.StaticMonitor.Log("Loaded custom tapper data with " + data.Count + " entries.", LogLevel.Info);
    }
  }

  public void OnGameLaunched(object sender, GameLaunchedEventArgs e) {
    this.data = Game1.content.Load<Dictionary<string, TapperModel>>(this.dataPath);
    ModEntry.StaticMonitor.Log("Loaded custom tapper data with " + data.Count + " entries.", LogLevel.Info);
    IAutomateAPI automate = ModEntry.Helper.ModRegistry.GetApi<IAutomateAPI>("Pathoschild.Automate");
    if (automate != null) {
      automate.AddFactory(new ResourceClumpConnectorFactory());
    }
  }

  public void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e) {
    foreach (var name in e.NamesWithoutLocale) {
      if (name.IsEquivalentTo(this.dataPath)) {
        this.data = Game1.content.Load<Dictionary<string, TapperModel>>(this.dataPath);
      }
    }
  }

}

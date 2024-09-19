using System;
using System.Linq;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace Selph.StardewMods.CoopFeed;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; }
  internal static IMonitor StaticMonitor { get; set; }

  private ModConfig Config;

  bool IsDirtForager(string animal) {
    return Config.AllDirtForagers ||
    Array.IndexOf([
      "Rabbit",
      "Domestic Silkmoth",
      "Oak Silkmoth",
      "Luna Silkmoth",
      "Tropical Silkmoth",
    ], animal) == -1;
  }

  public override void Entry(IModHelper helper) {
    this.Config = helper.ReadConfig<ModConfig>();
    Helper = helper;
    StaticMonitor = this.Monitor;

    helper.Events.Content.AssetRequested += OnAssetRequested;
    helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
  }

  public void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (!Config.DirtForagers) return;
    //if (e.NameWithoutLocale.IsEquivalentTo("Data/FarmAnimals")) {
    //  e.Edit(asset => {
    //      var farmAnimals = asset.AsDictionary<string, FarmAnimalData>();
    //      foreach (var pair in farmAnimals.Data) {
    //        if (IsDirtForager(pair.Key) && pair.Value.House == "Coop") {
    //          pair.Value.GrassEatAmount = 0;
    //        }
    //      }
    //  }, AssetEditPriority.Late);
    //}

    if (e.NameWithoutLocale.IsEquivalentTo("selph.ExtraAnimalConfig/AnimalExtensionData")) {
      e.Edit(asset => {
          var farmAnimalExtensionData = asset.AsDictionary<string, ExtraAnimalConfig.AnimalExtensionData>();
          foreach (var pair in DataLoader.FarmAnimals(Game1.content) ?? new Dictionary<string, FarmAnimalData>()) {
            if (pair.Value.House == "Coop" || pair.Value.House == "mytigio.dwarven_expansion_CaveCoop") {
              if (!farmAnimalExtensionData.Data.ContainsKey(pair.Key)) {
                farmAnimalExtensionData.Data[pair.Key] = new();
              }
              farmAnimalExtensionData.Data[pair.Key].FeedItemId = "(O)selph.CoopFeed.ChickenFeed";
              if (IsDirtForager(pair.Key)) {
                farmAnimalExtensionData.Data[pair.Key].OutsideForager = true;
              }
            }
          }
      }, AssetEditPriority.Late);
    }
  }

  public void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e) {
    if (!Config.DirtForagers) return;
    if (e.NamesWithoutLocale.Any(name => name.Name == "Data/FarmAnimals")) {
      Helper.GameContent.InvalidateCache("selph.ExtraAnimalConfig/AnimalExtensionData");
    }
  }

  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    // get Generic Mod Config Menu's API (if it's installed)
    var configMenu = Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
    if (configMenu is null)
      return;

    // register mod
    configMenu.Register(
        mod: this.ModManifest,
        reset: () => this.Config = new ModConfig(),
        save: () => {
          Helper.WriteConfig(this.Config);
          Helper.GameContent.InvalidateCache("selph.ExtraAnimalConfig/AnimalExtensionData");
        });

    // add some config options
    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("config.DirtForagers.name"),
        tooltip: () => Helper.Translation.Get("config.DirtForagers.description"),
        getValue: () => this.Config.DirtForagers,
        setValue: value => this.Config.DirtForagers = value
        );

    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("config.AllDirtForagers.name"),
        tooltip: () => Helper.Translation.Get("config.AllDirtForagers.description"),
        getValue: () => this.Config.AllDirtForagers,
        setValue: value => this.Config.AllDirtForagers = value
        );
  }

}

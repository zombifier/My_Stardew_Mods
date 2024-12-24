using System;
using System.Linq;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace Selph.StardewMods.ImmersiveManure;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; }
  internal static IMonitor StaticMonitor { get; set; }
  internal static string UniqueId;

  private ModConfig Config;

  //private ModConfig Config;

  public override void Entry(IModHelper helper) {
    this.Config = helper.ReadConfig<ModConfig>();
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    helper.Events.Content.AssetRequested += OnAssetRequested;
    helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
  }

  public void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo("selph.ExtraAnimalConfig/AnimalExtensionData")) {
      e.Edit(asset => {
          var farmAnimalExtensionData = asset.AsDictionary<string, ExtraAnimalConfig.AnimalExtensionData>();
          foreach (var pair in DataLoader.FarmAnimals(Game1.content) ?? new Dictionary<string, FarmAnimalData>()) {
            if (!farmAnimalExtensionData.Data.ContainsKey(pair.Key)) {
              farmAnimalExtensionData.Data[pair.Key] = new();
            }
            if (farmAnimalExtensionData.Data[pair.Key].ExtraProduceSpawnList is null) {
              farmAnimalExtensionData.Data[pair.Key].ExtraProduceSpawnList = new();
            }
            if (farmAnimalExtensionData.Data[pair.Key].AnimalProduceExtensionData is null) {
              farmAnimalExtensionData.Data[pair.Key].AnimalProduceExtensionData = new();
            }
            if (pair.Value.House == "Coop") {
              farmAnimalExtensionData.Data[pair.Key].ExtraProduceSpawnList!.Add(new ExtraAnimalConfig.ExtraProduceSpawnData {
                Id = $"{ModEntry.UniqueId}.Manure",
                ProduceItemIds = new() {
                  new ExtraAnimalConfig.ProduceData() {
                    Id = $"{ModEntry.UniqueId}.GoldenManure",
                    ItemId = "selph.ImmersiveManure.GoldenPoultryManure",
                    Condition = "RANDOM 0.001 @addDailyLuck, ITEM_ID Input GoldenAnimalCracker",
                    MinimumFriendship = 800,
                  },
                  new ExtraAnimalConfig.ProduceData() {
                    Id = $"{ModEntry.UniqueId}.Manure",
                    ItemId = "selph.ImmersiveManure.PoultryManure",
                    Condition = $"RANDOM {Config.DropChance}",
                  }
                },
              });
              farmAnimalExtensionData.Data[pair.Key].AnimalProduceExtensionData["(O)selph.ImmersiveManure.PoultryManure"]
                = new ExtraAnimalConfig.AnimalProduceExtensionData() {
                  HarvestTool = Config.EvenMoreImmersiveManure ? "DropOvernight" : "Debris",
                };
              farmAnimalExtensionData.Data[pair.Key].AnimalProduceExtensionData["(O)selph.ImmersiveManure.GoldenPoultryManure"]
                = new ExtraAnimalConfig.AnimalProduceExtensionData() {
                  HarvestTool = Config.EvenMoreImmersiveManure ? "DropOvernight" : "Debris",
                };
            } else {
              // Defaults to livestock manure 
              farmAnimalExtensionData.Data[pair.Key].ExtraProduceSpawnList!.Add(new ExtraAnimalConfig.ExtraProduceSpawnData {
                Id = $"{ModEntry.UniqueId}.Manure",
                ProduceItemIds = new() {
                  new ExtraAnimalConfig.ProduceData() {
                    Id = $"{ModEntry.UniqueId}.GoldenManure",
                    ItemId = "selph.ImmersiveManure.GoldenLivestockManure",
                    Condition = "RANDOM 0.001 @addDailyLuck, ITEM_ID Input GoldenAnimalCracker",
                    MinimumFriendship = 800,
                  },
                  new ExtraAnimalConfig.ProduceData() {
                    Id = $"{ModEntry.UniqueId}.Manure",
                    ItemId = "selph.ImmersiveManure.LivestockManure",
                    Condition = $"RANDOM {Config.DropChance}",
                  }
                },
              });
              farmAnimalExtensionData.Data[pair.Key].AnimalProduceExtensionData["(O)selph.ImmersiveManure.LivestockManure"]
                = new ExtraAnimalConfig.AnimalProduceExtensionData() {
                  HarvestTool = Config.EvenMoreImmersiveManure ? "DropOvernight" : "Debris",
                };
              farmAnimalExtensionData.Data[pair.Key].AnimalProduceExtensionData["(O)selph.ImmersiveManure.GoldenLivestockManure"]
                = new ExtraAnimalConfig.AnimalProduceExtensionData() {
                  HarvestTool = Config.EvenMoreImmersiveManure ? "DropOvernight" : "Debris",
                };
            }
          }
      }, AssetEditPriority.Late);
    }
  }

  public void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e) {
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
        name: () => Helper.Translation.Get("config.EvenMoreImmersiveManure.name"),
        tooltip: () => Helper.Translation.Get("config.EvenMoreImmersiveManure.description"),
        getValue: () => this.Config.EvenMoreImmersiveManure,
        setValue: value => this.Config.EvenMoreImmersiveManure = value
        );

    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("config.DropChance.name"),
        tooltip: () => Helper.Translation.Get("config.DropChance.description"),
        getValue: () => this.Config.DropChance,
        setValue: value => this.Config.DropChance = value
        );
  }
}

using StardewModdingAPI;
using StardewModdingAPI.Events;
using HarmonyLib;
using System.Collections.Generic;
using StardewValley;
using Microsoft.Xna.Framework;
using xTile.Dimensions;
using VanillaPlusProfessions.Compatibility;

namespace Selph.StardewMods.ExtraAnimalConfig;

internal sealed class ModEntry : Mod {
  internal new static IModHelper Helper {
    get;
    set;
  } = null!;

  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static AnimalExtensionDataAssetHandler animalExtensionDataAssetHandler = null!;
  internal static EggExtensionDataAssetHandler eggExtensionDataAssetHandler = null!;
  internal static GrassDropExtensionDataAssetHandler grassDropExtensionDataAssetHandler = null!;
  internal static string UniqueId = null!;
  internal static ExtraAnimalConfigApi ModApi = null!;

  internal static IVanillaPlusProfessions? vppApi;

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;
    ModApi = new ExtraAnimalConfigApi();

    animalExtensionDataAssetHandler = new AnimalExtensionDataAssetHandler();
    eggExtensionDataAssetHandler = new EggExtensionDataAssetHandler();
    grassDropExtensionDataAssetHandler = new GrassDropExtensionDataAssetHandler();

    var harmony = new Harmony(this.ModManifest.UniqueID);

    animalExtensionDataAssetHandler.RegisterEvents(Helper);
    eggExtensionDataAssetHandler.RegisterEvents(Helper);
    grassDropExtensionDataAssetHandler.RegisterEvents(Helper);

    AnimalDataPatcher.ApplyPatches(harmony);

    GameLocation.RegisterTileAction($"{UniqueId}.CustomFeedSilo", CustomFeedSilo);
    GameLocation.RegisterTileAction($"{UniqueId}.CustomFeedHopper", CustomFeedHopper);

    GameStateQuery.Register($"{UniqueId}_ANIMAL_HOUSE_COUNT", AnimalGameStateQueries.ANIMAL_HOUSE_COUNT);
    GameStateQuery.Register($"{UniqueId}_ANIMAL_COUNT", AnimalGameStateQueries.ANIMAL_COUNT);
    GameStateQuery.Register($"{UniqueId}_ANIMAL_LOCATION_COUNT", AnimalGameStateQueries.ANIMAL_LOCATION_COUNT);
    GameStateQuery.Register($"{UniqueId}_ANIMAL_AGE", AnimalGameStateQueries.ANIMAL_AGE);
    GameStateQuery.Register($"{UniqueId}_ANIMAL_FRIENDSHIP", AnimalGameStateQueries.ANIMAL_FRIENDSHIP);

    helper.Events.GameLoop.DayStarted += OnDayStarted;
    helper.Events.GameLoop.DayEnding += OnDayEnding;
    helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    helper.Events.World.LocationListChanged += OnLocationListChanged;
  }

  public override object GetApi() {
    return ModApi;
  }

  void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    vppApi = Helper.ModRegistry.GetApi<IVanillaPlusProfessions>("KediDili.VanillaPlusProfessions");
  }

  // Set animal override speed, and clear the attack dictionaries
  static void OnDayStarted(object? sender, DayStartedEventArgs e) {
    AnimalUtils.ClearDicts();
    Utility.ForEachLocation((GameLocation location) => {
      foreach (FarmAnimal animal in location.animals.Values) {
        if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
            animalExtensionData.SpeedOverride is not null) {
          animal.speed = animalExtensionData.SpeedOverride ?? 2;
        }
        LightUtils.RemoveLight(animal, location);
        LightUtils.AddLight(animal, location);
      }
      return true;
    });
  }

  // Reset speed so this mod is save to uninstall
  static void OnDayEnding(object? sender, DayEndingEventArgs e) {
    Utility.ForEachLocation((GameLocation location) => {
      foreach (FarmAnimal animal in location.animals.Values) {
        if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
            animalExtensionData.SpeedOverride is not null) {
          // we *could* set everyone to 2, but that might catch animals modified outside of this mod.
          animal.speed = 2;
        }
      }
      return true;
    });
  }

  static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e) {
    Utility.ForEachLocation((GameLocation location) => {
      foreach (var animal in location.animals.Values) {
        LightUtils.UpdateLight(animal, location);
      }
      return true;
    });
  }

  static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
    Utility.ForEachLocation((GameLocation location) => {
      location.animals.OnValueAdded += (long id, FarmAnimal animal) => {
        LightUtils.AddLight(animal, location);
      };
      location.animals.OnValueRemoved += (long id, FarmAnimal animal) => {
        LightUtils.RemoveLight(animal, location);
      };
      return true;
    });
  }

  static void OnLocationListChanged(object? sender, LocationListChangedEventArgs e) {
    foreach (var location in e.Added) {
      location.animals.OnValueAdded += (long id, FarmAnimal animal) => {
        LightUtils.AddLight(animal, location);
      };
      location.animals.OnValueRemoved += (long id, FarmAnimal animal) => {
        LightUtils.RemoveLight(animal, location);
      };
    }
  }

  private static bool CustomFeedSilo(GameLocation location, string[] args, Farmer player, Point tile) {
    if (!ArgUtility.TryGetOptional(args, 1, out var parsedItemId, out var error)) {
      ModEntry.StaticMonitor.Log(error, LogLevel.Warn);
      return false;
    }
    List<string> itemIds = parsedItemId is not null ? new(parsedItemId.Split(",")) :
      SiloUtils.GetFeedForThisBuilding(location.getBuildingAt(new Vector2(tile.X, tile.Y)));

    if (player.ActiveObject?.QualifiedItemId == "(O)178" ||
        (itemIds.Contains("(O)178") && itemIds.Count == 1)) {
      location.performAction("BuildingSilo", player, new Location(tile.X, tile.Y));
      return true;
    }

    if (player.ActiveObject is not null && itemIds.Contains(player.ActiveObject.QualifiedItemId)) {
      var itemId = player.ActiveObject.QualifiedItemId;
      int remainingCount = SiloUtils.StoreFeedInAnySilo(itemId, player.ActiveObject.Stack);
      if (remainingCount < player.ActiveObject.Stack) {
        Game1.playSound("Ship");
        DelayedAction.playSoundAfterDelay("grassyStep", 100);
        Game1.drawObjectDialogue(Helper.Translation.Get($"{UniqueId}.AddedToSiloMsg",
              new {
                count = player.ActiveObject.Stack - remainingCount,
                displayName = player.ActiveObject.DisplayName,
              }));
        player.ActiveObject.Stack = remainingCount;
        if (player.ActiveObject.Stack <= 0) {
          player.removeItemFromInventory(player.ActiveObject);
        }
      }
    } else {
      List<string> display = new();
      foreach (var itemId in itemIds) {
        if (itemId == "(O)178") {
          display.Add(Game1.content.LoadString("Strings\\Buildings:PiecesOfHay", location.piecesOfHay.Value, location.GetHayCapacity()));
        } else {
          display.Add(Helper.Translation.Get($"{UniqueId}.SiloCountMsg",
              new {
                displayName = ItemRegistry.GetDataOrErrorItem(itemId).DisplayName,
                count = SiloUtils.GetFeedCountFor(location, itemId),
                maxCount = SiloUtils.GetFeedCapacityFor(location, itemId),
              }));
        }
      }
      Game1.drawObjectDialogue(string.Join("  ^", display.ToArray()));
    }
    return true;
  }

  private static bool CustomFeedHopper(GameLocation location, string[] args, Farmer player, Point tile) {
    if (!ArgUtility.TryGet(args, 1, out var itemId, out var error)) {
      ModEntry.StaticMonitor.Log(error, LogLevel.Warn);
      return false;
    }
    if (player.ActiveObject?.QualifiedItemId == itemId) {
      int remainingCount = SiloUtils.StoreFeedInAnySilo(itemId, player.ActiveObject.Stack);
      if (remainingCount < player.ActiveObject.Stack) {
        Game1.playSound("Ship");
        DelayedAction.playSoundAfterDelay("grassyStep", 100);
        Game1.drawObjectDialogue(Helper.Translation.Get($"{UniqueId}.AddedToSiloMsg",
              new {
                count = player.ActiveObject.Stack - remainingCount,
                displayName = player.ActiveObject.DisplayName,
              }));
        player.ActiveObject.Stack = remainingCount;
        if (player.ActiveObject.Stack <= 0) {
          player.removeItemFromInventory(player.ActiveObject);
        }
      }
    } else if (location is AnimalHouse animalHouse) {
      if (player.freeSpotsInInventory() > 0) {
        var obj = SiloUtils.GetFeedFromAnySilo(itemId, animalHouse.animalLimit.Value);
        if (obj is not null) {
          player.addItemToInventory(obj);
          Game1.playSound("shwip");
        } else {
          Game1.drawObjectDialogue(ModEntry.Helper.Translation.Get($"{ModEntry.UniqueId}.HopperEmpty"));
        }
      } else {
        Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
      }
    }
    return true;
  }
}

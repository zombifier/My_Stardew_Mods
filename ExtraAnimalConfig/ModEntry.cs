using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using HarmonyLib;
using System.Collections.Generic;
using StardewValley;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using VanillaPlusProfessions.Compatibility;
using LeFauxMods.Common.Integrations.CustomBush;

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
  public static ICustomBushApi? cbApi = null;

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
    if (!Helper.ModRegistry.IsLoaded("selph.AnimalSqueezeThrough")) {
      helper.Events.GameLoop.SaveLoaded += OnSaveLoadedSqueeze;
      helper.Events.World.LocationListChanged += OnLocationListChangedSqueeze;
    }
    helper.Events.Display.RenderingHud += OnRenderingHud;
    helper.Events.Player.Warped += OnWarped;
  }

  public override object GetApi() {
    return ModApi;
  }

  void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    try {
      vppApi = Helper.ModRegistry.GetApi<IVanillaPlusProfessions>("KediDili.VanillaPlusProfessions");
    }
    catch (Exception exception) {
      Monitor.Log($"Error registering the VPP API: {exception.ToString()}", LogLevel.Warn);
    }
    try {
      cbApi = Helper.ModRegistry.GetApi<ICustomBushApi>("furyx639.CustomBush");
    }
    catch (Exception exception) {
      Monitor.Log($"Error registering the Custom Bush API: {exception.ToString()}", LogLevel.Warn);
    }
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
    // TODO:REMOVE
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

  static void OnSaveLoadedSqueeze(object? sender, SaveLoadedEventArgs e) {
    if (!Context.IsMainPlayer) return;
    Utility.ForEachLocation((GameLocation location) => {
      location.animals.OnValueAdded += (long id, FarmAnimal animal) => {
        DelayedAction.functionAfterDelay(() => HandleStuckAnimals(animal, location), 10);
      };
      return true;
    });
  }

  static void OnLocationListChangedSqueeze(object? sender, LocationListChangedEventArgs e) {
    if (!Context.IsMainPlayer) return;
    foreach (var location in e.Added) {
      location.animals.OnValueAdded += (long id, FarmAnimal animal) => {
        DelayedAction.functionAfterDelay(() => HandleStuckAnimals(animal, location), 10);
      };
    }
  }

  static void HandleStuckAnimals(FarmAnimal animal, GameLocation location) {
    if (animal.home is not null &&
        (animal.GetAnimalData()?.SpriteWidth ?? 16) / 16 > (animal.home.GetData()?.AnimalDoor.Width ?? 1) &&
        location.buildings.Contains(animal.home) &&
        animal.home.intersects(animal.GetBoundingBox())) {
      ModEntry.StaticMonitor.Log($"Squeezing the big {animal.type.Value} through the {animal.home.buildingType.Value}'s teeny door", LogLevel.Info);
      var rectForAnimalDoor = animal.home.getRectForAnimalDoor();
      animal.Position = new Vector2(rectForAnimalDoor.X - 32, rectForAnimalDoor.Y);
      return;
    }
  }

  private static bool IsNormalGameplay() {
    return StardewModdingAPI.Context.CanPlayerMove
      && Game1.player != null
      && Game1.currentLocation != null
      && !Game1.eventUp
      && !Game1.isFestival()
      && !Game1.IsFading();
  }

  // Draw emote icon if the animal has produce ready
  static void OnRenderingHud(object? sender, RenderingHudEventArgs e) {
    if (!IsNormalGameplay()) return;
    foreach (var animal in Game1.currentLocation.animals.Values) {
      if (animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData)
          && animalExtensionData.IsHarvester
          && HarvestUtils.GetAnimalHarvestChest(animal).Count > 0) {
        var animalData = animal.GetAnimalData();
        int num4 = animal.Sprite.SpriteWidth / 2 * 4 - 32 + (animalData?.EmoteOffset.X ?? 0);
        int num5 = -64 + (animalData?.EmoteOffset.Y ?? 0);
        Vector2 vector = new Vector2(0f, animal.yJumpOffset);
        Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(animal.Position.X + vector.X + (float)num4, animal.Position.Y + vector.Y + (float)num5));
        e.SpriteBatch.Draw(Game1.emoteSpriteSheet, vector2, new Microsoft.Xna.Framework.Rectangle(16 * 16 % Game1.emoteSpriteSheet.Width, 16 * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)animal.GetBoundingBox().Bottom / 10000f);
      }
    }
  }

  // Draw emote icon if the animal has produce ready
  static void OnWarped(object? sender, WarpedEventArgs e) {
    if (e.NewLocation is AnimalHouse animalHouse) {
      foreach (var o in animalHouse.objects.Values) {
        if (o.QualifiedItemId == "(BC)99") {
          SiloUtils.MaybeResetHopperNextIndex(o);
        }
      }
    }
  }
}

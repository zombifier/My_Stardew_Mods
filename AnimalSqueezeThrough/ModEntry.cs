using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;

using BlueprintEntry = StardewValley.Menus.CarpenterMenu.BlueprintEntry;

namespace Selph.StardewMods.AnimalSqueezeThrough;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    helper.Events.World.LocationListChanged += OnLocationListChanged;
  }

  static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
    if (!Context.IsMainPlayer) return;
    Utility.ForEachLocation((GameLocation location) => {
      location.animals.OnValueAdded += (long id, FarmAnimal animal) => {
        DelayedAction.functionAfterDelay(() => HandleStuckAnimals(animal, location), 10);
      };
      return true;
    });
  }

  static void OnLocationListChanged(object? sender, LocationListChangedEventArgs e) {
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
}

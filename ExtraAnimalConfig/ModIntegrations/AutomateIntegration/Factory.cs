using StardewModdingAPI;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using Pathoschild.Stardew.Automate;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ExtraAnimalConfig;

public class ModdedSiloFactory : IAutomationFactory {
  public IAutomatable? GetFor(SObject obj, GameLocation location, in Vector2 tile) {
    //if (obj.QualifiedItemId == "(BC)99"
    //    && location is AnimalHouse animalHouse
    //    && AnimalUtils.GetAllCustomFeedForThisAnimalHouse(animalHouse).Count > 0) {
    //  return new ModdedSiloMachine(animalHouse, tile);
    //}
    return null;
  }

  public IAutomatable? GetFor(TerrainFeature feature, GameLocation location, in Vector2 tile) {
    return null;
  }

  public IAutomatable? GetFor(Building building, GameLocation location, in Vector2 tile) {
    //var feedIds = SiloUtils.GetFeedForThisBuilding(building);
    //feedIds.Remove("(O)178");
    //if (feedIds.Count >= 0) {
    //  ModEntry.StaticMonitor.Log($"Initializing new modded silo Automate machine at {tile}", LogLevel.Info);
    //  return new ModdedSiloMachine(building);
    //}
    return null;
  }

  static ConditionalWeakTable<GameLocation, List<Building>> moddedSilos = new();

  static List<Building> GetOrFillModdedSilos(GameLocation location) {
    return moddedSilos.GetValue(location, (l) => {
      List<Building> list = new();
      foreach (var building in location.buildings) {
        var feedIds = SiloUtils.GetFeedForThisBuilding(building);
        feedIds.Remove("(O)178");
        if (feedIds.Count >= 0) {
          list.Add(building);
        }
      }
      return list;
    });
  }

  public static void ClearBuildings(GameLocation location) {
    moddedSilos.Remove(location);
  }

  public IAutomatable? GetForTile(GameLocation location, in Vector2 tile) {
    if (location is AnimalHouse animalHouse
        && AnimalUtils.GetAllCustomFeedForThisAnimalHouse(animalHouse).Count > 0
        && location.objects.TryGetValue(tile, out var obj)
        && obj.QualifiedItemId == "(BC)99") {
      ModEntry.StaticMonitor.Log($"Registering modded feed hopper at {tile} in {location.NameOrUniqueName} for Automate", LogLevel.Info);
      return new ModdedSiloMachine(animalHouse, tile);
    }
    foreach (var building in GetOrFillModdedSilos(location)) {
      //if (building.occupiesTile(tile)) {
      if (building.tileX.Value == tile.X && building.tileY.Value == tile.Y) {
        ModEntry.StaticMonitor.Log($"Registering modded silo at {building.tileX} {building.tileY} in {location.NameOrUniqueName} for Automate", LogLevel.Info);
        return new ModdedSiloMachine(building);
      }
    }
    return null;
  }
}

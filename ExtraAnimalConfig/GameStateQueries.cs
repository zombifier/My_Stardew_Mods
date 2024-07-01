using Netcode;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Delegates;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Events;
using StardewValley.Tools;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.GameData.Machines;
using StardewValley.GameData.FarmAnimals;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ExtraAnimalConfig;

// Contains new game state queries related to animals and animal house.
sealed class AnimalGameStateQueries {
  // Checks whether the location is an animal house, and that it has between the specified count of animals
  public static bool ANIMAL_HOUSE_COUNT(string[] query, GameStateQueryContext context) {
    GameLocation location = context.Location;
    if (!GameStateQuery.Helpers.TryGetLocationArg(query, 1, ref location, out var error) ||
        !ArgUtility.TryGet(query, 2, out var animalType, out error) ||
        !ArgUtility.TryGetOptionalInt(query, 3, out var minCount, out error, 0) ||
        !ArgUtility.TryGetOptionalInt(query, 4, out var maxCount, out error, int.MaxValue)
        ) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    if (location is AnimalHouse animalHouse) {
      var count = animalHouse.animalsThatLiveHere
        .Select(animalId => Utility.getAnimal(animalId))
        .Where(animal => animal.type.Value == animalType)
        .Count();
      return count >= minCount && count <= maxCount;
    }
    else {
      return false;
    }
  }
}

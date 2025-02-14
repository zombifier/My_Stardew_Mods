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
  // Checks whether the location is an animal house, and that it has between the specified count of animals with at least the amount of friendship
  public static bool ANIMAL_HOUSE_COUNT(string[] query, GameStateQueryContext context) {
    GameLocation location = context.Location;
    if (!GameStateQuery.Helpers.TryGetLocationArg(query, 1, ref location, out var error) ||
        !ArgUtility.TryGet(query, 2, out var animalType, out error) ||
        !ArgUtility.TryGetInt(query, 3, out var minFriendship, out error) ||
        !ArgUtility.TryGetOptionalInt(query, 3, out var minCount, out error, 0) ||
        !ArgUtility.TryGetOptionalInt(query, 4, out var maxCount, out error, int.MaxValue)
        ) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    if (location is AnimalHouse animalHouse) {
      var count = animalHouse.animalsThatLiveHere
        .Select(animalId => Utility.getAnimal(animalId))
        .Where(animal => (animalType == "ANY" || animal.type.Value == animalType) && animal.friendshipTowardFarmer.Value >= minFriendship)
        .Count();
      return count >= minCount && count <= maxCount;
    }
    else {
      return false;
    }
  }

  // Check that the player has the specified count of farm animals overall
  public static bool ANIMAL_COUNT(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGet(query, 1, out var animalType, out var error) ||
        !ArgUtility.TryGetInt(query, 2, out var minFriendship, out error) ||
        !ArgUtility.TryGetOptionalInt(query, 3, out var minCount, out error, 0) ||
        !ArgUtility.TryGetOptionalInt(query, 4, out var maxCount, out error, int.MaxValue)
        ) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    var count = 0;
    Utility.ForEachLocation(delegate(GameLocation location) {
      if (location is AnimalHouse animalHouse) {
        var locationCount = animalHouse.animalsThatLiveHere
        .Select(animalId => Utility.getAnimal(animalId))
        .Where(animal => (animalType == "ANY" || animal.type.Value == animalType) && animal.friendshipTowardFarmer.Value >= minFriendship)
        .Count();
        count += locationCount;
      }
      return true;
    });
    return count >= minCount && count <= maxCount;
  }

  public static bool ANIMAL_AGE(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGetInt(query, 1, out var minAge, out var error) ||
        !ArgUtility.TryGetOptionalInt(query, 2, out var maxAge, out error, int.MaxValue)) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    return
      (context.InputItem?.modData.TryGetValue(AnimalUtils.AnimalAge, out var ageStr) ?? false) &&
      Int32.TryParse(ageStr, out var age) &&
      age >= minAge && age <= maxAge;
  }

  public static bool ANIMAL_FRIENDSHIP(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGetInt(query, 1, out var minFriendship, out var error) ||
        !ArgUtility.TryGetOptionalInt(query, 2, out var maxFriendship, out error, int.MaxValue)) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    return
      (context.InputItem?.modData.TryGetValue(AnimalUtils.AnimalFriendship, out var ageStr) ?? false) &&
      Int32.TryParse(ageStr, out var age) &&
      age >= minFriendship && age <= maxFriendship;
  }
}

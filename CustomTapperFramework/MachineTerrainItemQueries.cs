using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.GameData;
using StardewValley.GameData.FishPonds;
using StardewValley.GameData.Locations;
using StardewValley.GameData.LocationContexts;
using System;
using System.Linq;
using System.Collections.Generic;
using StardewValley.Tools;

namespace Selph.StardewMods.MachineTerrainFramework;

using SObject = StardewValley.Object;
using Helpers = ItemQueryResolver.Helpers;

public static class MachineTerrainItemQueries {
  public static IEnumerable<ItemQueryResult> MACHINE_FISH_LOCATION(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError) {
    if (context.CustomFields == null ||
        !context.CustomFields.TryGetValue("Tile", out object? tileObj) ||
        tileObj is not Vector2 tile) {
      return Helpers.ErrorResult(key, arguments, logError, "No tile found - called outside TerrainCondition?");
    }
    var args = Helpers.SplitArguments(arguments);
    bool getAllFish = ArgUtility.GetBool(args, 0);
    if (!getAllFish) {
      return ItemQueryResolver.TryResolve(
          $"LOCATION_FISH {context.Location.Name} {tile.X} {tile.Y} {FishingRod.distanceToLand((int)tile.X, (int)tile.Y, context.Location)}",
          context, avoidRepeat: avoidRepeat, avoidItemIds: avoidItemIds, logError: logError);
    } else {
      LocationData? locationData = context.Location.GetData();
      Dictionary<string, string> allFishData = DataLoader.Fish(Game1.content);
      Season seasonForLocation = Game1.GetSeasonForLocation(context.Location);
      if (!context.Location.TryGetFishAreaForTile(tile, out var fishAreaId, out var _)) {
        fishAreaId = null;
      }
      IEnumerable<SpawnFishData> possibleFish = Game1.locationData["Default"].Fish;
      if (locationData != null && locationData.Fish?.Count > 0) {
        possibleFish = possibleFish.Concat(locationData.Fish);
      }
      possibleFish = from p in possibleFish
        orderby p.Precedence, Game1.random.Next()
        select p;
      List<ItemQueryResult> fishList = new();
      bool alsoCatchBossFish = ArgUtility.GetBool(args, 1);
      bool usingMagicBait = ArgUtility.GetBool(args, 2);
      bool allowNonObjects = ArgUtility.GetBool(args, 3);
      int waterDepth = ArgUtility.GetInt(args, 4, FishingRod.distanceToLand((int)tile.X, (int)tile.Y, context.Location));
      int tileX = ArgUtility.GetInt(args, 5, (int)tile.X);
      int tileY = ArgUtility.GetInt(args, 6, (int)tile.Y);
      HashSet<string>? ignoreQueryKeys = (usingMagicBait ? GameStateQuery.MagicBaitIgnoreQueryKeys : null);
      foreach (SpawnFishData spawn in possibleFish) {
        if ((spawn.FishAreaId != null && fishAreaId != spawn.FishAreaId) ||
            (spawn.Season.HasValue && !usingMagicBait && spawn.Season != seasonForLocation) ||
            (spawn.RequireMagicBait && !usingMagicBait)) {
          continue;
        }
        if (spawn.Condition != null && !GameStateQuery.CheckConditions(spawn.Condition, context.Location, null, null, null, null, ignoreQueryKeys)) {
          continue;
        }
        if (!alsoCatchBossFish && spawn.IsBossFish) {
          continue;
        }
        fishList.AddRange(ItemQueryResolver.TryResolve(spawn, context, avoidRepeat: false, formatItemId: (string query) => query.Replace("BOBBER_X", (tileX).ToString()).Replace("BOBBER_Y", (tileY).ToString()).Replace("WATER_DEPTH", waterDepth.ToString())));
      }
      if (!allowNonObjects) {
        fishList = fishList.Where(itemQueryResult => itemQueryResult.Item is SObject).ToList();
      }
      return fishList;
      // Hallowed grounds below, abandon all hope ye who enter here
      //LocationData? locationData = context.Location.GetData();
      //Dictionary<string, string> allFishData = DataLoader.Fish(Game1.content);
      //Season seasonForLocation = Game1.GetSeasonForLocation(context.Location);
      //if (!context.Location.TryGetFishAreaForTile(tile, out var fishAreaId, out var _)) {
      //  fishAreaId = null;
      //}
      //bool usingMagicBait = ArgUtility.GetBool(args, 1);
      //bool hasCuriosityLure = ArgUtility.GetBool(args, 2);
      //string? baitTargetFish = ArgUtility.Get(args, 3);
      //bool usingGoodBait = ArgUtility.GetBool(args, 4);
      //int waterDepth = ArgUtility.GetInt(args, 5, FishingRod.distanceToLand((int)tile.X, (int)tile.Y, context.Location));
      //int fishingLevel = ArgUtility.GetInt(args, 6, context.Player.FishingLevel);
      //Point tilePoint = new Point((int)tile.X, (int)tile.Y);
      //IEnumerable<SpawnFishData> possibleFish = Game1.locationData["Default"].Fish;
      //if (locationData != null && locationData.Fish?.Count > 0) {
      //  possibleFish = possibleFish.Concat(locationData.Fish);
      //}
      //possibleFish = from p in possibleFish
      //  orderby p.Precedence, Game1.random.Next()
      //  select p;
      //int targetedBaitTries = 0;
      //HashSet<string>? ignoreQueryKeys = (usingMagicBait ? GameStateQuery.MagicBaitIgnoreQueryKeys : null);
      //Item? item = null;
      //for (int i = 0; i < 2; i++) {
      //  foreach (SpawnFishData spawn in possibleFish) {
      //    if ((false && !spawn.CanBeInherited) || (spawn.FishAreaId != null && fishAreaId != spawn.FishAreaId) || (spawn.Season.HasValue && !usingMagicBait && spawn.Season != seasonForLocation)) {
      //      continue;
      //    }
      //    Microsoft.Xna.Framework.Rectangle? playerPosition = spawn.PlayerPosition;
      //    if (playerPosition.HasValue && !playerPosition.GetValueOrDefault().Contains(tilePoint.X, tilePoint.Y))
      //    {
      //      continue;
      //    }
      //    playerPosition = spawn.BobberPosition;
      //    if ((playerPosition.HasValue && !playerPosition.GetValueOrDefault().Contains((int)tile.X, (int)tile.Y)) || fishingLevel < spawn.MinFishingLevel || waterDepth < spawn.MinDistanceFromShore || (spawn.MaxDistanceFromShore > -1 && waterDepth > spawn.MaxDistanceFromShore) || (spawn.RequireMagicBait && !usingMagicBait)) {
      //      continue;
      //    }
      //    float chance = spawn.GetChance(hasCuriosityLure, context.Player.DailyLuck, context.Player.LuckLevel, (float value, IList<QuantityModifier> modifiers, QuantityModifier.QuantityModifierMode mode) => Utility.ApplyQuantityModifiers(value, modifiers, mode, context.Location), spawn.ItemId == baitTargetFish);
      //    if (!Game1.random.NextBool(chance)) {
      //      continue;
      //    }
      //    if (spawn.Condition != null && !GameStateQuery.CheckConditions(spawn.Condition, context.Location, null, null, null, null, ignoreQueryKeys)) { continue;
      //    }
      //    Item item2 = ItemQueryResolver.TryResolveRandomItem(spawn, context, avoidRepeat: false, null, (string query) => query.Replace("BOBBER_X", ((int)tile.X).ToString()).Replace("BOBBER_Y", ((int)tile.Y).ToString()).Replace("WATER_DEPTH", waterDepth.ToString()), null, delegate(string query, string error) {
      //      logError($"Location {context.Location.NameOrUniqueName} failed parsing item query {query} for fish {spawn.Id}", error);
      //    });
      //    if (item2 == null) {
      //      continue;
      //    }
      //    if (!string.IsNullOrWhiteSpace(spawn.SetFlagOnCatch)) {
      //      item2.SetFlagOnPickup = spawn.SetFlagOnCatch;
      //    }
      //    if (spawn.IsBossFish) {
      //      item2.SetTempData("IsBossFish", value: true);
      //    }
      //    Item item3 = item2;
      //    if ((spawn.CatchLimit <= -1 || !context.Player.fishCaught.TryGetValue(item3.QualifiedItemId, out var value2) || value2[0] < spawn.CatchLimit) && ModEntry.Helper.Reflection.GetMethod(typeof(GameLocation), "CheckGenericFishRequirements").Invoke<bool>(item3, allFishData, context.Location, context.Player, spawn, waterDepth, usingMagicBait, hasCuriosityLure, spawn.ItemId == baitTargetFish, false)) {
      //      if (baitTargetFish == null || !(item3.QualifiedItemId != baitTargetFish) || targetedBaitTries >= 2) {
      //        return item3;
      //      }
      //      if (item == null) {
      //        item = item3;
      //      }
      //      targetedBaitTries++;
      //    }
      //  }
      //  if (!usingGoodBait)
      //  {
      //    i++;
      //  }
      //}
      //if (item != null) {
      //  return item;
      //}
      //return ItemRegistry.Create("(O)145");

    }
  }

  public static IEnumerable<ItemQueryResult> MACHINE_CRAB_POT_OUTPUT(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError) {
    if (context.CustomFields == null ||
        !context.CustomFields.TryGetValue("Tile", out object? tileObj) ||
        tileObj is not Vector2 tile) {
      return Helpers.ErrorResult(key, arguments, logError, "No tile found - called outside machine rules?");
    }
    var args = Helpers.SplitArguments(arguments);
    if (!context.Location.TryGetFishAreaForTile(tile, out var _, out var fishArea)) {
      fishArea = null;
    }
    bool ignoreLocationJunkChance = ArgUtility.GetBool(args, 0);
    bool usingGoodBait = ArgUtility.GetBool(args, 1);
    bool isMariner = ArgUtility.GetBool(args, 2);
    string? baitTargetFish = ArgUtility.Get(args, 3);
    double chanceForJunk = (ignoreLocationJunkChance ? 0.0 : (((double?)fishArea?.CrabPotJunkChance) ?? 0.2));
    if (usingGoodBait) {
      chanceForJunk /= 2.0;
    }
    Dictionary<string, string> fishData = DataLoader.Fish(Game1.content);
    IList<string> targetAreas = context.Location.GetCrabPotFishForTile(tile);
    List<string> marinerList = new List<string>();
    if (context.Random.NextBool(chanceForJunk)) {
      return [new ItemQueryResult(ItemRegistry.Create("" + context.Random.Next(168, 173)))];
    }
    foreach (KeyValuePair<string, string> v in fishData) {
      if (!v.Value.Contains("trap")) {
        continue;
      }
      string[] rawSplit = v.Value.Split('/');
      string[] crabPotAreas = ArgUtility.SplitBySpace(rawSplit[4]);
      bool found = false;
      if (targetAreas.Intersect(crabPotAreas).Any()) {
        found = true;
      }
      if (!found) continue;
      if (isMariner) {
        marinerList.Add(v.Key);
        continue;
      }
      double chanceForCatch = Convert.ToDouble(rawSplit[2]);
      if (baitTargetFish != null && baitTargetFish == v.Key)
      {
        chanceForCatch *= (double)((chanceForCatch < 0.1) ? 4 : ((chanceForCatch < 0.2) ? 3 : 2));
      }
      if (!(context.Random.NextDouble() < chanceForCatch)) {
        continue;
      }
      return [new ItemQueryResult(ItemRegistry.Create(v.Key))];
    }
    if (isMariner && marinerList.Count > 0) {
      return marinerList.Select(fish => new ItemQueryResult(ItemRegistry.Create(fish)));
    } else {
      return [new ItemQueryResult(ItemRegistry.Create("" + context.Random.Next(168, 173)))];
    }
  }

  public static IEnumerable<ItemQueryResult> FISH_POND_DROP(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError) {
    var args = Helpers.SplitArguments(arguments);
    if (!ArgUtility.TryGet(args, 0, out var fishId, out var error, allowBlank: false, "fishId")) {
      return Helpers.ErrorResult(key, arguments, logError, error);
    }
    var fallbackItemId = ArgUtility.Get(args, 1, "(O)153");
    FishPondData fishPondData = FishPond.GetRawData(fishId);
    if (fishPondData is null) {
      return new ItemQueryResult[1] {new(ItemRegistry.Create(fallbackItemId))};
    }
    Item fish = ItemRegistry.Create(fishId);
    FishPondReward? selectedOutput = null;
    foreach (FishPondReward producedItem in fishPondData.ProducedItems) {
      if (!(selectedOutput?.Precedence <= producedItem.Precedence)
          //&& base.currentOccupants.Value >= producedItem.RequiredPopulation
          && context.Random.NextBool(producedItem.Chance)
          && GameStateQuery.CheckConditions(producedItem.Condition, context.Location, null, null, fish)) {
        selectedOutput = producedItem;
      }
    }
    Item? item = null;
    if (selectedOutput != null) {
      FishPondReward data = selectedOutput;
      item = ItemQueryResolver.TryResolveRandomItem(data, new ItemQueryContext(context.Location, null, null, "Machine Terrain Framework pond query"), avoidRepeat: false, null, (string id) => (!(ItemRegistry.QualifyItemId(selectedOutput.ItemId) == "(O)812")) ? id : ("FLAVORED_ITEM Roe " + fish.QualifiedItemId), fish);
    }
    if (item != null) {
      if (item.Name.Contains("Roe")) {
        while (context.Random.NextDouble() < 0.2) {
          item.Stack++;
        }
      }
      //if (this.goldenAnimalCracker.Value) {
      //  item.Stack *= 2;
      //}
    }
    return new ItemQueryResult[1] {item is not null ? new(item) : new(ItemRegistry.Create(fallbackItemId))};
  }
}

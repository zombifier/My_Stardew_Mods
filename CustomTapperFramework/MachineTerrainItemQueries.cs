using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Internal;
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
    //if (context.CustomFields == null ||
    //    !context.CustomFields.TryGetValue("Machine", out object machineObj) ||
    //    machineObj is not SObject machine) {
    //  return Helpers.ErrorResult(key, arguments, logError, "No tile found - called outside TerrainCondition?");
    //}
    return ItemQueryResolver.TryResolve(
        $"LOCATION_FISH {context.Location.Name} {tile.X} {tile.Y} {FishingRod.distanceToLand((int)tile.X, (int)tile.Y, context.Location)}",
        context, avoidRepeat: avoidRepeat, avoidItemIds: avoidItemIds, logError: logError);
  }

	public static IEnumerable<ItemQueryResult> MACHINE_CRAB_POT_OUTPUT(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError) {
    if (context.CustomFields == null ||
        !context.CustomFields.TryGetValue("Tile", out object? tileObj) ||
        tileObj is not Vector2 tile) {
				return Helpers.ErrorResult(key, arguments, logError, "No tile found - called outside machine rules?");
    }
    //if (context.CustomFields == null ||
    //    !context.CustomFields.TryGetValue("Machine", out object machineObj) ||
    //    machineObj is not SObject machine) {
		//		logError?.Invoke((key + " " + arguments).Trim(), "No tile found - called outside machine rules?");
		//		return Helpers.ErrorResult(key, arguments, logError, "No tile found - called outside machine rules?");
    //}
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
}

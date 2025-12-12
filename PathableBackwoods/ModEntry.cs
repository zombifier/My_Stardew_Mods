using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Pathfinding;
using StardewValley.GameData.Locations;
using System.Linq;
using System.Collections.Generic;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.PathableBackwoods;

internal sealed class ModEntry : Mod {
  static IMonitor StaticMonitor = null!;
  static IModHelper StaticHelper = null!;
  public override void Entry(IModHelper helper) {
    StaticMonitor = Monitor;
    StaticHelper = helper;
    helper.Events.Content.AssetRequested += OnAssetRequested;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
    helper.Events.Player.Warped += OnWarped;
    //var harmony = new Harmony(this.ModManifest.UniqueID);
    //harmony.Patch(
    //    original: AccessTools.Method(typeof(ItemGrabMenu),
    //      nameof(ItemGrabMenu.FillOutStacks)),
    //    // High priority so other mods that process the inventory after the fact like Convenient Inventory can do their logic
    //    postfix: new HarmonyMethod(AccessTools.Method(typeof(ModEntry),
    //      nameof(ModEntry.ItemGrabMenu_FillOutStacks_Postfix)), Priority.First));
  }

  static void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo("Data/Locations")) {
      e.Edit(asset => {
        var data = asset.AsDictionary<string, LocationData>().Data;
        if (data.TryGetValue("Backwoods", out var backwoodsData)) {
          backwoodsData.ExcludeFromNpcPathfinding = false;
        } else {
          StaticMonitor.Log("Backwoods data not found? This should not happen.", LogLevel.Error);
        }
      });
    }
  }

  static void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    WarpPathfindingCache.IgnoreLocationNames.Remove("Backwoods");
  }

  static void OnDayStarted(object? sender, DayStartedEventArgs e) {
    var backwoods = Game1.getLocationFromName("Backwoods");
    if (backwoods is null) {
      StaticMonitor.Log("Backwoods not found? This should not happen.", LogLevel.Error);
      return;
    }
    AddStairs(backwoods);
  }

  static void OnWarped(object? sender, WarpedEventArgs e) {
    if (e.NewLocation?.Name == "Backwoods") {
      AddStairs(e.NewLocation);
    }
  }

  static void AddStairs(GameLocation backwoods) {
    if (!Game1.MasterPlayer.mailReceived.Contains("communityUpgradeShortcuts")
        && !StaticHelper.Reflection.GetField<HashSet<string>>(backwoods, "_appliedMapOverrides").GetValue().Contains("Backwoods_Staircase")) {
      backwoods.ApplyMapOverride("Backwoods_Staircase");
      foreach (LargeTerrainFeature largeTerrainFeature in backwoods.largeTerrainFeatures) {
        if (largeTerrainFeature.Tile == new Vector2(37f, 16f)) {
          backwoods.largeTerrainFeatures.Remove(largeTerrainFeature);
          break;
        }
      }
    }
  }
}

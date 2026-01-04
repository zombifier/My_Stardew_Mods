using HarmonyLib;
using Microsoft.Xna.Framework;
using xTile;
using xTile.Tiles;
using xTile.Dimensions;
using xTile.Layers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Pathfinding;
using StardewValley.GameData.Locations;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Selph.StardewMods.PathableBackwoods;

internal sealed class ModEntry : Mod {
  static IMonitor StaticMonitor = null!;
  static IModHelper StaticHelper = null!;
  static bool HasDownhill = false;
  public override void Entry(IModHelper helper) {
    WarpPathfindingCache.IgnoreLocationNames.Remove("Backwoods");
    StaticMonitor = Monitor;
    StaticHelper = helper;
    helper.Events.Content.AssetRequested += OnAssetRequested;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
    //helper.Events.Player.Warped += OnWarped;
    HasDownhill = Helper.ModRegistry.IsLoaded("DH.Kiyuga.Main");
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
    if (e.NameWithoutLocale.IsEquivalentTo("Maps/Backwoods")) {
      e.Edit(asset => {
        Map targetMap = asset.AsMap().Data;

        Map sourceMap = Game1.content.Load<Map>("Maps/Backwoods_Staircase");
        ApplyMapOverride(sourceMap, targetMap);
        // Downhill project inserts its own location between the backwoods and mountain, without
        // removing the vanilla warps between them. This confuses the NPC pathfinder gravely, and as a
        // remedy remove the vanilla warps
        if (HasDownhill) {
          // Remove the mountain warps to not confuse the game
          if (targetMap.Properties.TryGetValue("Warp", out var warps)) {
            targetMap.Properties["Warp"] = RemoveWarps(warps, "Mountain");
          }
        }
      }, Priority.Last);
    }
    if (HasDownhill && e.NameWithoutLocale.IsEquivalentTo("Maps/Mountain")) {
      e.Edit(asset => {
        Map targetMap = asset.AsMap().Data;
        if (targetMap.Properties.TryGetValue("Warp", out var warps)) {
          targetMap.Properties["Warp"] = RemoveWarps(warps, "Backwoods");
        }
      }, Priority.Last);
    }
  }

  static void OnDayStarted(object? sender, DayStartedEventArgs e) {
    var backwoods = Game1.getLocationFromName("Backwoods");
    if (backwoods is null) {
      StaticMonitor.Log("Backwoods not found? This should not happen.", LogLevel.Error);
      return;
    }
    foreach (LargeTerrainFeature largeTerrainFeature in backwoods.largeTerrainFeatures) {
      if (largeTerrainFeature.Tile == new Vector2(37f, 16f)) {
        backwoods.largeTerrainFeatures.Remove(largeTerrainFeature);
        break;
      }
    }
    //AddStairs(backwoods);
  }

  //static void OnWarped(object? sender, WarpedEventArgs e) {
  //  if (e.NewLocation?.Name == "Backwoods") {
  //    AddStairs(e.NewLocation);
  //  }
  //}

  // Turns out the NPC pathfinder doesn't consider map overrides to exist. It does not acknowledge it at all...
  //static void AddStairs(GameLocation backwoods) {
  //  if (!Game1.MasterPlayer.mailReceived.Contains("communityUpgradeShortcuts")
  //      && !StaticHelper.Reflection.GetField<HashSet<string>>(backwoods, "_appliedMapOverrides").GetValue().Contains("Backwoods_Staircase")) {
  //    backwoods.ApplyMapOverride("Backwoods_Staircase");
  //    foreach (LargeTerrainFeature largeTerrainFeature in backwoods.largeTerrainFeatures) {
  //      if (largeTerrainFeature.Tile == new Vector2(37f, 16f)) {
  //        backwoods.largeTerrainFeatures.Remove(largeTerrainFeature);
  //        break;
  //      }
  //    }
  //  }
  //}

  static void ApplyMapOverride(Map override_map, Map target_map) {
    //this.updateSeasonalTileSheets(override_map);
    Dictionary<TileSheet, TileSheet> tilesheet_lookup = new Dictionary<TileSheet, TileSheet>();
    foreach (TileSheet override_tile_sheet in override_map.TileSheets) {
      TileSheet map_tilesheet = target_map.GetTileSheet(override_tile_sheet.Id);
      string source_image_source = "";
      string dest_image_source = "";
      if (map_tilesheet != null) {
        source_image_source = map_tilesheet.ImageSource;
      }
      if (dest_image_source != null) {
        dest_image_source = override_tile_sheet.ImageSource;
      }
      if (map_tilesheet == null || dest_image_source != source_image_source) {
        map_tilesheet = new TileSheet(GameLocation.GetAddedMapOverrideTilesheetId("Backwoods_Stairs", override_tile_sheet.Id), target_map, override_tile_sheet.ImageSource, override_tile_sheet.SheetSize, override_tile_sheet.TileSize);
        for (int i = 0; i < override_tile_sheet.TileCount; i++) {
          map_tilesheet.TileIndexProperties[i].CopyFrom(override_tile_sheet.TileIndexProperties[i]);
        }
        target_map.AddTileSheet(map_tilesheet);
      } else if (map_tilesheet.TileCount < override_tile_sheet.TileCount) {
        int tileCount = map_tilesheet.TileCount;
        map_tilesheet.SheetWidth = override_tile_sheet.SheetWidth;
        map_tilesheet.SheetHeight = override_tile_sheet.SheetHeight;
        for (int j = tileCount; j < override_tile_sheet.TileCount; j++) {
          map_tilesheet.TileIndexProperties[j].CopyFrom(override_tile_sheet.TileIndexProperties[j]);
        }
      }
      tilesheet_lookup[override_tile_sheet] = map_tilesheet;
    }
    Dictionary<Layer, Layer> layer_lookup = new Dictionary<Layer, Layer>();
    int map_width = 0;
    int map_height = 0;
    for (int layer_index = 0; layer_index < override_map.Layers.Count; layer_index++) {
      map_width = Math.Max(map_width, override_map.Layers[layer_index].LayerWidth);
      map_height = Math.Max(map_height, override_map.Layers[layer_index].LayerHeight);
    }
    var source_rect = new Microsoft.Xna.Framework.Rectangle(0, 0, map_width, map_height);
    map_width = 0;
    map_height = 0;
    for (int k = 0; k < target_map.Layers.Count; k++) {
      map_width = Math.Max(map_width, target_map.Layers[k].LayerWidth);
      map_height = Math.Max(map_height, target_map.Layers[k].LayerHeight);
    }
    bool layersDirty = false;
    for (int l = 0; l < override_map.Layers.Count; l++) {
      Layer original_layer = target_map.GetLayer(override_map.Layers[l].Id);
      if (original_layer == null) {
        original_layer = new Layer(override_map.Layers[l].Id, target_map, new Size(map_width, map_height), override_map.Layers[l].TileSize);
        target_map.AddLayer(original_layer);
        layersDirty = true;
      }
      layer_lookup[override_map.Layers[l]] = original_layer;
    }
    if (layersDirty) {
      //  this.SortLayers();
    }
    var dest_rect = new Microsoft.Xna.Framework.Rectangle(0, 0, map_width, map_height);
    int source_rect_x = source_rect.X;
    int source_rect_y = source_rect.Y;
    int dest_rect_x = dest_rect.X;
    int dest_rect_y = dest_rect.Y;
    for (int x = 0; x < source_rect.Width; x++) {
      for (int y = 0; y < source_rect.Height; y++) {
        Point source_tile_pos = new Point(source_rect_x + x, source_rect_y + y);
        Point dest_tile_pos = new Point(dest_rect_x + x, dest_rect_y + y);
        bool lower_layer_overridden = false;
        for (int m = 0; m < override_map.Layers.Count; m++) {
          Layer override_layer = override_map.Layers[m];
          Layer target_layer = layer_lookup[override_layer];
          if (target_layer == null || dest_tile_pos.X >= target_layer.LayerWidth || dest_tile_pos.Y >= target_layer.LayerHeight || (!lower_layer_overridden && override_map.Layers[m].Tiles[source_tile_pos.X, source_tile_pos.Y] == null)) {
            continue;
          }
          lower_layer_overridden = true;
          if (source_tile_pos.X >= override_layer.LayerWidth || source_tile_pos.Y >= override_layer.LayerHeight) {
            continue;
          }
          if (override_layer.Tiles[source_tile_pos.X, source_tile_pos.Y] == null) {
            target_layer.Tiles[dest_tile_pos.X, dest_tile_pos.Y] = null;
            continue;
          }
          Tile override_tile = override_layer.Tiles[source_tile_pos.X, source_tile_pos.Y];
          Tile? new_tile = null;
          if (!(override_tile is StaticTile)) {
            if (override_tile is AnimatedTile override_animated_tile) {
              StaticTile[] tiles = new StaticTile[override_animated_tile.TileFrames.Length];
              for (int n = 0; n < override_animated_tile.TileFrames.Length; n++) {
                StaticTile frame_tile = override_animated_tile.TileFrames[n];
                tiles[n] = new StaticTile(target_layer, tilesheet_lookup[frame_tile.TileSheet], frame_tile.BlendMode, frame_tile.TileIndex);
              }
              new_tile = new AnimatedTile(target_layer, tiles, override_animated_tile.FrameInterval);
            }
          } else {
            new_tile = new StaticTile(target_layer, tilesheet_lookup[override_tile.TileSheet], override_tile.BlendMode, override_tile.TileIndex);
          }
          new_tile?.Properties.CopyFrom(override_tile.Properties);
          target_layer.Tiles[dest_tile_pos.X, dest_tile_pos.Y] = new_tile;
        }
      }
    }
    target_map.LoadTileSheets(Game1.mapDisplayDevice);
  }

  static string RemoveWarps(string warps, string locationToRemove) {
    var split = warps.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    for (int i = split.Count() - 3; i >= 2; i -= 5) {
      if (split[i] == locationToRemove) {
        if (i < 2 || i > split.Count() - 3) {
          StaticMonitor.Log($"Warp string malformed? Location at index {i}, which is unexpected", LogLevel.Error);
          continue;
        }
        split.RemoveRange(i - 2, 5);
      }
    }
    return String.Join(' ', split);
  }
}

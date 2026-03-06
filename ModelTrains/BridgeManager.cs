using System;

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ModelTrains;

public static class BridgeManager {
  public static string BridgeNTag = $"{ModEntry.CpUniqueId}_bridge_n";
  public static string BridgeSTag = $"{ModEntry.CpUniqueId}_bridge_s";
  public static string BridgeWTag = $"{ModEntry.CpUniqueId}_bridge_w";
  public static string BridgeETag = $"{ModEntry.CpUniqueId}_bridge_e";
  public static string IsTunnelTag = $"{ModEntry.CpUniqueId}_tunnel";

  public class BridgeData {
    public Vector2[] CoordPair = [];
    public bool IsTunnel = false;
  }

  // Contains pairs of N-S and W-E bridge heads
  static ConditionalWeakTable<GameLocation, List<BridgeData>> BridgesPairs = new();

  public static List<BridgeData> GetBridges(GameLocation location, bool update = false) {
    return BridgesPairs.GetValue(location, (l => {
      var bridges = new List<BridgeData>();
      foreach (var (tile, bridge) in l.Objects.Pairs) {
        if (ItemContextTagManager.HasBaseTag(bridge.QualifiedItemId, BridgeNTag)) {
          for (int i = 0; i < maxLength; i++) {
            if (l.Objects.TryGetValue(tile + new Vector2(0, i), out var otherBridge)
                && ItemContextTagManager.HasBaseTag(otherBridge.QualifiedItemId, BridgeSTag)) {
              bridges.Add(new BridgeData {
                CoordPair = [tile, otherBridge.TileLocation],
                IsTunnel = IsTunnel(bridge),
              });
              break;
            }
          }
        }
        if (ItemContextTagManager.HasBaseTag(bridge.QualifiedItemId, BridgeWTag)) {
          for (int i = 0; i < maxLength; i++) {
            if (l.Objects.TryGetValue(tile + new Vector2(i, 0), out var otherBridge)
                && ItemContextTagManager.HasBaseTag(otherBridge.QualifiedItemId, BridgeETag)) {
              bridges.Add(new BridgeData {
                CoordPair = [tile, otherBridge.TileLocation],
                IsTunnel = IsTunnel(bridge),
              });
              break;
            }
          }
        }
      }
      return bridges;
    }));
  }

  public static bool IsBridge(SObject obj) {
    return ItemContextTagManager.DoAnyTagsMatch([BridgeNTag, BridgeSTag, BridgeWTag, BridgeETag], obj.GetContextTags());
  }

  // assumes the above is true
  public static bool IsTunnel(SObject obj) {
    return ItemContextTagManager.DoesTagMatch(IsTunnelTag, obj.GetContextTags());
  }

  const int maxLength = 16;

  public static void UpdateBridges(GameLocation location) {
    BridgesPairs.Remove(location);
  }

  public static void RemoveBridge(GameLocation location, Vector2 tile) {
    var bridges = GetBridges(location);
    bridges.RemoveAll(data => data.CoordPair[0] == tile || data.CoordPair[1] == tile);
  }

  public static bool IsOnBridge(GameLocation location, Vector2 tile, bool mustBeTunnelForDraw = false) {
    var bridges = GetBridges(location);
    foreach (var data in bridges) {
      var pair = data.CoordPair;
      // If in tunnel stricten the requirement a bit so trains don't disppear halfway in
      if (mustBeTunnelForDraw && (!data.IsTunnel || tile == pair[0] || tile == pair[1])) continue;
      if (pair[0].X == tile.X && pair[0].Y <= tile.Y && tile.Y <= pair[1].Y) {
        return true;
      }
      if (pair[0].Y == tile.Y && pair[0].X <= tile.X && tile.X <= pair[1].X) {
        return true;
      }
    }
    return false;
  }
}

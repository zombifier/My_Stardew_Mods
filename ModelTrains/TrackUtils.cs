using StardewModdingAPI;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using StardewValley;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;

namespace Selph.StardewMods.ModelTrains;

static class TrackUtils {
  // A non-track floor can have a track attached to it
  public static string AttachedTrackKey = $"{ModEntry.UniqueId}_AttachedTrack";

  public static void GetNextNeighbor(GameLocation location, Vector2 startTile, int currentDirection,
      out Vector2 nextTile, out int nextDirection) {
    string? trackId = null;
    if (location.terrainFeatures.TryGetValue(startTile, out var t)
        && t is Flooring flooring) {
      trackId = flooring.modData.GetValueOrDefault(AttachedTrackKey, flooring.whichFloor.Value);
    }
    nextTile = startTile;
    nextDirection = currentDirection;
    List<int> directionsToCheck = currentDirection switch {
      Game1.up => [Game1.up, Game1.right, Game1.left],
      Game1.down => [Game1.down, Game1.left, Game1.right],
      Game1.left => [Game1.left, Game1.up, Game1.down],
      Game1.right => [Game1.right, Game1.down, Game1.up],
      _ => [Game1.up, Game1.down, Game1.right, Game1.left],
    };
    foreach (var direction in directionsToCheck) {
      var next = direction switch {
        Game1.up => new Vector2(0, -1),
        Game1.down => new Vector2(0, 1),
        Game1.left => new Vector2(-1, 0),
        Game1.right => new Vector2(1, 0),
        _ => new Vector2(0, 0),
      };
      if (HasPathAt(location, startTile + next, out var _, trackId)) {
        nextDirection = direction;
        nextTile = startTile + next;
        return;
      }
    }
  }

  public static int GetNextDirectionTo(GameLocation location, Vector2 startTile, int currentDirection, Vector2 nextTile) {
    string? trackId = null;
    if (location.terrainFeatures.TryGetValue(startTile, out var t)
        && t is Flooring flooring) {
      trackId = flooring.modData.GetValueOrDefault(AttachedTrackKey, flooring.whichFloor.Value);
    }
    List<int> directionsToCheck = new();
    if (nextTile.X > startTile.X) {
      directionsToCheck.Add(Game1.right);
    }
    if (nextTile.X < startTile.X) {
      directionsToCheck.Add(Game1.left);
    }
    if (nextTile.Y > startTile.Y) {
      directionsToCheck.Add(Game1.down);
    }
    if (nextTile.Y < startTile.Y) {
      directionsToCheck.Add(Game1.up);
    }
    // always check current direction first
    var index = directionsToCheck.IndexOf(currentDirection);
    if (index > 0) {
      directionsToCheck.RemoveAt(index);
      directionsToCheck.Insert(0, currentDirection);
    }
    foreach (var direction in directionsToCheck) {
      var next = direction switch {
        Game1.up => new Vector2(0, -1),
        Game1.down => new Vector2(0, 1),
        Game1.left => new Vector2(-1, 0),
        Game1.right => new Vector2(1, 0),
        _ => new Vector2(0, 0),
      };
      if (HasPathAt(location, startTile + next, out var _, trackId)) {
        return direction;
      }
    }
    return -1;
  }

  // This function is on hiatus until I want to add complex pathing
  //public static Stack<Point> PlotPath(GameLocation location, Vector2 startTile, int startDirection, out int finalFacingDirection) {
  //  List<Point> path = new();
  //  var currentTile = startTile;
  //  var currentDirection = startDirection;
  //  finalFacingDirection = startDirection;
  //  int maxNodes = 0;
  //  ModEntry.StaticMonitor.Log($"Starting path: {startTile} {startDirection}", LogLevel.Alert);
  //  while (true) {
  //    GetNextNeighbor(location, currentTile, currentDirection,
  //        out var nextTile, out var nextDirection);
  //    finalFacingDirection = nextDirection;
  //    // we either reached a deadend or looped to the start
  //    if (nextTile == currentTile) {
  //      break;
  //    }
  //    if (nextTile == startTile) {
  //      path.Add(currentTile.ToPoint());
  //      break;
  //    }
  //    if (currentDirection != nextDirection) {
  //      ModEntry.StaticMonitor.Log($"Node: {currentTile} {currentDirection}", LogLevel.Alert);
  //      path.Add(currentTile.ToPoint());
  //      maxNodes++;
  //    }
  //    currentTile = nextTile;
  //    currentDirection = nextDirection;
  //    // safety measure, avoid infinite loops
  //    if (maxNodes >= 128) {
  //      break;
  //    }
  //  }
  //  return new Stack<Point>(Enumerable.Reverse(path));
  //}
  //
  public static bool IsTrackItem(string? itemId) {
    return itemId is not null
      && ItemContextTagManager.HasBaseTag(itemId, ModEntry.TrackItemTag);
  }

  public static bool IsPath(Flooring flooring, string? trackId = null) {
    // This is valid train tracks
    if (trackId is null
        && flooring.GetData()?.ItemId is { } itemId
        && ItemContextTagManager.HasBaseTag(itemId, ModEntry.TrackItemTag)) {
      return true;
    }
    // This is the same track we are checking
    if (trackId is not null && flooring.whichFloor.Value == trackId) {
      return true;
    }
    var attachedTrack = flooring.modData.GetValueOrDefault(AttachedTrackKey);
    // Has valid attached tracks
    if (trackId is null && attachedTrack is not null) {
      return true;
    }
    // The attached track is the same one we're checking
    if (trackId is not null && attachedTrack == trackId) {
      return true;
    }
    return false;
  }

  public static bool HasPathAt(GameLocation location, Vector2 tile, out Flooring? track, string? trackId = null) {
    track = null;
    if (location.terrainFeatures.TryGetValue(tile, out var t)
        && t is Flooring flooring) {
      track = flooring;
      if (IsPath(flooring, trackId)) {
        return true;
      }
    }
    // Else return if we're on bridges
    // (Don't check track ID on bridges for now, unless people report issues lol)
    return BridgeManager.IsOnBridge(location, tile);
  }

  public static int GetReverseDirection(int direction) {
    return direction switch {
      Game1.up => Game1.down,
      Game1.down => Game1.up,
      Game1.right => Game1.left,
      _ => Game1.right,
    };
  }

  public static void GetPreviousNeighbor(GameLocation location, Vector2 startTile, Vector2 startPosition, int currentDirection,
      out Vector2 previousTile, out int previousDirection, out Vector2 previousPosition) {
    previousTile = startTile;
    previousDirection = currentDirection;
    previousPosition = previousTile * 64;
    for (int i = 0; i <= 1; i++) {
      GetNextNeighbor(location, startTile, GetReverseDirection(currentDirection),
          out previousTile, out var previousDirectionOpposite);
      previousDirection = GetReverseDirection(previousDirectionOpposite);
      var offset = currentDirection switch {
        Game1.up => (int)(startTile.Y * Game1.tileSize - startPosition.Y),
        Game1.down => (int)(startPosition.Y - startTile.Y * Game1.tileSize),
        Game1.right => (int)(startPosition.X - startTile.X * Game1.tileSize),
        _ => (int)(startTile.X * Game1.tileSize - startPosition.X),
      };
      previousPosition = previousTile * 64;
      switch (previousDirection) {
        case Game1.up:
          previousPosition -= new Vector2(0, offset);
          break;
        case Game1.down:
          previousPosition += new Vector2(0, offset);
          break;
        case Game1.right:
          previousPosition += new Vector2(offset, 0);
          break;
        default:
          previousPosition -= new Vector2(offset, 0);
          break;
      }

      startTile = previousTile;
      startPosition = previousPosition;
      currentDirection = previousDirection;
    }
    //ModEntry.StaticMonitor.Log($"current wagon : {startTile} {startPosition} {ModEntry.PrintDirection(currentDirection)}", LogLevel.Alert);
    //ModEntry.StaticMonitor.Log($"previous wagon: {previousTile} {previousPosition} {ModEntry.PrintDirection(previousDirection)}", LogLevel.Alert);
  }
}

static class DrawNeighborManager {

  class NeighborStruct {
    public List<Flooring.Neighbor> trackNeighbors = new();
    public byte neighborMask = new();
  }

  static ConditionalWeakTable<Flooring, NeighborStruct> NeighborData = new();

  static NeighborStruct GetNeighborData(Flooring f, GameLocation? location = null) {
    return NeighborData.GetValue(f, (flooring) => {
      var neighborStruct = new NeighborStruct();
      var neighbors = neighborStruct.trackNeighbors;
      location ??= flooring.Location;
      Vector2 tile = flooring.Tile;
      var terrainFeatures = location.terrainFeatures;
      Flooring.NeighborLoc[] offsets = Flooring._offsets;
      var trackId = flooring.modData.GetValueOrDefault(TrackUtils.AttachedTrackKey, flooring.whichFloor.Value);
      for (int i = 0; i < offsets.Length; i++) {
        Flooring.NeighborLoc neighborLoc = offsets[i];
        Vector2 vector = tile + neighborLoc.Offset;
        if (location.map != null && !location.isTileOnMap(vector)) {
          var item = new Flooring.Neighbor(null, neighborLoc.Direction, neighborLoc.InvDirection);
          neighbors.Add(item);
        } else if (TrackUtils.HasPathAt(location, vector, out var f, trackId)) {
          var item2 = new Flooring.Neighbor(f, neighborLoc.Direction, neighborLoc.InvDirection);
          neighbors.Add(item2);
        }
      }
      byte neighborMask = 0;
      foreach (var item in neighbors) {
        neighborMask |= item.direction;
        //OnNeighborAdded(item.feature, item.invDirection);
      }
      neighborStruct.neighborMask = neighborMask;
      return neighborStruct;
    });
  }

  public static void UpdateTrackNeighbors(GameLocation l) {
    foreach (var (tile, t) in l.terrainFeatures.Pairs) {
      if (t is Flooring flooring && TrackUtils.IsPath(flooring)) {
        OnAdded(flooring);
      }
    }
  }

  public static void OnAdded(Flooring flooring, GameLocation? location = null) {
    NeighborData.Remove(flooring);
    var neighborData = GetNeighborData(flooring, location);
    var list = neighborData.trackNeighbors;
    byte neighborMask = neighborData.neighborMask;
    foreach (var item in list) {
      OnNeighborAdded(item.feature, item.invDirection);
    }
  }

  public static void OnRemoved(Flooring flooring, GameLocation location) {
    NeighborData.Remove(flooring);
    var neighborData = GetNeighborData(flooring, location);
    var list = neighborData.trackNeighbors;
    foreach (var item in list) {
      OnNeighborRemoved(item.feature, item.invDirection);
    }
  }

  static void OnNeighborAdded(Flooring? flooring, byte direction) {
    if (flooring is null) return;
    var neighborData = GetNeighborData(flooring);
    neighborData.neighborMask |= direction;
  }

  static void OnNeighborRemoved(Flooring? flooring, byte direction) {
    if (flooring is null) return;
    var neighborData = GetNeighborData(flooring);
    neighborData.neighborMask = (byte)(neighborData.neighborMask & ~direction);
  }

  public static byte GetNeighborMask(Flooring flooring) {
    return GetNeighborData(flooring).neighborMask;
  }
}

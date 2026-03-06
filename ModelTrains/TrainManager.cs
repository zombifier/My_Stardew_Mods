using System;

using System.Collections.Generic;
using System.Linq;
using StardewValley;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Selph.StardewMods.ModelTrains;

class WagonComparer : IComparer<NPC> {
  public static int GetOrder(NPC? npc) {
    if (npc?.modData.TryGetValue(TrainManager.WagonOrderKey, out var str) is true
        && Int32.TryParse(str, out var order)) {
      return order;
    }
    return 0;
  }

  public int Compare(NPC? x, NPC? y) {
    return GetOrder(x).CompareTo(GetOrder(y));
  }
}

public static class TrainManager {
  // Shared between locomotives and wagons, used to identify which locomotive this wagon is
  // following
  public static string LocomotiveUniqueIdKey = $"{ModEntry.UniqueId}_UniqueId";
  // Values for the following 2 is the source item ID
  public static string IsLocomotiveKey = $"{ModEntry.UniqueId}_IsLocomotive";
  public static string IsWagonKey = $"{ModEntry.UniqueId}_IsWagon";
  // Value is the wagon's order in the list
  public static string WagonOrderKey = $"{ModEntry.UniqueId}_WagonOrderKey";

  public static bool IsLocomotive(NPC npc) {
    return npc.modData.ContainsKey(IsLocomotiveKey);
  }

  public static bool IsWagon(NPC npc) {
    return npc.modData.ContainsKey(IsWagonKey);
  }

  public static bool IsWagonOrLocomotive(NPC npc) {
    return IsLocomotive(npc) || IsWagon(npc);
  }

  public static bool AddLocomotive(string itemId, GameLocation location, Vector2 tileLocation, out NPC locomotive) {
    locomotive = new NPC(
        new AnimatedSprite($"Characters/{itemId}", 0, 32, 32),
        tileLocation * Game1.tileSize,
        -1, //facingDir
        $"{ModEntry.UniqueId}_Train"
        );
    locomotive.modData[IsLocomotiveKey] = itemId;
    locomotive.modData[LocomotiveUniqueIdKey] = Game1.Multiplayer.getNewID().ToString();
    locomotive.Speed = 1;
    SetRuntimeFields(locomotive, location);
    location.characters.Add(locomotive);
    return true;
  }

  public static bool AddWagon(string itemId, NPC locomotive, Vector2? tile = null, int? direction = null) {
    var existingWagons = GetWagons(locomotive);
    var lastWagon = existingWagons.Count == 0 ? locomotive : existingWagons.Max!;
    int order = lastWagon == locomotive ? 0 : (WagonComparer.GetOrder(lastWagon) + 1);
    NPC wagon;
    if (tile is not null && direction is not null) {
      wagon = new NPC(
          new AnimatedSprite($"Characters/{itemId}", 0, 32, 32),
          tile.Value * Game1.tileSize,
          direction.Value,
          $"{ModEntry.UniqueId}_Train"
          );
    } else {
      TrackUtils.GetPreviousNeighbor(
          lastWagon.currentLocation,
          lastWagon.Tile,
          lastWagon.Position,
          lastWagon.FacingDirection,
          out var _,
          out var previousDirection,
          out var previousPosition);
      wagon = new NPC(
          new AnimatedSprite($"Characters/{itemId}", 0, 32, 32),
          previousPosition,
          previousDirection,
          $"{ModEntry.UniqueId}_Train"
          );
    }
    wagon.Speed = 1;
    wagon.modData[IsWagonKey] = itemId;
    wagon.modData[LocomotiveUniqueIdKey] = locomotive.modData.GetValueOrDefault(LocomotiveUniqueIdKey);
    wagon.modData[WagonOrderKey] = order.ToString();
    SetRuntimeFields(wagon, locomotive.currentLocation);
    locomotive.currentLocation.characters.Add(wagon);
    existingWagons.Add(wagon);
    ParentLocomotive.AddOrUpdate(wagon, new(locomotive));
    if (locomotive.isMoving()) {
      wagon.setMovingInFacingDirection();
    }
    return true;

  }

  public static void FlipLocomotive(NPC locomotive) {
    locomotive.stopWithoutChangingFrame();
    List<NPC> allWagons = new List<NPC>();
    allWagons.Add(locomotive);
    allWagons.AddRange(GetWagons(locomotive));
    for (var i = 0; i < (allWagons.Count() + 1) / 2; i++) {
      var car1 = allWagons[i];
      var car2 = allWagons[allWagons.Count() - i - 1];
      var car1FacingDirection = car1.FacingDirection;
      var car2FacingDirection = car2.FacingDirection;
      var car1Position = car1.Position;
      var car2Position = car2.Position;
      car1.FacingDirection = TrackUtils.GetReverseDirection(car2FacingDirection);
      car1.Position = car2Position;
      car1.stopWithoutChangingFrame();
      car1.setMovingInFacingDirection();
      if (car1 != car2) {
        car2.FacingDirection = TrackUtils.GetReverseDirection(car1FacingDirection);
        car2.Position = car1Position;
        car2.stopWithoutChangingFrame();
        car2.setMovingInFacingDirection();
      }
    }
  }

  public static string GetItem(NPC car) {
    return IsLocomotive(car)
      ? car.modData.GetValueOrDefault(IsLocomotiveKey, "0")
      : car.modData.GetValueOrDefault(IsWagonKey, "0");
  }

  public static List<string> RemoveCar(NPC car) {
    List<string> items = new();
    car.currentLocation.characters.Remove(car);
    items.Add(GetItem(car));
    if (IsLocomotive(car)) {
      foreach (var wagon in GetWagons(car)) {
        wagon.currentLocation.characters.Remove(wagon);
        items.Add(GetItem(wagon));
      }
    } else if (GetParentLocomotive(car) is { } locomotive) {
      var wagons = GetWagons(locomotive);
      Vector2? prevPosition = null;
      int? prevDirection = null;
      foreach (var wagon in wagons) {
        if (wagon == car) {
          prevPosition = wagon.Position;
          prevDirection = wagon.FacingDirection;
        } else if (prevPosition is not null && prevDirection is not null) {
          var fd = wagon.FacingDirection;
          var p = wagon.Position;
          wagon.FacingDirection = prevDirection.Value;
          wagon.Position = prevPosition.Value;
          wagon.stopWithoutChangingFrame();
          wagon.setMovingInFacingDirection();
          prevDirection = fd;
          prevPosition = p;
        }
      }
      wagons.Remove(car);
    }
    return items;
  }

  // A mapping of locomotives to every wagons attached to it.
  static ConditionalWeakTable<NPC, SortedSet<NPC>> AttachedWagons = new();
  // A mapping of a wagon to a parent locomotive
  static ConditionalWeakTable<NPC, WeakReference<NPC?>> ParentLocomotive = new();

  public static SortedSet<NPC> GetWagons(NPC locomotive) {
    if (!IsLocomotive(locomotive)) {
      return new();
    }
    var wagons = AttachedWagons.GetValue(locomotive, (l) => {
      var wagons = new SortedSet<NPC>(new WagonComparer());
      Utility.ForEachCharacter(c => {
        if (IsWagon(c)
        && c.modData[LocomotiveUniqueIdKey] == l.modData[LocomotiveUniqueIdKey]) {
          wagons.Add(c);
        }
        return true;
      });
      return wagons;
    });
    //if (update) {
    //  UpdateWagonsInternal(locomotive, wagons);
    //}
    return wagons;
  }

  // Run every save load for updating
  //public static void UpdateWagons(NPC locomotive) {
  //  var wagons = GetWagons(locomotive);
  //  foreach (var c in locomotive.currentLocation.characters) {
  //    if (c.modData.ContainsKey(IsWagonKey)
  //        && c.modData[LocomotiveUniqueIdKey] == locomotive.modData[LocomotiveUniqueIdKey]) {
  //      wagons.Add(c);
  //    }
  //  }
  //}

  //public static void StartLocomotive(NPC locomotive, GameLocation location) {
  //  if (!Game1.IsMasterGame || locomotive.controller is not null) return;
  //  if (IsLocomotive(locomotive)) {
  //    FixWagonPositions(locomotive);
  //    foreach (var wagon in GetWagons(locomotive)) {
  //      StartLocomotive(wagon, location);
  //    }
  //  }
  //  var path = TrackUtils.PlotPath(
  //      location,
  //      locomotive.Tile,
  //      locomotive.FacingDirection,
  //      out var finalFacingDirection);
  //  if (path.Count == 0) {
  //    return;
  //  }
  //  var controller = new PathFindController(
  //      path,
  //      locomotive,
  //      location);
  //  controller.endBehaviorFunction = (c, l) => {
  //    c.controller = null;
  //    StartLocomotive((NPC)c, l);
  //  };
  //  controller.finalFacingDirection = finalFacingDirection;
  //  locomotive.controller = controller;
  //}


  // Ideally a wagon should never be added to a location before its parent is
  public static NPC? GetParentLocomotive(NPC c) {
    var locomotiveRef = ParentLocomotive.GetValue(c, wagon => {
      return new(wagon.currentLocation.characters.FirstOrDefault(c =>
          TrainManager.IsLocomotive(c)
          && c.modData.GetValueOrDefault(LocomotiveUniqueIdKey) == wagon.modData.GetValueOrDefault(LocomotiveUniqueIdKey)));
    });
    if (locomotiveRef.TryGetTarget(out var value)) {
      return value;
    }
    return null;
  }

  public static string GetId(NPC c) {
    return c.modData.GetValueOrDefault(LocomotiveUniqueIdKey, "");
  }

  public static void SetRuntimeFields(NPC c, GameLocation location) {
    c.currentLocation = location;
    c.collidesWithOtherCharacters.Value = false;
    c.farmerPassesThrough = true;
    c.SimpleNonVillagerNPC = true;
    c.HideShadow = true;
  }
}

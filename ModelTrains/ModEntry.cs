using System;

using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Objects;
using StardewValley.Mods;
using StardewValley.GameData.FloorsAndPaths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ModelTrains;

using SavedLocomotiveType = Dictionary<string, List<SavedLocomotive>>;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;

  public const string CpUniqueId = "selph.ModelTrains";
  const string LocomotiveItemTag = $"{CpUniqueId}_locomotive";
  public const string TrackItemTag = $"{CpUniqueId}_track";
  const string WagonItemTag = $"{CpUniqueId}_wagon";
  const string BridgeItemTag = $"{CpUniqueId}_bridge";

  //static ModConfig Config = null!;

  public override void Entry(IModHelper helper) {
    //Config = helper.ReadConfig<ModConfig>();
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    helper.Events.Input.ButtonPressed += OnButtonPressed;
    helper.Events.GameLoop.DayEnding += OnDayEnding;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
    //helper.Events.Display.RenderedWorld += OnRenderedWorld;
    helper.Events.Display.RenderedStep += OnRenderedStep;
    helper.Events.World.ObjectListChanged += OnObjectListChanged;
    helper.Events.World.TerrainFeatureListChanged += OnTerrainFeatureListChanged;
    helper.Events.World.NpcListChanged += OnNpcListChanged;
    helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

    var harmony = new Harmony(this.ModManifest.UniqueID);
    // TODO: Maybe move to subclass to avoid all these patches
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.GetBoundingBox)),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(NPC_GetBoundingBox_Prefix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(GameLocation), nameof(GameLocation.isCollidingPosition),
          new Type[] {
              typeof(Microsoft.Xna.Framework.Rectangle),
              typeof(xTile.Dimensions.Rectangle),
              typeof(bool),
              typeof(int),
              typeof(bool),
              typeof(Character),
              typeof(bool),
              typeof(bool),
              typeof(bool),
              typeof(bool)
          }),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(GameLocation_IsCollidingPosition_Prefix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(NPC), nameof(NPC.draw), new Type[] { typeof(SpriteBatch), typeof(float) }),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(NPC_draw_Prefix)));
    harmony.Patch(
        original: AccessTools.DeclaredPropertyGetter(typeof(NPC), nameof(NPC.IsVillager)),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(NPC_IsVillager_Prefix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.update), new Type[] { typeof(GameTime), typeof(GameLocation), typeof(long), typeof(bool) }),
        postfix: new HarmonyMethod(typeof(ModEntry), nameof(Character_update_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Furniture), nameof(Furniture.checkForAction)),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(Furniture_checkForAction_Prefix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.isPassable)),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(SObject_isPassable_Prefix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(NPC), nameof(NPC.canTalk)),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(NPC_canTalk_Prefix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Flooring), nameof(Flooring.initNetFields)),
        postfix: new HarmonyMethod(typeof(ModEntry), nameof(Flooring_initNetFields_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Flooring), nameof(Flooring.draw)),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(Flooring_draw_Prefix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Furniture), "loadDescription"),
        postfix: new HarmonyMethod(typeof(ModEntry), nameof(Furniture_loadDescription_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(SObject),
          nameof(SObject.draw),
          new Type[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(SObject_draw_Prefix)));
  }

  static bool IsNormalGameplay() {
    return StardewModdingAPI.Context.CanPlayerMove
      && Game1.player != null
      && !Game1.player.isRidingHorse()
      && Game1.currentLocation != null
      && !Game1.eventUp
      && !Game1.isFestival()
      && !Game1.IsFading();
  }

  static void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
    if (!IsNormalGameplay()) return;
    bool toolButton = e.Button.IsUseToolButton();
    bool actionButton = e.Button.IsActionButton();
    if (!toolButton && !actionButton) {
      return;
    }
    // Add tracks to existing flooring
    if (Game1.player.ActiveObject is SObject obj
        && TrackUtils.IsTrackItem(obj.ItemId)
        && Game1.currentLocation.terrainFeatures.TryGetValue(e.Cursor.GrabTile, out var t)
        && t is Flooring flooring
        && !flooring.modData.ContainsKey(TrackUtils.AttachedTrackKey)
        && !TrackUtils.IsTrackItem(flooring.GetData()?.ItemId)) {
      flooring.modData[TrackUtils.AttachedTrackKey] = Flooring.GetFloorPathItemLookup()[obj.ItemId];
      //DrawNeighborManager.OnAdded(flooring, Game1.currentLocation);
      Game1.currentLocation.playSound("axe");
      ModEntry.Helper.Input.Suppress(e.Button);
      return;
    }

    NPC? locomotiveOnTile = null;
    NPC? wagonOnTile = null;
    foreach (var character in Game1.currentLocation.characters.ToList()) {
      var boundingBox = character.GetBoundingBox();
      boundingBox.Inflate(32, 32);
      if (!boundingBox.Contains(e.Cursor.GetScaledAbsolutePixels())
          && !boundingBox.Contains(e.Cursor.GrabTile * 64f)) {
        continue;
      }
      if (TrainManager.IsLocomotive(character)) {
        locomotiveOnTile = character;
      }
      if (TrainManager.IsWagon(character)) {
        wagonOnTile = character;
        locomotiveOnTile = TrainManager.GetParentLocomotive(character);
      }
    }
    NPC? wagonOrLocomotiveOnTile = wagonOnTile ?? locomotiveOnTile;

    if (Game1.player.ActiveObject is SObject obj2) {
      // Placing locomotive
      if (wagonOrLocomotiveOnTile is null
          && ItemContextTagManager.HasBaseTag(obj2.QualifiedItemId, LocomotiveItemTag)
          && (TrackUtils.HasPathAt(Game1.currentLocation, e.Cursor.GrabTile, out var _))) {
        if (Game1.IsMasterGame) {
          TrainManager.AddLocomotive(obj2.ItemId, Game1.currentLocation, e.Cursor.GrabTile, out var _);
        } else {
          ModEntry.Helper.Multiplayer.SendMessage(new AddCarInfo {
            ItemId = obj2.ItemId,
            LocationName = Game1.currentLocation.NameOrUniqueName,
            Tile = e.Cursor.GrabTile,
          }, "AddCarRequest", modIDs: new[] { UniqueId }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }
        Game1.currentLocation.playSound("axe");
        Game1.player.reduceActiveItemByOne();
        return;
      }
      // Placing wagons
      if (locomotiveOnTile is not null
          && ItemContextTagManager.HasBaseTag(obj2.QualifiedItemId, WagonItemTag)) {
        if (Game1.IsMasterGame) {
          TrainManager.AddWagon(obj2.ItemId, locomotiveOnTile);
        } else {
          ModEntry.Helper.Multiplayer.SendMessage(new AddCarInfo {
            ItemId = obj2.ItemId,
            LocomotiveId = TrainManager.GetId(locomotiveOnTile),
            LocationName = Game1.currentLocation.NameOrUniqueName,
          }, "AddCarRequest", modIDs: new[] { UniqueId }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }
        Game1.currentLocation.playSound("axe");
        Game1.player.reduceActiveItemByOne();
        return;
      }
      // Removing locomotives and wagons
      if (obj2.QualifiedItemId == $"(O){CpUniqueId}_CarRemover") {
        if (toolButton && wagonOrLocomotiveOnTile is not null) {
          if (Game1.IsMasterGame) {
            Game1.player.addItemsByMenuIfNecessary(TrainManager.RemoveCar(wagonOrLocomotiveOnTile).Select(str => ItemRegistry.Create(str)).ToList());
          } else {
            ModEntry.Helper.Multiplayer.SendMessage(new CarInfo {
              Id = TrainManager.GetId(wagonOrLocomotiveOnTile),
              WagonOrder = locomotiveOnTile == wagonOrLocomotiveOnTile ? -1 : WagonComparer.GetOrder(wagonOrLocomotiveOnTile)
            }, "RemoveCarRequest", modIDs: new[] { UniqueId }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
          }
          Game1.currentLocation.playSound("coin");
          ModEntry.Helper.Input.Suppress(e.Button);
        } else if (actionButton && locomotiveOnTile is not null) {
          if (Game1.IsMasterGame) {
            TrainManager.FlipLocomotive(locomotiveOnTile);
          } else {
            ModEntry.Helper.Multiplayer.SendMessage(new CarInfo {
              Id = TrainManager.GetId(locomotiveOnTile)
            }, "FlipLocomotiveRequest", modIDs: new[] { UniqueId }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
          }
          Game1.currentLocation.playSound("coin");
          ModEntry.Helper.Input.Suppress(e.Button);
        }
      }
    }
  }

  //static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
  //  //Utility.ForEachCharacter(c => {
  //  //  if (TrainManager.IsWagonOrLocomotive(c)) {
  //  //    if (TrainManager.IsLocomotive(c)) {
  //  //      TrainManager.UpdateWagons(c);
  //  //    }
  //  //    TrainManager.StartLocomotive(c, c.currentLocation);
  //  //  }
  //  //  return true;
  //  //});
  //  //Utility.ForEachLocation(l => {
  //  //  BridgeManager.UpdateBridges(l);
  //  //  TrackUtils.UpdateTrackNeighbors(l);
  //  //  return true;
  //  //});
  //}

  //static void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e) {
  //  Utility.ForEachCharacter(c => {
  //    if (TrainManager.IsLocomotive(c)) {
  //      TrainManager.StartLocomotive(c, c.currentLocation);
  //    }
  //    return true;
  //  });
  //}

  static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e) {
    foreach (var c in Game1.currentLocation.characters) {
      if (TrainManager.IsWagonOrLocomotive(c)) {
        e.SpriteBatch.Draw(
            Game1.staminaRect,
            Game1.GlobalToLocal(Game1.viewport, c.GetBoundingBox()),
            Game1.staminaRect.Bounds, Color.Green * 0.5f, 0f, Vector2.Zero, SpriteEffects.None, 1);
        e.SpriteBatch.Draw(
            Game1.staminaRect,
            Game1.GlobalToLocal(Game1.viewport, c.Position),
            Game1.staminaRect.Bounds, Color.Red * 0.5f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1);
      }
    }
  }

  static void OnRenderedStep(object? sender, RenderedStepEventArgs e) {
    if (e.Step != RenderSteps.World_Sorted) {
      return;
    }
    var isWinter = Game1.currentLocation.IsWinterHere() && !Game1.currentLocation.IsGreenhouse;
    foreach (var bridgeData in BridgeManager.GetBridges(Game1.currentLocation)) {
      var pair = bridgeData.CoordPair;
      if (Game1.currentLocation.objects.TryGetValue(pair[0], out var bridgeObj)) {
        Flooring? trackToDraw = null;
        if (Game1.currentLocation.terrainFeatures.TryGetValue(pair[0], out var t)
          && t is Flooring f) {
          trackToDraw = f;
        }
        var trackId = trackToDraw?.modData.GetValueOrDefault(TrackUtils.AttachedTrackKey, trackToDraw.whichFloor.Value);
        FloorPathData? trackData = null;
        if (trackId is not null) { Flooring.TryGetData(trackId, out trackData); }
        FloorPathData? floorData = null;
        if (!(Game1.bigCraftableData.TryGetValue(bridgeObj.ItemId, out var itemData)
            && itemData.CustomFields?.TryGetValue($"{CpUniqueId}_BasePath", out var basePath) is true
            && Game1.floorPathData.TryGetValue(basePath, out floorData))) {
          return;
        }

        var trackTexture = trackData is not null ? (Game1.content.Load<Texture2D>(
            isWinter
            ? (trackData.WinterTexture ?? trackData.Texture) : trackData.Texture)) : null;
        var floorTexture = Game1.content.Load<Texture2D>(
            isWinter
            ? (floorData.WinterTexture ?? floorData.Texture) : floorData.Texture);
        Point? trackTextureCorner = isWinter ? trackData?.WinterCorner : trackData?.Corner;
        Point floorTextureCorner = isWinter ? floorData.WinterCorner : floorData.Corner;
        var x = pair[0].X;
        var y = pair[0].Y;
        var vertical = pair[0].X == pair[1].X;
        var horizontal = pair[0].Y == pair[1].Y;
        while (x < pair[1].X - 1 || y < pair[1].Y - 1) {
          if (vertical) {
            y++;
          }
          if (horizontal) {
            x++;
          }
          int xOffset = vertical ? 0 : 32;
          int yOffset = vertical ? 32 : 48;
          if (y == pair[0].Y + 1) {
            yOffset -= 16;
          }
          if (y == pair[1].Y - 1) {
            yOffset += 16;
          }
          if (x == pair[0].X + 1) {
            xOffset -= 16;
          }
          if (x == pair[1].X - 1) {
            xOffset += 16;
          }
          // Draw the tracks on top
          if (trackData is not null && trackTexture is not null && trackTextureCorner is not null) {
            e.SpriteBatch.Draw(
                trackTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64f, y * 64f)),
                new Rectangle(trackTextureCorner.Value.X + xOffset, trackTextureCorner.Value.Y + yOffset, 16, 16),
                bridgeData.IsTunnel ? Color.Black * .166f : Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-09f);
          }
          // Draw the base bridge floor
          if (!bridgeData.IsTunnel && floorTexture is not null) {
            e.SpriteBatch.Draw(
                floorTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64f, y * 64f)),
                new Rectangle(floorTextureCorner.X + xOffset, floorTextureCorner.Y + yOffset, 16, 16),
                Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-09f - 1E-10f);
          }
        }
      }
    }
  }

  // the default bounding box is very small; this makes the pathfinder keeps the locomotive strictly on track
  static bool NPC_GetBoundingBox_Prefix(NPC __instance, ref Rectangle __result) {
    if (TrainManager.IsWagonOrLocomotive(__instance)) {
      __result = new Rectangle(
          (int)__instance.Position.X,
          (int)__instance.Position.Y,
          Game1.tileSize,
          Game1.tileSize);
      __result.Inflate(-2, -2);
      return false;
    }
    return true;
  }

  public static string PrintDirection(int direction) {
    return direction switch {
      Game1.up => "up",
      Game1.down => "down",
      Game1.left => "left",
      Game1.right => "right",
      _ => "unknown",
    };
  }

  static bool GameLocation_IsCollidingPosition_Prefix(ref bool __result, Character character) {
    if (character is NPC npc && TrainManager.IsWagonOrLocomotive(npc)) {
      __result = false;
      return false;
    }
    return true;
  }

  static void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e) {
    foreach (var obj in e.Added) {
      if (BridgeManager.IsBridge(obj.Value)) {
        BridgeManager.UpdateBridges(e.Location);
        if (TrackUtils.HasPathAt(e.Location, obj.Key, out var f)
            && f is not null) {
          DrawNeighborManager.OnAdded(f, e.Location);
        }
        return;
      }
    }
    foreach (var obj2 in e.Removed) {
      if (BridgeManager.IsBridge(obj2.Value)) {
        BridgeManager.RemoveBridge(e.Location, obj2.Key);
      }
    }
  }

  static void OnTerrainFeatureListChanged(object? sender, TerrainFeatureListChangedEventArgs e) {
    foreach (var (tile, t) in e.Added) {
      if (t is not Flooring flooring) continue;
      if (TrackUtils.IsPath(flooring)) {
        DrawNeighborManager.OnAdded(flooring, e.Location);
      }
    }
    foreach (var (tile, t) in e.Removed) {
      if (t is not Flooring flooring) continue;
      if (TrackUtils.IsPath(flooring)) {
        DrawNeighborManager.OnRemoved(flooring, e.Location);
      }
      if (Game1.IsMasterGame
          && t.modData.TryGetValue(TrackUtils.AttachedTrackKey, out var trackId)
          && Flooring.TryGetData(trackId, out var data)
          && data.ItemId is not null) {
        e.Location.debris.Add(new Debris(ItemRegistry.Create(data.ItemId),
              tile * 64f + new Vector2(32f, 32f)));
      }
    }
  }

  static void OnNpcListChanged(object? sender, NpcListChangedEventArgs e) {
    foreach (NPC car in e.Removed) {
      if (TrainManager.IsWagon(car)
          && TrainManager.GetParentLocomotive(car) is { } locomotive) {
        // Mainly for MP where the wagons havent been updated
        TrainManager.GetWagons(locomotive).Remove(car);
      }
    }
    foreach (NPC car in e.Added) {
      if (TrainManager.IsWagon(car)
          && TrainManager.GetParentLocomotive(car) is { } locomotive) {
        // Mainly for MP where the wagons havent been updated
        TrainManager.GetWagons(locomotive).Add(car);
      }
    }
  }

  static bool NPC_draw_Prefix(NPC __instance, SpriteBatch b, float alpha = 1f) {
    if (!TrainManager.IsWagonOrLocomotive(__instance)) {
      return true;
    }
    int y = __instance.StandingPixel.Y;
    int x = __instance.StandingPixel.X;
    bool isInTunnel = BridgeManager.IsOnBridge(__instance.currentLocation, __instance.Tile, true);
    float layerDepth = Math.Max(0f, __instance.drawOnTop || isInTunnel ? 0.991f : ((float)(y - 16) / 10000f));
    if (__instance.Sprite.Texture == null) {
      Vector2 vector = Game1.GlobalToLocal(Game1.viewport, __instance.Position);
      Microsoft.Xna.Framework.Rectangle screenArea = new Microsoft.Xna.Framework.Rectangle((int)vector.X, (int)vector.Y - __instance.Sprite.SpriteWidth * 4, __instance.Sprite.SpriteWidth * 4, __instance.Sprite.SpriteHeight * 4);
      Utility.DrawErrorTexture(b, screenArea, layerDepth);
    } else if (!__instance.IsInvisible && (Utility.isOnScreen(__instance.Position, 128))) {
      b.Draw(
              __instance.Sprite.Texture,
              __instance.getLocalPosition(Game1.viewport)
              + new Vector2(
                // Draw so that the bounding box is at the center of the sprite and the bottom
                // touches the rail
                32,
                64 - 16)
              + ((__instance.shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), __instance.Sprite.SourceRect,
              isInTunnel ? Color.Black * alpha * .33f : Color.White * alpha,
              __instance.rotation, new Vector2(__instance.Sprite.SpriteWidth / 2, (float)__instance.Sprite.SpriteHeight * 3f / 4f), Math.Max(0.2f, __instance.Scale) * 4f, (__instance.flip || (__instance.Sprite.CurrentAnimation != null && __instance.Sprite.CurrentAnimation[__instance.Sprite.currentAnimationIndex].flip)) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
      //__instance.DrawGlow(b);
    }
    return false;
  }

  static bool NPC_IsVillager_Prefix(NPC __instance, ref bool __result) {
    if (TrainManager.IsWagonOrLocomotive(__instance)) {
      __result = false;
      return false;
    }
    return true;
  }

  // Remove locomotives when the day ends, and readds them on day start to make this mod safe to
  // remove at any time
  static void OnDayEnding(object? sender, DayEndingEventArgs e) {
    if (!Game1.IsMasterGame) return;
    SavedLocomotiveType savedLocomotives = new();
    Utility.ForEachLocation(l => {
      var locomotiveList = new List<SavedLocomotive>();
      foreach (var c in l.characters.ToList()) {
        if (TrainManager.IsLocomotive(c)) {
          var item = TrainManager.GetItem(c);
          StaticMonitor.Log($"saving locomotive {item} in {l.NameOrUniqueName}");
          var savedLocomotive = new SavedLocomotive(
              item,
              c.Tile,
              c.FacingDirection
              );
          var wagons = TrainManager.GetWagons(c);
          foreach (var wagon in wagons) {
            savedLocomotive.Wagons.Add(new SavedWagon(
              TrainManager.GetItem(wagon),
              wagon.Tile,
              wagon.FacingDirection
              ));
          }
          locomotiveList.Add(savedLocomotive);
          TrainManager.RemoveCar(c);
        }
      }
      if (locomotiveList.Count > 0) {
        savedLocomotives[l.NameOrUniqueName] = locomotiveList;
      }
      // Remove orphans just in case
      foreach (var c in l.characters.ToList()) {
        if (TrainManager.IsWagon(c)) {
          StaticMonitor.Log($"orphan detected in {l.NameOrUniqueName}; this shouldn't happen.", LogLevel.Warn);
          TrainManager.RemoveCar(c);
        }
      }
      return true;
    });
    Helper.Data.WriteSaveData($"{UniqueId}_SavedLocomotives", savedLocomotives);
  }

  static void OnDayStarted(object? sender, DayStartedEventArgs e) {
    if (!Game1.IsMasterGame) return;
    var savedLocomotives = Helper.Data.ReadSaveData<SavedLocomotiveType>($"{UniqueId}_SavedLocomotives");
    if (savedLocomotives is not null) {
      foreach (var (locationName, locomotiveList) in savedLocomotives) {
        StaticMonitor.Log($"restoring saved locomotives in {locationName}");
        var location = Game1.getLocationFromName(locationName, true) ?? Game1.getLocationFromName(locationName);
        if (location is null) continue;
        foreach (var locomotiveEntry in locomotiveList) {
          if (TrackUtils.HasPathAt(location, locomotiveEntry.Tile, out var _)) {
            TrainManager.AddLocomotive(locomotiveEntry.LocomotiveItem, location, locomotiveEntry.Tile, out var locomotive);
            locomotive.FacingDirection = locomotiveEntry.FacingDirection;
            foreach (var wagon in locomotiveEntry.Wagons) {
              StaticMonitor.Log($"adding back wagon {wagon.LocomotiveItem}");
              if (TrackUtils.HasPathAt(location, wagon.Tile, out var _)) {
                TrainManager.AddWagon(wagon.LocomotiveItem, locomotive, wagon.Tile, wagon.FacingDirection);
              } else {
                ModEntry.StaticMonitor.Log($"Tried adding wagon to {locationName} at {wagon.Tile}, but no track was found. The train was derailed?", LogLevel.Warn);
              }
            }
          } else {
            ModEntry.StaticMonitor.Log($"Tried adding locomotive to {locationName} at {locomotiveEntry.Tile}, but no track was found. The train was derailed?", LogLevel.Warn);
          }
        }
      }
    }
  }

  static void Character_update_Postfix(Character __instance, GameTime time, GameLocation location, long id, bool move) {
    if (__instance is not NPC npc) return;
    if (!Game1.IsMasterGame && TrainManager.IsWagonOrLocomotive(npc)) {
      if (__instance.Sprite?.CurrentAnimation != null) {
        __instance.Sprite.animateOnce(time);
      } else {
        __instance.Sprite?.faceDirection(__instance.FacingDirection);
        if (__instance.isMoving()) {
          __instance.animateInFacingDirection(time);
        } else {
          __instance.Sprite?.StopAnimation();
        }
      }
      return;
    }
    if (!Game1.IsMasterGame || !TrainManager.IsLocomotive(npc)) return;
    npc.MovePosition(time, Game1.viewport, location);
    var wagons = TrainManager.GetWagons(npc);
    foreach (var wagon in wagons) {
      wagon.MovePosition(time, Game1.viewport, location);
    }
    // still moving through rails
    if (npc.Position.X % 64 > 0 || npc.Position.Y % 64 > 0) return;
    npc.stopWithoutChangingFrame();
    foreach (var wagon in wagons) {
      wagon.stopWithoutChangingFrame();
    }
    TrackUtils.GetNextNeighbor(location, npc.Tile, npc.FacingDirection, out var nextTile, out var nextDirection);
    if (npc.Tile != nextTile) {
      npc.FacingDirection = nextDirection;
      npc.setMovingInFacingDirection();
      var currentCar = npc;
      foreach (var wagon in wagons) {
        wagon.FacingDirection = TrackUtils.GetNextDirectionTo(location, wagon.Tile, wagon.FacingDirection, currentCar.Tile);
        wagon.setMovingInFacingDirection();
        currentCar = wagon;
      }
    }
  }

  static bool Furniture_checkForAction_Prefix(Furniture __instance, ref bool __result, Farmer who, bool justCheckingForActivity) {
    if (who is null || __instance.Location is null
        || __instance.isTemporarilyInvisible || justCheckingForActivity) {
      return true;
    }
    if (__instance.QualifiedItemId == $"(F){CpUniqueId}_Collector") {
      foreach (var c in who.currentLocation.characters.ToList()) {
        if (TrainManager.IsLocomotive(c)) {
          if (Game1.IsMasterGame) {
            Game1.player.addItemsByMenuIfNecessary(TrainManager.RemoveCar(c).Select(str => ItemRegistry.Create(str)).ToList());
          } else {
            ModEntry.Helper.Multiplayer.SendMessage(new CarInfo {
              Id = TrainManager.GetId(c),
            }, "RemoveCarRequest", modIDs: new[] { UniqueId }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
          }
        }
      }
      // Remove orphaned wagons in case any of those are around
      foreach (var c in who.currentLocation.characters.ToList()) {
        if (TrainManager.IsWagon(c)) {
          if (Game1.IsMasterGame) {
            Game1.player.addItemsByMenuIfNecessary(TrainManager.RemoveCar(c).Select(str => ItemRegistry.Create(str)).ToList());
          } else {
            ModEntry.Helper.Multiplayer.SendMessage(new CarInfo {
              Id = TrainManager.GetId(c),
              WagonOrder = WagonComparer.GetOrder(c)
            }, "RemoveCarRequest", modIDs: new[] { UniqueId }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
          }
        }
      }
      Game1.currentLocation.playSound("coin");
      __result = true;
      return false;
    }
    if (__instance.QualifiedItemId == $"(F){CpUniqueId}_Catalogue") {
      Utility.TryOpenShopMenu($"{CpUniqueId}_Catalogue", who.currentLocation);
      __result = true;
      return false;
    }
    return true;
  }

  static bool SObject_isPassable_Prefix(SObject __instance, ref bool __result) {
    if (BridgeManager.IsBridge(__instance)) {
      __result = true;
      return false;
    }
    return true;
  }

  static bool NPC_canTalk_Prefix(NPC __instance, ref bool __result) {
    if (TrainManager.IsWagonOrLocomotive(__instance)) {
      __result = false;
      return false;
    }
    return true;
  }

  static bool Flooring_draw_Prefix(Flooring __instance, SpriteBatch spriteBatch) {
    if (!TrackUtils.IsPath(__instance)) {
      return true;
    }
    Vector2 tile = __instance.Tile;
    var trackId = __instance.modData.GetValueOrDefault(TrackUtils.AttachedTrackKey, __instance.whichFloor.Value);
    if (Flooring.TryGetData(trackId, out var trackData)) {
      var isWinter = __instance.Location.IsWinterHere() && !__instance.Location.IsGreenhouse;
      try {
        var texture2D = Game1.content.Load<Texture2D>(
            isWinter
            ? (trackData.WinterTexture ?? trackData.Texture) : trackData.Texture);
        if (texture2D is null) {
          return true;
        }
        Point textureCorner = isWinter ? trackData.WinterCorner : trackData.Corner;
        byte key = (byte)(DrawNeighborManager.GetNeighborMask(__instance) & 0xFu);
        int num = Flooring.drawGuide[key];
        spriteBatch.Draw(texture2D, Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64f, tile.Y * 64f)), new Microsoft.Xna.Framework.Rectangle(textureCorner.X + num * 16 % 256, num / 16 * 16 + textureCorner.Y, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tile.Y * 64f + 2f + tile.X / 10000f + 1f) / 20000f);
      }
      catch (Exception e) {
        ModEntry.StaticMonitor.Log(e.ToString(), LogLevel.Error);
      }
    }
    // If regular flooring do the rest of the draw code, otherwise exit
    return __instance.modData.ContainsKey(TrackUtils.AttachedTrackKey);
  }

  static void Furniture_loadDescription_Postfix(Furniture __instance, ref string __result) {
    if (__instance.QualifiedItemId == $"(F){CpUniqueId}_Collector") {
      __result = Game1.content.LoadString($"Strings/Objects:{CpUniqueId}_Collector");
    }
    if (__instance.QualifiedItemId == $"(F){CpUniqueId}_Catalogue") {
      __result = Game1.content.LoadString($"Strings/Objects:{CpUniqueId}_Catalogue");
    }
  }

  static void Flooring_initNetFields_Postfix(Flooring __instance) {
    __instance.modData.OnValueAdded += (string key, string value) => {
      if (__instance.Location is not null && key == TrackUtils.AttachedTrackKey) {
        DrawNeighborManager.OnAdded(__instance);
      }
    };
  }

  static bool SObject_draw_Prefix(SObject __instance, SpriteBatch spriteBatch, int x, int y, float alpha) {
    if (!BridgeManager.IsBridge(__instance) || __instance.isTemporarilyInvisible || !__instance.bigCraftable.Value) return true;
    Vector2 drawCorner = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 - 64, y * 64 - 64));
    var dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
    try {
      var textureName = dataOrErrorItem.GetTextureName();
      var textureFront = Game1.content.Load<Texture2D>(textureName + "_Front");
      var textureBack = Game1.content.Load<Texture2D>(textureName + "_Back");
      var sheetIndex = dataOrErrorItem.SpriteIndex;
      float layer = Math.Max(0f, (float)((y + 1) * 64 - 12) / 10000f);
      // Front layer
      spriteBatch.Draw(
          textureFront,
          drawCorner,
          Game1.getSourceRectForStandardTileSheet(textureFront, sheetIndex, 16 * 3, 16 * 2),
          Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layer);
      // Back layer
      spriteBatch.Draw(
          textureBack,
          drawCorner,
          Game1.getSourceRectForStandardTileSheet(textureBack, sheetIndex, 16 * 3, 16 * 2),
          Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layer - 64 / 10000f);
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log(e.ToString(), LogLevel.Error);
    }
    return false;
  }

  class AddCarInfo {
    public string ItemId = "";
    public string LocomotiveId = "";
    public string LocationName = "";
    public Vector2 Tile = Vector2.Zero;
  }

  class CarInfo {
    public string Id = "";
    public int WagonOrder = -1;
  }

  static void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e) {
    if (e.FromModID != UniqueId) return;
    switch (e.Type) {
      case "FlipLocomotiveRequest":
        CarInfo message = e.ReadAs<CarInfo>();
        Utility.ForEachCharacter(c => {
          if (TrainManager.IsLocomotive(c)
              && TrainManager.GetId(c) == message.Id) {
            TrainManager.FlipLocomotive(c);
            return false;
          }
          return true;
        });
        break;
      case "RemoveCarRequest":
        CarInfo message2 = e.ReadAs<CarInfo>();
        Utility.ForEachCharacter(c => {
          if (c.modData.GetValueOrDefault(TrainManager.LocomotiveUniqueIdKey) == message2.Id
              && ((TrainManager.IsLocomotive(c) && message2.WagonOrder == -1)
                || (TrainManager.IsWagon(c) && message2.WagonOrder == WagonComparer.GetOrder(c)))) {
            var items = TrainManager.RemoveCar(c);
            ModEntry.Helper.Multiplayer.SendMessage(items, "AddItemRequest",
                modIDs: new[] { UniqueId }, playerIDs: new[] { e.FromPlayerID });
            return false;
          }
          return true;
        });
        break;
      case "AddCarRequest":
        AddCarInfo message3 = e.ReadAs<AddCarInfo>();
        var location = Game1.getLocationFromName(message3.LocationName, true) ?? Game1.getLocationFromName(message3.LocationName);
        if (location is null) {
          break;
        }
        if (!string.IsNullOrEmpty(message3.LocomotiveId)) {
          foreach (var c in location.characters) {
            if (TrainManager.IsLocomotive(c)
                && TrainManager.GetId(c) == message3.LocomotiveId
                && TrainManager.AddWagon(message3.ItemId, c)) { }
          }
        } else if (TrainManager.AddLocomotive(message3.ItemId, location, message3.Tile, out var _)) { }
        break;
      case "AddItemRequest":
        Game1.player.addItemsByMenuIfNecessary(e.ReadAs<List<string>>().Select(str => ItemRegistry.Create(str)).ToList());
        break;
      default:
        break;
    }
  }
}

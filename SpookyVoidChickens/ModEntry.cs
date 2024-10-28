using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Extensions;
using StardewValley.GameData.FarmAnimals;
using Microsoft.Xna.Framework;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.SpookyVoidChickens;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;

  private ModConfig Config = null!;

  //private ModConfig Config;

  public override void Entry(IModHelper helper) {
    this.Config = helper.ReadConfig<ModConfig>();
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

//    helper.Events.GameLoop.GameLaunched += OnGameLaunched;

    helper.Events.Player.Warped += OnWarped;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
    helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
    helper.Events.GameLoop.TimeChanged += OnTimeChanged;
    helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

    var harmony = new Harmony(this.ModManifest.UniqueID);
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.performUseAction)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.SObject_performUseAction_prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Farmer),
          nameof(Farmer.OnItemReceived)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Farmer_OnItemReceived_postfix)));
  }

  // State stuff
  // Whether the host has entered the arena
  static bool enteredArena = false;
  // Whether the arena should start next
  static bool arenaStarting = false;
  // If set, whether the arena is active and spawning enemies
  static int? arenaStartTime = null;
  static int eggsCollected = 0;

  // Host farmer ModData key
  static string VoidArenaHighScoreKey { get { return $"{UniqueId}.VoidArenaHighScore"; } }

  // Items from CP Component
  const string VoidTotemItem = "(O)selph.SpookyVoidChickensCP.VoidTotem";
  const string GooseTotemItem = "(O)selph.SpookyVoidChickensCP.GooseTotem";
  const string VoidArenaLocation = "selph.SpookyVoidChickensCP.VoidCave";
  const string DIMENSIONLocation = "selph.SpookyVoidChickensCP.DIMENSION";
  // debug warp selph.SpookyVoidChickensCP.VoidCave 9 9

  // Multiplayer messages
  // Tells farmhands to warp home
  static string WarpHomeMessageType { get { return $"{UniqueId}.WarpHome"; } }
  // Tells farmhands to warp to arena
  static string WarpToArenaMessageType { get { return $"{UniqueId}.WarpToArena"; } }
  // Farmhands tell host about their collected eggs
  static string FarmhandEggsCollectedMessageType { get { return $"{UniqueId}.FarmhandEggsCollected"; } }
  // Sends high score to farmhands
  static string ShowHighScoreMsgMessageType { get { return $"{UniqueId}.ShowHighScoreMsg"; } }

  private static bool IsNormalGameplay() {
    return
      Context.IsWorldReady
//      && Context.CanPlayerMove
      && Game1.player != null
//      && !Game1.player.isRidingHorse()
      && Game1.currentLocation != null
      && !Game1.eventUp
      && !Game1.isFestival()
      && !Game1.IsFading();
  }

  static string SpawnPointsTileProperty { get { return $"{UniqueId}.SpawnPoints"; } }
  static string GooseChestTileProperty { get { return $"{UniqueId}.GooseChest"; } }
  static List<Vector2> MonsterSpawnTiles = [];

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
    var location = Game1.getLocationFromName(VoidArenaLocation);
    if (location is not null) {
      MonsterSpawnTiles = [];
      for (int i = 0; i < location.map.Layers[0].LayerWidth; i++) {
        for (int j = 0; j < location.map.Layers[0].LayerHeight; j++) {
          if (location.doesTileHaveProperty(i, j, SpawnPointsTileProperty, "Back") != null) {
            MonsterSpawnTiles.Add(new Vector2(i, j));
          }
        }
      }
    }
  }

  //private void OnLocationListChanged(object? sender, LocationListChangedEventArgs e) {
  //  foreach (var location in e.Added) {
  //    if (location.Name == VoidArenaLocation) {
  //      MonsterSpawnTiles = [];
  //      for (int i = 0; i < location.map.Layers[0].LayerWidth; i++) {
  //        for (int j = 0; j < location.map.Layers[0].LayerHeight; j++) {
  //          if (location.doesTileHaveProperty(i, j, SpawnPointsTileProperty, "Back") != null) {
  //            MonsterSpawnTiles.Add(new Vector2(i, j));
  //          }
  //        }
  //      }
  //    }
  //    if (location.Name == DIMENSIONLocation) {
  //      for (int i = 0; i < location.map.Layers[0].LayerWidth; i++) {
  //        for (int j = 0; j < location.map.Layers[0].LayerHeight; j++) {
  //          if (location.doesTileHaveProperty(i, j, SpawnPointsTileProperty, "Back") != null) {
  //            location.addCharacter(new ShadowBrute(new Vector2(i*64f, j*64f)) {
  //                speed = 10,
  //                MaxHealth = 99999,
  //                Health = 99999,
  //                Sprite = new AnimatedSprite("selph.SpookyVoidChickensCP/GOOSE"),
  //                });
  //          }
  //        }
  //      }
  //    }
  //  }
  //}

  private void OnDayStarted(object? sender, DayStartedEventArgs e) {
    if (!Context.IsMainPlayer) {
      return;
    }
    arenaStartTime = null;
    enteredArena = false;
    arenaStarting = false;
    eggsCollected = 0;
    var DIMENSION = Game1.getLocationFromName(DIMENSIONLocation);
    if (DIMENSION is not null) {
      DIMENSION.characters.RemoveWhere((NPC npc) => npc is Monster);
      for (int i = 0; i < DIMENSION.map.Layers[0].LayerWidth; i++) {
        for (int j = 0; j < DIMENSION.map.Layers[0].LayerHeight; j++) {
          if (DIMENSION.doesTileHaveProperty(i, j, SpawnPointsTileProperty, "Back") != null) {
            DIMENSION.addCharacter(new ShadowBrute(new Vector2(i*64f, j*64f)) {
                speed = 10,
                MaxHealth = 99999,
                Health = 99999,
                DamageToFarmer = 10,
                Sprite = new AnimatedSprite("selph.SpookyVoidChickensCP/GOOSE", 0, 32, 32),
                });
          }
          if (DIMENSION.doesTileHaveProperty(i, j, GooseChestTileProperty, "Back") != null) {
            DIMENSION.objects.Remove(new Vector2(i, j));
            DIMENSION.objects.Add(new Vector2(i, j), new Chest(new List<Item> {
                  ItemRegistry.Create("(BC)selph.SpookyVoidChickensCP.GooseStatue"),
                  ItemRegistry.Create("(O)688"),
                  }, new Vector2(i, j)));
          }
        }
      }
    }
  }
  
  // Only run on host
  private static bool UpdateHighScore(out int highScore) {
    highScore = 0;
    if (Game1.player.modData.TryGetValue(VoidArenaHighScoreKey, out var highScoreStr) &&
        Int32.TryParse(highScoreStr, out int hs)) {
      highScore = hs;
    }
    if (eggsCollected < 50) {
      Game1.player.mailForTomorrow.Add("selph.SpookyVoidChickensCP.RewardBronze");
    } else if (eggsCollected < 100) {
      Game1.player.mailForTomorrow.Add("selph.SpookyVoidChickensCP.RewardSilver");
    } else if (eggsCollected < 200) {
      Game1.player.mailForTomorrow.Add("selph.SpookyVoidChickensCP.RewardGold");
    } else {
      Game1.player.mailForTomorrow.Add("selph.SpookyVoidChickensCP.RewardMAX");
      if (Game1.player.hasOrWillReceiveMail("selph.SpookyVoidChickensCP.VoidTribe")) {
        Game1.player.mailForTomorrow.Add("selph.SpookyVoidChickensCP.VoidTribe2");
      }
    }
    if (!Game1.player.hasOrWillReceiveMail("selph.SpookyVoidChickensCP.VoidTribe")) {
      Game1.addMailForTomorrow("selph.SpookyVoidChickensCP.VoidTribe");
    }
    if (eggsCollected > highScore) {
      Game1.player.modData[VoidArenaHighScoreKey] = eggsCollected.ToString();
      return true;
    }
    return false;
  }

  private void OnWarped(object? sender, WarpedEventArgs e) {
    if (!Context.IsMainPlayer) {
      if (e.NewLocation.Name == VoidArenaLocation) {
        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidArena.prepare")));
      }
      return;
    }
    // Left arena early, clean up and stuff
    if (e.OldLocation.Name == VoidArenaLocation) {
      if (arenaStartTime is not null) {
        arenaStartTime = null;
        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidArena.leftEarly")));
        UpdateHighScore(out int highScore);
        DelayedAction.functionAfterDelay(() => {
            Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidArena.highScore", new { eggsCollected = eggsCollected, highScore = highScore } )));
        }, 1000);
        e.OldLocation.characters.RemoveWhere((NPC c) => c is Monster);
        Helper.Multiplayer.SendMessage(true, WarpHomeMessageType, [UniqueId], Game1.getAllFarmhands().Select((Farmer f) => f.UniqueMultiplayerID).ToArray());
        DelayedAction.functionAfterDelay(() => {
            Helper.Multiplayer.SendMessage(new HighScoreMsg {eggsCollected = eggsCollected, highScore = highScore}, ShowHighScoreMsgMessageType, [UniqueId], Game1.getAllFarmhands().Select((Farmer f) => f.UniqueMultiplayerID).ToArray());
        }, 3500);
      } else {
        // Returned triumphantly
        UpdateHighScore(out int highScore);
        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidArena.highScore", new { eggsCollected = eggsCollected, highScore = highScore } )));
        Helper.Multiplayer.SendMessage(new HighScoreMsg {eggsCollected = eggsCollected, highScore = highScore}, ShowHighScoreMsgMessageType, [UniqueId], Game1.getAllFarmhands().Select((Farmer f) => f.UniqueMultiplayerID).ToArray());
      }
    }
    // Start arena logic
    if (e.NewLocation.Name != VoidArenaLocation) {
      return;
    }
    enteredArena = true;
    arenaStarting = true;
    Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidArena.prepare")));
  }

  private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e) {
    if (!IsNormalGameplay() || e.FromModID != UniqueId) {
      return;
    }
    if (!Context.IsMainPlayer && e.Type == WarpHomeMessageType) {
      Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get(e.ReadAs<bool>() ? "VoidArena.hostLeftEarly" : "VoidArena.finished")));
      Game1.delayedActions.Add(new DelayedAction(3000, () => {
        Game1.player.completelyStopAnimatingOrDoingAction();
        totemWarp(Game1.player, "Farm");
      }));
    }
    if (!Context.IsMainPlayer && e.Type == WarpToArenaMessageType) {
      Game1.player.completelyStopAnimatingOrDoingAction();
      totemWarp(Game1.player, VoidArenaLocation);
    }
    if (Context.IsMainPlayer && e.Type == FarmhandEggsCollectedMessageType) {
      var eggsAdded = e.ReadAs<int>();
      eggsCollected += eggsAdded;
    }
    if (!Context.IsMainPlayer && e.Type != ShowHighScoreMsgMessageType) {
      var msg = e.ReadAs<HighScoreMsg>();
      Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidArena.highScore", new { eggsCollected = msg.eggsCollected, highScore = msg.highScore } )));
    }
  }

  class HighScoreMsg {
    public int eggsCollected = 0;
    public int highScore = 0;
  }

  private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e) {
    if (!IsNormalGameplay() || !Context.IsMainPlayer || Game1.currentLocation.Name != VoidArenaLocation || arenaStartTime is null || !Game1.shouldTimePass()) {
      return;
    }
    // Starts at 0 and ramps to 1 over 2 hours
    float difficultyMultiplier = (float)(Game1.timeOfDay - (arenaStartTime ?? Game1.timeOfDay)) / 200;
    if (difficultyMultiplier <= 1.0) {
      // starts at 1 per second, ramps to 10 per second by game end
      int monstersToSpawn = (int)(1 + difficultyMultiplier * 9);
      StaticMonitor.Log($"Spawning {monstersToSpawn} monsters", LogLevel.Trace);
      for (int i = 0; i < monstersToSpawn; i++) {
        var tile = Game1.random.ChooseFrom(MonsterSpawnTiles);
        bool hasGoose = Game1.currentLocation.characters.Any((NPC c) => c.Name == "GOOSE" && c is Monster);
        bool hasBigOne = Game1.currentLocation.characters.Any((NPC c) => c.Name == "Big Void Chicken" && c is Monster);
        Monster? monster;
        if (!hasGoose && Game1.random.NextDouble() < 0.0001) {
          monster = new ShadowBrute(tile*64f) {
            focusedOnFarmers = true,
            speed = 10,
            MaxHealth = 99999,
            Health = 99999,
            DamageToFarmer = 0,
            Sprite = new AnimatedSprite("selph.SpookyVoidChickensCP/GOOSE", 0, 32, 32),
          };
        } else if (!hasBigOne && difficultyMultiplier >= 0.5) {
          monster = new ShadowBrute(tile * 64f) {
            Name = "Big Void Chicken",
            focusedOnFarmers = true,
            speed = 1,
            Sprite = new AnimatedSprite("selph.SpookyVoidChickensCP/Big", 0, 32, 32),
            MaxHealth = 1000,
            Health = 1000,
            DamageToFarmer = 20 + (int)(difficultyMultiplier * 10),
          };
        } else if (Game1.random.NextDouble() < 0.2) {
          monster = new Bat(tile * 64f) {
            Name = "Flying Void Chicken",
            focusedOnFarmers = true,
            //            speed = (int)(2 + difficultyMultiplier * 2),
            Sprite = new AnimatedSprite("selph.SpookyVoidChickensCP/Bat", 0, 16, 16),
            MaxHealth = 120 + (int)(difficultyMultiplier * 60),
            Health = 120 + (int)(difficultyMultiplier * 60),
            DamageToFarmer = 10 + (int)(difficultyMultiplier * 10)
          };
        } else {
          monster = new ShadowBrute(tile * 64f) {
            Name = "Walking Void Chicken",
                 focusedOnFarmers = true,
                 speed = (int)(2 + difficultyMultiplier * 2),
                 Sprite = new AnimatedSprite("selph.SpookyVoidChickensCP/Shadow Brute", 0, 16, 16),
                 MaxHealth = 160 + (int)(difficultyMultiplier * 80),
                 Health = 160 + (int)(difficultyMultiplier * 80),
                 DamageToFarmer = 10 + (int)(difficultyMultiplier * 10),
          };
        }
        monster.objectsToDrop.Clear();
        // Void egg
        monster.objectsToDrop.Add(("305"));
        if (monster.Name == "Big Void Chicken") {
          for (int j = 0; j < 18; j++) {
            monster.objectsToDrop.Add(("305"));
          }
        }
        // Void essence 1 to 3
        for (int j = 0; j < Game1.random.Next(3); j++) {
          monster.objectsToDrop.Add(("769"));
        }
        // Farm totem (1% chance) in case they wanna run lmao
        if (Game1.random.NextDouble() < 0.01) {
          monster.objectsToDrop.Add(("688"));
        }
        Game1.currentLocation.addCharacter(monster);
      }
    }
  }

  private void OnTimeChanged(object? sender, TimeChangedEventArgs e) {
    if (!IsNormalGameplay() || !Context.IsMainPlayer || Game1.currentLocation.Name != VoidArenaLocation) {
      return;
    }
    if (arenaStarting) {
      Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidArena.begin")));
      arenaStartTime = e.NewTime;
      arenaStarting = false;
    } else if (arenaStartTime is not null && e.NewTime - arenaStartTime >= 200) {
      arenaStartTime = null;
      Game1.currentLocation.characters.RemoveWhere((NPC c) => c is Monster);
      Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidArena.finished")));
      Helper.Multiplayer.SendMessage(false, WarpHomeMessageType, [UniqueId], Game1.getAllFarmhands().Select((Farmer f) => f.UniqueMultiplayerID).ToArray());
      Game1.player.modData[VoidArenaHighScoreKey] = eggsCollected.ToString();
      Game1.delayedActions.Add(new DelayedAction(3000, () => {
            Game1.player.completelyStopAnimatingOrDoingAction();
            totemWarp(Game1.player, "Farm");
      }));
    }
  }

  // Mainly copied from game code
  private static void totemWarp(Farmer who, string location) {
    GameLocation currentLocation = who.currentLocation;
    for (int i = 0; i < 12; i++)
    {
      Game1.Multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)), flicker: false, Game1.random.NextBool()));
    }
    who.playNearbySoundAll("wand");
    Game1.displayFarmer = false;
    Game1.player.temporarilyInvincible = true;
    Game1.player.temporaryInvincibilityTimer = -2000;
    Game1.player.freezePause = 1000;
    Game1.flashAlpha = 1f;
    DelayedAction.fadeAfterDelay(() => totemWarpForReal(location), 1000);
    Microsoft.Xna.Framework.Rectangle rectangle = who.GetBoundingBox();
    new Microsoft.Xna.Framework.Rectangle(rectangle.X, rectangle.Y, 64, 64).Inflate(192, 192);
    int num = 0;
    Point tilePoint = who.TilePoint;
    for (int num2 = tilePoint.X + 8; num2 >= tilePoint.X - 8; num2--) {
      Game1.Multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(6, new Vector2(num2, tilePoint.Y) * 64f, Color.White, 8, flipped: false, 50f)
          {
          layerDepth = 1f,
          delayBeforeAnimationStart = num * 25,
          motion = new Vector2(-0.25f, 0f)
          });
      num++;
    }
    // Warp nearby farmers as well
    if (location == VoidArenaLocation && Context.IsMainPlayer) {
      foreach (var farmer in Utility.GetPlayersWithinDistance(Game1.player.Tile, 5, Game1.currentLocation)) {
        if (farmer != Game1.player) {
          Helper.Multiplayer.SendMessage("", WarpToArenaMessageType, [UniqueId], [farmer.UniqueMultiplayerID]);
        }
      }
    }
  }

  private static void totemWarpForReal(string location) {
    int x = 0, y = 0;
    Utility.getDefaultWarpLocation(location, ref x, ref y);
    Game1.warpFarmer(location, x, y, flip: false);
		Game1.fadeToBlackAlpha = 0.99f;
		Game1.screenGlow = false;
		Game1.player.temporarilyInvincible = false;
		Game1.player.temporaryInvincibilityTimer = 0;
		Game1.displayFarmer = true;
  }

  static bool SObject_performUseAction_prefix(SObject __instance, ref bool __result, GameLocation location) {
    if (__instance.QualifiedItemId != VoidTotemItem &&
        __instance.QualifiedItemId != GooseTotemItem) {
      return true;
    }
    bool normalGameplay = !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack && !Game1.player.swimming.Value && !Game1.player.bathingClothes.Value && !Game1.player.onBridge.Value;
  	if (!Game1.player.canMove || __instance.isTemporarilyInvisible || !normalGameplay) {
      __result = false;
      return false;
    }
    if (__instance.QualifiedItemId == VoidTotemItem) {
      if (enteredArena) {
        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidTotem.alreadyVisited")));
        __result = false;
        return false;
      }
      if (!Context.IsMainPlayer) {
        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("VoidTotem.onlyHost")));
        __result = false;
        return false;
      }
    }
    Game1.player.jitterStrength = 1f;
    Color glowColor = (__instance.QualifiedItemId == VoidTotemItem) ? Color.Black : Color.White;
    location.playSound("warrior");
    Game1.player.faceDirection(2);
    Game1.player.CanMove = false;
    Game1.player.temporarilyInvincible = true;
    Game1.player.temporaryInvincibilityTimer = -4000;
    Game1.changeMusicTrack("silence");
    Game1.player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[2]
        {
        new FarmerSprite.AnimationFrame(57, 2000, secondaryArm: false, flip: false),
        new FarmerSprite.AnimationFrame((short)Game1.player.FarmerSprite.CurrentFrame, 0, secondaryArm: false, flip: false,
            (Farmer f) => totemWarp(f, __instance.QualifiedItemId == VoidTotemItem ? VoidArenaLocation : DIMENSIONLocation), behaviorAtEndOfFrame: true)
        });
    TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(0f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f) {
      motion = new Vector2(0f, -1f),
             scaleChange = 0.01f,
             alpha = 1f,
             alphaFade = 0.0075f,
             shakeIntensity = 1f,
             initialPosition = Game1.player.Position + new Vector2(0f, -96f),
             xPeriodic = true,
             xPeriodicLoopTime = 1000f,
             xPeriodicRange = 4f,
             layerDepth = 1f
    };
    temporaryAnimatedSprite.CopyAppearanceFromItemId(__instance.QualifiedItemId);
    Game1.Multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
    temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(-64f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f) {
      motion = new Vector2(0f, -0.5f),
             scaleChange = 0.005f,
             scale = 0.5f,
             alpha = 1f,
             alphaFade = 0.0075f,
             shakeIntensity = 1f,
             delayBeforeAnimationStart = 10,
             initialPosition = Game1.player.Position + new Vector2(-64f, -96f),
             xPeriodic = true,
             xPeriodicLoopTime = 1000f,
             xPeriodicRange = 4f,
             layerDepth = 0.9999f
    };
    temporaryAnimatedSprite.CopyAppearanceFromItemId(__instance.QualifiedItemId);
    Game1.Multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
    temporaryAnimatedSprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(64f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f) {
      motion = new Vector2(0f, -0.5f),
             scaleChange = 0.005f,
             scale = 0.5f,
             alpha = 1f,
             alphaFade = 0.0075f,
             delayBeforeAnimationStart = 20,
             shakeIntensity = 1f,
             initialPosition = Game1.player.Position + new Vector2(64f, -96f),
             xPeriodic = true,
             xPeriodicLoopTime = 1000f,
             xPeriodicRange = 4f,
             layerDepth = 0.9988f
    };
    temporaryAnimatedSprite.CopyAppearanceFromItemId(__instance.QualifiedItemId);
    Game1.Multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
    Game1.screenGlowOnce(glowColor, hold: false);
    Utility.addSprinklesToLocation(location, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, 16, 16, 1300, 20, Color.White, null, motionTowardCenter: true);
    __result = true;
    return false;
  }

  static void Farmer_OnItemReceived_postfix(Farmer __instance, Item item, int countAdded, Item mergedIntoStack, bool hideHudNotification = false) {
    if (Game1.currentLocation.Name != VoidArenaLocation || item.QualifiedItemId != "(O)305" || item.hasbeenInInventory.Value) {
      return;
    }
    if (Context.IsMainPlayer) {
      eggsCollected += countAdded;
    } else {
      Helper.Multiplayer.SendMessage(countAdded, FarmhandEggsCollectedMessageType, [UniqueId], [Game1.MasterPlayer.UniqueMultiplayerID]);
    }
  }

  //private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
  //  // get Generic Mod Config Menu's API (if it's installed)
  //  var configMenu = Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
  //  if (configMenu is null)
  //    return;

  //  // register mod
  //  configMenu.Register(
  //      mod: this.ModManifest,
  //      reset: () => this.Config = new ModConfig(),
  //      save: () => {
  //        Helper.WriteConfig(this.Config);
  //        Helper.GameContent.InvalidateCache("selph.ExtraAnimalConfig/AnimalExtensionData");
  //      });

  //  // add some config options
  //  configMenu.AddBoolOption(
  //      mod: this.ModManifest,
  //      name: () => Helper.Translation.Get("config.EvenMoreSpookyVoidChickens.name"),
  //      tooltip: () => Helper.Translation.Get("config.EvenMoreSpookyVoidChickens.description"),
  //      getValue: () => this.Config.EvenMoreSpookyVoidChickens,
  //      setValue: value => this.Config.EvenMoreSpookyVoidChickens = value
  //      );
  //}
}

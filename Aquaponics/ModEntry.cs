using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Extensions;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LeFauxMods.Common.Integrations.CustomBush;
using ItemExtensions;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Aquaponics;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;

  public static ModConfig Config = null!;
  public static IAquaponicsApi api = null!;
  public static ICustomBushApi? cbApi = null;
  public static IItemExtensionsApi? ieApi = null;

  public static string AquaponicsFishPondBuilding = null!;
  public static string AlreadyDoubled = null!;

  public override object GetApi() {
      return api;
   }

  public override void Entry(IModHelper helper) {
    api = new AquaponicsApi();
    Config = helper.ReadConfig<ModConfig>();
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;
    AquaponicsFishPondBuilding =  $"{UniqueId}_AquaponicsFishPond";
    AlreadyDoubled =  $"{UniqueId}_AlreadyDoubled";

    ImageAssetsManager.RegisterEvents(helper);
    helper.Events.Content.AssetRequested += OnAssetRequested;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

    // Harmony!
    var harmony = new Harmony(this.ModManifest.UniqueID);
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(FishPond),
          nameof(FishPond.doAction)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.FishPond_doAction_Prefix)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.FishPond_doAction_Postfix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(FishPond),
          nameof(FishPond.draw)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.FishPond_draw_Postfix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(FishPond),
          nameof(FishPond.drawInMenu)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.FishPond_drawInMenu_Postfix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(HoeDirt),
          nameof(HoeDirt.GetFertilizerSpeedBoost)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.HoeDirt_GetFertilizerSpeedBoost_Postfix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(HoeDirt),
          nameof(HoeDirt.GetFertilizerQualityBoostLevel)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.HoeDirt_GetFertilizerQualityBoostLevel_Postfix)));

    PondQueryMenuPatcher.ApplyPatches(harmony);

    // Debug command
    helper.ConsoleCommands.Add(
        $"{UniqueId}_OpenCropsChest",
        Helper.Translation.Get("Command.openCropsChest"),
        OpenCropsChest);
    helper.ConsoleCommands.Add(
        $"{UniqueId}_DowngradeAllAquaponicsPonds",
        Helper.Translation.Get("Command.downgradeAllAquaponicsPonds"),
        DowngradeAllPonds);
    helper.ConsoleCommands.Add(
        $"{UniqueId}_DowngradePond",
        Helper.Translation.Get("Command.downgradePond"),
        DowngradePond);
  }

  public static bool IsAquaponicsPond(Building building, [NotNullWhen(true)] out FishPond? pond) {
    if (building is FishPond p && building.buildingType.Value == AquaponicsFishPondBuilding) {
      pond = p;
      return true;
    }
    pond = null;
    return false;
  }

  void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo($"Data/Buildings")) {
      e.Edit(asset => {
        var data = asset.AsDictionary<string, BuildingData>().Data;
        if (data.TryGetValue("Fish Pond", out var fishPondData)) {
          fishPondData.UpgradeSignTile = new(0, 0);
          data[AquaponicsFishPondBuilding] = new() {
            Name = Helper.Translation.Get("AquaponicsFishPond.name"),
            Description = Helper.Translation.Get("AquaponicsFishPond.description"),
            Texture = fishPondData.Texture,
            Size = {
              X = fishPondData.Size.X,
              Y = fishPondData.Size.Y,
            },
            BuildingType = "StardewValley.Buildings.FishPond",
            Builder = fishPondData.Builder,
            BuildDays = 2,
            BuildCost = 5000,
            BuildMaterials = new() {
              new() {
                // Wood
                ItemId = "(O)388",
                Amount = 50
              },
              new() {
                // Iron
                ItemId = "(O)335",
                Amount = 10
              },
              new() {
                // Gold
                ItemId = "(O)336",
                Amount = 5
              },
            },
            BuildingToUpgrade = "Fish Pond",
            Chests = new() {
              // The type doesn't really matter, all logic is custom
              new() {
                Id = FishPondCropManager.CropsChestName,
                Type = BuildingChestType.Load,
              },
              new() {
                Id = FishPondCropManager.OutputChestName,
                Type = BuildingChestType.Collect,
              }
            },
            MaxOccupants = -1,
          };
        } else {
          Monitor.Log($"IMPOSSIBLE: No fish pond building detected?", LogLevel.Error);
        }
      }, AssetEditPriority.Late + 1);
    }
  }
  
  void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    try {
      cbApi = Helper.ModRegistry.GetApi<ICustomBushApi>("furyx639.CustomBush");
    } catch (Exception exception) {
      Monitor.Log($"Error registering the Custom Bush API: {exception.ToString()}", LogLevel.Warn);
    }
    try {
      ieApi = Helper.ModRegistry.GetApi<IItemExtensionsApi>("mistyspring.ItemExtensions");
    } catch (Exception exception) {
      Monitor.Log($"Error registering the Item Extensions API: {exception.ToString()}", LogLevel.Warn);
    }

    // get Generic Mod Config Menu's API (if it's installed)
    var configMenu = Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
    if (configMenu is null) 
      return;

    // register mod
    configMenu.Register(
        mod: this.ModManifest,
        reset: () => Config = new ModConfig(),
        save: () => {
          Helper.WriteConfig(Config);
        });

    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.seedCount.name"),
        tooltip: () => Helper.Translation.Get("Config.seedCount.description"),
        getValue: () => Config.SeedCount,
        setValue: value => { Config.SeedCount = value; },
        min: 1
    );
  }

  // If there is planted crop, make fish spawn faster and give a small chance to double stack
  void OnDayStarted(object? sender, DayStartedEventArgs e) {
    Utility.ForEachBuilding(building => {
      if (IsAquaponicsPond(building, out var pond) &&
          FishPondCropManager.TryGetOnePot(pond, out var pot) &&
          (pot.hoeDirt.Value.crop is not null || pot.bush.Value is not null || pot.heldObject.Value is not null)) {
        FishPondCropManager.DayUpdateHoeDirt(pond);
        if (Game1.random.NextBool(0.25)) {
          pond.daysSinceSpawn.Value += 1;
        }
        if (pond.output.Value is not null &&
            !pond.output.Value.modData.ContainsKey(AlreadyDoubled) &&
            Game1.random.NextBool(0.50)) {
          pond.output.Value.Stack = (int)(pond.output.Value.Stack * (pond.goldenAnimalCracker.Value ? 1.5 : 2));
          pond.output.Value.modData[AlreadyDoubled] = "";
        }
      }
      return true;
    });
  }

  // Initialize our pots with data that was expected to be populated for regular location pots
  void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
    FishPondCropManager.SaveLoaded();
  }

  // Harvest crops if no output
  static bool FishPond_doAction_Prefix(FishPond __instance, ref bool __result, out bool __state, Vector2 tileLocation, Farmer who) {
    if (!IsAquaponicsPond(__instance, out var _) ||
        who.isRidingHorse() ||
        __instance.daysOfConstructionLeft.Value > 0 ||
        !__instance.occupiesTile(tileLocation) ||
        __instance.output.Value is not null
        ) {
      __state = false;
      return true;
    }
    __state = true;
    if (FishPondCropManager.HarvestCrops(__instance, who, out var farmingExp, out var foragingExp)) {
      Utility.CollectSingleItemOrShowChestMenu(FishPondCropManager.GetFishPondOutputChest(__instance));
      who.gainExperience(Farmer.farmingSkill, farmingExp);
      who.gainExperience(Farmer.foragingSkill, foragingExp);
      __result = true;
      return false;
    }
    return true;
  }

  // If nothing happened, do our seed thingy
  static void FishPond_doAction_Postfix(FishPond __instance, ref bool __result, bool __state, Vector2 tileLocation, Farmer who) {
    if (__result || !__state) return;
    if (who.ActiveObject is not null) {
      __result = FishPondCropManager.PlantCrops(__instance, who.ActiveObject, who, true);
      if (__result && who.ActiveObject.Stack <= 0) {
        who.removeItemFromInventory(who.ActiveObject);
        who.showNotCarrying();
      }
    }
  }

  static void FishPond_draw_Postfix(FishPond __instance, SpriteBatch b) {
    if (!IsAquaponicsPond(__instance, out var _)) return;
    var layerDepth = (((float)__instance.tileY.Value + 0.5f) * 64f + 1f + 2f) / 10000f;
    var scaleModifier = (__instance.tilesHigh.Value / 5f);
    var scale = Game1.pixelZoom * scaleModifier;
    // Draw crops
    Vector2 topLeft = new Vector2(__instance.tileX.Value, __instance.tileY.Value) * 64f;
    var cropsChest = FishPondCropManager.GetFishPondCropsChest(__instance);
    if (cropsChest is not null && FishPondCropManager.TryGetOnePot(__instance, out var pot)) {
      int potIndexToDraw = 0;
      for (int x = 1; x < 5; x++) {
        for (int y = 1; y < 3; y++) {
          var yBob = (float)(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500.0 + (double)(x * 64)) * 4.0);
          var position = topLeft + new Vector2(x*64f, y*64f + yBob) * scaleModifier;
          if (pot.hoeDirt.Value.crop is not null) {
            var crop = pot.hoeDirt.Value.crop;
            b.Draw(
                crop.DrawnCropTexture,
                Game1.GlobalToLocal(Game1.viewport, position),
                crop.sourceRect,
                (crop.currentPhase.Value == 0 && crop.shouldDrawDarkWhenWatered()) ? (new Color(180, 100, 200) * 1f) : Color.White,
                0,
                new Vector2(8f, 24f),
                scale,
                crop.flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                layerDepth + (x+y)/10000000f);
            if (!crop.tintColor.Equals(Color.White) && crop.currentPhase.Value == crop.phaseDays.Count - 1 && !crop.dead.Value) {
              b.Draw(
                crop.DrawnCropTexture,
                Game1.GlobalToLocal(Game1.viewport, position),
                crop.coloredSourceRect,
                crop.tintColor.Value,
                0,
                new Vector2(8f, 24f), 4f,
                crop.flip.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                layerDepth + (x+y+1)/10000000f);
            }
          } else if (pot.bush.Value is not null) {
            var bush = pot.bush.Value;
            var xOffset = 0;
            Texture2D? texture = null;
            if (ModEntry.cbApi?.TryGetTexture(bush, out texture) ?? false) {
              if (__instance.modData.TryGetValue("furyx639.CustomBush/SpriteOffset", out var str) &&
                  Int32.TryParse(str, out var offset)) {
                xOffset = x * 16;
              }
            }
            b.Draw(
                texture ?? Bush.texture.Value,
                Game1.GlobalToLocal(Game1.viewport, position),
                bush.sourceRect.Value with { X = bush.sourceRect.Value.X + xOffset },
                Color.White,
                0,
                new Vector2(8, 32),
                scale,
                bush.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                layerDepth + (x+y)/10000000f);
          } else if (pot.heldObject.Value is not null) {
            var obj = pot.heldObject.Value;
            var data = ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId);
            b.Draw(
                Game1.shadowTexture,
                Game1.GlobalToLocal(Game1.viewport, position + new Vector2(0, 23*scaleModifier)),
                Game1.shadowTexture.Bounds,
                Color.White,
                0f,
                new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                scale,
                SpriteEffects.None,
                layerDepth + (x+y-1)/10000000f);
            b.Draw(
                data.GetTexture(),
                Game1.GlobalToLocal(Game1.viewport, position),
                data.GetSourceRect(),
                Color.White,
                0f,
                new Vector2(8, 8),
                scale,
                obj.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                layerDepth + (x+y)/10000000f);
            // The game seems to discard object scales below 1
            //var objScale = Math.Max(scale, 1.0001f);
            //pot.heldObject.Value.Scale = new(objScale, objScale);
            //pot.heldObject.Value.draw(b, (int)position.X - 64, (int)position.Y - 64, layerDepth + (x+y)/10000000f, 1f);
          }
          // Change to next pot
          potIndexToDraw++;
          if (potIndexToDraw >= cropsChest.Items.Count) {
            potIndexToDraw = 0;
          }
          pot = (cropsChest.Items[potIndexToDraw] as IndoorPot) ?? pot;
        }
      }
    }
    // Draw tank
    b.Draw(
        ImageAssetsManager.AquaponicsTankFront,
        Game1.GlobalToLocal(Game1.viewport, topLeft),
        null,
        Color.White * __instance.alpha,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth + 1/10000f);
    b.Draw(
        ImageAssetsManager.AquaponicsTank,
        Game1.GlobalToLocal(Game1.viewport, topLeft),
        null,
        Color.White * __instance.alpha,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth - 1/10000f);
    b.Draw(
        ImageAssetsManager.AquaponicsTankBack,
        Game1.GlobalToLocal(Game1.viewport, topLeft),
        null,
        Color.White * __instance.alpha,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth - 1.1f/10000f);
    var color = (__instance.GetWaterColor(Vector2.Zero) ?? new(60, 126, 150)) * __instance.alpha;
    b.Draw(
        ImageAssetsManager.AquaponicsTankWater,
        Game1.GlobalToLocal(Game1.viewport, topLeft),
        null,
        //Game1.getSquareSourceRectForNonStandardTileSheet(ImageAssetsManager.AquaponicsTankWater, 80, 80, (Game1.currentLocation.waterAnimationIndex % 10 < 5) ? 0 : 1),
        color,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth - 0.5f/10000);

    // Water effects! Goodness me
    // The water tank consists of four 15 pixel squares, and water tiles are 16 pixels, so we scale most distances down by 15/16 (eg.64 -> 60)
    var topLeftWater = topLeft + new Vector2(10, 6) * scale;
    for (int y = 0; y < 2; y++) {
      for (int x = 0; x < 4; x++) {
        b.Draw(
            Game1.mouseCursors,
            Game1.GlobalToLocal(Game1.viewport, topLeftWater + new Vector2(x * 60, y * 60) * scaleModifier),
            //Game1.GlobalToLocal(Game1.viewport, topLeft + (new Vector2(10, 6) * scale) + new Vector2(x * 60, y * 60) * scaleModifier),
            new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((x + y) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 0 : 0) : (Game1.currentLocation.waterTileFlip ? 0 : 0)), 64, 64),
            //new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((x + y) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)), 64, 64),
            (__instance.overrideWaterColor.Equals(Color.White) ? Game1.currentLocation.waterColor.Value : (__instance.overrideWaterColor.Value * 0.5f)) * 0.7f * __instance.alpha,
            0f,
            Vector2.Zero,
            1f * 15f/16 * scaleModifier,
            SpriteEffects.None,
            layerDepth - 0.4f/10000);
        //bool num = y == 1;
        //bool flag = y == 0;
        //b.Draw(
        //    Game1.mouseCursors,
        //    Game1.GlobalToLocal(Game1.viewport, topLeftWater + new Vector2(x * 60, y * 60 - (int)((!flag) ? Game1.currentLocation.waterPosition * 15f/16 : 0f)) * scaleModifier),
        //    //Game1.GlobalToLocal(Game1.viewport, topLeft + (new Vector2(10, 6) * scale) + new Vector2(x * 60, y * 60) * scaleModifier),
        //    new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((x + y) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)) + (flag ? ((int)Game1.currentLocation.waterPosition) : 0), 64, 64 + (flag ? ((int)(0f - Game1.currentLocation.waterPosition)) : 0)),
        //    //new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((x + y) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)), 64, 64),
        //    (__instance.overrideWaterColor.Equals(Color.White) ? Game1.currentLocation.waterColor.Value : (__instance.overrideWaterColor.Value * 0.5f)) * 0.7f * __instance.alpha,
        //    0f,
        //    Vector2.Zero,
        //    1f * 15f/16 * scaleModifier,
        //    SpriteEffects.None,
        //    layerDepth - 0.4f/10000);
        //if (num) {
        //  b.Draw(
        //      Game1.mouseCursors,
        //      Game1.GlobalToLocal(Game1.viewport, topLeftWater + new Vector2(x * 60, (y + 1) * 60 - (int)Game1.currentLocation.waterPosition * 15f/16) * scaleModifier),
        //      new Microsoft.Xna.Framework.Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((x + (y + 1)) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)), 64, 64 - (int)(64f - Game1.currentLocation.waterPosition) - 1),
        //      (__instance.overrideWaterColor.Equals(Color.White) ? Game1.currentLocation.waterColor.Value : (__instance.overrideWaterColor.Value * 0.5f)) * 0.7f * __instance.alpha,
        //      0f,
        //      Vector2.Zero,
        //      1f * 15f/16 * scaleModifier,
        //      SpriteEffects.None,
        //      layerDepth - 0.4f/10000);
        //}
      }
    }
    b.Draw(
        ImageAssetsManager.AquaponicsTankWaterHighlights,
        Game1.GlobalToLocal(Game1.viewport, topLeft),
        null,
        //Game1.getSquareSourceRectForNonStandardTileSheet(ImageAssetsManager.AquaponicsTankWaterHighlights, 80, 80, Game1.currentLocation.waterAnimationIndex % 2),
        (__instance.overrideWaterColor.Value.Equals(Color.White) ? __instance.color : __instance.overrideWaterColor.Value) * __instance.alpha,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth - 0.3f/10000);
    // (Re)draw the sign so it's above the tank
    // I should probably transpile this in the original function, but meh
    if (__instance.sign.Value != null) {
      ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(__instance.sign.Value.QualifiedItemId);
      b.Draw(
          dataOrErrorItem.GetTexture(),
          Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.tileX.Value * 64 + 8, __instance.tileY.Value * 64 + __instance.tilesHigh.Value * 64 - 128 - 32)),
          dataOrErrorItem.GetSourceRect(),
          __instance.color * __instance.alpha,
          0f,
          Vector2.Zero,
          Game1.pixelZoom,
          SpriteEffects.None,
          layerDepth + 1.1f/10000f);
      if (__instance.fishType.Value != null) {
        ParsedItemData data2 = ItemRegistry.GetData(__instance.fishType.Value);
        if (data2 != null) {
          Texture2D texture2D = data2.GetTexture();
          Microsoft.Xna.Framework.Rectangle sourceRect = data2.GetSourceRect();
          float num3 = ((__instance.maxOccupants.Value == 1) ? 6f : 0f);
          b.Draw(
              texture2D,
              Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.tileX.Value * 64 + 8 + 8 - 4, (float)(__instance.tileY.Value * 64 + __instance.tilesHigh.Value * 64 - 128 - 8 + 4) + num3)),
              sourceRect,
              Color.Black * 0.4f * __instance.alpha,
              0f,
              Vector2.Zero,
              3f,
              SpriteEffects.None,
              layerDepth + 1.2f/10000f);
          b.Draw(
              texture2D,
              Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.tileX.Value * 64 + 8 + 8 - 1, (float)(__instance.tileY.Value * 64 + __instance.tilesHigh.Value * 64 - 128 - 8 + 1) + num3)),
              sourceRect,
              __instance.color * __instance.alpha,
              0f,
              Vector2.Zero,
              3f,
              SpriteEffects.None,
              layerDepth + 1.3f/10000f);
          if (__instance.maxOccupants.Value > 1) {
            Utility.drawTinyDigits(
                __instance.currentOccupants.Value,
                b,
                Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.tileX.Value * 64 + 32 + 8 + ((__instance.currentOccupants.Value < 10) ? 8 : 0), __instance.tileY.Value * 64 + __instance.tilesHigh.Value * 64 - 96)),
                3f,
                layerDepth + 1.4f/10000f,
                Color.LightYellow * __instance.alpha);
          }
        }
      }
    }
  }

  static void FishPond_drawInMenu_Postfix(FishPond __instance, SpriteBatch b, int x, int y) {
    if (!IsAquaponicsPond(__instance, out var _)) return;
    var layerDepth = 1.1f;
    var scaleModifier = (__instance.tilesHigh.Value / 5f);
    var scale = Game1.pixelZoom * scaleModifier;
    // Draw tank
    Vector2 topLeft = new Vector2(x, y);
    b.Draw(
        ImageAssetsManager.AquaponicsTank,
        topLeft,
        null,
        Color.White * __instance.alpha,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth - 1/10000f);
    b.Draw(
        ImageAssetsManager.AquaponicsTankFront,
        topLeft,
        null,
        Color.White * __instance.alpha,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth + 1/10000f);
    b.Draw(
        ImageAssetsManager.AquaponicsTankBack,
        topLeft,
        null,
        Color.White * __instance.alpha,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth - 1.1f/10000f);
    var color = (__instance.GetWaterColor(Vector2.Zero) ?? new(60, 126, 150)) * __instance.alpha;
    b.Draw(
        ImageAssetsManager.AquaponicsTankWater,
        topLeft,
        null,
        color,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth - 0.5f/10000);

    // Water effects! Goodness me
    var topLeftWater = topLeft + new Vector2(10, 6) * scale;
    for (int j = 0; j < 2; j++) {
      for (int i = 0; i < 4; i++) {
        b.Draw(
            Game1.mouseCursors,
            topLeftWater + new Vector2(i * 60, j * 60) * scaleModifier,
            new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((i + j) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 0 : 0) : (Game1.currentLocation.waterTileFlip ? 0 : 0)), 64, 64),
            (__instance.overrideWaterColor.Equals(Color.White) ? Game1.currentLocation.waterColor.Value : (__instance.overrideWaterColor.Value * 0.5f)) * 0.7f * __instance.alpha,
            0f,
            Vector2.Zero,
            1f * 15f/16 * scaleModifier,
            SpriteEffects.None,
            layerDepth - 0.4f/10000);
        //bool num = j == 1;
        //bool flag = j == 0;
        //b.Draw(
        //    Game1.mouseCursors,
        //    // We add 0,1 because of all the floating point crimes I did occasionally making a gap in the water
        //    topLeftWater + new Vector2(i * 60, j * 60 - (int)((!flag) ? Game1.currentLocation.waterPosition * 15f/16 : 0f)) * scaleModifier + new Vector2(0f, 1f),
        //    //Game1.GlobalToLocal(Game1.viewport, topLeft + (new Vector2(10, 6) * scale) + new Vector2(x * 60, y * 60) * scaleModifier),
        //    new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((i + j) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)) + (flag ? ((int)Game1.currentLocation.waterPosition) : 0), 64, 64 + (flag ? ((int)(0f - Game1.currentLocation.waterPosition)) : 0)),
        //    //new Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((x + y) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)), 64, 64),
        //    (__instance.overrideWaterColor.Equals(Color.White) ? Game1.currentLocation.waterColor.Value : (__instance.overrideWaterColor.Value * 0.5f)) * 0.7f * __instance.alpha,
        //    0f,
        //    Vector2.Zero,
        //    1f * 15f/16 * scaleModifier,
        //    SpriteEffects.None,
        //    layerDepth - 0.4f/10000);
        //if (num) {
        //  b.Draw(
        //      Game1.mouseCursors,
        //      topLeftWater + new Vector2(i * 60, (j + 1) * 60 - (int)Game1.currentLocation.waterPosition * 15f/16) * scaleModifier,
        //      new Microsoft.Xna.Framework.Rectangle(Game1.currentLocation.waterAnimationIndex * 64, 2064 + (((i + (j + 1)) % 2 != 0) ? ((!Game1.currentLocation.waterTileFlip) ? 128 : 0) : (Game1.currentLocation.waterTileFlip ? 128 : 0)), 64, 64 - (int)(64f - Game1.currentLocation.waterPosition) - 1),
        //      (__instance.overrideWaterColor.Equals(Color.White) ? Game1.currentLocation.waterColor.Value : (__instance.overrideWaterColor.Value * 0.5f)) * 0.7f * __instance.alpha,
        //      0f,
        //      Vector2.Zero,
        //      1f * 15f/16 * scaleModifier,
        //      SpriteEffects.None,
        //      layerDepth - 0.4f/10000);
        //}
      }
    }
    b.Draw(
        ImageAssetsManager.AquaponicsTankWaterHighlights,
        topLeft,
        null,
        (__instance.overrideWaterColor.Value.Equals(Color.White) ? __instance.color : __instance.overrideWaterColor.Value) * __instance.alpha,
        0,
        Vector2.Zero,
        scale,
        SpriteEffects.None,
        layerDepth - 0.3f/10000);

  }

  static void HoeDirt_GetFertilizerQualityBoostLevel_Postfix(HoeDirt __instance, ref int __result) {
    if (api.IsAquaponicsHoeDirt(__instance)) {
      __result = 3;
    }
  }

  static void HoeDirt_GetFertilizerSpeedBoost_Postfix(HoeDirt __instance, ref float __result) {
    if (api.IsAquaponicsHoeDirt(__instance)) {
      __result = 0.33f;
    }
  }

  private void OpenCropsChest(string command, string[] args) {
    int distance = Int32.MaxValue;
    FishPond? pondToReturn = null;
    foreach (var building in Game1.currentLocation.buildings) {
      if (IsAquaponicsPond(building, out var pond)) {
        var pondDistance = (int)Vector2.Distance(Game1.player.Tile, new(pond.tileX.Value, pond.tileY.Value));
        if (pondDistance < distance) {
          distance = pondDistance;
          pondToReturn = pond;
        }
      }
    }
    if (pondToReturn is not null) {
      var chest = FishPondCropManager.GetFishPondCropsChest(pondToReturn);
      if (chest is not null) { 
        Game1.activeClickableMenu = new ItemGrabMenu(chest.Items);
      }
    }
  }

  private void DowngradeAllPonds(string command, string[] args) {
    Utility.ForEachBuilding(building => {
      if (IsAquaponicsPond(building, out var pond)) {
        pond.buildingType.Value = "Fish Pond";
        pond.ReloadBuildingData();
        pond.daysUntilUpgrade.Value = 0;
      }
      return true;
    });
  }

  private void DowngradePond(string command, string[] args) {
    try {
      if (Game1.activeClickableMenu is PondQueryMenu menu) {
        var pond = Helper.Reflection.GetField<FishPond>(menu, "_pond").GetValue();
        if (pond.buildingType.Value != "Fish Pond") {
          pond.buildingType.Value = "Fish Pond";
          pond.ReloadBuildingData();
          pond.daysUntilUpgrade.Value = 0;
          pond.UpdateMaximumOccupancy();
        }
      }
    } catch (Exception e) {
      StaticMonitor.Log($"Error downgrading pond: {e.ToString()}", LogLevel.Warn);
    }
  }
}

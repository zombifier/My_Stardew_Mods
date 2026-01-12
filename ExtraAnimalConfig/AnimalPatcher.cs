using Netcode;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Buildings;
using StardewValley.Events;
using StardewValley.Extensions;
using StardewValley.Tools;
using StardewValley.Triggers;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.FarmAnimals;
using StardewValley.TerrainFeatures;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using xTile.Dimensions;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ExtraAnimalConfig;

// Contains Harmony patches related to animals.
sealed class AnimalDataPatcher {
  static string CachedAnimalIdKey = $"{ModEntry.UniqueId}.CachedAnimalId";
  static MethodInfo? GetProduceIdDelegate = null;

  public static void ApplyPatches(Harmony harmony) {
    // Animal patches
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.isMale)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_isMale_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.CanGetProduceWithTool)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_CanGetProduceWithTool_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.GetTexturePath),
          new Type[] { typeof(FarmAnimalData) }),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_GetTexturePath_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.TryGetAnimalDataFromEgg)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_TryGetAnimalDataFromEgg_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.minutesElapsed)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.SObject_minutesElapsed_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.dayUpdate)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_dayUpdate_Prefix)), Priority.High + 1),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_dayUpdate_Postfix)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_dayUpdate_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.OnDayStarted)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_OnDayStarted_Prefix)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_OnDayStarted_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.GetHarvestType)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_GetHarvestType_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(AnimalHouse),
          nameof(AnimalHouse.adoptAnimal)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.AnimalHouse_adoptAnimal_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.CanLiveIn)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_CanLiveIn_Postfix)));

    // Animal house patches for the non-hay food functionality
    harmony.Patch(
        original: AccessTools.Method(typeof(AnimalHouse),
          nameof(AnimalHouse.checkAction)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.AnimalHouse_checkAction_Postfix)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.AnimalHouse_checkAction_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(AnimalHouse),
          nameof(AnimalHouse.dropObject)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.AnimalHouse_dropObject_Postfix)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.AnimalHouse_dropObject_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(AnimalHouse),
          nameof(AnimalHouse.feedAllAnimals)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.AnimalHouse_feedAllAnimals_Postfix)));

    // Transpilers to override animal produce
    // Prefixes to set animal produce (if there are extras)
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.behaviors)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_behaviors_Prefix)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_behaviors_Postfix)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_behaviors_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.DigUpProduce)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimals_DigUpProduce_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.DayUpdate)),
        //        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.SObject_DayUpdate_Postfix)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.SObject_DayUpdate_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(MilkPail),
          nameof(MilkPail.DoFunction)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.MilkPail_DoFunction_Prefix)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.MilkPail_DoFunction_Postfix)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.MilkPail_DoFunction_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Shears),
          nameof(Shears.DoFunction)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.Shears_DoFunction_Prefix)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.Shears_DoFunction_Postfix)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.Shears_DoFunction_Transpiler)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(MilkPail),
          nameof(MilkPail.beginUsing)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.MilkPail_beginUsing_Prefix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Shears),
          nameof(Shears.beginUsing)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.Shears_beginUsing_Prefix)));

    // Patch functionalities for the feed hopper
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          "CheckForActionOnFeedHopper"),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.SObject_CheckForActionOnFeedHopper_Prefix)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.SObject_CheckForActionOnFeedHopper_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.performObjectDropInAction)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.SObject_performObjectDropInAction_Postfix)));

    // Patch the original building silo to support mixing in non-hay feed with hay
    //harmony.Patch(
    //    original: AccessTools.Method(typeof(GameLocation),
    //      nameof(GameLocation.performAction)),
    //    postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.GameLocation_performAction_Postfix)));

    // Dirt eating patch
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.UpdateRandomMovements)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_UpdateRandomMovements_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.Eat)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_Eat_Prefix)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_Eat_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Grass),
          nameof(Grass.reduceBy)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.Grass_reduceBy_Prefix)));

    // Scythe drop patch
    harmony.Patch(
        original: AccessTools.Method(typeof(Grass),
          nameof(Grass.TryDropItemsOnCut)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.Grass_TryDropItemsOnCut_Postfix)));

    // Pass in animal cracker into GetProduceID's GSQ checker
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.GetProduceID)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_GetProduceID_Transpiler)));
    harmony.Patch(
        original: GetProduceIdDelegate,
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_GetProduceIDDelegate_Transpiler)));

    // Rain/winter patches
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.updateWhenNotCurrentLocation)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_ReplaceRainWinterTranspiler)));
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.updatePerTenMinutes)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_ReplaceRainWinterTranspiler)));
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.behaviors)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_ReplaceRainWinterTranspiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.OutputIncubator)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.SObject_OutputIncubator_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.pet)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(FarmAnimal_pet_Prefix)));

    // skinny animals
    // this approach is dead, time for teleportation
    //harmony.Patch(
    //    original: AccessTools.Method(typeof(FarmAnimal),
    //      nameof(FarmAnimal.GetBoundingBox)),
    //    postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_GetBoundingBox_Postfix)));
    //harmony.Patch(
    //    original: AccessTools.Method(typeof(Character), nameof(Character.GetSpriteWidthForPositioning)),
    //    postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.Character_GetSpriteWidthForPositioning_Postfix)));
  }

  static void FarmAnimal_isMale_Postfix(FarmAnimal __instance, ref bool __result) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(__instance.type.Value, out var animalExtensionData) &&
        animalExtensionData.MalePercentage >= 0) {
      __result = __instance.myID.Value % 100 < animalExtensionData.MalePercentage;
    }
  }

  static void FarmAnimal_CanGetProduceWithTool_Postfix(FarmAnimal __instance, ref bool __result, Tool tool) {
    if (__instance.currentProduce.Value != null &&
        ExtraProduceUtils.GetHarvestMethodOverride(__instance, __instance.currentProduce.Value, out var harvestMethod) &&
        tool != null && tool.BaseName != null) {
      // In extremely rare cases (eg debug mode) an animal may spawn with DropOvernight/Debris produce in its body.
      // To help get the produce out, always allow them to harvest
      __result = (harvestMethod == "DropOvernight") ||
        (harvestMethod == "Debris") ||
        (harvestMethod == tool.BaseName);
    }
  }

  static void FarmAnimal_GetTexturePath_Postfix(FarmAnimal __instance, ref string __result, FarmAnimalData data) {
    if (Game1.farmAnimalData.TryGetValue(__instance.type.Value, out var animalData) &&
        ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(__instance.type.Value, out var animalExtensionData)) {
      // old deprecated method, via produce texture override
      if (__instance.currentProduce.Value is not null
          && animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(__instance.currentProduce.Value) ?? __instance.currentProduce.Value, out var animalProduceExtensionData)) {
        if (animalProduceExtensionData.ProduceTexture != null) {
          __result = animalProduceExtensionData.ProduceTexture;
        }
        if (__instance.skinID.Value is not null) {
          if (animalProduceExtensionData.ProduceTexture is not null &&
              ModEntry.Helper.GameContent.ParseAssetName(animalProduceExtensionData.ProduceTexture ?? "").IsEquivalentTo(animalData.HarvestedTexture)) {
            __result = animalData.Skins?.Find(m => m.Id == __instance.skinID.Value)?.HarvestedTexture ?? __result;
          }
          //if (animalProduceExtensionData.ProduceTexture == animalData.Texture) {
          //  __result = animalData.Skins?.Find(m => m.Id == __instance.skinID.Value)?.Texture ?? __result;
          //}
          if (animalProduceExtensionData.SkinProduceTexture.TryGetValue(__instance.skinID.Value, out var skinTexture)) {
            __result = skinTexture;
          }
        }
      }
      // new method, via dedicated field
      foreach (var appearanceData in animalExtensionData.TextureOverrides) {
        if ((appearanceData.Produce is not null /*&& __instance.currentProduce.Value is not null*/ && __instance.currentProduce.Value != appearanceData.Produce) ||
            (appearanceData.Skin is not null && __instance.skinID.Value != appearanceData.Skin) ||
            (appearanceData.Condition is not null && !GameStateQuery.CheckConditions(appearanceData.Condition, __instance.currentLocation, null, null, AnimalUtils.GetGoldenAnimalCracker(__instance)))) {
          continue;
        }
        __result = appearanceData.DefaultTextureToUse switch {
          DefaultTextureEnum.HarvestedTexture =>
            animalData.Skins?.Find(m => m.Id == __instance.skinID.Value)?.HarvestedTexture ?? animalData.HarvestedTexture,
          DefaultTextureEnum.Texture =>
            animalData.Skins?.Find(m => m.Id == __instance.skinID.Value)?.Texture ?? animalData.Texture,
          DefaultTextureEnum.BabyTexture =>
            animalData.Skins?.Find(m => m.Id == __instance.skinID.Value)?.BabyTexture ?? animalData.BabyTexture,
          _ => appearanceData.TextureToUse ?? __result,
        };
        break;
      }
    }
  }

  static void FarmAnimal_TryGetAnimalDataFromEgg_Postfix(ref bool __result, Item eggItem, GameLocation location, ref string id, ref FarmAnimalData data) {
    // If there's a result from the incubator being ready return it
    if (eggItem.modData.TryGetValue(CachedAnimalIdKey, out var cachedAnimalId) &&
        Game1.farmAnimalData.TryGetValue(cachedAnimalId, out var animalData)) {
      id = eggItem.modData[CachedAnimalIdKey];
      data = animalData;
      __result = true;
      return;
    }
    if (!__result && eggItem.HasTypeObject()) {
      List<string>? list = location?.ParentBuilding?.GetData()?.ValidOccupantTypes;
      foreach (var (animalId, farmAnimalData) in Game1.farmAnimalData) {
        // We don't need to check for egg extension data since that's already handled
        if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animalId, out var animalExtensionData) &&
            animalExtensionData.ExtraHouses.Count() > 0 &&
            (list?.Intersect(animalExtensionData.ExtraHouses).Any() ?? false) &&
            (farmAnimalData.EggItemIds?.Count() > 0) &&
            (farmAnimalData.EggItemIds?.Contains(eggItem.ItemId) ?? false)) {
          id = animalId;
          data = farmAnimalData;
          __result = true;
          return;
        }
      }
    }
  }

  static void SObject_minutesElapsed_Postfix(SObject __instance, int minutes) {
    if ((__instance.GetMachineData()?.IsIncubator ?? false) &&
        __instance.heldObject.Value != null &&
        __instance.MinutesUntilReady <= 0 &&
        !__instance.modData.ContainsKey(CachedAnimalIdKey) &&
        ModEntry.eggExtensionDataAssetHandler.data.TryGetValue(__instance.heldObject.Value.QualifiedItemId, out var eggExtensionData)) {
      foreach (var animalSpawnData in eggExtensionData.AnimalSpawnList) {
        if (animalSpawnData.Condition != null && !GameStateQuery.CheckConditions(animalSpawnData.Condition, __instance.Location)) {
          continue;
        }
        if (Game1.farmAnimalData.TryGetValue(animalSpawnData.AnimalId, out var animalData2)) {
          __instance.heldObject.Value.modData[CachedAnimalIdKey] = animalSpawnData.AnimalId;
          return;
        }
      }
    }
  }

  // If there are custom non-hay feed for this animal inside the building, feed the animal
  static void FarmAnimal_dayUpdate_Prefix(FarmAnimal __instance, GameLocation environment) {
    bool notOutsideEater = AnimalUtils.AnimalOnlyEatsModdedFood(__instance) && !AnimalUtils.AnimalIsOutsideForager(__instance);
    // Outside eaters are already handled by VPP's day start
    if (notOutsideEater &&
        ((!ModEntry.vppApi?.GetProfessionsForPlayer(Game1.GetPlayer(__instance.ownerID.Value)).Contains("Caretaker") ?? true) || Game1.random.NextBool(.65))) {
      __instance.fullness.Value = 0;
    }
    string? feedItemId = AnimalUtils.GetCustomFeedThisAnimalCanEat(__instance, environment);
    if (feedItemId is not null &&
        __instance.fullness.Value < 200 &&
        environment is AnimalHouse) {
      foreach (var pair in environment.objects.Pairs.ToArray()) {
        if (pair.Value.QualifiedItemId == ItemRegistry.QualifyItemId(feedItemId)) {
          __instance.fullness.Value = 255;
          environment.objects.Remove(pair.Key);
          if (notOutsideEater) {
            __instance.happiness.Value = 255;
          }
          break;
        }
      }
    }
    // Clear cached quality
    __instance.modData.Remove(ExtraProduceUtils.CachedProduceQualityKey);
  }

  // Set up produce and hunger state for a new day
  static void FarmAnimal_OnDayStarted_Prefix(FarmAnimal __instance, ref int __state) {
    ExtraProduceUtils.DropDebrisOnDayStart(__instance);
    ExtraProduceUtils.PopQueueAndReplaceProduce(__instance);
    __state = __instance.fullness.Value;
  }

  // Restore old value if an outside forager (the main game will set it to 255 if grass eat amount < 0)
  // (Future: maybe custom grass? That'd be a long, long time before I implement that though)
  static void FarmAnimal_OnDayStarted_Postfix(FarmAnimal __instance, int __state) {
    if (AnimalUtils.AnimalIsOutsideForager(__instance)) {
      __instance.fullness.Value = __state;
    }
  }

  static void FarmAnimal_GetHarvestType_Postfix(FarmAnimal __instance, ref FarmAnimalHarvestType? __result) {
    if (ExtraProduceUtils.GetHarvestMethodOverride(__instance, __instance.currentProduce.Value, out var harvestMethod)) {
      switch (harvestMethod) {
        case "DigUp":
          __result = FarmAnimalHarvestType.DigUp;
          break;
        case "Milk Pail":
        case "Shears":
          __result = FarmAnimalHarvestType.HarvestWithTool;
          break;
        // NOTE: This branch should NEVER happen (the produce should have been dropped last night) but I'm including it anyway just in case
        case "DropOvernight":
        case "Debris":
        default:
          __result = FarmAnimalHarvestType.DropOvernight;
          break;
      }
    }
  }

  // The following 2 patches allow placing feed on custom troughs.
  static void AnimalHouse_checkAction_Postfix(AnimalHouse __instance, ref bool __result, Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who) {
    if (who.ActiveObject is not null &&
        AnimalUtils.CanThisTileAcceptThisItem(__instance, tileLocation.X, tileLocation.Y, who.ActiveObject.QualifiedItemId) &&
         !__instance.objects.ContainsKey(new Vector2(tileLocation.X, tileLocation.Y))) {
      __instance.objects.Add(new Vector2(tileLocation.X, tileLocation.Y), (SObject)who.ActiveObject.getOne());
      who.reduceActiveItemByOne();
      who.currentLocation.playSound("coin");
      Game1.haltAfterCheck = false;
      __result = true;
    }
  }

  static void AnimalHouse_dropObject_Postfix(AnimalHouse __instance, ref bool __result, SObject obj, Vector2 location, xTile.Dimensions.Rectangle viewport, bool initialPlacement, Farmer? who = null) {
    Vector2 key = new Vector2((int)(location.X / 64f), (int)(location.Y / 64f));
    if (AnimalUtils.CanThisTileAcceptThisItem(__instance, (int)key.X, (int)key.Y, obj.QualifiedItemId)) {
      __result = __instance.objects.TryAdd(key, obj);
    }
  }

  static IEnumerable<CodeInstruction> AnimalHouse_checkAction_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: this.doesTileHaveProperty(tileLocation.X, tileLocation.Y, "Trough", "Back") != null
    // New: ... && !BuildingHasFeedOverride(this)
    matcher.MatchEndForward(
        new CodeMatch(OpCodes.Ldstr, "Trough"),
        new CodeMatch(OpCodes.Ldstr, "Back"),
        new CodeMatch(OpCodes.Ldc_I4_0),
#if SDV1616
        new CodeMatch(OpCodes.Ldc_I4_0),
#endif
        new CodeMatch(OpCodes.Callvirt),
        new CodeMatch(OpCodes.Brfalse_S)
        )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(AnimalHouse_checkAction_Transpiler)}");
    var label = (Label)matcher.Operand;
    matcher.Advance(1)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AnimalUtils), nameof(AnimalUtils.BuildingHasFeedOverride))),
          new CodeInstruction(OpCodes.Brtrue_S, label)
          );
    return matcher.InstructionEnumeration();
  }

  static IEnumerable<CodeInstruction> AnimalHouse_dropObject_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: this.doesTileHaveProperty(tileLocation.X, tileLocation.Y, "Trough", "Back") != null
    // New: ... && !BuildingHasFeedOverride(this)
    matcher.MatchEndForward(
        new CodeMatch(OpCodes.Ldstr, "Trough"),
        new CodeMatch(OpCodes.Ldstr, "Back"),
        new CodeMatch(OpCodes.Ldc_I4_0),
#if SDV1616
        new CodeMatch(OpCodes.Ldc_I4_0),
#endif
        new CodeMatch(OpCodes.Callvirt),
        new CodeMatch(OpCodes.Brfalse_S)
        )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(AnimalHouse_checkAction_Transpiler)}");
    var label = (Label)matcher.Operand;
    matcher.Advance(1)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AnimalUtils), nameof(AnimalUtils.BuildingHasFeedOverride))),
          new CodeInstruction(OpCodes.Brtrue_S, label)
          );
    return matcher.InstructionEnumeration();
  }

  static void AnimalHouse_feedAllAnimals_Postfix(AnimalHouse __instance) {
    GameLocation rootLocation = __instance.GetRootLocation();
    // If using a building wide feed item override, delete existing hay
    bool shouldRemoveHay = AnimalUtils.BuildingHasFeedOverride(__instance);
    int num = 0;
    for (int i = 0; i < __instance.map.Layers[0].LayerWidth; i++) {
      for (int j = 0; j < __instance.map.Layers[0].LayerHeight; j++) {
        Vector2 key = new Vector2(i, j);
        if (shouldRemoveHay && __instance.objects.TryGetValue(key, out var obj) && obj.QualifiedItemId == "(O)178") {
          __instance.objects.Remove(key);
          GameLocation.StoreHayInAnySilo(1, rootLocation);
        }
        var feedId = AnimalUtils.GetCustomFeedForTile(__instance, i, j);
        if (feedId is null) {
          continue;
        }
        if (!__instance.objects.ContainsKey(key)) {
          SObject? feedObj = SiloUtils.GetFeedFromAnySilo(feedId!);
          if (feedObj == null) {
            return;
          }
          __instance.objects.Add(key, feedObj);
          num++;
        }
        if (num >= __instance.animalLimit.Value) {
          return;
        }
      }
    }
    return;
  }

  // When a new animal gives birth, maybe change it to a different animal depending on the custom spawn data
  static void AnimalHouse_adoptAnimal_Prefix(AnimalHouse __instance, ref FarmAnimal animal) {
    // NamingMenu should only be active for newly birthed animals... hopefully.
    if (animal.parentId.Value != -1 &&
        Game1.activeClickableMenu is NamingMenu &&
        (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
         animalExtensionData.AnimalSpawnList != null)) {
      foreach (var animalSpawnData in animalExtensionData.AnimalSpawnList) {
        if (animalSpawnData.Condition != null && !GameStateQuery.CheckConditions(animalSpawnData.Condition, __instance)) {
          continue;
        }
        if (animalSpawnData.AnimalId == animal.type.Value) {
          continue;
        }
        string name = animal.Name;
        long previousParentId = animal.parentId.Value;
        animal = new FarmAnimal(animalSpawnData.AnimalId, animal.myID.Value, animal.ownerID.Value) {
          Name = name,
          displayName = name,
        };
        animal.parentId.Value = previousParentId;
        return;
      }
    }
  }


  static readonly MethodInfo ItemRegistryCreateObjectType = AccessTools
    .GetDeclaredMethods(typeof(ItemRegistry))
    .First(method => method.Name == nameof(ItemRegistry.Create) && method.IsGenericMethod)
    .MakeGenericMethod(typeof(SObject));

  static readonly MethodInfo CreateProduceType = AccessTools.Method(
      typeof(ExtraProduceUtils),
      nameof(ExtraProduceUtils.CreateProduce));


  static IEnumerable<CodeInstruction> FarmAnimal_behaviors_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: ItemRegistry.Create<Object>(this.currentProduce.Value);
    // New: AnimalDataPatcher.CreateProduce(this.currentProduce.Value, this);
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FarmAnimal), nameof(FarmAnimal.currentProduce))),
        new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(NetFieldBase<string, NetString>), nameof(NetFieldBase<string, NetString>.Value))),
        new CodeMatch(OpCodes.Ldc_I4_1),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Call, ItemRegistryCreateObjectType)
        )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(FarmAnimal_behaviors_Transpiler)}")
      .Advance(3)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Ldc_I4_S, (int)ProduceMethod.DigUp),
          new CodeInstruction(OpCodes.Ldnull),
          new CodeInstruction(OpCodes.Call, CreateProduceType)
          )
      .RemoveInstructions(4);
    return matcher.InstructionEnumeration();
  }

  static IEnumerable<CodeInstruction> SObject_DayUpdate_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: ItemRegistry.Create<Object>("(O)" + value2.currentProduce.Value);
    // New: AnimalDataPatcher.CreateProduce("(O)" + value2.currentProduce.Value, value2);
    matcher.MatchEndForward(
        new CodeMatch(OpCodes.Ldstr, "(O)"),
        new CodeMatch(OpCodes.Ldloc_S));
    var animalVar = matcher.Operand;
    matcher.MatchStartBackwards(
        new CodeMatch(OpCodes.Ldstr, "(O)"),
        new CodeMatch(OpCodes.Ldloc_S),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FarmAnimal), nameof(FarmAnimal.currentProduce))),
        new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(NetFieldBase<string, NetString>), nameof(NetFieldBase<string, NetString>.Value))),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(String), nameof(String.Concat), new Type[] { typeof(string), typeof(string) })),
        new CodeMatch(OpCodes.Ldc_I4_1),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Call, ItemRegistryCreateObjectType)
        )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(SObject_DayUpdate_Transpiler)}")
      .Advance(5)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldloc_S, animalVar),
          //          new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(KeyValuePair<long, FarmAnimal>), nameof(KeyValuePair<long, FarmAnimal>.Value))),
          new CodeInstruction(OpCodes.Ldc_I4_S, (int)ProduceMethod.Tool),
          new CodeInstruction(OpCodes.Ldnull),
          new CodeInstruction(OpCodes.Call, CreateProduceType)
          )
      .RemoveInstructions(4);
    return matcher.InstructionEnumeration();
  }

  static IEnumerable<CodeInstruction> MilkPail_DoFunction_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: ItemRegistry.Create<Object>("(O)" + this.animal.currentProduce.Value);
    // New: AnimalDataPatcher.CreateProduce("(O)" + this.animal.currentProduce.Value);
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldstr, "(O)"),
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(MilkPail), nameof(MilkPail.animal))),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FarmAnimal), nameof(FarmAnimal.currentProduce))),
        new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(NetFieldBase<string, NetString>), nameof(NetFieldBase<string, NetString>.Value))),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(String), nameof(String.Concat), new Type[] { typeof(string), typeof(string) })),
        new CodeMatch(OpCodes.Ldc_I4_1),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Call, ItemRegistryCreateObjectType)
        )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(MilkPail_DoFunction_Transpiler)}")
      .Advance(6)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MilkPail), nameof(MilkPail.animal))),
          new CodeInstruction(OpCodes.Ldc_I4_S, (int)ProduceMethod.Tool),
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Call, CreateProduceType)
          )
      .RemoveInstructions(4);
    return matcher.InstructionEnumeration();
  }

  static IEnumerable<CodeInstruction> Shears_DoFunction_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: ItemRegistry.Create<Object>("(O)" + this.animal.currentProduce.Value);
    // New: AnimalDataPatcher.CreateProduce("(O)" + this.animal.currentProduce.Value);
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldstr, "(O)"),
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Shears), nameof(Shears.animal))),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FarmAnimal), nameof(FarmAnimal.currentProduce))),
        new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(NetFieldBase<string, NetString>), nameof(NetFieldBase<string, NetString>.Value))),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(String), nameof(String.Concat), new Type[] { typeof(string), typeof(string) })),
        new CodeMatch(OpCodes.Ldc_I4_1),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Call, ItemRegistryCreateObjectType)
        )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(Shears_DoFunction_Transpiler)}")
      .Advance(6)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Shears), nameof(Shears.animal))),
          new CodeInstruction(OpCodes.Ldc_I4_S, (int)ProduceMethod.Tool),
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Call, CreateProduceType)
          )
      .RemoveInstructions(4);
    return matcher.InstructionEnumeration();
  }

  // This transpiler does 3 things:
  // * Disallow eating hay if not a hay eater
  // * Override the item create call with the override item query
  // * Don't drop produce on the ground if override is specified
  static IEnumerable<CodeInstruction> FarmAnimal_dayUpdate_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: (int)this.fullness < 200 && environment is AnimalHouse
    // New: ... && !AnimalOnlyEatsModdedFood(this)
    matcher.MatchEndForward(
      new CodeMatch(OpCodes.Ldarg_0),
      new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FarmAnimal), nameof(FarmAnimal.fullness))),
      //new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(NetInt), "op_Implicit")),
      new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(NetFieldBase<Int32, NetInt>), nameof(NetInt.Value))),
      new CodeMatch(OpCodes.Ldc_I4, 200),
      new CodeMatch(OpCodes.Bge_S),
      new CodeMatch(OpCodes.Ldarg_1),
      new CodeMatch(OpCodes.Isinst, typeof(AnimalHouse)),
      new CodeMatch(OpCodes.Brfalse_S)
        )
    .ThrowIfNotMatch($"Could not find entry point for hunger portion of {nameof(FarmAnimal_dayUpdate_Transpiler)}");
    var label = (Label)matcher.Operand;
    matcher.Advance(1)
      .InsertAndAdvance(
        new CodeInstruction(OpCodes.Ldarg_0),
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AnimalUtils), nameof(AnimalUtils.AnimalOnlyEatsModdedFood))),
        new CodeInstruction(OpCodes.Brtrue_S, label)
        );

    // Find the variable value of 'text' (aka the produce item ID)
    matcher.MatchEndForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FarmAnimal), nameof(FarmAnimal.currentProduce))),
        new CodeMatch(OpCodes.Ldloc_S),
        new CodeMatch(OpCodes.Callvirt),
        new CodeMatch(OpCodes.Ldnull),
        new CodeMatch(OpCodes.Stloc_S)
        );

    var produceIdVar = matcher.Operand;

    // Old: animalData.HarvestType != FarmAnimalHarvestType.DropOvernight
    // New: DoNotDropCurrentProduce(animal, text)
    matcher.MatchStartBackwards(
      new CodeMatch(OpCodes.Ldloc_0),
      new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FarmAnimalData), nameof(FarmAnimalData.HarvestType))),
      new CodeMatch(OpCodes.Ldc_I4_0),
      new CodeMatch(OpCodes.Cgt_Un)
        )
      .ThrowIfNotMatch($"Could not find entry point for drop harvest type check portion of {nameof(FarmAnimal_dayUpdate_Transpiler)}");
    var labels = matcher.Labels;
    matcher.RemoveInstructions(4)
      .InsertAndAdvance(
        new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
        new CodeInstruction(OpCodes.Ldloc_S, produceIdVar),
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExtraProduceUtils), nameof(ExtraProduceUtils.DoNotDropCurrentProduce))));


    // Old: ItemRegistry.Create<Object>("(O)" + text);
    // New: AnimalDataPatcher.CreateProduce("(O)" + text, this);
    matcher.MatchStartForward(
      new CodeMatch(OpCodes.Ldstr, "(O)"),
      new CodeMatch(OpCodes.Ldloc_S),
      new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(String), nameof(String.Concat), new Type[] { typeof(string), typeof(string) })),
      new CodeMatch(OpCodes.Ldc_I4_1),
      new CodeMatch(OpCodes.Ldc_I4_0),
      new CodeMatch(OpCodes.Ldc_I4_0),
      new CodeMatch(OpCodes.Call, ItemRegistryCreateObjectType)
    )
    .ThrowIfNotMatch($"Could not find entry point for item create {nameof(FarmAnimal_dayUpdate_Transpiler)}")
    .Advance(3)
    .InsertAndAdvance(
      new CodeInstruction(OpCodes.Ldarg_0),
      new CodeInstruction(OpCodes.Ldc_I4_S, (int)ProduceMethod.DropOvernight),
      new CodeInstruction(OpCodes.Ldnull),
      new CodeInstruction(OpCodes.Call, CreateProduceType))
    .RemoveInstructions(4);

    return matcher.InstructionEnumeration();
  }

  // Allow stashing non-hay items into silos
  //static void GameLocation_performAction_Postfix(GameLocation __instance, bool __result, string[] action, Farmer who, Location tileLocation) {
  //  if (__result && who.IsLocalPlayer && ArgUtility.TryGet(action, 0, out var value, out var error) && value == "BuildingSilo") {
  //    if (who.ActiveObject?.QualifiedItemId != "(O)178") {
  //      TriggerActionManager.TryRunAction($"{ModEntry.UniqueId}.CustomFeedSilo {who.ActiveObject?.QualifiedItemId} true", out var error2, out var exception);
  //      // TODO: LOG EXCEPTIONS OR SOMETHING
  //    }
  //  }
  //}

  // Override hay with override feed
  static bool SObject_CheckForActionOnFeedHopper_Prefix(SObject __instance, ref bool __result, Farmer who, bool justCheckingForActivity = false) {
    if (justCheckingForActivity || who.ActiveObject != null || !(__instance.Location is AnimalHouse animalHouse) || !AnimalUtils.BuildingHasFeedOverride(__instance.Location)) {
      return true;
    }
    //if (who.freeSpotsInInventory() > 0) {
    //  who.addItemToInventory(SiloUtils.GetFeedFromAnySilo(itemId!, animalHouse.animalsThatLiveHere.Count));
    //  Game1.playSound("shwip");
    //}
    __result = true;
    return false;
  }

  // Check for any additional tiles with custom feed and add them
  static void SObject_CheckForActionOnFeedHopper_Postfix(SObject __instance, Farmer who, bool justCheckingForActivity = false) {
    if (justCheckingForActivity || !(__instance.Location is AnimalHouse animalHouse) || who.ActiveObject != null) {
      return;
    }
    HashSet<string> itemIds = new();
    for (int i = 0; i < animalHouse.map.Layers[0].LayerWidth; i++) {
      for (int j = 0; j < animalHouse.map.Layers[0].LayerHeight; j++) {
        var itemId = AnimalUtils.GetCustomFeedForTile(animalHouse, i, j);
        if (itemId is not null && !animalHouse.objects.ContainsKey(new Vector2(i, j))) {
          itemIds.Add(itemId);
        }
      }
    }
    bool shownMessage = false;
    foreach (var itemId in itemIds) {
      if (who.freeSpotsInInventory() > 0) {
        var obj = SiloUtils.GetFeedFromAnySilo(itemId, animalHouse.animalLimit.Value);
        if (obj is not null) {
          who.addItemToInventory(obj);
          Game1.playSound("shwip");
        } else if (!shownMessage) {
          Game1.drawObjectDialogue(ModEntry.Helper.Translation.Get($"{ModEntry.UniqueId}.HopperEmpty"));
          shownMessage = true;
        }
      } else if (!shownMessage) {
        Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
        shownMessage = true;
      }
    }
    SiloUtils.MaybeResetHopperNextIndex(__instance);
  }

  // Disable dropping hay into the hay hopper if feed override is specified
  //static bool SObject_performObjectDropInAction_Prefix(SObject __instance, ref bool __result, Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false) {
  //  if (AnimalUtils.BuildingHasFeedOverride(__instance.Location) && __instance.QualifiedItemId == "(BC)99" && dropInItem.QualifiedItemId == "(O)178") {
  //    __result = false;
  //    return false;
  //  }
  //  return true;
  //}

  static void SObject_performObjectDropInAction_Postfix(SObject __instance, ref bool __result, Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false) {
    if (dropInItem is null || __instance.QualifiedItemId != "(BC)99" || dropInItem.QualifiedItemId == "(O)178") {
      return;
    }
    int remainingCount = SiloUtils.StoreFeedInAnySilo(dropInItem.QualifiedItemId, dropInItem.Stack, probe);
    if (remainingCount < dropInItem.Stack) {
      if (probe) {
        __result = true;
        return;
      }
      Game1.playSound("Ship");
      DelayedAction.playSoundAfterDelay("grassyStep", 100);
      dropInItem.Stack = remainingCount;
      if (dropInItem.Stack <= 0) {
        __result = true;
        return;
      }
      __result = false;
      SiloUtils.MaybeResetHopperNextIndex(__instance);
    }
  }

  static void FarmAnimal_UpdateRandomMovements_Prefix(FarmAnimal __instance) {
    if (!Game1.IsMasterGame || Game1.timeOfDay >= 2000 || __instance.pauseTimer > 0) {
      return;
    }
    if (AnimalUtils.AnimalIsOutsideForager(__instance) &&
        __instance.currentLocation.IsOutdoors &&
        __instance.fullness.Value < 255 &&
        !__instance.IsActuallySwimming() &&
        Game1.random.NextDouble() < 0.0015 &&
        !__instance.isEating.Value) {
      __instance.Eat(__instance.currentLocation);
    }
  }

  static bool shouldNotConsumeGrass = false;

  static void FarmAnimal_Eat_Prefix(FarmAnimal __instance, GameLocation location) {
    if (AnimalUtils.AnimalIsOutsideForager(__instance)) {
      shouldNotConsumeGrass = true;
    }
  }

  static void FarmAnimal_Eat_Postfix(FarmAnimal __instance, GameLocation location) {
    shouldNotConsumeGrass = false;
  }

  static bool Grass_reduceBy_Prefix(int number, bool showDebris) {
    if (shouldNotConsumeGrass) {
      shouldNotConsumeGrass = false;
      return false;
    }
    return true;
  }

  // These patches handle the extra produce queue
  static void FarmAnimal_dayUpdate_Postfix(FarmAnimal __instance, GameLocation environment) {
    ExtraProduceUtils.DecrementProduceDays(__instance);
    ExtraProduceUtils.QueueExtraProduceIds(__instance, environment);
  }

  static void MilkPail_beginUsing_Prefix(MilkPail __instance, GameLocation location, int x, int y, Farmer who) {
    x = (int)who.GetToolLocation().X;
    y = (int)who.GetToolLocation().Y;
    var animal = Utility.GetBestHarvestableFarmAnimal(toolRect: new Microsoft.Xna.Framework.Rectangle(x - 32, y - 32, 64, 64), animals: location.animals.Values, tool: __instance);
    if (animal is not null) {
      ExtraProduceUtils.ReplaceCurrentProduceWithMatching(animal, FarmAnimalHarvestType.HarvestWithTool, "Milk Pail");
    }
  }

  static void MilkPail_DoFunction_Prefix(ref FarmAnimal __state, MilkPail __instance, GameLocation location, int x, int y, int power, Farmer who) {
    if (__instance.animal is null) {
      return;
    }
    __state = __instance.animal;
  }

  static void MilkPail_DoFunction_Postfix(FarmAnimal? __state, MilkPail __instance, GameLocation location, int x, int y, int power, Farmer who) {
    if (__state is null) {
      return;
    }
    ExtraProduceUtils.PopQueueAndReplaceProduce(__state);
  }


  static void Shears_beginUsing_Prefix(Shears __instance, GameLocation location, int x, int y, Farmer who) {
    x = (int)who.GetToolLocation().X;
    y = (int)who.GetToolLocation().Y;
    var animal = Utility.GetBestHarvestableFarmAnimal(toolRect: new Microsoft.Xna.Framework.Rectangle(x - 32, y - 32, 64, 64), animals: location.animals.Values, tool: __instance);
    if (animal is not null) {
      ExtraProduceUtils.ReplaceCurrentProduceWithMatching(animal, FarmAnimalHarvestType.HarvestWithTool, "Shears");
    }
  }

  static void Shears_DoFunction_Prefix(ref FarmAnimal __state, Shears __instance, GameLocation location, int x, int y, int power, Farmer who) {
    if (__instance.animal is null) {
      return;
    }
    __state = __instance.animal;
  }

  static void Shears_DoFunction_Postfix(FarmAnimal? __state, Shears __instance, GameLocation location, int x, int y, int power, Farmer who) {
    if (__state is null) {
      return;
    }
    ExtraProduceUtils.PopQueueAndReplaceProduce(__state);
  }

  static void FarmAnimals_DigUpProduce_Postfix(FarmAnimal __instance, GameLocation location, SObject produce) {
    ExtraProduceUtils.PopQueueAndReplaceProduce(__instance);
  }

  static void FarmAnimal_behaviors_Prefix(FarmAnimal __instance, GameTime time, GameLocation location) {
    ExtraProduceUtils.ReplaceCurrentProduceWithMatching(__instance, FarmAnimalHarvestType.DigUp);
  }

  static void FarmAnimal_behaviors_Postfix(FarmAnimal __instance, ref bool __result, GameTime time, GameLocation location) {
    // Evil patch >:)
    if (!Game1.IsMasterGame || __instance.isBaby()
        //|| __instance.home is null
        || __instance.pauseTimer > 0 ||
        Game1.timeOfDay >= 2000) {
      return;
    }
    if (__instance.currentLocation.farmers.Any()) AnimalUtils.AnimalAttack(__instance, time, ref __result);
    HarvestUtils.AnimalHarvest(__instance, time, ref __result);
  }

  static void Grass_TryDropItemsOnCut_Postfix(Grass __instance, Tool? tool, bool addAnimation = true) {
    if (!(tool?.isScythe() ?? false) ||
        __instance.numberOfWeeds.Value > 0 ||
        !(__instance.grassType.Value == 1 || __instance.grassType.Value == 7)) {
      return;
    }
    Vector2 tile = __instance.Tile;
    GameLocation location = __instance.Location;
    Farmer farmer = tool.getLastFarmerToUse() ?? Game1.player;
    Random random = (Game1.IsMultiplayer ? Game1.recentMultiplayerRandom : Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)tile.X * 1000.0, (double)tile.Y * 11.0));
    foreach (var keyVal in ModEntry.grassDropExtensionDataAssetHandler.data) {
      double num = ((tool.ItemId == "66") ? 2.0 : ((tool.ItemId == "53") ? 1.5 : 1.0)) * keyVal.Value.BaseChance;
      if (farmer.currentLocation.IsWinterHere()) {
        num *= 0.33;
      }
      if (random.NextDouble() < num) {
        var itemId = ItemRegistry.QualifyItemId(keyVal.Key);
        var itemData = ItemRegistry.GetDataOrErrorItem(itemId);
        int count = ((__instance.grassType.Value != 7) ? 1 : 2);
        if (SiloUtils.ScytheHasGatherer(tool) && random.NextBool()) {
          count += 1;
        }
        var remainingCount = SiloUtils.StoreFeedInAnySilo(itemId, count);
        if (remainingCount == 0) {
          if (addAnimation) {
            TemporaryAnimatedSprite temporaryAnimatedSprite = new TemporaryAnimatedSprite(itemData.GetTextureName(), Game1.getSourceRectForStandardTileSheet(itemData.GetTexture(), itemData.SpriteIndex, 16, 16), 750f, 1, 0, farmer.Position - new Vector2(0f, 128f), flicker: false, flipped: false, farmer.Position.Y / 10000f, 0.005f, Color.White, 4f, -0.005f, 0f, 0f);
            temporaryAnimatedSprite.motion.Y = -3f + (float)Game1.random.Next(-10, 11) / 100f;
            temporaryAnimatedSprite.acceleration.Y = 0.07f + (float)Game1.random.Next(-10, 11) / 1000f;
            temporaryAnimatedSprite.motion.X = (float)Game1.random.Next(-20, 21) / 10f;
            temporaryAnimatedSprite.layerDepth = 1f - (float)Game1.random.Next(100) / 10000f;
            temporaryAnimatedSprite.delayBeforeAnimationStart = Game1.random.Next(150);
            Game1.Multiplayer.broadcastSprites(location, temporaryAnimatedSprite);
          }
          Game1.addHUDMessage(HUDMessage.ForItemGained(ItemRegistry.Create(itemId), count));
        } else if (keyVal.Value.EnterInventoryIfSilosFull) {
          farmer.addItemToInventory(ItemRegistry.Create(itemId, count));
        }
      }
    }
  }

  static IEnumerable<CodeInstruction> FarmAnimal_GetProduceID_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: GameStateQuery.CheckConditions(list[i].Condition, base.currentLocation, null, null, null, r)
    // New: GameStateQuery.CheckConditions(list[i].Condition, base.currentLocation, null, null, GetGoldenAnimalCracker(this), r)
    // Find the bloody delegate passed into RemoveAll and patch that one instead, ugh
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldftn),
        new CodeMatch(OpCodes.Newobj),
        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<FarmAnimalProduce>), nameof(List<FarmAnimalProduce>.RemoveAll)))
          )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(FarmAnimal_GetProduceID_Transpiler)}");

    GetProduceIdDelegate = (MethodInfo)matcher.Operand;

    return matcher.InstructionEnumeration();
  }

  static IEnumerable<CodeInstruction> FarmAnimal_GetProduceIDDelegate_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: GameStateQuery.CheckConditions(list[i].Condition, base.currentLocation, null, null, null, r)
    // New: GameStateQuery.CheckConditions(list[i].Condition, base.currentLocation, null, null, GetGoldenAnimalCracker(this), r)
    // 1. Find where the capture class is getting the farm animal reference
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldfld),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(FarmAnimal), nameof(FarmAnimal.currentLocation)))
    );
    var thisField = matcher.Operand;

    // 2. Use that reference and pass into the function
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldnull), // farmer
        new CodeMatch(OpCodes.Ldnull), // target item
        new CodeMatch(OpCodes.Ldnull), // input item (THIS!)
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldfld),
        new CodeMatch(OpCodes.Ldnull),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GameStateQuery), nameof(GameStateQuery.CheckConditions),
            new Type[] { typeof(string), typeof(GameLocation), typeof(Farmer), typeof(Item), typeof(Item), typeof(Random), typeof(HashSet<string>) }))
        )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(FarmAnimal_GetProduceIDDelegate_Transpiler)}");
    matcher.Advance(2)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Ldfld, thisField),
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AnimalUtils), nameof(AnimalUtils.GetGoldenAnimalCracker)))
          )
      .RemoveInstructions(1);
    return matcher.InstructionEnumeration();
  }

  static void FarmAnimal_CanLiveIn_Postfix(FarmAnimal __instance, ref bool __result, Building building) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(__instance.type.Value, out var animalExtensionData) &&
        animalExtensionData.ExtraHouses.Count() > 0 &&
        building is not null && !building.isUnderConstruction() && building.GetIndoors() is AnimalHouse &&
         (building.GetData()?.ValidOccupantTypes?.Intersect(animalExtensionData.ExtraHouses).Any() ?? false)) {
      __result = true;
    }
  }

  static IEnumerable<CodeInstruction> FarmAnimal_ReplaceRainWinterTranspiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: location.IsRainingHere()
    // New: AnimalUtils.AnimalAffectedByRain(this, location)
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameLocation), nameof(GameLocation.IsRainingHere))));
    matcher.Repeat((cm) => {
      cm
      .RemoveInstruction()
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AnimalUtils), nameof(AnimalUtils.AnimalAffectedByRain)))
      );
    });
    matcher.Start();
    // Old: location.IsWinterHere()
    // New: AnimalUtils.AnimalAffectedByWinter(this, location)
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameLocation), nameof(GameLocation.IsWinterHere))));
    matcher.Repeat((cm) => {
      cm
      .RemoveInstruction()
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AnimalUtils), nameof(AnimalUtils.AnimalAffectedByWinter)))
      );
    });
    return matcher.InstructionEnumeration();
  }

  static void SObject_OutputIncubator_Postfix(ref Item? __result, SObject machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, ref int? overrideMinutesUntilReady) {
    if (__result is not null) return;
    BuildingData? buildingData = machine.Location.ParentBuilding?.GetData();
    if (buildingData == null) {
      return;
    }
    if (FarmAnimal.TryGetAnimalDataFromEgg(inputItem, machine.Location, out string id, out var animalDataFromEgg) &&
        ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(id, out var animalExtensionData) &&
        animalExtensionData.ExtraHouses.Count() > 0 &&
        (buildingData.ValidOccupantTypes?.Intersect(animalExtensionData.ExtraHouses).Any() ?? false)) {
      overrideMinutesUntilReady = ((animalDataFromEgg.IncubationTime > 0) ? animalDataFromEgg.IncubationTime : 9000);
      __result = inputItem.getOne();
    }
  }

  //static void FarmAnimal_GetBoundingBox_Postfix(FarmAnimal __instance, ref Microsoft.Xna.Framework.Rectangle __result) {
  //  if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(__instance.type.Value, out var animalExtensionData) &&
  //      animalExtensionData.CollisionWidthOverride is not null && animalExtensionData.CollisionHeightOverride is not null) {
  //    Vector2 vector = __instance.Position;
  //    __result = new Microsoft.Xna.Framework.Rectangle((int)(vector.X + (float)(animalExtensionData.CollisionWidthOverride * 4 / 2) - 32f + 8f), (int)(vector.Y + (float)(animalExtensionData.CollisionHeightOverride * 4) - 64f + 8f), 32, 32);
  //  }
  //}

  //static void Character_GetSpriteWidthForPositioning_Postfix(Character __instance, ref int __result) {
  //  if (__instance is FarmAnimal animal &&
  //      ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
  //      animalExtensionData.CollisionWidthOverride is not null) {
  //    __result = animalExtensionData.CollisionWidthOverride.Value;
  //  }
  //}
  //

  static bool FarmAnimal_pet_Prefix(FarmAnimal __instance, Farmer who, bool is_auto_pet) {
    if (is_auto_pet) return true;
    return HarvestUtils.DropHarvestIfAvailable(__instance, who);
  }
}

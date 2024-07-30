using Netcode;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Events;
using StardewValley.Tools;
using StardewValley.Triggers;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.GameData.Machines;
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
          new Type[] {typeof(FarmAnimalData)}),
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
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_dayUpdate_Prefix)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_dayUpdate_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.OnDayStarted)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_OnDayStarted_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.GetHarvestType)),
        postfix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_GetHarvestType_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(AnimalHouse),
          nameof(AnimalHouse.adoptAnimal)),
        prefix: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.AnimalHouse_adoptAnimal_Prefix)));

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
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal),
          nameof(FarmAnimal.behaviors)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.FarmAnimal_behaviors_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.DayUpdate)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.SObject_DayUpdate_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(MilkPail),
          nameof(MilkPail.DoFunction)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.MilkPail_DoFunction_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Shears),
          nameof(Shears.DoFunction)),
        transpiler: new HarmonyMethod(typeof(AnimalDataPatcher), nameof(AnimalDataPatcher.Shears_DoFunction_Transpiler)));

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
  }

  static void FarmAnimal_isMale_Postfix(FarmAnimal __instance, ref bool __result) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(__instance.type.Value, out var animalExtensionData) &&
        animalExtensionData.MalePercentage >= 0) {
      __result = __instance.myID.Value % 100 < animalExtensionData.MalePercentage;
    }
  }

  static void FarmAnimal_CanGetProduceWithTool_Postfix(FarmAnimal __instance, ref bool __result, Tool tool) {
    if (__instance.currentProduce.Value != null &&
        ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(__instance.type.Value, out var animalExtensionData) &&
        animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(__instance.currentProduce.Value) ?? __instance.currentProduce.Value, out var animalProduceExtensionData) &&
        tool != null && tool.BaseName != null && animalProduceExtensionData.HarvestTool != null) {
      // In extremely rare cases (eg debug mode) an animal may spawn with DropOvernight produce in its body.
      // To help get the produce out, always allow them to harvest
      __result = (animalProduceExtensionData.HarvestTool == "DropOvernight") ||
        (animalProduceExtensionData.HarvestTool == tool.BaseName);
    }
  }

  static void FarmAnimal_GetTexturePath_Postfix(FarmAnimal __instance, ref string __result, FarmAnimalData data) {
    if (__instance.currentProduce.Value != null &&
        ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(__instance.type.Value, out var animalExtensionData) &&
        animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(__instance.currentProduce.Value) ?? __instance.currentProduce.Value, out var animalProduceExtensionData)) {
      if (animalProduceExtensionData.ProduceTexture != null) {
        __result = animalProduceExtensionData.ProduceTexture;
      }
      if (__instance.skinID.Value != null &&
          animalProduceExtensionData.SkinProduceTexture.TryGetValue(__instance.skinID.Value, out var skinTexture)) {
        __result = skinTexture;
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
    if (notOutsideEater) {
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
  }

  // Set up hunger state for a new day
  static bool FarmAnimal_OnDayStarted_Prefix(FarmAnimal __instance) {
    if (AnimalUtils.AnimalIsOutsideForager(__instance)) {
      return false;
    }
    return true;
  }

  static void FarmAnimal_GetHarvestType_Postfix(FarmAnimal __instance, ref FarmAnimalHarvestType? __result) {
    if (__instance.currentProduce.Value != null &&
        ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(__instance.type.Value, out var animalExtensionData) &&
        animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(__instance.currentProduce.Value) ?? __instance.currentProduce.Value, out var animalProduceExtensionData)) {
      switch (animalProduceExtensionData.HarvestTool) {
        case "DigUp":
          __result = FarmAnimalHarvestType.DigUp;
          break;
        case "Milk Pail":
        case "Shears":
          __result = FarmAnimalHarvestType.HarvestWithTool;
          break;
        // NOTE: This branch should NEVER happen (the produce should have been dropped last night) but I'm including it anyway just in case
        case "DropOvernight":
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
          SObject feedObj = SiloUtils.GetFeedFromAnySilo(feedId!);
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
        (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
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

  static SObject CreateProduce(string produceId, FarmAnimal animal) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
        animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(produceId) ?? produceId, out var animalProduceExtensionData) &&
        animalProduceExtensionData.ItemQuery != null) {
      var context = new ItemQueryContext(animal.home?.GetIndoors(), Game1.getFarmer(animal.ownerID.Value), Game1.random);
      var item = ItemQueryResolver.TryResolveRandomItem(animalProduceExtensionData.ItemQuery, context);
      if (item is SObject obj) {
        return obj;
      }
    }
    // Vanilla fallback
    return ItemRegistry.Create<SObject>(produceId);
  }

  static readonly MethodInfo ItemRegistryCreateObjectType = AccessTools
    .GetDeclaredMethods(typeof(ItemRegistry))
    .First(method => method.Name == nameof(ItemRegistry.Create) && method.IsGenericMethod)
    .MakeGenericMethod(typeof(SObject));

  static readonly MethodInfo CreateProduceType = AccessTools.Method(
      typeof(AnimalDataPatcher),
      nameof(AnimalDataPatcher.CreateProduce));


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
          new CodeInstruction(OpCodes.Call, CreateProduceType)
          )
      .RemoveInstructions(4);
    return matcher.InstructionEnumeration();
  }

  static IEnumerable<CodeInstruction> SObject_DayUpdate_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: ItemRegistry.Create<Object>("(O)" + pair2.Value.currentProduce.Value);
    // New: AnimalDataPatcher.CreateProduce("(O)" + pair2.currentProduce.Value, pair2);
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldstr, "(O)"),
        new CodeMatch(OpCodes.Ldloca_S),
        new CodeMatch(OpCodes.Call),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FarmAnimal), nameof(FarmAnimal.currentProduce))),
        new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(NetFieldBase<string, NetString>), nameof(NetFieldBase<string, NetString>.Value))),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(String), nameof(String.Concat), new Type[] {typeof(string), typeof(string)})),
        new CodeMatch(OpCodes.Ldc_I4_1),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Call, ItemRegistryCreateObjectType)
        )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(SObject_DayUpdate_Transpiler)}")
      .Advance(6)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldloca_S, 14),
          new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(KeyValuePair<long, FarmAnimal>), nameof(KeyValuePair<long, FarmAnimal>.Value))),
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
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(String), nameof(String.Concat), new Type[] {typeof(string), typeof(string)})),
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
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(String), nameof(String.Concat), new Type[] {typeof(string), typeof(string)})),
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
          new CodeInstruction(OpCodes.Call, CreateProduceType)
          )
      .RemoveInstructions(4);
    return matcher.InstructionEnumeration();
  }

  // Returns whether the animal's current produce is hardcoded to drop instead of harvested by tool
  static bool DoNotDropCurrentProduce(FarmAnimal animal, string produceId) {
    if (produceId != null && animal?.type?.Value != null &&
        ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value, out var animalExtensionData) &&
        animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(produceId) ?? produceId, out var animalProduceExtensionData) &&
        animalProduceExtensionData.HarvestTool != null) {
      return animalProduceExtensionData.HarvestTool != "DropOvernight";
    }
    return animal.GetHarvestType() != FarmAnimalHarvestType.DropOvernight;
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
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(NetInt), "op_Implicit")),
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

      // Old: animalData.HarvestType != FarmAnimalHarvestType.DropOvernight
      // New: DoNotDropCurrentProduce(animal, text)
      matcher.MatchStartForward(
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
          new CodeInstruction(OpCodes.Ldloc_S, 7),
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AnimalDataPatcher), nameof(DoNotDropCurrentProduce))));


      // Old: ItemRegistry.Create<Object>("(O)" + text);
      // New: AnimalDataPatcher.CreateProduce("(O)" + text, this);
      matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldstr, "(O)"),
        new CodeMatch(OpCodes.Ldloc_S),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(String), nameof(String.Concat), new Type[] {typeof(string), typeof(string)})),
        new CodeMatch(OpCodes.Ldc_I4_1),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Call, ItemRegistryCreateObjectType)
      )
      .ThrowIfNotMatch($"Could not find entry point for item create {nameof(FarmAnimal_dayUpdate_Transpiler)}")
      .Advance(3)
      .InsertAndAdvance(
        new CodeInstruction(OpCodes.Ldarg_0),
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
        var obj = SiloUtils.GetFeedFromAnySilo(itemId, animalHouse.animalsThatLiveHere.Count);
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
    }
  }
  
  static void FarmAnimal_UpdateRandomMovements_Prefix(FarmAnimal __instance) {
    if (!Game1.IsMasterGame || Game1.timeOfDay >= 2000 || __instance.pauseTimer > 0) {
      return;
    }
    if (AnimalUtils.AnimalIsOutsideForager(__instance) &&
        __instance.currentLocation.isOutdoors.Value &&
        __instance.fullness.Value < 255 &&
        !__instance.IsActuallySwimming() &&
        Game1.random.NextDouble() < 0.0015 &&
        !__instance.isEating.Value) {
      __instance.Eat(__instance.currentLocation);
    }
  }

  static bool shouldNotConsumeGrass = false;

  static void FarmAnimal_Eat_Prefix(FarmAnimal __instance, GameLocation location) {
    if (AnimalUtils.AnimalIsOutsideForager(__instance) && (__instance.GetAnimalData()?.GrassEatAmount ?? 2) == 0) {
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
}

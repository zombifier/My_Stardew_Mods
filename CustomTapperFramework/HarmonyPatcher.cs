using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Delegates;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Selph.StardewMods.MachineTerrainFramework;

using SObject = StardewValley.Object;

public class HarmonyPatcher {
  public static void ApplyPatches(Harmony harmony) {
    // Patch object interactions
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.canBePlacedHere)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_canBePlacedHere_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.placementAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_placementAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.checkForAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_checkForAction_Prefix)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_checkForAction_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.performRemoveAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_performRemoveAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.actionOnPlayerEntry)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_actionOnPlayerEntry_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.updateWhenCurrentLocation)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_updateWhenCurrentLocation_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.draw),
          new Type[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)}),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_draw_Prefix)),
        transpiler: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_draw_Transpiler)));

    // Water planter patches

    harmony.Patch(
        original: AccessTools.Method(typeof(IndoorPot),
          nameof(IndoorPot.checkForAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.IndoorPot_checkForAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(IndoorPot),
          nameof(IndoorPot.draw),
          new Type[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)}),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.IndoorPot_draw_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(IndoorPot),
          nameof(IndoorPot.performObjectDropInAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.IndoorPot_performObjectDropInAction_Prefix)));

    // Patch tool actions
    harmony.Patch(
        original: AccessTools.Method(typeof(FruitTree),
          nameof(FruitTree.performToolAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.FruitTree_performToolAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Tree),
          nameof(Tree.performToolAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.Tree_performToolAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(GiantCrop),
          nameof(GiantCrop.performToolAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.GiantCrop_performToolAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Tree),
          nameof(Tree.UpdateTapperProduct)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.Tree_UpdateTapperProduct_Prefix)));

    // Misc patches for water pot logic
    harmony.Patch(
        original: AccessTools.Method(typeof(GameLocation),
          nameof(GameLocation.doesTileSinkDebris)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.GameLocation_doesTileSinkDebris_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(HoeDirt),
          nameof(HoeDirt.canPlantThisSeedHere)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.HoeDirt_canPlantThisSeedHere_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(HoeDirt),
          nameof(HoeDirt.paddyWaterCheck)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.HoeDirt_paddyWaterCheck_Postfix)));

    // Machine condition logic patch
    harmony.Patch(
        original: AccessTools.Method(
          typeof(StardewValley.MachineDataUtility),
          nameof(StardewValley.MachineDataUtility.GetOutputData),
          new Type[] { typeof(SObject), typeof(MachineData), typeof(MachineOutputRule),
          typeof(Item), typeof(Farmer), typeof(GameLocation) }),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(HarmonyPatcher.MachineDataUtility_GetOutputDataParent_Prefix)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(HarmonyPatcher.MachineDataUtility_GetOutputDataParent_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(
          typeof(StardewValley.MachineDataUtility),
          nameof(StardewValley.MachineDataUtility.GetOutputData),
          new Type[] { typeof(List<MachineItemOutput>), typeof(bool), typeof(Item),
          typeof(Farmer), typeof(GameLocation) }),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(HarmonyPatcher.MachineDataUtility_GetOutputData_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(MachineDataUtility),
          nameof(MachineDataUtility.GetOutputItem)),
        transpiler: new HarmonyMethod(typeof(HarmonyPatcher), nameof(HarmonyPatcher.MachineDataUtility_GetOutputItem_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.onReadyForHarvest)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(HarmonyPatcher.SObject_onReadyForHarvest_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.PlaceInMachine)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(HarmonyPatcher.SObject_PlaceInMachine_postfix)));
    
    // Custom lightning rod patch
    harmony.Patch(
        original: AccessTools.Method(typeof(Utility),
          nameof(Utility.performLightningUpdate)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(Utility_performLightningUpdate_Prefix)), Priority.High + 1));
  }

  static void SObject_canBePlacedHere_Postfix(SObject __instance, ref bool __result, GameLocation l, Vector2 tile, CollisionMask collisionMask = CollisionMask.All, bool showError = false) {
    // Check crab pots
    if (Utils.IsCrabPot(__instance)) {
      __result = CrabPot.IsValidCrabPotLocationTile(l, (int)tile.X, (int)tile.Y);
      return;
    }

    // Disallow bushes for now
    if (__instance.IsTeaSapling() && l.objects.TryGetValue(tile, out var pot) &&
        WaterIndoorPotUtils.isWaterPlanter(pot)) {
      __result = false;
      return;
    }

    // Check tappers
    if (!__instance.IsTapper()) return;
    __result = Utils.IsModdedTapperPlaceableAt(__instance, l, tile, out bool isVanillaTapper, out var unnused, out var unused2);
    if (isVanillaTapper) {
      __result = true;
    }
  }

  static bool SObject_placementAction_Prefix(SObject __instance, ref bool __result, GameLocation location, int x, int y, Farmer? who = null) {
    Vector2 vector = new Vector2(x / 64, y / 64);
    //if (__instance.IsTapper() &&
    //    Utils.GetFeatureAt(location, vector, out var feature, out var centerPos) &&
    //    !location.objects.ContainsKey(centerPos) &&
    //    Utils.GetOutputRules(__instance, feature, TileFeature.REGULAR, out bool unused) is var outputRules &&
    //    outputRules != null) {
    //  // Place tapper if able
    //  SObject @object = (SObject)__instance.getOne();
    //  @object.heldObject.Value = null;
    //  @object.Location = location;
    //  @object.TileLocation = centerPos;
    //  location.objects.Add(centerPos, @object);
    //  Utils.UpdateTapperProduct(@object);
    //  location.playSound("axe");
    //  Utils.Shake(feature, centerPos);
    //  __result = true;
    //  return false;
    //}
    if (Utils.IsCrabPot(__instance) &&
        CrabPot.IsValidCrabPotLocationTile(location,
          (int)vector.X, (int)vector.Y)) {
      if (__instance.QualifiedItemId == WaterIndoorPotUtils.WaterPlanterQualifiedItemId) {
        IndoorPot @object = new IndoorPot(vector);
        WaterIndoorPotUtils.transformIndoorPotToItem(@object, WaterIndoorPotUtils.WaterPlanterItemId);
        @object.hoeDirt.Value.state.Value = 1;
        @object.hoeDirt.Value.modData[WaterIndoorPotUtils.HoeDirtIsWaterModDataKey] = "true";
        @object.hoeDirt.Value.modData[WaterIndoorPotUtils.HoeDirtIsWaterPlanterModDataKey] = "true";
        __result = CustomCrabPotUtils.placementAction(@object, location, x, y, who);
      } else {
        SObject @object = (SObject)__instance.getOne();
        __result = CustomCrabPotUtils.placementAction(@object, location, x, y, who);
        @object.performDropDownAction(who);
      }
      return false;
    }
    if (__instance.QualifiedItemId == WaterIndoorPotUtils.WaterPotQualifiedItemId ||
        WaterIndoorPotUtils.IsCustomPot(__instance)) {
        IndoorPot @object = new IndoorPot(vector);
        WaterIndoorPotUtils.transformIndoorPotToItem(@object, __instance.ItemId);
        // water the pot if water pot
        if (__instance.QualifiedItemId == WaterIndoorPotUtils.WaterPotQualifiedItemId) {
          @object.hoeDirt.Value.modData[WaterIndoorPotUtils.HoeDirtIsWaterModDataKey] = "true";
          @object.hoeDirt.Value.state.Value = 1;
        }
        location.objects.Add(vector, @object);
        location.playSound("woodyStep");
        __result = true;
        return false;
    }
    return true;
  }

  static void SObject_performRemoveAction_Prefix(SObject __instance) {
    if (Utils.IsCrabPot(__instance)) {
      CustomCrabPotUtils.performRemoveAction(__instance.Location, __instance.TileLocation);
    }
  }

  static void SObject_actionOnPlayerEntry_Prefix(SObject __instance) {
    if (Utils.IsCrabPot(__instance)) {
      CustomCrabPotUtils.actionOnPlayerEntry(__instance.Location, __instance.TileLocation);
    }
  }

  static void SObject_updateWhenCurrentLocation_Prefix(SObject __instance, GameTime time) {
    if (Utils.IsCrabPot(__instance)) {
      CustomCrabPotUtils.updateWhenCurrentLocation(__instance, time);
    }
  }
  

  // If a tapper is present, only shake and remove the tapper instead of damaging the tree.
  static bool FruitTree_performToolAction_Prefix(FruitTree __instance, ref bool __result, Tool t, int explosion, Vector2 tileLocation) {
    if (__instance.Location.objects.TryGetValue(tileLocation, out SObject obj) &&
        obj.IsTapper()) {
      __instance.shake(tileLocation, false);
      return false;
    }
    return true;
  }

  // If a tapper is present, only shake and remove the tapper instead of damaging the tree.
  static bool Tree_performToolAction_Prefix(Tree __instance, ref bool __result, Tool t, int explosion, Vector2 tileLocation) {
    if (__instance.Location.objects.TryGetValue(tileLocation, out SObject obj) &&
        obj.IsTapper() && !__instance.tapped.Value) {
      __instance.shake(tileLocation, false);
      return false;
    }
    return true;
  }

  // If a tapper is present, only shake and remove the tapper instead of damaging the tree.
  static bool GiantCrop_performToolAction_Prefix(GiantCrop __instance, ref bool __result, Tool t, int damage, Vector2 tileLocation) {
    Vector2 centerPos = __instance.Tile;
        centerPos.X = (int)centerPos.X + (int)__instance.width.Value / 2;
        centerPos.Y = (int)centerPos.Y + (int)__instance.height.Value - 1;
    if (__instance.Location.objects.TryGetValue(centerPos, out SObject obj) &&
        obj.IsTapper() && t.isHeavyHitter() && !(t is MeleeWeapon)) {
      // Has tapper, try to dislodge it
      // For some reason performToolAction on the object directly doesn't work
      obj.playNearbySoundAll("hammer");
      obj.performRemoveAction();
      __instance.Location.objects.Remove(centerPos);
      Game1.createItemDebris(obj, centerPos * 64f, -1);
      // Shake the crop
      __instance.shakeTimer = 100f;
      __instance.NeedsUpdate = true;
      return false;
    }
    return true;
  }

  // For tappers: Save the currently held item so the PreviousItemId rule can work, and regenerate the output if that is enabled
  static bool SObject_checkForAction_Prefix(SObject __instance, out Item? __state, ref bool __result, Farmer who, bool justCheckingForActivity) {
    __state = null;
    // Crab pot code
    if (Utils.IsCrabPot(__instance)) {
      if (CustomCrabPotUtils.checkForAction(__instance, who, justCheckingForActivity)) {
        __result = true;
        return false;
      }
      if (!justCheckingForActivity)
        CustomCrabPotUtils.resetRemovalTimer(__instance);
    }
    // Common code
    if (!__instance.IsTapper() || justCheckingForActivity || !__instance.readyForHarvest.Value) return true;
    __state = __instance.heldObject.Value;
    var rules = Utils.GetOutputRulesForPlacedTapper(__instance, out var unused, __instance.lastOutputRuleId.Value);
    if (rules != null && rules.Count > 0 && rules[0].RecalculateOnCollect) {
      Item newItem = ItemQueryResolver.TryResolveRandomItem(rules[0], new ItemQueryContext(__instance.Location, who, null, "MachineTerrainFramework custom tapper '" + __instance.QualifiedItemId + "' > output rules"),
          avoidRepeat: false, null, (string id) =>
          id.Replace("DROP_IN_ID", /*inputItem?.QualifiedItemId ??*/ "0")
          .Replace("NEARBY_FLOWER_ID", MachineDataUtility.GetNearbyFlowerItemId(__instance) ?? "-1"));
      if (newItem is SObject newObject)
      __instance.heldObject.Value = newObject;
    }
    return true;
  }

  // Update the tapper product after collection
  static void SObject_checkForAction_Postfix(SObject __instance, Item? __state, bool __result, Farmer who, bool justCheckingForActivity) {
    if (__state == null || !__result) return;
    Utils.UpdateTapperProduct(__instance);
  }

  static bool SObject_draw_Prefix(SObject __instance, SpriteBatch spriteBatch, int x, int y, float alpha = 1f) {
    // Crab pot draw code
    if (Utils.IsCrabPot(__instance) && __instance.Location != null) {
      CustomCrabPotUtils.draw(__instance, spriteBatch, x, y, alpha);
      return false;
    }
    return true;
  }
  // Patch the draw code to push the tapper draw layer up a tiny amount. ugh...
  public static IEnumerable<CodeInstruction> SObject_draw_Transpiler(IEnumerable<CodeInstruction> instructions) {
    var codes = new List<CodeInstruction>(instructions);
    bool afterIsTapperCall = false;
    for (var i = 0; i < codes.Count; i++) {
      if (codes[i].opcode == OpCodes.Callvirt &&
          codes[i].operand is MethodInfo method &&
          method == AccessTools.Method(typeof(SObject), nameof(SObject.IsTapper))) {
        afterIsTapperCall = true;
      }
      if (afterIsTapperCall &&
          codes[i].opcode == OpCodes.Call &&
          codes[i].operand is MethodInfo method2 &&
          method2 == AccessTools.Method(typeof(Math),
            nameof(Math.Max),
            new Type[] { typeof(float), typeof(float) })) {
        afterIsTapperCall = false;
        // 0.001f seems to work...
        // TODO: calc this better
        yield return new CodeInstruction(OpCodes.Ldc_R4, 0.001f);
        yield return new CodeInstruction(OpCodes.Add);
      }
      yield return codes[i];
    }
  }

  static bool IndoorPot_checkForAction_Prefix(IndoorPot __instance, ref bool __result, Farmer who, bool justCheckingForActivity) {
    if (__instance.QualifiedItemId == WaterIndoorPotUtils.WaterPlanterQualifiedItemId &&
        CustomCrabPotUtils.checkForAction(__instance, who, justCheckingForActivity)) {
      __result = true;
      return false;
    }
    return true;
  }

  static bool IndoorPot_draw_Prefix(IndoorPot __instance, SpriteBatch spriteBatch, int x, int y, float alpha = 1f) {
    if (__instance.QualifiedItemId == WaterIndoorPotUtils.WaterPlanterQualifiedItemId &&
        __instance.Location != null) {
      WaterIndoorPotUtils.draw(__instance, spriteBatch, x, y, alpha);
      return false;
    }
    if (WaterIndoorPotUtils.GetDrawOverridesForPot(__instance, out var cropYOffset, out var cropTintColor)) {
      WaterIndoorPotUtils.drawPotOverride(__instance, spriteBatch, x, y, alpha, cropYOffset, cropTintColor);
      return false;
    }
    return true;
  }

  // Disallow tea bushes in water planters and custom pots
  static bool IndoorPot_performObjectDropInAction_Prefix(IndoorPot __instance, ref bool __result, Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false) {
    if (!probe &&
        (__instance.QualifiedItemId == WaterIndoorPotUtils.WaterPlanterQualifiedItemId ||
         __instance.QualifiedItemId == WaterIndoorPotUtils.WaterPotQualifiedItemId ||
         (WaterIndoorPotUtils.IsCustomPot(__instance) && !WaterIndoorPotUtils.AcceptsRegularCrops(__instance))) &&
        dropInItem.QualifiedItemId == "(O)251") {
      __result = false;
      return false;
    }
    return true;
  }

  static bool Tree_UpdateTapperProduct_Prefix(Tree __instance, SObject tapper, SObject previousOutput, bool onlyPerformRemovals) {
    // Context tag based
    if (Utils.IsCustomTreeTappers(tapper)) {
      return false;
    }

    // Legacy
    var rules = Utils.GetOutputRules(tapper, __instance, out var disallowBaseTapperRules);
    if (rules != null || disallowBaseTapperRules) {
      return false;
    }
    return true;
  }

  // Don't sink debris if there's a building at that tile or in the adjacent tiles
  static void GameLocation_doesTileSinkDebris_Postfix(GameLocation __instance, ref bool __result, int xTile, int yTile, Debris.DebrisType type) {
    if (__instance.objects.ContainsKey(new Vector2(xTile, yTile)) ||
        __instance.objects.ContainsKey(new Vector2(xTile+1, yTile)) ||
        __instance.objects.ContainsKey(new Vector2(xTile, yTile+1)) ||
        __instance.objects.ContainsKey(new Vector2(xTile-1, yTile)) ||
        __instance.objects.ContainsKey(new Vector2(xTile, yTile-1)) ||
        // diagonal
        __instance.objects.ContainsKey(new Vector2(xTile+1, yTile+1)) ||
        __instance.objects.ContainsKey(new Vector2(xTile+1, yTile-1)) ||
        __instance.objects.ContainsKey(new Vector2(xTile-1, yTile+1)) ||
        __instance.objects.ContainsKey(new Vector2(xTile-1, yTile-1))
              ) {
      __result = false;
    }
  }

  static void HoeDirt_canPlantThisSeedHere_Postfix(HoeDirt __instance, ref bool __result, string itemId, bool isFertilizer = false) {
    if (!__result || isFertilizer) return;
    WaterIndoorPotUtils.canPlant(__instance, itemId, ref __result);
  }

  // Make paddy crops inside water planters considered to be near water.
  static void HoeDirt_paddyWaterCheck_Postfix(HoeDirt __instance, ref bool __result, bool forceUpdate = false) {
    if (__result ||
        !__instance.modData.ContainsKey(WaterIndoorPotUtils.HoeDirtIsWaterPlanterModDataKey) ||
        !__instance.hasPaddyCrop()) return;
    __instance.nearWaterForPaddy.Value = 1;
    __result = true;
  }

  internal static string TerrainConditionKey = $"{ModEntry.UniqueId}.TerrainCondition";
  private static SObject? machineBeingChecked = null;

  // This is a super hacky way of essentially passing in the machine object as an extra parameter to (the second) GetOutputData,
  // but it's the only way that guarantees maximum compatibility and not outright replace the entire function, so...
  static void MachineDataUtility_GetOutputDataParent_Prefix(SObject machine, MachineData machineData, MachineOutputRule outputRule, Item inputItem, Farmer who, GameLocation location) {
    machineBeingChecked = machine;
  }
  static void MachineDataUtility_GetOutputDataParent_Postfix(SObject machine, MachineData machineData, MachineOutputRule outputRule, Item inputItem, Farmer who, GameLocation location) {
    machineBeingChecked = null;
  }

  // Checks for terrain conditions and remove rules that cannot be satisfied
  static void MachineDataUtility_GetOutputData_Prefix(ref List<MachineItemOutput> outputs,
      bool useFirstValidOutput, Item inputItem, Farmer who,
      GameLocation location) {
    if (outputs == null || outputs.Count < 0 || machineBeingChecked == null) {
      return;
    }
    List<MachineItemOutput> newOutputs = new List<MachineItemOutput>();
    foreach (MachineItemOutput output in outputs) {
      if (output.CustomData == null || !output.CustomData.TryGetValue(TerrainConditionKey, out var terrainCondition)) {
        newOutputs.Add(output);
        continue;
      }
      Utils.GetFeatureAt(machineBeingChecked.Location, machineBeingChecked.TileLocation, out var feature, out var unused);
      Item? produceItem = Utils.GetFeatureItem(feature, who);
      var customFields = new Dictionary<string, object>() {
        {"Tile", machineBeingChecked.TileLocation}
      };
      GameStateQueryContext context = new(location, who, produceItem, machineBeingChecked, Game1.random, null, customFields);
      if (GameStateQuery.CheckConditions(terrainCondition, context)) {
        newOutputs.Add(output);
      }
    }
    outputs = newOutputs;
  }

  static void PopulateContext(ItemQueryContext context, SObject machine) {
    if (context.CustomFields is null) {
      context.CustomFields = new();
    }
    context.CustomFields["Tile"] = machine.TileLocation;
    context.CustomFields["Machine"] = machine;
  }

  // Insert "Machine" and "Tile" into the item query context
  public static IEnumerable<CodeInstruction> MachineDataUtility_GetOutputItem_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Old: ItemQueryContext context = new ItemQueryContext(machine.Location, who, Game1.random);
    // New: insert PopulateContext(context, machine) below
    matcher.MatchEndForward(
        new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(ItemQueryContext), [typeof(GameLocation), typeof(Farmer), typeof(Random)])),
        new CodeMatch(OpCodes.Stloc_1))
      .ThrowIfNotMatch($"Could not find entry point for {nameof(MachineDataUtility_GetOutputItem_Transpiler)}")
      .Advance(1)
      .InsertAndAdvance(
        new CodeInstruction(OpCodes.Ldloc_1),
        new CodeInstruction(OpCodes.Ldarg_0),
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatcher), nameof(HarmonyPatcher.PopulateContext)))
    );
    return matcher.InstructionEnumeration();
  }
  static string readyForHarvestModData = $"{ModEntry.UniqueId}.CountForPerfection";

  public static void SObject_onReadyForHarvest_Postfix(SObject __instance) {
    if (__instance.heldObject.Value?.modData?.ContainsKey(readyForHarvestModData) ?? false) {
      // mark fish caught for achievements and stats
      IDictionary<string, string> fishData = DataLoader.Fish(Game1.content);
      if (fishData.TryGetValue(__instance.heldObject.Value.ItemId, out string? fishRow)) {
          var item = __instance.heldObject.Value;
          int size = 0;
          try {
            string[] fields = fishRow.Split('/');
            bool isValid = fields.Length > 5;
            bool isRegularFish = fields.Length > 10;
            int lowerSize = isValid ? (isRegularFish ? Convert.ToInt32(fields[3]) : Convert.ToInt32(fields[5])) : 1;
            int upperSize = isValid ? (isRegularFish ? Convert.ToInt32(fields[4]) : Convert.ToInt32(fields[6])) : 1;
            size = Game1.random.Next(lowerSize, upperSize + 1);
          }
          catch (Exception e) {
            ModEntry.StaticMonitor.Log($"Error getting fish length: {e.Message}", LogLevel.Warn);
          }
          (Game1.GetPlayer(__instance.owner.Value) ?? Game1.player).caughtFish(item.ItemId, size);
      }
    }
  }
  static void SObject_PlaceInMachine_postfix(SObject __instance, bool __result, MachineData machineData, Item inputItem, bool probe, Farmer who, bool showMessages = true, bool playSounds = true) {
    if (Utils.IsCrabPot(__instance) && __result && !probe) {
      CustomCrabPotUtils.resetRemovalTimer(__instance);
    }
  }

  public static bool Utility_performLightningUpdate_Prefix(int time_of_day) {
    Random random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.stats.DaysPlayed, time_of_day);
    if (random.NextDouble() < 0.125 + Game1.player.team.AverageDailyLuck() + Game1.player.team.AverageLuckLevel() / 100.0) {
      Farm.LightningStrikeEvent lightningStrikeEvent = new Farm.LightningStrikeEvent();
      lightningStrikeEvent.bigFlash = true;
      Farm farm = Game1.getFarm();
      List<Vector2> list = new List<Vector2>();
      foreach (KeyValuePair<Vector2, SObject> pair in farm.objects.Pairs) {
        if (Utils.IsCustomLightningRod(pair.Value.QualifiedItemId)) {
          list.Add(pair.Key);
        }
      }
      if (list.Count > 0) {
        for (int i = 0; i < 2; i++) {
          Vector2 vector = random.ChooseFrom(list);
          if (farm.objects[vector].heldObject.Value == null) {
            if (!Utils.UpdateCustomLightningRod(farm.objects[vector])) {
              farm.objects[vector].heldObject.Value = ItemRegistry.Create<SObject>("(O)787");
              farm.objects[vector].minutesUntilReady.Value = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay);
              farm.objects[vector].shakeTimer = 1000;
              lightningStrikeEvent.createBolt = true;
              lightningStrikeEvent.boltPosition = vector * 64f + new Vector2(32f, 0f);
              farm.lightningStrikeEvent.Fire(lightningStrikeEvent);
            }
            return false;
          }
        }
      }
    }
    return true;
  }

  // This transpiler is cleaner, but alas we need a skipping prefix to beat Safe Lightning's...
  public static IEnumerable<CodeInstruction> Utility_performLightningUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
    CodeMatcher matcher = new(instructions, generator);
    // Old: if (pair.Value.QualifiedItemId == "(BC)9")
    // New: if (Utils.IsCustomLightningRod(pair.Value.QualifiedItemId) || ...
    matcher.MatchEndForward(
        new CodeMatch(OpCodes.Ldloca_S),
        new CodeMatch(OpCodes.Call),
        new CodeMatch(OpCodes.Callvirt),
        new CodeMatch(OpCodes.Ldstr, "(BC)9"),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(String), "op_Equality")),
        new CodeMatch(OpCodes.Brfalse_S)
        )
      .ThrowIfNotMatch($"Could not find lightning rod check point for {nameof(Utility_performLightningUpdate_Transpiler)}")
      .CreateLabelWithOffsets(1, out var labelToJumpTo)
      .Advance(-5)
      // This is the "pair.Value.QualifiedItemId" block
      .InsertAndAdvance(matcher.Instructions(3))
      .InsertAndAdvance(
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Utils), nameof(Utils.IsCustomLightningRod))),
        new CodeInstruction(OpCodes.Brtrue_S, labelToJumpTo)
      );

    // Old: if (farm.objects[vector].heldObject.Value == null)
    // New: put this line below:
    // if (Utils.UpdateCustomLightningRod(farm.objects[vector])) { return; }
    matcher.MatchStartForward(
        new CodeMatch((CodeInstruction instruction) => instruction.IsLdloc()),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Property(typeof(GameLocation), nameof(GameLocation.objects))),
        new CodeMatch(OpCodes.Ldloc_S),
        new CodeMatch(OpCodes.Callvirt),
        new CodeMatch(OpCodes.Ldfld, AccessTools.Property(typeof(SObject), nameof(SObject.heldObject))),
        new CodeMatch(OpCodes.Callvirt),
        new CodeMatch(OpCodes.Brtrue)
        )
      .ThrowIfNotMatch($"Could not find lightning rod handler point for {nameof(Utility_performLightningUpdate_Transpiler)}");
    var getMachineBlock = matcher.Instructions(4);
    matcher
      .Advance(7)
      .CreateLabel(out Label regularLightningRodHandler)
      .InsertAndAdvance(getMachineBlock)
      .InsertAndAdvance(
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Utils), nameof(Utils.UpdateCustomLightningRod))),
        new CodeInstruction(OpCodes.Brfalse_S, regularLightningRodHandler),
        new CodeInstruction(OpCodes.Ret)
          );
    return matcher.InstructionEnumeration();
  }
}

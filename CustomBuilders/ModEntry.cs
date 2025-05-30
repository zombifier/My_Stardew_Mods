﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;
using StardewValley.Delegates;
using StardewValley.Menus;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;

using BlueprintEntry = StardewValley.Menus.CarpenterMenu.BlueprintEntry;

namespace Selph.StardewMods.CustomBuilders;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;
  //internal static BuilderDataAssetHandler builderDataAssetHandler = null!;
  internal static BuildingOverrideManager buildingOverrideManager = null!;

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    buildingOverrideManager = new();

    //builderDataAssetHandler = new BuilderDataAssetHandler();
    //builderDataAssetHandler.RegisterEvents(Helper);

    GameLocation.RegisterTileAction($"{UniqueId}_ShowConstruct", ShowConstruct);
    GameStateQuery.Register($"{UniqueId}_IS_BUILDER", IS_BUILDER);

    Helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;

    // Harmony!
    var harmony = new Harmony(this.ModManifest.UniqueID);
    harmony.Patch(
        original: AccessTools.Method(typeof(NPC),
          "updateConstructionAnimation"),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.NPC_updateConstructionAnimation_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(CarpenterMenu),
          nameof(CarpenterMenu.robinConstructionMessage)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.CarpenterMenu_robinConstructionMessage_Prefix)));

    harmony.Patch(
        original: AccessTools.Constructor(typeof(CarpenterMenu), new Type[] {typeof(string), typeof(GameLocation)}),
        transpiler: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.CarpenterMenu_Constructor_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(NPC),
          "doPlayRobinHammerAnimation"),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.NPC_doPlayRobinHammerAnimation_Prefix)));

    // Blueprint cost patches
    harmony.Patch(
        original: AccessTools.PropertyGetter(typeof(BlueprintEntry),
          nameof(BlueprintEntry.BuildDays)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.BlueprintEntry_BuildDays_Postfix)));
    harmony.Patch(
        original: AccessTools.PropertyGetter(typeof(BlueprintEntry),
          nameof(BlueprintEntry.BuildCost)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.BlueprintEntry_BuildCost_Postfix)));
    harmony.Patch(
        original: AccessTools.PropertyGetter(typeof(BlueprintEntry),
          nameof(BlueprintEntry.BuildMaterials)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.BlueprintEntry_BuildMaterials_Postfix)));
  }

  static string currentBuilder => Helper.Reflection.GetField<string>(Game1.currentLocation, "_constructLocationBuilderName").GetValue();

  static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e) {
    if (e.NamesWithoutLocale.Any(name => name.Name == "Data/Buildings")) {
      buildingOverrideManager.Clear();
    }
  }

  public static bool IS_BUILDER(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGet(query, 1, out var builderName, out var error)) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    return builderName == currentBuilder;
  }

  public static bool ShowConstruct(GameLocation location, string[] args, Farmer farmer, Point point){
    if (!ArgUtility.TryGet(args, 1, out var builderName, out var error) ||
        !ArgUtility.TryGetOptional(args, 2, out var direction, out error, null, allowBlank: true, "string direction") ||
        !ArgUtility.TryGetOptionalInt(args, 3, out var openTime, out error, -1, "int openTime") ||
        !ArgUtility.TryGetOptionalInt(args, 4, out var closeTime, out error, -1, "int closeTime") ||
        !ArgUtility.TryGetOptionalInt(args, 5, out var shopAreaX, out error, -1, "int shopAreaX") ||
        !ArgUtility.TryGetOptionalInt(args, 6, out var shopAreaY, out error, -1, "int shopAreaY") ||
        !ArgUtility.TryGetOptionalInt(args, 7, out var shopAreaWidth, out error, -1, "int shopAreaWidth") ||
        !ArgUtility.TryGetOptionalInt(args, 8, out var shopAreaHeight, out error, -1, "int shopAreaHeight")) {
      ModEntry.StaticMonitor.Log(error, LogLevel.Warn);
      return false;
    }

    // Check NPC is within area
    if (shopAreaX != -1 || shopAreaY != -1 || shopAreaWidth != -1 || shopAreaHeight != -1) {
      if (shopAreaX == -1 || shopAreaY == -1 || shopAreaWidth == -1 || shopAreaHeight == -1) {
        ModEntry.StaticMonitor.Log("when specifying any of the shop area 'x y width height' arguments (indexes 5-8), all four must be specified", LogLevel.Warn);
        return false;
      }
      Rectangle ownerSearchArea = new(shopAreaX, shopAreaY, shopAreaWidth, shopAreaHeight);
      bool foundNpc = false;
      IList<NPC>? npcs = location.currentEvent?.actors;
      npcs ??= location.characters;
      foreach (var npc in npcs) {
        if (npc.Name == builderName && ownerSearchArea.Contains(npc.TilePoint)) {
          foundNpc = true;
        }
      }
      if (!foundNpc) {
        ModEntry.StaticMonitor.Log($"{builderName} not found in area.");
        return false;
      }
    }

    // Check direction
    switch (direction) {
      case "down":
        if (farmer.TilePoint.Y < point.Y) {
          ModEntry.StaticMonitor.Log($"player not down of {builderName}.");
          return false;
        }
        break;
      case "up":
        if (farmer.TilePoint.Y > point.Y) {
          ModEntry.StaticMonitor.Log($"player not up of {builderName}.");
          return false;
        }
        break;
      case "left":
        if (farmer.TilePoint.X > point.X) {
          ModEntry.StaticMonitor.Log($"player not left of {builderName}.");
          return false;
        }
        break;
      case "right":
        if (farmer.TilePoint.X < point.X) {
          ModEntry.StaticMonitor.Log($"player not right of {builderName}.");
          return false;
        }
        break;
    }

    // Check opening and closing times
    if ((openTime >= 0 && Game1.timeOfDay < openTime) || (closeTime >= 0 && Game1.timeOfDay >= closeTime)) {
      ModEntry.StaticMonitor.Log($"{builderName} is closed.");
      return false;
    }

    if (Game1.IsThereABuildingUnderConstruction(builderName)) {
      var buildingData = Game1.GetBuildingUnderConstruction(builderName).GetData();
      var name = TokenParser.ParseText(buildingData?.Name ?? "");
      var nameForGeneralType = TokenParser.ParseText(buildingData?.NameForGeneralType ?? "");
      Game1.DrawDialogue(Game1.getCharacterFromName(builderName),
          $"Characters/Dialogue/{builderName}:{UniqueId}_Busy",
          name.ToLower(),
          nameForGeneralType.ToLower(),
          name,
          nameForGeneralType);
    } else {
      location.ShowConstructOptions(builderName);
    }
    return true;
  }

  static void NPC_updateConstructionAnimation_Postfix(NPC __instance) {
    if (__instance.Name == "Robin") return;
    if (Game1.IsMasterGame && !Utility.isFestivalDay() && (!Game1.isGreenRain || Game1.year > 1)) {
      //if (Game1.player.daysUntilHouseUpgrade.Value > 0) {
      //  Farm farm = Game1.getFarm();
      //  Game1.warpCharacter(__instance, farm.NameOrUniqueName, new Vector2(farm.GetMainFarmHouseEntry().X + 4, farm.GetMainFarmHouseEntry().Y - 1));
      //  __instance.isPlayingRobinHammerAnimation = false;
      //  __instance.shouldPlayRobinHammerAnimation.Value = true;
      //  return;
      //}
      if (Game1.IsThereABuildingUnderConstruction(__instance.Name)) {
        Building buildingUnderConstruction = Game1.GetBuildingUnderConstruction(__instance.Name);
        if (buildingUnderConstruction == null) {
          return;
        }
        GameLocation indoors = buildingUnderConstruction.GetIndoors();
        if (buildingUnderConstruction.daysUntilUpgrade.Value > 0 && indoors != null) {
          __instance.currentLocation?.characters.Remove(__instance);
          __instance.currentLocation = indoors;
          if (__instance.currentLocation != null && !__instance.currentLocation.characters.Contains(__instance)) {
            __instance.currentLocation.addCharacter(__instance);
          }
          string indoorsName = buildingUnderConstruction.GetIndoorsName();
          if (indoorsName != null && indoorsName.StartsWith("Shed")) {
            __instance.setTilePosition(2, 2);
            __instance.position.X -= 28f;
          }
          else {
            __instance.setTilePosition(1, 5);
          }
        }
        else {
          Game1.warpCharacter(__instance, buildingUnderConstruction.parentLocationName.Value, new Vector2(buildingUnderConstruction.tileX.Value + buildingUnderConstruction.tilesWide.Value / 2, buildingUnderConstruction.tileY.Value + buildingUnderConstruction.tilesHigh.Value / 2));
          __instance.position.X += 16f;
          __instance.position.Y -= 32f;
        }
        //__instance.isPlayingRobinHammerAnimation = false;
        __instance.shouldPlayRobinHammerAnimation.Value = true;
        return;
      }
      //if (Game1.RequireLocation<Town>("Town").daysUntilCommunityUpgrade.Value > 0)
      //{
      //  if (Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
      //  {
      //    Game1.warpCharacter(__instance, "Backwoods", new Vector2(41f, 23f));
      //    __instance.isPlayingRobinHammerAnimation = false;
      //    __instance.shouldPlayRobinHammerAnimation.Value = true;
      //  }
      //  else if (Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
      //  {
      //    Game1.warpCharacter(__instance, "Town", new Vector2(77f, 68f));
      //    __instance.isPlayingRobinHammerAnimation = false;
      //    __instance.shouldPlayRobinHammerAnimation.Value = true;
      //  }
      //  return;
      //}
    }
    //__instance.shouldPlayRobinHammerAnimation.Value = false;
  }

  static bool CarpenterMenu_robinConstructionMessage_Prefix(CarpenterMenu __instance) {
    if (__instance.Builder == "Robin" || __instance.Blueprint.MagicalConstruction) {
      return true;
    }
		__instance.exitThisMenu();
		Game1.player.forceCanMove();
    string dialogueKeySuffix = 
	    __instance.Blueprint.BuildDays <= 0 ? "_Instant" :
      (((__instance.Action == CarpenterMenu.CarpentryAction.Upgrade) ? "UpgradeConstruction" : "NewConstruction") +
       (Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.season) ? "_Festival" : ""));
    string displayName = __instance.Blueprint.DisplayName;
    string displayNameForGeneralType = __instance.Blueprint.DisplayNameForGeneralType;
    Game1.DrawDialogue(Game1.getCharacterFromName(__instance.Builder),
        $"Characters/Dialogue/{__instance.Builder}:{UniqueId}_{dialogueKeySuffix}",
        displayName.ToLower(),
        displayNameForGeneralType.ToLower(),
        displayName,
        displayNameForGeneralType);
    return false;
  }

  static bool IsEligibleBuilder(BuildingData data, string builder) {
    var result = new List<string>();
    if (data.Builder is not null) {
      result.Add(data.Builder);
    }
    if (data.CustomFields is not null) {
      foreach (var keyVal in data.CustomFields) {
        if (keyVal.Key.StartsWith($"{UniqueId}_ExtraBuilder")) {
          result.Add(keyVal.Value);
        }
      }
    }
    return result.Contains(builder);
  }

  //static bool IsBaseSkinEligible(BuildingData data, string builder, GameLocation? targetLocation) {
  //  if (data.CustomFields?.TryGetValue($"{UniqueId}_BaseSkinCondition", out var condition) ?? false) {
  //    return GameStateQuery.CheckConditions(condition, targetLocation);
  //  }
  //  return true;
  //}

  //static void CarpenterMenu_Constructor_Postfix(CarpenterMenu __instance, string builder, GameLocation targetLocation) {
  //  var num = __instance.Blueprints.Count;
  //  foreach (KeyValuePair<string, BuildingData> buildingDatum in Game1.buildingData) {
  //    if (!GetExtraBuilders(buildingDatum.Value).Contains(builder) ||
  //        !GameStateQuery.CheckConditions(buildingDatum.Value.BuildCondition, targetLocation) ||
  //        (buildingDatum.Value.BuildingToUpgrade != null && __instance.TargetLocation.getNumberBuildingsConstructed(buildingDatum.Value.BuildingToUpgrade) == 0) ||
  //        !__instance.IsValidBuildingForLocation(buildingDatum.Key, buildingDatum.Value, __instance.TargetLocation)) {
  //      continue;
  //    }
  //    __instance.Blueprints.Add(new CarpenterMenu.BlueprintEntry(num++, buildingDatum.Key, buildingDatum.Value, null));
  //    if (buildingDatum.Value.Skins == null) {
  //      continue;
  //    }
  //    foreach (BuildingSkin skin in buildingDatum.Value.Skins) {
  //      if (skin.ShowAsSeparateConstructionEntry &&
  //          GameStateQuery.CheckConditions(skin.Condition, __instance.TargetLocation)) {
  //        __instance.Blueprints.Add(new CarpenterMenu.BlueprintEntry(num++, buildingDatum.Key, buildingDatum.Value, skin.Id));
  //      }
  //    }
  //  }
  //}
  //
  static IEnumerable<CodeInstruction> CarpenterMenu_Constructor_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
    CodeMatcher matcher = new(instructions, generator);
    // Old: buildingDatum.Value.Builder != builder
    // New: IsEligibleBuilder(buildingDatum.Value, builder)
    matcher.MatchStartForward(
      new CodeMatch(OpCodes.Ldloca_S), // buildingDatum
      new CodeMatch(OpCodes.Call),
      new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildingData), nameof(BuildingData.Builder))),
      new CodeMatch(OpCodes.Ldarg_1),
      new CodeMatch(OpCodes.Call),
      new CodeMatch(OpCodes.Brtrue)
    )
    .ThrowIfNotMatch($"Could not find eligibility entry point for {nameof(CarpenterMenu_Constructor_Transpiler)}");
    var buildingDatumVar = matcher.Operand;
    matcher.MatchStartForward(
      new CodeMatch(OpCodes.Brtrue)
    );
    var labelToJumpTo = matcher.Operand;
    matcher.Advance(-5)
    .InsertAndAdvance(
      new CodeInstruction(OpCodes.Ldloca_S, buildingDatumVar),
      new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(KeyValuePair<string, BuildingData>), nameof(KeyValuePair<string, BuildingData>.Value))),
      new CodeInstruction(OpCodes.Ldarg_1),
      new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.IsEligibleBuilder))),
      new CodeInstruction(OpCodes.Brfalse, labelToJumpTo))
    .RemoveInstructions(6);

    // This doesn't work the way I want it...
    // Old: this.Blueprints.Add(new BlueprintEntry(num++, buildingDatum.Key, buildingDatum.Value, null));
    // New: Wrap it around `if (IsBaseSkinEligible(buildingDatum.Value, builder, targetLocation))`
    //matcher.MatchStartForward(
    //  new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<CarpenterMenu.BlueprintEntry>), nameof(List<CarpenterMenu.BlueprintEntry>.Add)))
    //)
    //.ThrowIfNotMatch($"Could not find blueprint adding entry point for {nameof(CarpenterMenu_Constructor_Transpiler)}")
    //.CreateLabelWithOffsets(1, out var labelToJumpTo2)
    //.MatchStartBackwards(
    //  new CodeMatch(OpCodes.Ldarg_0),
    //  new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CarpenterMenu), nameof(CarpenterMenu.Blueprints)))
    //)
    //.ThrowIfNotMatch($"Could not find second blueprint adding entry point for {nameof(CarpenterMenu_Constructor_Transpiler)}")
    //.InsertAndAdvance(
    //  new CodeInstruction(OpCodes.Ldloca_S, buildingDatumVar),
    //  new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(KeyValuePair<string, BuildingData>), nameof(KeyValuePair<string, BuildingData>.Value))),
    //  new CodeInstruction(OpCodes.Ldarg_1),
    //  new CodeInstruction(OpCodes.Ldarg_2),
    //  new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.IsBaseSkinEligible))),
    //  new CodeInstruction(OpCodes.Brfalse, labelToJumpTo2)
    //);
    //foreach (var m in matcher.InstructionEnumeration()) {
    //  StaticMonitor.Log($"{m.opcode} {m.operand}", LogLevel.Info);
    //}
    return matcher.InstructionEnumeration();
  }

  // Copypastaed from base game
	static private void robinHammerSound(NPC instance, Farmer who) {
		if (Game1.currentLocation.Equals(instance.currentLocation) && Utility.isOnScreen(instance.Position, 256)) {
			Game1.playSound((Game1.random.NextDouble() < 0.1) ? "clank" : "axchop");
			instance.shakeTimer = 250;
		}
	}

	static private void robinVariablePause(NPC instance, Farmer who, int idleIndex1, int idleIndex2) {
		if (Game1.random.NextDouble() < 0.4) {
			instance.Sprite.CurrentAnimation[instance.Sprite.currentAnimationIndex] = new FarmerSprite.AnimationFrame(idleIndex2, 300, secondaryArm: false, flip: false, (who) => robinVariablePause(instance, who, idleIndex1, idleIndex2));
		}
		else if (Game1.random.NextDouble() < 0.25) {
			instance.Sprite.CurrentAnimation[instance.Sprite.currentAnimationIndex] = new FarmerSprite.AnimationFrame(idleIndex1, Game1.random.Next(500, 4000), secondaryArm: false, flip: false, (who) => robinVariablePause(instance, who, idleIndex1, idleIndex2));
		}
		else {
			instance.Sprite.CurrentAnimation[instance.Sprite.currentAnimationIndex] = new FarmerSprite.AnimationFrame(idleIndex2, Game1.random.Next(1000, 4000), secondaryArm: false, flip: false, (who) => robinVariablePause(instance, who, idleIndex1, idleIndex2));
		}
	}

  static bool NPC_doPlayRobinHammerAnimation_Prefix(NPC __instance) {
    if (__instance.Name != "Robin") {
      var indices = buildingOverrideManager.GetConstructAnimationIndices(__instance.Name);
      if (indices is null) return true;
      var (idleIndex1, idleIndex2, hammerIndex) = indices.Value;
			__instance.Sprite.ClearAnimation();
			__instance.Sprite.AddFrame(new FarmerSprite.AnimationFrame(hammerIndex, 75));
			__instance.Sprite.AddFrame(new FarmerSprite.AnimationFrame(hammerIndex + 1, 75));
			__instance.Sprite.AddFrame(new FarmerSprite.AnimationFrame(hammerIndex + 2, 300, secondaryArm: false, flip: false, (who) => robinHammerSound(__instance, who)));
			__instance.Sprite.AddFrame(new FarmerSprite.AnimationFrame(idleIndex2, 1000, secondaryArm: false, flip: false, (who) => robinVariablePause(__instance, who, idleIndex1, idleIndex2)));
			__instance.ignoreScheduleToday = true;
			//bool flag = Game1.player.daysUntilHouseUpgrade.Value == 1 || Game1.RequireLocation<Town>("Town").daysUntilCommunityUpgrade.Value == 1;
			//__instance.CurrentDialogue.Clear();
			//__instance.CurrentDialogue.Push(new Dialogue(__instance, flag ? "Strings\\StringsFromCSFiles:NPC.cs.3927" : "Strings\\StringsFromCSFiles:NPC.cs.3926"));
      return false;
    }
    return true;
  }

  static void BlueprintEntry_BuildDays_Postfix(BlueprintEntry __instance, ref int __result) {
    __result = buildingOverrideManager.GetBuildDaysOverrideFor(currentBuilder, __instance.Data, __instance.Skin) ?? __result;
  }

  static void BlueprintEntry_BuildCost_Postfix(BlueprintEntry __instance, ref int __result) {
    __result = buildingOverrideManager.GetBuildCostOverrideFor(currentBuilder, __instance.Data, __instance.Skin) ?? __result;
  }

  static void BlueprintEntry_BuildMaterials_Postfix(BlueprintEntry __instance, ref List<BuildingMaterial> __result) {
    __result = buildingOverrideManager.GetBuildMaterialsOverrideFor(currentBuilder, __instance.Data, __instance.Skin) ?? __result;
  }
}

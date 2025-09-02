using Force.DeepCloner;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TokenizableStrings;
using StardewValley.Delegates;
using StardewValley.Menus;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;

using BlueprintEntry = StardewValley.Menus.CarpenterMenu.BlueprintEntry;
using SObject = StardewValley.Object;

namespace Selph.StardewMods.CustomBuilders;

static class Carpenters {
  // Manages builder-specific override data
  internal static readonly PerScreen<BuildingOverrideManager> buildingOverrideManager = new(createNewState: () => new());
  // If a blueprint is not in this list, just use vanilla behavior
  public class BoolWrapper {
    public bool? Value { get; set; }
    public BoolWrapper(bool? value) {
      this.Value = value;
    }
  }
  internal static readonly PerScreen<ConditionalWeakTable<BlueprintEntry, BoolWrapper>> isDirectBuildModeManager = new(createNewState: () => new());

  // maybe one day when i'm feeling less lazy/have a better design
  //public static string AdditionalUpgradesKey = $"{ModEntry.UniqueId}_AdditionalUpgrades";
  public static string CanBeDirectBuild = $"{ModEntry.UniqueId}_CanBeDirectBuild";

  public static void RegisterCustomTriggers() {
    GameLocation.RegisterTileAction($"{ModEntry.UniqueId}_ShowConstruct", ShowConstruct);
    GameStateQuery.Register($"{ModEntry.UniqueId}_IS_BUILDER", IS_BUILDER);
  }

  public static void RegisterEvents(IModHelper helper) {
    helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
    helper.Events.Input.ButtonPressed += OnButtonPressed;
    helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
  }

  public static void ApplyPatches(Harmony harmony) {
    harmony.Patch(
        original: AccessTools.Method(typeof(NPC),
          "updateConstructionAnimation"),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(Carpenters.NPC_updateConstructionAnimation_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(CarpenterMenu),
          nameof(CarpenterMenu.robinConstructionMessage)),
        prefix: new HarmonyMethod(typeof(Carpenters),
          nameof(Carpenters.CarpenterMenu_robinConstructionMessage_Prefix)));

    harmony.Patch(
        original: AccessTools.Constructor(typeof(CarpenterMenu), new Type[] { typeof(string), typeof(GameLocation) }),
        transpiler: new HarmonyMethod(typeof(Carpenters),
          nameof(Carpenters.CarpenterMenu_Constructor_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(NPC),
          "doPlayRobinHammerAnimation"),
        prefix: new HarmonyMethod(typeof(Carpenters),
          nameof(Carpenters.NPC_doPlayRobinHammerAnimation_Prefix)));

    // Blueprint cost patches
    harmony.Patch(
        original: AccessTools.PropertyGetter(typeof(BlueprintEntry),
          nameof(BlueprintEntry.BuildDays)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(Carpenters.BlueprintEntry_BuildDays_Postfix)));
    harmony.Patch(
        original: AccessTools.PropertyGetter(typeof(BlueprintEntry),
          nameof(BlueprintEntry.BuildCost)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(Carpenters.BlueprintEntry_BuildCost_Postfix)));
    harmony.Patch(
        original: AccessTools.PropertyGetter(typeof(BlueprintEntry),
          nameof(BlueprintEntry.BuildMaterials)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(Carpenters.BlueprintEntry_BuildMaterials_Postfix)));
    // Blueprint upgrade patches
    harmony.Patch(
        original: AccessTools.PropertyGetter(typeof(BlueprintEntry),
          nameof(BlueprintEntry.IsUpgrade)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(Carpenters.BlueprintEntry_IsUpgrade_Postfix)));
    //harmony.Patch(
    //    original: AccessTools.PropertyGetter(typeof(BlueprintEntry),
    //      nameof(BlueprintEntry.DisplayName)),
    //    postfix: new HarmonyMethod(typeof(Carpenters),
    //      nameof(Carpenters.BlueprintEntry_DisplayName_Postfix)));
    //harmony.Patch(
    //    original: AccessTools.PropertyGetter(typeof(BlueprintEntry),
    //      nameof(BlueprintEntry.UpgradeFrom)),
    //    postfix: new HarmonyMethod(typeof(Carpenters),
    //      nameof(Carpenters.BlueprintEntry_UpgradeFrom_Postfix)));


    // Menu patches
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(IClickableMenu),
          nameof(IClickableMenu.populateClickableComponentList)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(IClickableMenu_populateClickableComponentList_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(CarpenterMenu),
          "resetBounds"),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(CarpenterMenu_resetBounds_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(CarpenterMenu),
          nameof(CarpenterMenu.performHoverAction)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(CarpenterMenu_performHoverAction_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(CarpenterMenu),
          nameof(CarpenterMenu.receiveLeftClick)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(CarpenterMenu_receiveLeftClick_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(CarpenterMenu),
          nameof(CarpenterMenu.draw)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(CarpenterMenu_draw_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(CarpenterMenu),
          nameof(CarpenterMenu.UpdateAppearanceButtonVisibility)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(CarpenterMenu_UpdateAppearanceButtonVisibility_Postfix)));
    // Dirty dirty hack to make > 999 item count show up
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(SObject),
          nameof(SObject.maximumStackSize)),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(SObject_maximumStackSize_Postfix)));
    // Another dirty patch to make buildings respect the build days override
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(GameLocation),
          nameof(GameLocation.buildStructure),
          new[] {
          typeof(string),
          typeof(BuildingData),
          typeof(Vector2),
          typeof(Farmer),
          typeof(Building).MakeByRefType(),
          typeof(bool),
          typeof(bool)}),
        postfix: new HarmonyMethod(typeof(Carpenters),
          nameof(GameLocation_buildStructure_Postfix)));
    // Not quite a dirty patch, but make direct built upgrades also mark its upgrades as built in
    // `Game1.farmer.team.constructedBuildings`.
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Building),
          nameof(Building.FinishConstruction)),
        prefix: new HarmonyMethod(typeof(Carpenters),
          nameof(Buildings_FinishConstruction_Prefix)));

    // Niceties (autoscrolling ingredient list)
    try {
      harmony.Patch(
          original: AccessTools.DeclaredMethod(typeof(CarpenterMenu),
            nameof(CarpenterMenu.SetNewActiveBlueprint), new[] { typeof(BlueprintEntry) }),
          postfix: new HarmonyMethod(typeof(Carpenters),
            nameof(CarpenterMenu_SetNewActiveBlueprint_Postfix)));
      harmony.Patch(
          original: AccessTools.DeclaredMethod(typeof(CarpenterMenu), nameof(CarpenterMenu.draw)),
          transpiler: new HarmonyMethod(typeof(Carpenters),
            nameof(CarpenterMenu_draw_Transpiler)));
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log($"Error patching in scrolling ingredients list: {e.ToString()}", LogLevel.Error);
    }
  }

  static string currentBuilder => ModEntry.Helper.Reflection.GetField<string>(Game1.currentLocation, "_constructLocationBuilderName").GetValue();

  static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e) {
    if (e.NamesWithoutLocale.Any(name => name.Name == "Data/Buildings")) {
      buildingOverrideManager.Value.Clear();
      isDirectBuildModeManager.Value.Clear();
    }
  }

  public static bool IS_BUILDER(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGet(query, 1, out var builderName, out var error)) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    return builderName == currentBuilder;
  }

  public static bool ShowConstruct(GameLocation location, string[] args, Farmer farmer, Point point) {
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
          $"Characters/Dialogue/{builderName}:{ModEntry.UniqueId}_Busy",
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
          } else {
            __instance.setTilePosition(1, 5);
          }
        } else {
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
        $"Characters/Dialogue/{__instance.Builder}:{ModEntry.UniqueId}_{dialogueKeySuffix}",
        displayName.ToLower(),
        displayNameForGeneralType.ToLower(),
        displayName,
        displayNameForGeneralType);
    return false;
  }

  static bool IsEligibleBuilder(BuildingData buildingData, string builder) {
    var result = new HashSet<string>();
    if (buildingData.Builder == builder) {
      return true;
    }
    if (buildingData.CustomFields is not null) {
      foreach (var keyVal in buildingData.CustomFields) {
        if (keyVal.Key.StartsWith($"{ModEntry.UniqueId}_ExtraBuilder")
            && keyVal.Value == builder) {
          return true;
        }
      }
    }
    if (Game1.characterData.TryGetValue(builder, out var builderData) && builderData.CustomFields is not null) {
      foreach (var keyVal in builderData.CustomFields) {
        if (keyVal.Key.StartsWith($"{ModEntry.UniqueId}_InheritBuilder")
            && keyVal.Value == buildingData.Builder) {
          return true;
        }
      }
    }
    return false;
  }

  static bool HasExtraUpgrades(BuildingData buildingData, GameLocation location) {
    if (buildingData.CustomFields?.ContainsKey(CanBeDirectBuild) ?? false) {
      return true;
    }
    //if (buildingData.CustomFields?.TryGetValue(AdditionalUpgradesKey, out var additionalUpgrades) ?? false) {
    //  foreach (var a in additionalUpgrades.Split(",", StringSplitOptions.RemoveEmptyEntries)) {
    //    var additionalUpgrade = a.Trim();
    //    if (location.getNumberBuildingsConstructed(additionalUpgrade) == 0) {
    //      return true;
    //    }
    //  }
    //}
    return false;
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
    // New: !IsEligibleBuilder(buildingDatum.Value, builder)
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
      new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Carpenters), nameof(Carpenters.IsEligibleBuilder))),
      new CodeInstruction(OpCodes.Brfalse, labelToJumpTo))
    .RemoveInstructions(6);

    // Old: buildingDatum.Value.BuildingToUpgrade != null
    // New: !HasDirectBuildUpgrade(buildingDatum.Value) && ...
    matcher.MatchEndForward(
      new CodeMatch(OpCodes.Ldloca_S), // buildingDatum
      new CodeMatch(OpCodes.Call),
      new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BuildingData), nameof(BuildingData.BuildingToUpgrade))),
      new CodeMatch(OpCodes.Brfalse_S)
    )
    .ThrowIfNotMatch($"Could not find upgrade eligibility entry point for {nameof(CarpenterMenu_Constructor_Transpiler)}");
    var labelToJumpTo2 = matcher.Operand;
    matcher
    .Advance(-3)
    .InsertAndAdvance(
      new CodeInstruction(OpCodes.Ldloca_S, buildingDatumVar),
      new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(KeyValuePair<string, BuildingData>), nameof(KeyValuePair<string, BuildingData>.Value))),
      new CodeInstruction(OpCodes.Ldarg_2),
      new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Carpenters), nameof(Carpenters.HasExtraUpgrades))),
      new CodeInstruction(OpCodes.Brtrue, labelToJumpTo2));

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
    } else if (Game1.random.NextDouble() < 0.25) {
      instance.Sprite.CurrentAnimation[instance.Sprite.currentAnimationIndex] = new FarmerSprite.AnimationFrame(idleIndex1, Game1.random.Next(500, 4000), secondaryArm: false, flip: false, (who) => robinVariablePause(instance, who, idleIndex1, idleIndex2));
    } else {
      instance.Sprite.CurrentAnimation[instance.Sprite.currentAnimationIndex] = new FarmerSprite.AnimationFrame(idleIndex2, Game1.random.Next(1000, 4000), secondaryArm: false, flip: false, (who) => robinVariablePause(instance, who, idleIndex1, idleIndex2));
    }
  }

  static bool NPC_doPlayRobinHammerAnimation_Prefix(NPC __instance) {
    if (__instance.Name != "Robin") {
      var indices = buildingOverrideManager.Value.GetConstructAnimationIndices(__instance.Name);
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
    __result = buildingOverrideManager.Value.GetBuildDaysOverrideFor(currentBuilder, __instance.Data, __instance.Skin, GetIsDirectBuildMode(__instance) ?? false) ?? __result;
  }

  static void BlueprintEntry_BuildCost_Postfix(BlueprintEntry __instance, ref int __result) {
    __result = buildingOverrideManager.Value.GetBuildCostOverrideFor(currentBuilder, __instance.Data, __instance.Skin, GetIsDirectBuildMode(__instance) ?? false) ?? __result;
  }

  static void BlueprintEntry_BuildMaterials_Postfix(BlueprintEntry __instance, ref List<BuildingMaterial> __result) {
    __result = buildingOverrideManager.Value.GetBuildMaterialsOverrideFor(currentBuilder, __instance.Data, __instance.Skin, GetIsDirectBuildMode(__instance) ?? false) ?? __result;
  }

  static void BlueprintEntry_IsUpgrade_Postfix(BlueprintEntry __instance, ref bool __result) {
    var directBuildMode = GetIsDirectBuildMode(__instance);
    if (directBuildMode is not null) {
      __result = !directBuildMode.Value;
    }
  }


  // List building stuff
  static ClickableTextureComponent? showListButton;
  static ClickableTextureComponent? toggleDirectBuildModeButton;

  static void IClickableMenu_populateClickableComponentList_Postfix(IClickableMenu __instance) {
    if (__instance is not CarpenterMenu carpenterMenu) return;
    if (Game1.options.SnappyMenus) {
      if (showListButton is not null) {
        __instance.allClickableComponents.Add(showListButton);
      }
      if (toggleDirectBuildModeButton is not null) {
        __instance.allClickableComponents.Add(toggleDirectBuildModeButton);
      }
    }
  }

  static void CarpenterMenu_resetBounds_Postfix(CarpenterMenu __instance) {
    showListButton = new ClickableTextureComponent(
        ModEntry.Helper.Translation.Get("ShowBuildingList"),
        new Microsoft.Xna.Framework.Rectangle(
          __instance.xPositionOnScreen + __instance.width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 320 - 20 - (16 * 8) - 4,
          __instance.yPositionOnScreen + __instance.maxHeightOfBuildingViewer + 64, 64, 64),
        null, null, Game1.mouseCursors_1_6, new Microsoft.Xna.Framework.Rectangle(6, 260, 15, 15), 4f) {
      myID = 999,
      rightNeighborID = -99998,
      leftNeighborID = -99998,
      upNeighborID = 109,
    };
    toggleDirectBuildModeButton = new ClickableTextureComponent(
        ModEntry.Helper.Translation.Get("ToggleDirectBuildMode"),
        new Microsoft.Xna.Framework.Rectangle(
          __instance.xPositionOnScreen + __instance.width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 320 - 20 - (16 * 4) - 4,
          __instance.yPositionOnScreen + __instance.maxHeightOfBuildingViewer + 64, 64, 64),
        null, null, Game1.mouseCursors2, new Microsoft.Xna.Framework.Rectangle(48, 208, 16, 16), 4f) {
      myID = 998,
      rightNeighborID = -99998,
      leftNeighborID = -99998,
      upNeighborID = 109,
    };
  }

  static void CarpenterMenu_performHoverAction_Postfix(CarpenterMenu __instance, int x, int y) {
    if (__instance.onFarm) return;
    foreach (var button in new[] { showListButton, toggleDirectBuildModeButton }) {
      if (button is null) continue;
      button.tryHover(x, y);
      if (button.containsPoint(x, y)) {
        try {
          ModEntry.Helper.Reflection.GetField<string>(__instance, "hoverText").SetValue(button.name);
        }
        catch (Exception e) {
          ModEntry.StaticMonitor.Log($"ERROR drawing building list button: {e.ToString()}", LogLevel.Error);
        }
      }
    }
  }

  static void CarpenterMenu_receiveLeftClick_Postfix(CarpenterMenu __instance, int x, int y, bool playSound = true) {
    if (!__instance.freeze && !__instance.onFarm && !Game1.IsFading()) {
      if (toggleDirectBuildModeButton is not null
          && toggleDirectBuildModeButton.visible
          && toggleDirectBuildModeButton.containsPoint(x, y)) {
        ToggleIsDirectBuildMode(__instance.Blueprint);
        __instance.SetNewActiveBlueprint(__instance.Blueprint);
        Game1.playSound("smallSelect");
      }
      if (showListButton?.containsPoint(x, y) ?? false) {
        var buildingListMenu = new ShopMenu(
            $"{ModEntry.UniqueId}_BuildingListMenu",
            itemsForSale: __instance.Blueprints.Select((b) =>
              new BuildingEntry(
                b.Id,
                b.DisplayName,
                b.Description,
                b.Skin,
                (Farmer) => {
                  __instance.SetNewActiveBlueprint(b.Index);
                  __instance.GetChildMenu()?.exitThisMenu();
                }
                )).ToList<ISalable>()
            );
        foreach (var value in buildingListMenu.itemPriceAndStock.Values) {
          value.StackDrawType = StackDrawType.Hide;
        }
        __instance.SetChildMenu(buildingListMenu);
      }
    }
  }

  static void CarpenterMenu_draw_Postfix(CarpenterMenu __instance, SpriteBatch b) {
    if (!__instance.freeze && !__instance.onFarm && !Game1.IsFading()) {
      showListButton?.draw(b);
      toggleDirectBuildModeButton?.draw(b, GetIsDirectBuildMode(__instance.Blueprint) is not null ? Color.White : (Color.Gray * 0.8f), 0.88f);
      __instance.drawMouse(b);
      try {
        var hoverText = ModEntry.Helper.Reflection.GetField<string>(__instance, "hoverText").GetValue();
        if (hoverText.Length > 0) {
          IClickableMenu.drawHoverText(b, hoverText, Game1.dialogueFont);
        }
      }
      catch (Exception e) {
        ModEntry.StaticMonitor.Log($"ERROR drawing building list button: {e.ToString()}", LogLevel.Error);
      }
    }
  }

  // upgrade manager stuff
  static BoolWrapper GetIsDirectBuildModeWrapper(BlueprintEntry blueprintEntry) {
    return isDirectBuildModeManager.Value.GetValue(blueprintEntry, (be) => {
      if (be.Data.CustomFields?.ContainsKey(CanBeDirectBuild) ?? false) {
        return new(false);
      } else {
        return new(null);
      }
    });
  }

  static bool? GetIsDirectBuildMode(BlueprintEntry blueprintEntry) {
    return GetIsDirectBuildModeWrapper(blueprintEntry).Value;
  }

  static void ToggleIsDirectBuildMode(BlueprintEntry blueprintEntry) {
    var currentMode = GetIsDirectBuildModeWrapper(blueprintEntry);
    if (currentMode.Value is not null) {
      currentMode.Value = !currentMode.Value;
    }
  }

  static void CarpenterMenu_UpdateAppearanceButtonVisibility_Postfix(CarpenterMenu __instance) {
    if (toggleDirectBuildModeButton is not null && __instance.Blueprint is not null)
      toggleDirectBuildModeButton.visible = GetIsDirectBuildMode(__instance.Blueprint) is not null;
  }

  static void SObject_maximumStackSize_Postfix(SObject __instance, ref int __result) {
    if (Game1.activeClickableMenu is CarpenterMenu && __result >= 999) {
      __result = 99999;
    }
  }

  static void GameLocation_buildStructure_Postfix(GameLocation __instance, bool __result, string typeId, BuildingData data, Vector2 tileLocation, Farmer who, ref Building constructed, bool magicalConstruction = false, bool skipSafetyChecks = false) {
    if (__result && constructed is not null
        && Game1.activeClickableMenu is CarpenterMenu carpenterMenu
        && carpenterMenu.Blueprint.Data == data
        && constructed.isUnderConstruction()) {
      //&& data.BuildingToUpgrade is not null
      //&& data.CustomFields?.ContainsKey(CanBeDirectBuild) is true) {
      constructed.daysOfConstructionLeft.Value = carpenterMenu.Blueprint.BuildDays;
    }
  }

  static void Buildings_FinishConstruction_Prefix(Building __instance, bool onGameStart = false) {
    if (__instance.daysOfConstructionLeft.Value > 0
        && __instance.GetData()?.CustomFields?.ContainsKey(CanBeDirectBuild) is true) {
      var parentBuilding = __instance.GetData()?.BuildingToUpgrade;
      // Avoid potential infinite loops (not that it should happen... right?) by exiting if we
      // already added a building
      while (parentBuilding is not null && !Game1.player.team.constructedBuildings.Contains(parentBuilding)) {
        Game1.player.team.constructedBuildings.Add(parentBuilding);
        if (!onGameStart) {
          foreach (Farmer allFarmer in Game1.getAllFarmers()) {
            allFarmer.autoGenerateActiveDialogueEvent("structureBuilt_" + parentBuilding);
          }
        }
        if (Building.TryGetData(parentBuilding, out var data)) {
          parentBuilding = data.BuildingToUpgrade;
        } else {
          parentBuilding = null;
        }
      }
    }
  }

  static PerScreen<int> currentScroll = new();
  static PerScreen<bool> isScrollingDown = new();
  static PerScreen<int> maxScrollLinger = new();

  static void CarpenterMenu_SetNewActiveBlueprint_Postfix() {
    currentScroll.Value = 0;
    maxScrollLinger.Value = 0;
    isScrollingDown.Value = true;
  }

  static IEnumerable<CodeInstruction> CarpenterMenu_draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
    CodeMatcher matcher = new(instructions, generator);
    matcher
      .MatchStartForward(
      new CodeMatch(OpCodes.Ldloca_S), // ingredientPosition
      new CodeMatch(OpCodes.Ldflda, AccessTools.Field(typeof(Vector2), nameof(Vector2.Y))),
      new CodeMatch(OpCodes.Dup),
      new CodeMatch(OpCodes.Ldind_R4),
      new CodeMatch(OpCodes.Ldc_R4, (float)21))
    .ThrowIfNotMatch($"Could not find entry point for {nameof(CarpenterMenu_draw_Transpiler)}");
    var ingredientPositionVar = matcher.Operand;
    matcher
      .MatchStartForward(
      new CodeMatch(OpCodes.Ldarg_0),
      new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CarpenterMenu), nameof(CarpenterMenu.ingredients))))
    .ThrowIfNotMatch($"Could not find entry point 2 for {nameof(CarpenterMenu_draw_Transpiler)}")
    .InsertAndAdvance(
      new CodeInstruction(OpCodes.Ldarg_0),
      new CodeInstruction(OpCodes.Ldloca_S, ingredientPositionVar),
      new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(Vector2), nameof(Vector2.Y))),
      new CodeInstruction(OpCodes.Ldarg_1),
      new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Carpenters), nameof(MaybePrepIngredientPositionAndSpriteBatch))))
    .MatchStartForward(
      new CodeMatch(OpCodes.Endfinally))
    .Advance(1);
    var labels = matcher.Instruction.ExtractLabels();
    matcher
    .InsertAndAdvance(
      new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
      new CodeInstruction(OpCodes.Ldarg_1),
      new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Carpenters), nameof(MaybeResetSpriteBatch))));
    return matcher.InstructionEnumeration();
  }

  static void MaybePrepIngredientPositionAndSpriteBatch(CarpenterMenu menu, ref float ingredientPosition, SpriteBatch b) {
    var ingredientCount = menu.ingredients.Count;
    if (ingredientCount > 3) {
      // Set limited draw rectangle
      var blueprint = menu.Blueprint;
      b.End();
      int num = LocalizedContentManager.CurrentLanguageCode switch {
        LocalizedContentManager.LanguageCode.es => menu.maxWidthOfDescription + 64 + ((blueprint.Id == "Deluxe Barn") ? 96 : 0),
        LocalizedContentManager.LanguageCode.it => menu.maxWidthOfDescription + 96,
        LocalizedContentManager.LanguageCode.fr => menu.maxWidthOfDescription + 96 + ((blueprint.Id == "Slime Hutch" || blueprint.Id == "Deluxe Coop" || blueprint.Id == "Deluxe Barn") ? 72 : 0),
        LocalizedContentManager.LanguageCode.ko => menu.maxWidthOfDescription + 96 + ((blueprint.Id == "Slime Hutch") ? 64 : ((blueprint.Id == "Deluxe Coop") ? 96 : ((blueprint.Id == "Deluxe Barn") ? 112 : ((blueprint.Id == "Big Barn") ? 64 : 0)))),
        _ => menu.maxWidthOfDescription + 64,
      };
      Rectangle scissorRectangle = new(menu.xPositionOnScreen + menu.maxWidthOfBuildingViewer, menu.yPositionOnScreen + 256 + 84, num + 20, 68 * 3);
      b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, Utility.ScissorEnabled);
      b.GraphicsDevice.ScissorRectangle = scissorRectangle;
      // Set draw offset
      ingredientPosition -= currentScroll.Value;
      if (maxScrollLinger.Value < 60) {
        maxScrollLinger.Value++;
      } else {
        if (isScrollingDown.Value) {
          currentScroll.Value++;
          if (currentScroll.Value >= (ingredientCount - 3) * 68) {
            isScrollingDown.Value = false;
            maxScrollLinger.Value = 0;
          }
        } else {
          currentScroll.Value--;
          if (currentScroll.Value <= 0) {
            isScrollingDown.Value = true;
            maxScrollLinger.Value = 0;
          }
        }
      }
    }
  }
  static void MaybeResetSpriteBatch(CarpenterMenu menu, SpriteBatch b) {
    if (menu.ingredients.Count > 3) {
      b.End();
      b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }
  }

  static void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
    if (Game1.activeClickableMenu is not CarpenterMenu menu
        || menu.onFarm
        || menu.ingredients.Count <= 3) return;
    if (e.Button == SButton.DPadDown || e.Button == SButton.DPadUp) {
      maxScrollLinger.Value = 0;
      isScrollingDown.Value = true;
      currentScroll.Value += e.Button == SButton.DPadDown ? 20 : -20;
      currentScroll.Value = Utility.Clamp(currentScroll.Value, 0, (menu.ingredients.Count - 3) * 68);
    }
  }

  static void OnMouseWheelScrolled(object? sender, MouseWheelScrolledEventArgs e) {
    if (Game1.activeClickableMenu is not CarpenterMenu menu
        || menu.onFarm
        || menu.ingredients.Count <= 3) return;
    maxScrollLinger.Value = 0;
    isScrollingDown.Value = true;
    currentScroll.Value += e.Delta < 0 ? 20 : -20;
    currentScroll.Value = Utility.Clamp(currentScroll.Value, 0, (menu.ingredients.Count - 3) * 68);
  }
}

class BuildingEntry : ISalable {
  protected string _displayName = "";

  protected string _id = "";

  protected string _description = "";

  protected Action<Farmer> _onPurchase;

  public string TypeDefinitionId => "(BuildingEntry)";

  public string QualifiedItemId => this.TypeDefinitionId + "BuildingEntry." + this._id;

  public string DisplayName => this._displayName;

  public string Name => this._id;

  public bool IsRecipe {
    get {
      return false;
    }
    set {
    }
  }

  public int Stack {
    get {
      return 1;
    }
    set {
    }
  }

  public int Quality {
    get {
      return 0;
    }
    set {
    }
  }

  Lazy<Texture2D> texture;

  public BuildingEntry(string name, string display_name, string display_description, BuildingSkin skin, Action<Farmer> on_purchase) {
    this._id = name;
    this._displayName = display_name;
    this._description = display_description;
    this._onPurchase = on_purchase;
    this.texture = new Lazy<Texture2D>(() => {
      if (Game1.buildingData.TryGetValue(_id, out var data)) {
        try {
          return Game1.content.Load<Texture2D>(skin?.Texture ?? data.Texture ?? "Buildings/" + _id);
        }
        catch (Exception e) {
          ModEntry.StaticMonitor.Log($"Error reading texture: {e}", LogLevel.Error);
          return Game1.content.Load<Texture2D>("Buildings\\Error");
        }
      } else {
        return Game1.content.Load<Texture2D>("Buildings\\Error");
      }
    });
  }

  /// <inheritdoc />
  public string GetItemTypeId() {
    return this.TypeDefinitionId;
  }

  public void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow) {
    if (Game1.buildingData.TryGetValue(_id, out var data)) {
      bool isFishPond = data.BuildingType == "StardewValley.Buildings.FishPond";
      Rectangle sourceRect = data.SourceRect;
      if (isFishPond) {
        sourceRect = new Rectangle(0, 0, 80, 80);
      }
      if (data.BuildingType == "StardewValley.Buildings.JunimoHut") {
        sourceRect = new Rectangle(Game1.seasonIndex * 48, 0, 48, 64);
      }
      if (sourceRect == Rectangle.Empty) {
        sourceRect = this.texture.Value.Bounds;
      }
      var maxWidth = sourceRect.Width;
      var maxHeight = sourceRect.Height;
      // The amount to shift the base draw position because of draw layers bigger than the main
      // building
      var xShift = 0;
      var yShift = 0;
      if (data.DrawLayers is not null) {
        foreach (var drawLayer in data.DrawLayers) {
          xShift -= (int)Math.Min(0, drawLayer.DrawPosition.X + xShift);
          yShift -= (int)Math.Min(0, drawLayer.DrawPosition.Y + yShift);
          maxWidth -= (int)Math.Min(0, Math.Min(0, drawLayer.DrawPosition.X) + maxWidth);
          maxWidth += (int)Math.Max(0, Math.Max(0, drawLayer.DrawPosition.X) + drawLayer.SourceRect.Width - maxWidth);
          maxHeight -= (int)Math.Min(0, Math.Min(0, drawLayer.DrawPosition.Y) + maxHeight);
          maxHeight += (int)Math.Max(0, Math.Max(0, drawLayer.DrawPosition.Y) + drawLayer.SourceRect.Height - maxHeight);
        }
      }
      var actualScale = 16f / Math.Max(maxHeight, maxWidth);
      // center the draw location
      if (maxHeight > maxWidth) {
        location.X += (maxHeight - maxWidth) * actualScale / 2 * Game1.pixelZoom;
      }
      if (maxWidth > maxHeight) {
        location.Y += (maxWidth - maxHeight) * actualScale / 2 * Game1.pixelZoom;
      }
      location.X += xShift * actualScale * Game1.pixelZoom;
      location.Y += yShift * actualScale * Game1.pixelZoom;
      if (isFishPond) {
        spriteBatch.Draw(
            this.texture.Value,
            location,
            new Rectangle(0, 80, 80, 80),
            new Color(60, 126, 150),
            0f,
            new Vector2(0, 0),
            actualScale * Game1.pixelZoom,
            SpriteEffects.None,
            layerDepth - 0.0001f);
      }
      spriteBatch.Draw(
          this.texture.Value,
          location,
          sourceRect,
          Color.White,
          0f,
          new Vector2(0, 0),
          actualScale * Game1.pixelZoom,
          SpriteEffects.None,
          layerDepth);
      if (_id == "selph.Aquaponics_AquaponicsFishPond") {
        spriteBatch.Draw(
            Game1.content.Load<Texture2D>("Mods/selph.Aquaponics/AquaponicsTank"),
            location,
            null,
            Color.White,
            0,
            Vector2.Zero,
            actualScale * Game1.pixelZoom,
            SpriteEffects.None,
            layerDepth - 1 / 10000f);
        spriteBatch.Draw(
            Game1.content.Load<Texture2D>("Mods/selph.Aquaponics/AquaponicsTankFront"),
            location,
            null,
            Color.White,
            0,
            Vector2.Zero,
            actualScale * Game1.pixelZoom,
            SpriteEffects.None,
            layerDepth + 1 / 10000f);
        spriteBatch.Draw(
            Game1.content.Load<Texture2D>("Mods/selph.Aquaponics/AquaponicsTankBack"),
            location,
            null,
            Color.White,
            0,
            Vector2.Zero,
            actualScale * Game1.pixelZoom,
            SpriteEffects.None,
            layerDepth - 1.1f / 10000f);
        spriteBatch.Draw(
            Game1.content.Load<Texture2D>("Mods/selph.Aquaponics/AquaponicsTankWater"),
            location,
            null,
            new Color(60, 126, 150),
            0,
            Vector2.Zero,
            actualScale * Game1.pixelZoom,
            SpriteEffects.None,
            layerDepth - 0.5f / 10000);
      }
      if (data.DrawLayers is not null) {
        foreach (BuildingDrawLayer drawLayer in data.DrawLayers) {
          if (drawLayer.OnlyDrawIfChestHasContents == null) {
            var drawLayerLayerDepth = layerDepth - drawLayer.SortTileOffset * 0.0001f;
            if (drawLayer.DrawInBackground) {
              drawLayerLayerDepth = 0;
            }
            Microsoft.Xna.Framework.Rectangle sourceRect2 = drawLayer.GetSourceRect((int)Game1.currentGameTime.TotalGameTime.TotalMilliseconds);
            spriteBatch.Draw(
                drawLayer.Texture is null ? this.texture.Value : Game1.content.Load<Texture2D>(drawLayer.Texture),
                location + drawLayer.DrawPosition * actualScale * Game1.pixelZoom,
                sourceRect2,
                Color.White,
                0f,
                new Vector2(0, 0),
                actualScale * Game1.pixelZoom,
                SpriteEffects.None,
                drawLayerLayerDepth);
          }
        }
      }
    }
  }

  public bool ShouldDrawIcon() {
    return true;
  }

  public string getDescription() {
    return this._description;
  }

  public int maximumStackSize() {
    return 1;
  }

  public int addToStack(Item stack) {
    return 1;
  }

  public bool canStackWith(ISalable other) {
    return false;
  }

  /// <inheritdoc />
  public int sellToStorePrice(long specificPlayerID = -1L) {
    return -1;
  }

  /// <inheritdoc />
  public int salePrice(bool ignoreProfitMargins = false) {
    return 0;
  }

  /// <inheritdoc />
  public bool appliesProfitMargins() {
    return false;
  }

  /// <inheritdoc />
  public bool actionWhenPurchased(string shopId) {
    this._onPurchase?.Invoke(Game1.player);
    return true;
  }

  public bool CanBuyItem(Farmer farmer) {
    return true;
  }

  public bool IsInfiniteStock() {
    return true;
  }

  public ISalable GetSalableInstance() {
    return this;
  }

  /// <inheritdoc />
  public void FixStackSize() {
  }

  /// <inheritdoc />
  public void FixQuality() {
  }
}

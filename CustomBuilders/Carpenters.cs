using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TokenizableStrings;
using StardewValley.Delegates;
using StardewValley.Menus;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;

using BlueprintEntry = StardewValley.Menus.CarpenterMenu.BlueprintEntry;

namespace Selph.StardewMods.CustomBuilders;

static class Carpenters {
  internal static BuildingOverrideManager buildingOverrideManager = new();

  public static void RegisterCustomTriggers() {
    GameLocation.RegisterTileAction($"{ModEntry.UniqueId}_ShowConstruct", ShowConstruct);
    GameStateQuery.Register($"{ModEntry.UniqueId}_IS_BUILDER", IS_BUILDER);
  }

  public static void RegisterEvents(IModHelper helper) {
    helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
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
  }

  static string currentBuilder => ModEntry.Helper.Reflection.GetField<string>(Game1.currentLocation, "_constructLocationBuilderName").GetValue();

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


  // List building stuff
  static ClickableTextureComponent? showListButton;

  static void IClickableMenu_populateClickableComponentList_Postfix(IClickableMenu __instance) {
    if (__instance is not CarpenterMenu) return;
    if (showListButton is not null && Game1.options.SnappyMenus) {
      __instance.allClickableComponents.Add(showListButton);
    }
  }

  static void CarpenterMenu_resetBounds_Postfix(CarpenterMenu __instance) {
    showListButton = new ClickableTextureComponent(
        ModEntry.Helper.Translation.Get("ShowBuildingList"),
        new Microsoft.Xna.Framework.Rectangle(
          __instance.xPositionOnScreen + __instance.width - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder - 320 - 20 - (16 * 4) - 4,
          __instance.yPositionOnScreen + __instance.maxHeightOfBuildingViewer + 64, 64, 64),
        null, null, Game1.mouseCursors_1_6, new Microsoft.Xna.Framework.Rectangle(6, 260, 15, 15), 4f) {
      myID = 999,
      rightNeighborID = -99998,
      leftNeighborID = -99998,
      upNeighborID = 109,
    };
  }

  static void CarpenterMenu_performHoverAction_Postfix(CarpenterMenu __instance, int x, int y) {
    if (!__instance.onFarm && showListButton is not null) {
      showListButton.tryHover(x, y);
      if (showListButton.containsPoint(x, y)) {
        try {
          ModEntry.Helper.Reflection.GetField<string>(__instance, "hoverText").SetValue(showListButton.name);
        }
        catch (Exception e) {
          ModEntry.StaticMonitor.Log($"ERROR drawing building list button: {e.ToString()}", LogLevel.Error);
        }
      }
    }
  }

  static void CarpenterMenu_receiveLeftClick_Postfix(CarpenterMenu __instance, int x, int y, bool playSound = true) {
    if (!__instance.freeze && !__instance.onFarm && !Game1.IsFading()
        && (showListButton?.containsPoint(x, y) ?? false)) {
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

  static void CarpenterMenu_draw_Postfix(CarpenterMenu __instance, SpriteBatch b) {
    if (!__instance.freeze && !__instance.onFarm && !Game1.IsFading() && showListButton is not null) {
      showListButton.draw(b);
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
      //spriteBatch.Draw(Game1.objectSpriteSheet, location + new Vector2((int)(32f * scaleSize), (int)(32f * scaleSize)), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, this._id, 16, 16), color * transparency, 0f, new Vector2(8f, 8f) * scaleSize, 4f * scaleSize, SpriteEffects.None, layerDepth);
      var actualScale = 16f / Math.Max(sourceRect.Height, sourceRect.Width);
      // center the draw location
      if (sourceRect.Height > sourceRect.Width) {
        location.X += (sourceRect.Height - sourceRect.Width) * actualScale / 2 * Game1.pixelZoom;
      }
      if (sourceRect.Width > sourceRect.Height) {
        location.Y += (sourceRect.Width - sourceRect.Height) * actualScale / 2 * Game1.pixelZoom;
      }
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

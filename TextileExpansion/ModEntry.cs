using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TokenizableStrings;
using StardewValley.Inventories;
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.Delegates;
using StardewValley.Menus;
using StardewValley.Internal;
using SpaceCore;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.TextileExpansion;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;
  static ContentPatcher.IContentPatcherAPI cpApi = null!;
  static bool HasWOL = false;

  static string CouturierInventoryId = null!;
  static string CouturierModDataAgeKey = null!;
  static string ClothingDyeUsedCloth = null!;
  static string ClothingDyeUsedEmbroidery = null!;
  static string ClothingRandomImageType = null!;
  static string ClothingRandomImageId = null!;
  static string ObjAlreadyGrantedExp = null!;

  static string MigratedFrom100 = null!;

  public const string ContentPackId = "selph.TextileExpansion";

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    CouturierInventoryId = $"{UniqueId}_CouturierInventory";
    CouturierModDataAgeKey = $"{UniqueId}_CouturierAge";
    ClothingDyeUsedCloth = $"{UniqueId}_ClothingDyeUsedCloth";
    ClothingDyeUsedEmbroidery = $"{UniqueId}_ClothingDyeUsedEmbroidery";

    ClothingRandomImageType = $"{UniqueId}_ClothingRandomImageType";
    ClothingRandomImageId = $"{UniqueId}_ClothingRandomImageId";

    ObjAlreadyGrantedExp = $"{UniqueId}_ObjAlreadyGrantedExp";

    MigratedFrom100 = $"{UniqueId}_MigratedFrom100";

    helper.Events.Content.AssetRequested += OnAssetRequested;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.GameLoop.DayStarted += OnDayStartedResetTailorCount;
    helper.Events.GameLoop.DayStarted += OnDayStartedHandleCouturier;
    helper.Events.GameLoop.DayEnding += OnDayEnding;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    GameStateQuery.Register($"{UniqueId}_HAS_WEAVER", HAS_WEAVER);
    GameStateQuery.Register($"{UniqueId}_HAS_DYER", HAS_DYER);
    GameStateQuery.Register($"{UniqueId}_HAS_SERICULTURIST", HAS_SERICULTURIST);
    GameStateQuery.Register($"{UniqueId}_PLAYER_BUFFED_TEXTILE_LEVEL", PLAYER_BUFFED_TEXTILE_LEVEL);
    TokenParser.RegisterParser($"{UniqueId}_CouturierName", CouturierName);
    TokenParser.RegisterParser($"{UniqueId}_CouturierDescription", CouturierDescription);
    HasWOL = Helper.ModRegistry.IsLoaded("DaLion.Professions");

    // Harmony!
    var harmony = new Harmony(this.ModManifest.UniqueID);
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.sellToStorePrice)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(SObject_sellToStorePrice_Postfix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(TailoringMenu),
          nameof(TailoringMenu.CraftItem)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(TailoringMenu_CraftItem_Postfix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.getDescription)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.SObject_getDescription_Prefix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(TailoringMenu),
          nameof(TailoringMenu.IsValidCraft)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(TailoringMenu_IsValidCraft_Prefix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(TailoringMenu),
          "_UpdateDescriptionText"),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(TailoringMenu_UpdateDescriptionText_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Farmer),
          nameof(Farmer.OnItemReceived)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(Farmer_OnItemReceived_prefix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.onReadyForHarvest)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(SObject_onReadyForHarvest_Postfix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(NPC),
          nameof(NPC.getGiftTasteForThisItem)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(NPC_getGiftTasteForThisItem_Prefix)));
    // Disabled for now because it breaks Better Crafting bulk craft :((((
    //harmony.Patch(
    //    original: AccessTools.DeclaredConstructor(typeof(CraftingRecipe), new[] { typeof(string), typeof(bool) }),
    //    postfix: new HarmonyMethod(typeof(ModEntry),
    //      nameof(CraftingRecipe_Constructor_Postfix)));


    helper.ConsoleCommands.Add(
        $"{UniqueId}_OpenCouturierChest",
        Helper.Translation.Get("command.openCouturierChest"),
        OpenCouturierChest);
  }

  public void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo(TextileSkill.SkillIconTexture)) {
      e.LoadFromModFile<Texture2D>("Assets/SkillIcon.png", AssetLoadPriority.Medium);
    }
    if (e.NameWithoutLocale.IsEquivalentTo(TextileSkill.SkillPageIconTexture)) {
      e.LoadFromModFile<Texture2D>("Assets/SkillPageIcon.png", AssetLoadPriority.Medium);
    }
    foreach (var profession in Enum.GetValues(typeof(TextileProfessionEnum))) {
      if (e.NameWithoutLocale.IsEquivalentTo(TextileSkill.ProfessionIconTexture((TextileProfessionEnum)profession))) {
        e.LoadFromModFile<Texture2D>($"Assets/ProfessionIcon{(int)profession}.png", AssetLoadPriority.Medium);
      }
    }
  }

  public void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    cpApi = Helper.ModRegistry.GetApi<ContentPatcher.IContentPatcherAPI>("Pathoschild.ContentPatcher")!;
    if (cpApi is null) {
      ModEntry.StaticMonitor.Log("FATAL ERROR: cannot access Content Patcher API?", LogLevel.Error);
      return;
    }
    cpApi.RegisterToken(this.ModManifest, "TextileLevel", () => {
      // save is loaded
      if (Context.IsWorldReady)
        return new string[] { Game1.player.GetCustomBuffedSkillLevel(TextileSkill.SkillId).ToString() };

      // or save is currently loading
      //if (SaveGame.loaded?.player != null)
      //  return new[] { SaveGame.loaded.player.Name };

      // no save loaded (e.g. on the title screen)
      return null;
    });
    cpApi.RegisterToken(this.ModManifest, "TextileBaseLevel", () => {
      // save is loaded
      if (Context.IsWorldReady)
        return new string[] {
        Game1.player.GetCustomSkillLevel(TextileSkill.SkillId).ToString()
        };
      return null;
    });

    cpApi.RegisterToken(this.ModManifest, "AnyPlayerHasCouturier", () => {
      if (Context.IsWorldReady)
        return new string[] { AnyPlayerHasCouturier().ToString() };
      return null;
    });

    cpApi.RegisterToken(this.ModManifest, "PlayerHasOutfitter", () => {
      if (Context.IsWorldReady)
        return new[] { Game1.player.HasCustomProfession(TextileSkill.Outfitter).ToString() };
      return null;
    });
    SpaceCore.Skills.RegisterSkill(new TextileSkill());
  }

  static bool HAS_WEAVER(string[] query, GameStateQueryContext context) {
    return HasProfessionCommon(query, context, TextileSkill.Weaver);
  }
  static bool HAS_DYER(string[] query, GameStateQueryContext context) {
    return HasProfessionCommon(query, context, TextileSkill.Dyer);
  }
  static bool HAS_SERICULTURIST(string[] query, GameStateQueryContext context) {
    return HasProfessionCommon(query, context, TextileSkill.Sericulturist);
  }
  static bool PLAYER_BUFFED_TEXTILE_LEVEL(string[] query, GameStateQueryContext context) {
    return GameStateQuery.Helpers.PlayerSkillLevelImpl(query, context.Player, (f) => f.GetCustomBuffedSkillLevel(TextileSkill.SkillId));
  }

  static bool HasProfessionCommon(string[] query, GameStateQueryContext context, TextileProfession profession) {
    if (!ArgUtility.TryGet(query, 1, out var value, out var error, allowBlank: true, "string playerKey")) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    return GameStateQuery.Helpers.WithPlayer(context.Player, value, (Farmer target) => target.HasCustomProfession(profession));
  }

  static bool AnyPlayerHasCouturier() {
    if (Game1.player.useSeparateWallets) {
      return Game1.player.HasCustomProfession(TextileSkill.Couturier);
    }
    foreach (Farmer farmer in Game1.getAllFarmers()) {
      if (farmer.isActive() && farmer.HasCustomProfession(TextileSkill.Couturier)) {
        return true;
      }
    }
    return false;
  }

  // Increases sell price if Dyer/Couturier
  static void SObject_sellToStorePrice_Postfix(SObject __instance, ref int __result, long specificPlayerID) {
    bool hasDyer = false;
    bool hasCouturier = false;
    foreach (Farmer farmer in Game1.getAllFarmers()) {
      if (Game1.player.useSeparateWallets) {
        if (specificPlayerID == -1) {
          if (farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID || !farmer.isActive()) {
            continue;
          }
        } else if (farmer.UniqueMultiplayerID != specificPlayerID) {
          continue;
        }
      } else if (!farmer.isActive()) {
        continue;
      }
      hasDyer = hasDyer || farmer.HasCustomProfession(TextileSkill.Dyer);
      hasCouturier = hasCouturier || farmer.HasCustomProfession(TextileSkill.Couturier);
      if (hasDyer && hasCouturier) break;
    }
    if (hasDyer && ItemContextTagManager.DoAnyTagsMatch([
          $"{ContentPackId}_dyed_yarn_item",
          $"{ContentPackId}_dyed_cloth_item",
      ], ItemContextTagManager.GetBaseContextTags(__instance.QualifiedItemId))) {
      __result = (int)(__result * 1.2f);
    }
    if (hasCouturier) {
      __result = (int)(__result * CouturierMultipler);
    }
  }

  static PerScreen<int> DailyTailor = new();
  public void OnDayStartedResetTailorCount(object? sender, DayStartedEventArgs e) {
    DailyTailor.Value = 0;
    CachedDescriptions.Clear();
  }

  static int GetMaxTailor(Farmer farmer) {
    var level = Game1.player.GetCustomSkillLevel(TextileSkill.SkillId);
    if (level < 2) {
      return 0;
    } else {
      return 3 + (Game1.player.GetCustomBuffedSkillLevel(TextileSkill.SkillId)) / 2;
    }
  }

  // Don't allow sewing if over limit
  static bool TailoringMenu_IsValidCraft_Prefix(ref bool __result, Item left_item, Item right_item) {
    if (right_item is not null
        && ItemContextTagManager.HasBaseTag(right_item.QualifiedItemId, $"{ContentPackId}_clothing_template_item")
        && DailyTailor.Value >= GetMaxTailor(Game1.player)) {
      __result = false;
      return false;
    }
    return true;
  }

  // Update description for over sewing limit
  static void TailoringMenu_UpdateDescriptionText_Postfix(TailoringMenu __instance) {
    if (!__instance.IsBusy()
        && __instance.rightIngredientSpot.item is not null
        && ItemContextTagManager.HasBaseTag(__instance.rightIngredientSpot.item.QualifiedItemId, $"{ContentPackId}_clothing_template_item")
        && DailyTailor.Value >= GetMaxTailor(Game1.player)) {
      Helper.Reflection.GetField<string>(__instance, "displayedDescription").SetValue(
          Helper.Translation.Get("message.noTailoring"));
    }
  }

  static void SetTailoringQuality(Item baseItem) {
    if (baseItem.Quality < SObject.bestQuality && Game1.player.HasCustomProfession(TextileSkill.Tailor)) {
      var level = Game1.player.GetCustomSkillLevel(TextileSkill.SkillId) - 5;
      var silverChance = 0.2 + level * 0.1;
      var goldChance = (level - 1) * 0.1;
      var iridiumChance = (level - 3) * 0.1;
      if (Game1.random.NextBool(iridiumChance)) {
        baseItem.Quality = SObject.bestQuality;
      } else if (baseItem.Quality < SObject.highQuality && Game1.random.NextBool(goldChance)) {
        baseItem.Quality = SObject.highQuality;
      } else if (baseItem.Quality < SObject.medQuality && Game1.random.NextBool(silverChance)) {
        baseItem.Quality = SObject.medQuality;
      }
    }
  }

  // Sewing!
  static void TailoringMenu_CraftItem_Postfix(ref Item __result, Item left_item, Item right_item) {
    if (ItemContextTagManager.HasBaseTag(__result.QualifiedItemId, $"{ContentPackId}_base_clothing_item")
        && left_item is SObject cloth
        && ColoredObject.TrySetColor(__result, TailoringMenu.GetDyeColor(left_item) ?? Color.White, out var coloredClothing)) {
      coloredClothing.Price = (int)(cloth.Price * (cloth.QualifiedItemId == "(O)428" ? 1.5 : 2));
      coloredClothing.preservedParentSheetIndex.Value = cloth.GetPreservedItemId() ?? cloth.ItemId; //"440";
      coloredClothing.Name += $" {coloredClothing.preservedParentSheetIndex.Value}";
      coloredClothing.displayNameFormat = $"[LocalizedText Strings/Objects:{ContentPackId}_ClothingDynamicName %PRESERVED_DISPLAY_NAME %DISPLAY_NAME]";
      if (Game1.objectData.TryGetValue(cloth.ItemId, out var objectData)) {
        coloredClothing.modData[ClothingDyeUsedCloth] = objectData.CustomFields.GetValueOrDefault($"{ContentPackId}_Dye", "");
      }
      coloredClothing.Quality = left_item.Quality;
      SetTailoringQuality(coloredClothing);
      __result = coloredClothing;
    }
    if (ItemContextTagManager.HasBaseTag(__result.QualifiedItemId, $"{ContentPackId}_embroidered_clothing_item")
        && left_item is SObject clothing
        && right_item is SObject yarn
        && ColoredObject.TrySetColor(__result, TailoringMenu.GetDyeColor(left_item) ?? Color.White, out var embroideredClothing)) {
      embroideredClothing.Quality = clothing.Quality;
      embroideredClothing.Price = (int)(clothing.Price * 1.2) + (int)(yarn.Price * 1.2);
      embroideredClothing.preservedParentSheetIndex.Value = clothing.preservedParentSheetIndex.Value;
      embroideredClothing.modData.CopyFrom(clothing.modData);
      embroideredClothing.modData["selph.ExtraMachineConfig.ExtraPreserveId.1"] = yarn.GetPreservedItemId() ?? yarn.ItemId; // "440";
      var yarnColor = TailoringMenu.GetDyeColor(yarn) ?? Color.White;
      embroideredClothing.modData["selph.ExtraMachineConfig.ExtraColor.1"] = $"{yarnColor.R},{yarnColor.G},{yarnColor.B}";
      // Populate the field
      GetRandomImg(embroideredClothing, out var imageType, out var imageId);
      embroideredClothing.Name = clothing.Name + $" {yarn.ItemId} {yarn.GetPreservedItemId() ?? yarn.ItemId} {imageType} {imageId}";
      embroideredClothing.displayNameFormat = $"[LocalizedText Strings/Objects:{ContentPackId}_EmbroideredClothingDynamicName %PRESERVED_DISPLAY_NAME %DISPLAY_NAME]";
      if (Game1.objectData.TryGetValue(yarn.ItemId, out var objectData)) {
        embroideredClothing.modData[ClothingDyeUsedEmbroidery] = objectData.CustomFields.GetValueOrDefault($"{ContentPackId}_Dye", "");
      }
      embroideredClothing.Quality = left_item.Quality;
      SetTailoringQuality(embroideredClothing);
      __result = embroideredClothing;
    }
    if (ItemContextTagManager.HasBaseTag(__result.QualifiedItemId, $"{ContentPackId}_jeweled_clothing_item")
        && left_item is SObject clothing2
        && right_item is SObject gem
        && ColoredObject.TrySetColor(__result, TailoringMenu.GetDyeColor(left_item) ?? Color.White, out var jeweledClothing)) {
      jeweledClothing.Quality = clothing2.Quality;
      jeweledClothing.Price = (int)(clothing2.Price * 1.2) + (int)(gem.Price * 2);
      jeweledClothing.preservedParentSheetIndex.Value = clothing2.preservedParentSheetIndex.Value;
      jeweledClothing.modData.CopyFrom(clothing2.modData);
      jeweledClothing.modData["selph.ExtraMachineConfig.ExtraPreserveId.2"] = gem.ItemId;
      var gemColor = TailoringMenu.GetDyeColor(gem) ?? Color.White;
      jeweledClothing.modData["selph.ExtraMachineConfig.ExtraColor.2"] = $"{gemColor.R},{gemColor.G},{gemColor.B}";
      jeweledClothing.Name = clothing2.Name + $" {gem.ItemId}";
      jeweledClothing.displayNameFormat = $"[LocalizedText Strings/Objects:{ContentPackId}_JeweledClothingDynamicName %PRESERVED_DISPLAY_NAME %DISPLAY_NAME]";
      jeweledClothing.Quality = left_item.Quality;
      SetTailoringQuality(jeweledClothing);
      __result = jeweledClothing;
    }
  }

  static ConditionalWeakTable<SObject, string> CachedDescriptions = new();

  enum ImageType {
    Object,
    Animal,
  }

  static string GetRandomImg(SObject obj, out ImageType type, out string id) {
    if (obj.modData.TryGetValue(ClothingRandomImageType, out var typeStr)
        && Enum.TryParse(typeStr, out type)
        && obj.modData.TryGetValue(ClothingRandomImageId, out id)) {
    } else {
      if (Game1.random.NextBool(0.7)) {
        var randomItem = ItemQueryResolver.TryResolve("RANDOM_ITEMS (O)", null, ItemQuerySearchMode.RandomOfTypeItem,
            perItemCondition: "ITEM_CATEGORY Target -2 -4 -5 -6 -12 -17 -18 -23 -27 -75 -79 -80 -81").FirstOrDefault()?.Item as Item;
        // this should be impossible, but fallback to apple just in case
        randomItem ??= ItemRegistry.Create("(O)613");
        type = ImageType.Object;
        id = randomItem.ItemId;
      } else {
        type = ImageType.Animal;
        id = Game1.random.ChooseFrom<string>(Game1.farmAnimalData.Keys.ToList());
      }
      obj.modData[ClothingRandomImageType] = type.ToString();
      obj.modData[ClothingRandomImageId] = id;
    }
    string? str = null;
    switch (type) {
      case ImageType.Object:
        str = ItemRegistry.GetData(id)?.DisplayName;
        break;
      case ImageType.Animal:
        if (Game1.farmAnimalData.TryGetValue(id, out var animalData)) {
          str = animalData.DisplayName;
        }
        break;
    }
    if (!String.IsNullOrEmpty(str)) {
      return TokenParser.ParseText(str);
    } else {
      // Shouldn't happen
      return "Spoiled Apple";
    }
  }

  // Dynamic descriptions for clothing items.
  static bool SObject_getDescription_Prefix(SObject __instance, ref string __result) {
    if (CachedDescriptions.TryGetValue(__instance, out var cachedDesc)) {
      __result = cachedDesc;
      return false;
    }
    if (ItemContextTagManager.HasBaseTag(__instance.QualifiedItemId, $"{ContentPackId}_clothing_item")) {
      var isDyedCloth = __instance.modData.TryGetValue(ClothingDyeUsedCloth, out var dyeUsed) && !String.IsNullOrWhiteSpace(dyeUsed);
      var description = Game1.content.LoadString($"Strings/Objects:{ContentPackId}_ClothingDynamicDescription1" + (isDyedCloth ? "" : "_Undyed"),
          ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId).DisplayName,
          ItemRegistry.GetDataOrErrorItem(__instance.GetPreservedItemId() ?? __instance.ItemId).DisplayName,
          ItemRegistry.GetDataOrErrorItem(__instance.modData.GetValueOrDefault(ClothingDyeUsedCloth, $"{ContentPackId}_DyeWhite")).DisplayName);
      if (ItemContextTagManager.HasBaseTag(__instance.QualifiedItemId, $"{ContentPackId}_base_clothing_item")) {
        if (Game1.player.GetCustomSkillLevel(TextileSkill.SkillId) >= 6) {
          description += "\n" + Game1.content.LoadString($"Strings/Objects:{ContentPackId}_ClothingDynamicDescription2");
        }
      } else {
        var isDyedThread = __instance.modData.TryGetValue(ClothingDyeUsedEmbroidery, out var dyeEmbroideryUsed) && !String.IsNullOrWhiteSpace(dyeEmbroideryUsed);
        description += "\n" + Game1.content.LoadString($"Strings/Objects:{ContentPackId}_ClothingDynamicDescription3" + (isDyedThread ? "" : "_Undyed"),
          ItemRegistry.GetDataOrErrorItem(__instance.modData.GetValueOrDefault("selph.ExtraMachineConfig.ExtraPreserveId.1", __instance.ItemId)).DisplayName,
          ItemRegistry.GetDataOrErrorItem(__instance.modData.GetValueOrDefault(ClothingDyeUsedEmbroidery, $"{ContentPackId}_DyeWhite")).DisplayName,
          GetRandomImg(__instance, out _, out _));
        if (ItemContextTagManager.HasBaseTag(__instance.QualifiedItemId, $"{ContentPackId}_embroidered_clothing_item")) {
          if (Game1.player.GetCustomSkillLevel(TextileSkill.SkillId) >= 8) {
            description += "\n" + Game1.content.LoadString($"Strings/Objects:{ContentPackId}_ClothingDynamicDescription4");
          }
        } else {
          description += "\n" + Game1.content.LoadString($"Strings/Objects:{ContentPackId}_ClothingDynamicDescription5",
            ItemRegistry.GetDataOrErrorItem(__instance.modData.GetValueOrDefault("selph.ExtraMachineConfig.ExtraPreserveId.2", "72")).DisplayName);
        }
      }
      int width = 300;
      try {
        width = ModEntry.Helper.Reflection.GetMethod(__instance, "getDescriptionWidth").Invoke<int>();
      }
      catch (Exception e) {
        ModEntry.StaticMonitor.Log($"Error reflecting into getDescription: {e.Message}", LogLevel.Error);
      }
      __result = Game1.parseText(description, Game1.smallFont, width);
      CachedDescriptions.Add(__instance, __result);
      return false;
    }
    return true;
  }

  // Grant XP for sewing clothing items
  static void Farmer_OnItemReceived_prefix(Farmer __instance, Item item, int countAdded, Item mergedIntoStack, bool hideHudNotification = false) {
    if (!__instance.IsLocalPlayer || item.HasBeenInInventory || item is not SObject obj || obj.bigCraftable.Value) return;
    if (ItemContextTagManager.HasBaseTag(item.QualifiedItemId, $"{ContentPackId}_textile_xp")) {
      var multiplier = 1f;
      if (ItemContextTagManager.HasBaseTag(item.QualifiedItemId, $"{ContentPackId}_half_textile_xp")) {
        multiplier = 0.25f;
      }
      var exp = (int)(obj.Price * obj.Stack * multiplier) / 20;
      __instance.AddCustomSkillExperience(TextileSkill.SkillId, exp);
      ModEntry.StaticMonitor.Log($"Granting {exp} Sewing experience for {__instance.displayName}");
    }
    if (ItemContextTagManager.HasBaseTag(item.QualifiedItemId, $"{ContentPackId}_base_clothing_item")) {
      DailyTailor.Value += 1;
      if (DailyTailor.Value >= GetMaxTailor(__instance)) {
        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("message.noMoreTailoring")) {
          noIcon = true,
        });
      }
    }
  }

  // Grant XP for ready machines
  static void SObject_onReadyForHarvest_Postfix(SObject __instance) {
    if (__instance.heldObject.Value is not SObject obj || obj.modData.ContainsKey(ObjAlreadyGrantedExp)) return;
    if (ItemContextTagManager.HasBaseTag(__instance.QualifiedItemId, $"{ContentPackId}_textile_xp")) {
      var multiplier = 1f;
      if (ItemContextTagManager.HasBaseTag(__instance.QualifiedItemId, $"{ContentPackId}_half_textile_xp")) {
        multiplier = 0.25f;
      }
      var activeFarmers = Game1.getAllFarmers().Where(f => f.isActive());
      var playerXpModifier = new float[] { 1f, 0.7f, 0.6f, 0.5f }[Math.Clamp(activeFarmers.Count() - 1, 0, 3)];
      multiplier *= playerXpModifier;
      foreach (Farmer farmer in activeFarmers) {
        var exp = (int)(obj.Price * obj.Stack * multiplier) / 20;
        farmer.AddCustomSkillExperience(TextileSkill.SkillId, exp);
        ModEntry.StaticMonitor.Log($"Granting {exp} Sewing experience for {farmer.displayName}");
      }
      obj.modData[ObjAlreadyGrantedExp] = "true";
    }
  }

  // Couturier handlers
  public void OnDayEnding(object? sender, DayEndingEventArgs e) {
    if (Game1.player.useSeparateWallets || Game1.player.IsMainPlayer) {
      HandleShippedClothing(Game1.getFarm().getShippingBin(Game1.player));
      foreach (Farmer offlineFarmhand in Game1.getOfflineFarmhands()) {
        if (offlineFarmhand.isUnclaimedFarmhand) {
          continue;
        }
        HandleShippedClothing(Game1.getFarm().getShippingBin(offlineFarmhand));
      }
    }
    if (Game1.IsMasterGame) {
      Utility.ForEachLocation((GameLocation location) => {
        foreach (var obj in location.objects.Values) {
          if (obj is Chest chest && chest.SpecialChestType == Chest.SpecialChestTypes.MiniShippingBin) {
            if (chest.GlobalInventoryId is not null) {
              HandleShippedClothing(Game1.player.team.GetOrCreateGlobalInventory(chest.GlobalInventoryId));
            } else {
              HandleShippedClothing(chest.Items);
              foreach (var walletItems in chest.separateWalletItems.Values) {
                HandleShippedClothing(walletItems);
              }
            }
          }
        }
        return true;
      });
    }
  }

  static void HandleShippedClothing(IInventory inventory) {
    foreach (var obj in inventory) {
      if (ItemContextTagManager.HasBaseTag(obj.QualifiedItemId, $"{ContentPackId}_clothing_item")) {
        var couturierChest = Game1.player.team.GetOrCreateGlobalInventory(CouturierInventoryId);
        couturierChest.Add(obj.getOne());
      }
    }
  }

  static float? couturierMultiplier;
  static float CouturierMultipler {
    get {
      if (couturierMultiplier is null) {
        couturierMultiplier = 1f;
        var couturierChest = Game1.player.team.GetOrCreateGlobalInventory(CouturierInventoryId);
        if (couturierChest.Count == 0) return couturierMultiplier.Value;
        HashSet<string> clothingTypes = new();
        HashSet<string> clothingMaterials = new();
        HashSet<Color> clothingColors = new();
        HashSet<string> threadMaterials = new();
        HashSet<string> threadColors = new();
        HashSet<string> gems = new();
        foreach (var item in couturierChest) {
          if (item is not ColoredObject obj) continue;
          if (Game1.objectData.TryGetValue(obj.ItemId, out var objData)
              && objData.CustomFields?.TryGetValue($"{ContentPackId}_ClothingType", out var clothingType) is true) {
            clothingTypes.Add(clothingType);
          }
          clothingMaterials.Add(obj.GetPreservedItemId() ?? "440");
          clothingColors.Add(obj.color.Value);
          threadMaterials.Add(obj.modData.GetValueOrDefault("selph.ExtraMachineConfig.ExtraPreserveId.1", "0"));
          threadColors.Add(obj.modData.GetValueOrDefault("selph.ExtraMachineConfig.ExtraColor.1", "0"));
          gems.Add(obj.modData.GetValueOrDefault("selph.ExtraMachineConfig.ExtraPreserveId.2", "0"));
        }
        var uniquenessCount = (clothingColors.Count() + clothingMaterials.Count() + clothingTypes.Count() + threadMaterials.Count() + threadColors.Count() + gems.Count());
        couturierMultiplier += .005f * uniquenessCount;
        ModEntry.StaticMonitor.Log($"Couturier Profession Uniqueness Number: {uniquenessCount}", LogLevel.Info);
      }
      return couturierMultiplier.Value!;
    }
  }


  public void OnDayStartedHandleCouturier(object? sender, DayStartedEventArgs e) {
    couturierMultiplier = null;
    if (!Context.IsMainPlayer) return;
    // Increment age, and remove items that are too old
    var couturierChest = Game1.player.team.GetOrCreateGlobalInventory(CouturierInventoryId);
    couturierChest.RemoveWhere(obj => {
      if (obj is null) return true;
      int age = 0;
      if (obj.modData.TryGetValue(CouturierModDataAgeKey, out var ageStr)
          && Int32.TryParse(ageStr, out age)) { }
      age++;
      if (age >= 8) {
        return true;
      }
      obj.modData[CouturierModDataAgeKey] = age.ToString();
      return false;
    });
    // Just in case
    couturierChest.RemoveEmptySlots();
  }

  static bool CouturierName(string[] query, out string replacement, Random random, Farmer player) {
    replacement = Helper.Translation.Get("power.couturier.name");
    return true;
  }

  static bool CouturierDescription(string[] query, out string replacement, Random random, Farmer player) {
    replacement = Helper.Translation.Get("power.couturier.description",
        new {
          multiplier = Math.Round((CouturierMultipler - 1) * 100),
        }
      );
    return true;
  }

  static bool NPC_getGiftTasteForThisItem_Prefix(Item item, ref int __result) {
    if (Game1.player.HasCustomProfession(TextileSkill.Outfitter)
        && ItemContextTagManager.HasBaseTag(item.QualifiedItemId, $"{ContentPackId}_jeweled_clothing_item")) {
      __result = 7; // Stardrop
      return false;
    }
    return true;
  }

  // Make the recipes free (we can do it in Data/CraftingRecipes but it throws red errors)
  static void CraftingRecipe_Constructor_Postfix(CraftingRecipe __instance, string name, bool isCookingRecipe) {
    if (name.StartsWith($"{ContentPackId}_Template")) {
      __instance.recipeList.Clear();
    }
  }

  static void OpenCouturierChest(string command, string[] args) {
    var chest = Game1.player.team.GetOrCreateGlobalInventory(CouturierInventoryId);
    if (chest is not null) {
      Game1.activeClickableMenu = new ItemGrabMenu(chest);
    }
  }

  // Normalize the XP curve from 1.0.0
  [EventPriority(EventPriority.Low)]
  static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
    if (!Game1.player.modData.ContainsKey(MigratedFrom100)) {
      //var level = Game1.player.GetCustomSkillLevel(TextileSkill.SkillId);
      var exp = Game1.player.GetCustomSkillExperience(TextileSkill.SkillId);
      if (exp > 0) {
        var newExp = exp / 20;
        ModEntry.StaticMonitor.Log($"Save from 1.0.0 detected; migrating to smaller EXP curve by subtracting {-(newExp - exp)} experience points. If this is causing unintended effects in your game, file a bug report.", LogLevel.Alert);
        Game1.player.AddCustomSkillExperience(TextileSkill.SkillId, newExp - exp);
      }
      Game1.player.modData[MigratedFrom100] = "true";
    }
  }
}

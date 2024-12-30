using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TokenizableStrings;
using StardewValley.Quests;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Triggers;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Delegates;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Buildings;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.SpecialOrders.Rewards;
using StardewValley.GameData.SpecialOrders;
using StardewValley.GameData.Objects;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.FreshFarmProduce;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;
  internal static StardewUI.Framework.IViewEngine viewEngine = null!;

  internal static CompetitionDataAssetHandler competitionDataAssetHandler = null!;

  public static ModConfig Config = null!;

  public static string FarmCompetitionSpecialOrderId { get => $"{UniqueId}.FarmCompetition"; }

  // No, Bronze, Silver, Gold or Iridium
  static string GetCompetitionFinishedFlag(string reward) {
    return $"{UniqueId}.Finished{reward}";
  }

  public override void Entry(IModHelper helper) {
    Config = helper.ReadConfig<ModConfig>();
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    competitionDataAssetHandler = new CompetitionDataAssetHandler();
    competitionDataAssetHandler.RegisterEvents(Helper);

    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.GameLoop.DayEnding += OnDayEnding;

    // Register custom stuff
    TriggerActionManager.RegisterAction(
        $"{UniqueId}_AddGlobalFriendshipPoints",
        AddGlobalFriendshipPoints);
    TriggerActionManager.RegisterAction(
        $"{UniqueId}_AddFame",
        AddFame);

    GameStateQuery.Register(
        $"{UniqueId}_COMPETITION_ENABLED",
        CompetitionEnabled);

    GameStateQuery.Register(
        $"{UniqueId}_HAS_FAME",
        HasFame);

    Phone.PhoneHandlers.Add(new JojaDashPhoneHandler());
    Phone.PhoneHandlers.Add(new AdSpotPhoneHandler());

    helper.ConsoleCommands.Add(
        $"{UniqueId}_AddSpecialOrder",
        Helper.Translation.Get("AddSpecialOrder"),
        AddSpecialOrder);

    helper.ConsoleCommands.Add(
        $"{UniqueId}_RemoveSpecialOrder",
        Helper.Translation.Get("RemoveSpecialOrder"),
        RemoveSpecialOrder);

    helper.ConsoleCommands.Add(
        $"{UniqueId}_ResetSpecialOrder",
        Helper.Translation.Get("ResetSpecialOrder"),
        ResetSpecialOrder);

    helper.ConsoleCommands.Add(
        $"{UniqueId}_PrintDiagnostics",
        Helper.Translation.Get("PrintDiagnostics"),
        PrintDiagnostics);

    helper.ConsoleCommands.Add(
        $"{UniqueId}_AddWinningItems",
        Helper.Translation.Get("AddWinningItems"),
        AddWinningItems);

    TokenParser.RegisterParser(
        $"{UniqueId}_FameDescription",
        FameDescriptionToken);
    TokenParser.RegisterParser(
        $"{UniqueId}_FameName",
        FameNameToken);

    // Harmony!
    var harmony = new Harmony(this.ModManifest.UniqueID);
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.sellToStorePrice)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.SObject_sellToStorePrice_Postfix)));

    harmony.Patch(
        original: AccessTools.PropertyGetter(typeof(SObject),
          nameof(SObject.DisplayName)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.SObject_DisplayName_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.getDescription)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.SObject_getDescription_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Item),
          nameof(Item.canStackWith)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Item_canStackWith_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          "_PopulateContextTags"),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.SObject_PopulateContextTags_Postfix)));

    // Replace special order
    harmony.Patch(
        original: AccessTools.Method(typeof(SpecialOrder),
          nameof(SpecialOrder.GetSpecialOrder)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.SpecialOrder_GetSpecialOrder_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SpecialOrder),
          nameof(SpecialOrder.HostHandleQuestEnd)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.SpecialOrder_HostHandleQuestEnd_Postfix)));
    
    // usable objects
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.performUseAction)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.SObject_performUseAction_prefix)));

    // waow
    harmony.Patch(
        original: AccessTools.Method(typeof(QuestLog),
          nameof(QuestLog.receiveLeftClick)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.QuestLog_receiveLeftClick_postfix)));
  }
  
  void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    SpaceCore.IApi? scApi = Helper.ModRegistry.GetApi<SpaceCore.IApi>("spacechase0.SpaceCore");
    if (scApi is null) {
      StaticMonitor.Log("FATAL ERROR: SpaceCore API not detected! This should not happen.", LogLevel.Error);
      return;
    }
    viewEngine = Helper.ModRegistry.GetApi<StardewUI.Framework.IViewEngine>("focustense.StardewUI")!;
    if (viewEngine is null) {
      StaticMonitor.Log("FATAL ERROR: StardewUI API not detected! This should not happen.", LogLevel.Error);
      return;
    }
    scApi.RegisterSerializerType(typeof(ShipPointsObjective));
    scApi.RegisterSerializerType(typeof(ShippedItemEntry));

    viewEngine.RegisterViews($"Mods/{UniqueId}/Views", "assets/views");
    viewEngine.RegisterSprites($"Mods/{UniqueId}/Sprites", "assets/sprites");
    //viewEngine.EnableHotReloading();

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

    configMenu.AddSectionTitle(
        mod: this.ModManifest,
        text: () => Helper.Translation.Get("Config.freshSection.name")
    );

    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.disableStaleness.name"),
        tooltip: () => Helper.Translation.Get("Config.disableStaleness.description"),
        getValue: () => Config.DisableStaleness,
        setValue: value => {
          Config.DisableStaleness = value;
          if (Context.IsWorldReady) {
            Utility.ForEachItem((Item item) => {
              item.MarkContextTagsDirty();
              item.modData.Remove(Utils.CachedDescriptionKey);
              return true;
            });
          }
        });
    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.freshDisplayName.name"),
        tooltip: () => Helper.Translation.Get("Config.freshDisplayName.description"),
        getValue: () => Config.FreshDisplayName,
        setValue: value => Config.FreshDisplayName = value
        );
    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.showCategoriesInDescription.name"),
        tooltip: () => Helper.Translation.Get("Config.showCategoriesInDescription.description"),
        getValue: () => Config.ShowCategoriesInDescription,
        setValue: value => Config.ShowCategoriesInDescription = value
        );
    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.disableFreshPriceIncrease.name"),
        tooltip: () => Helper.Translation.Get("Config.disableFreshPriceIncrease.description"),
        getValue: () => Config.DisableFreshPriceIncrease,
        setValue: value => Config.DisableFreshPriceIncrease = value
        );
    configMenu.AddPageLink(
        mod: this.ModManifest,
        pageId: $"{UniqueId}.FreshPriceModifiers",
        text: () => Helper.Translation.Get("Config.freshPriceModifiers.name")
        );

    configMenu.AddSectionTitle(
        mod: this.ModManifest,
        text: () => Helper.Translation.Get("Config.competitionSection.name")
    );
    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.enableCompetition.name"),
        tooltip: () => Helper.Translation.Get("Config.enableCompetition.description"),
        getValue: () => Config.EnableCompetition,
        setValue: value => Config.EnableCompetition = value
        );
    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.enableFamePriceIncrease.name"),
        tooltip: () => Helper.Translation.Get("Config.enableFamePriceIncrease.description"),
        getValue: () => Config.EnableFamePriceIncrease,
        setValue: value => Config.EnableFamePriceIncrease = value
        );
    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.enableFameDifficultyIncrease.name"),
        tooltip: () => Helper.Translation.Get("Config.enableFameDifficultyIncrease.description"),
        getValue: () => Config.EnableFameDifficultyIncrease,
        setValue: value => Config.EnableFameDifficultyIncrease = value
        );
    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.enableDifficultyRandomization.name"),
        tooltip: () => Helper.Translation.Get("Config.enableDifficultyRandomization.description"),
        getValue: () => Config.EnableDifficultyRandomization,
        setValue: value => Config.EnableDifficultyRandomization = value
        );

    configMenu.AddSectionTitle(
        mod: this.ModManifest,
        text: () => Helper.Translation.Get("Config.otherSection.name")
    );
    configMenu.AddBoolOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.disableFlash.name"),
        tooltip: () => Helper.Translation.Get("Config.disableFlash.description"),
        getValue: () => Config.DisableFlash,
        setValue: value => Config.DisableFlash = value
        );

    // Fresh page begins
    configMenu.AddPage(
        mod: this.ModManifest,
        pageId: $"{UniqueId}.FreshPriceModifiers",
        pageTitle: () => Helper.Translation.Get("Config.freshPriceModifiers.name")
        );
    configMenu.AddParagraph(
        mod: this.ModManifest,
        text: () => Helper.Translation.Get("Config.freshPriceModifiers.description")
        );
    configMenu.AddSectionTitle(
        mod: this.ModManifest,
        text: () => Helper.Translation.Get("Config.freshPriceModifiersEarly.name")
    );
    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.regularQuality"),
        tooltip: () => Helper.Translation.Get("Config.defaultMultiplier", new {multiplier = ModConfig.DefaultEarlyFreshModifierRegular }),
        getValue: () => Config.EarlyFreshModifierRegular,
        setValue: value => Config.EarlyFreshModifierRegular = value,
        min: 1
        );
    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.silverQuality"),
        tooltip: () => Helper.Translation.Get("Config.defaultMultiplier", new {multiplier = ModConfig.DefaultEarlyFreshModifierSilver }),
        getValue: () => Config.EarlyFreshModifierSilver,
        setValue: value => Config.EarlyFreshModifierSilver = value,
        min: 1
        );
    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.goldQuality"),
        tooltip: () => Helper.Translation.Get("Config.defaultMultiplier", new {multiplier = ModConfig.DefaultEarlyFreshModifierGold }),
        getValue: () => Config.EarlyFreshModifierGold,
        setValue: value => Config.EarlyFreshModifierGold = value,
        min: 1
        );
    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.iridiumQuality"),
        tooltip: () => Helper.Translation.Get("Config.defaultMultiplier", new {multiplier = ModConfig.DefaultEarlyFreshModifierIridium }),
        getValue: () => Config.EarlyFreshModifierIridium,
        setValue: value => Config.EarlyFreshModifierIridium = value,
        min: 1
        );
    configMenu.AddSectionTitle(
        mod: this.ModManifest,
        text: () => Helper.Translation.Get("Config.freshPriceModifiersLate.name")
    );
    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.regularQuality"),
        tooltip: () => Helper.Translation.Get("Config.defaultMultiplier", new {multiplier = ModConfig.DefaultLateFreshModifierRegular }),
        getValue: () => Config.LateFreshModifierRegular,
        setValue: value => Config.LateFreshModifierRegular = value,
        min: 1
        );
    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.silverQuality"),
        tooltip: () => Helper.Translation.Get("Config.defaultMultiplier", new {multiplier = ModConfig.DefaultLateFreshModifierSilver }),
        getValue: () => Config.LateFreshModifierSilver,
        setValue: value => Config.LateFreshModifierSilver = value,
        min: 1
        );
    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.goldQuality"),
        tooltip: () => Helper.Translation.Get("Config.defaultMultiplier", new {multiplier = ModConfig.DefaultLateFreshModifierGold }),
        getValue: () => Config.LateFreshModifierGold,
        setValue: value => Config.LateFreshModifierGold = value,
        min: 1
        );
    configMenu.AddNumberOption(
        mod: this.ModManifest,
        name: () => Helper.Translation.Get("Config.iridiumQuality"),
        tooltip: () => Helper.Translation.Get("Config.defaultMultiplier", new {multiplier = ModConfig.DefaultLateFreshModifierIridium }),
        getValue: () => Config.LateFreshModifierIridium,
        setValue: value => Config.LateFreshModifierIridium = value,
        min: 1
        );
  }

  // Increase price of fresh items
  static void SObject_sellToStorePrice_Postfix(SObject __instance, ref int __result, long specificPlayerID) {
    if (!Config.DisableFreshPriceIncrease && Utils.IsFreshItem(__instance)) {
      bool notHasBook =
        (Game1.GetPlayer(specificPlayerID) ?? Game1.player).stats.Get("selph.FreshFarmProduceCP.FreshBook") == 0;
      float modifier =  __instance.Quality switch {
        // Default settings in comments
        // 1 -> 1.05 (5% more)
        // 1 -> 1.25 (25% more)
        SObject.lowQuality => notHasBook ? Config.EarlyFreshModifierRegular : Config.LateFreshModifierRegular,
        // 1.25 -> 1.375 (10% more)
        // 1.25 -> 1.75 (40% more)
        SObject.medQuality => notHasBook ? Config.EarlyFreshModifierSilver : Config.LateFreshModifierSilver,
        // 1.5 -> 1.725 (15% more)
        // 1.5 -> 2.5 (66% more)
        SObject.highQuality => notHasBook ? Config.EarlyFreshModifierGold : Config.LateFreshModifierGold,
        // 2 -> 2.4 (20% more)
        // 2 -> 4 (100% more)
        SObject.bestQuality => notHasBook ? Config.EarlyFreshModifierIridium : Config.LateFreshModifierIridium,
        _ => 1f,
      };
      __result = Math.Max((int)(__result * modifier), __result + 2);
    }
    if (Utils.IsJojaMealItem(__instance)) {
      __result = 5;
    }
    float fameModifier = Utils.GetFameSellPriceModifier();
    __result = (int)(__result * fameModifier);
  }

  static void SObject_DisplayName_Postfix(SObject __instance, ref string __result) {
    if (Utils.IsJojaMealItem(__instance)) {
      __result = ModEntry.Helper.Translation.Get("JojaMealItemName", new { Name = __result });
    }
    if (!Config.FreshDisplayName && Utils.IsStaleItem(__instance)) {
      __result = ModEntry.Helper.Translation.Get("StaleItemName", new { Name = __result });
    } else if (Config.FreshDisplayName && Utils.IsFreshItem(__instance)) {
      __result = ModEntry.Helper.Translation.Get("FreshItemName", new { Name = __result });
    }
  }

  // Makes fresh and non-fresh items non-stackable
  static void Item_canStackWith_Postfix(Item __instance, ref bool __result, ISalable other) {
    if (__result && other is Item otherItem) {
      __result =
        (Utils.IsFreshItem(__instance) == Utils.IsFreshItem(otherItem)) &&
        (Utils.IsJojaMealItem(__instance) == Utils.IsJojaMealItem(otherItem));
    }
  }

  // Spoil all fresh items
  void OnDayEnding(object? sender, DayEndingEventArgs e) {
    SpawnedItems.Clear();
    //Utility.ForEachItemContext((in ForEachItemContext context) => {
    //  var obj = context.Item as SObject;
    //  if (obj is null) {
    //    return true;
    //  }
    //  // Don't spoil items in mini shipping bins since they'll get yoinked after this function
    //  if (context.GetPath().Any((object path) => path is Chest chest && chest.specialChestType.Value == Chest.SpecialChestTypes.MiniShippingBin)) {
    //    return true;
    //  }
    //  // Only spoil indoor objects in animal houses
    //  if (obj.Location is not null && obj.Location is not AnimalHouse) {
    //    return true;
    //  }
    //  Utils.SpoilItem(context.Item);
    //  return true;
    //});

    Utility.ForEachLocation((GameLocation location) => {
      Chest? fridge = location.GetFridge(onlyUnlocked: false);
      if (fridge is not null) {
        Utils.SpoilItemInChest(fridge);
      }
      foreach (SObject obj in location.objects.Values) {
        if (obj != fridge) {
          if (obj is Chest chest && chest.specialChestType.Value != Chest.SpecialChestTypes.MiniShippingBin) {
            Utils.SpoilItemInChest(chest);
          }
          else if (obj.heldObject.Value is Chest chest2) {
            Utils.SpoilItemInChest(chest2);
          }
        }
      }
      foreach (Furniture furniture in location.furniture) {
      // Don't spoil fish inside fish tanks lmaooooo
        if (furniture is not FishTankFurniture) {
          furniture.ForEachItem((in ForEachItemContext context) => {
            Utils.SpoilItem(context.Item);
            return true;
          }, null);
        }
      }
      foreach (Building building in location.buildings) {
        foreach (Chest buildingChest in building.buildingChests) {
          Utils.SpoilItemInChest(buildingChest);
        }
      }
      return true;
    });
    foreach (Item returnedDonation in Game1.player.team.returnedDonations) {
      if (returnedDonation != null) {
        Utils.SpoilItem(returnedDonation);
      }
    }
    // Ignore Junimo Chests and Special Order Bins
    //foreach (Inventory globalInventory in Game1.player.team.globalInventories.Values) {
    //  foreach (Item item in globalInventory) {
    //    if (item != null) {
    //      Utils.SpoilItem(item);
    //    }
    //  }
    //}
    //foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders) {
    //  foreach (Item donatedItem in specialOrder.donatedItems) {
    //    if (donatedItem != null) {
    //      Utils.SpoilItem(donatedItem);
    //    }
    //  }
    //}
    foreach (var item in Game1.player.Items) {
      // Handle better chests? Idk
      if (item is Chest chest) {
        Utils.SpoilItemInChest(chest);
      }
      Utils.SpoilItem(item);
    }
  }

  static void SObject_PopulateContextTags_Postfix(SObject __instance, HashSet<string> tags) {
    if (Utils.IsFreshItem(__instance)) {
      tags.Add(Utils.FreshContextTag);
    }
  }

  static bool SpecialOrder_GetSpecialOrder_Prefix(ref SpecialOrder __result, string key, int? generation_seed) {
    if (key == FarmCompetitionSpecialOrderId) {
      ModEntry.StaticMonitor.Log("Spawning custom order", LogLevel.Info);
      generation_seed = generation_seed ?? Game1.random.Next();
      //Random random = Utility.CreateRandom(generation_seed.Value);
      SpecialOrder specialOrder = new SpecialOrder();
      specialOrder.generationSeed.Value = generation_seed.Value;
      specialOrder.questKey.Value = key;
      specialOrder.questName.Value = ModEntry.Helper.Translation.Get("CompetitionName");
      specialOrder.requester.Value = "Lewis";
      specialOrder.SetDuration(QuestDuration.Month);
      foreach (var categoryId in competitionDataAssetHandler.data.ActiveCategoryIds) {
        if (competitionDataAssetHandler.data.Categories.TryGetValue(categoryId, out var categoryData)) {
          var objective = new ShipPointsObjective(categoryId, categoryData.UseSalePrice);
          specialOrder.AddObjective(objective);
        } else {
          ModEntry.StaticMonitor.Log($"WARNING: Unknown objective ID found: {categoryId}.", LogLevel.Warn);
        }
      }
      specialOrder.questName.Value = ModEntry.Helper.Translation.Get("CompetitionName");
      __result = specialOrder;
      return false;
    }
    return true;
  }

  static void SpecialOrder_HostHandleQuestEnd_Postfix(SpecialOrder __instance) {
    if (!Game1.IsMasterGame || __instance.questKey.Value != FarmCompetitionSpecialOrderId) {
      return;
    }
    var completedObjectives = __instance.objectives.Select((OrderObjective objective) => {
      if (objective.IsComplete()) {
        return 1f;
      } else if (objective.GetCount() >= objective.GetMaxCount() / 2.0) {
        return 0.5f;
      } else {
        return 0f;
      }
    }).Sum();
    var completionRate = completedObjectives / __instance.objectives.Count;
    if (completionRate < 0.25) {
      Game1.addMail(GetCompetitionFinishedFlag(""), noLetter: true, sendToEveryone: true);
    } else if (completionRate < 0.5) {
      Game1.addMail(GetCompetitionFinishedFlag("Bronze"), noLetter: true, sendToEveryone: true);
    } else if (completionRate < 0.75) {
      Game1.addMail(GetCompetitionFinishedFlag("Silver"), noLetter: true, sendToEveryone: true);
    } else if (completionRate < 1) {
      Game1.addMail(GetCompetitionFinishedFlag("Gold"), noLetter: true, sendToEveryone: true);
    } else {
      Game1.addMail(GetCompetitionFinishedFlag("Iridium"), noLetter: true, sendToEveryone: true);
    }
  }

  const int SwagBagCount = 4;

  static List<string> SpawnedItems = new();

  static bool SObject_performUseAction_prefix(SObject __instance, ref bool __result, GameLocation location) {
    if (__instance.QualifiedItemId != "(O)selph.FreshFarmProduceCP.SwagBag" && __instance.QualifiedItemId != "(O)selph.FreshFarmProduceCP.JojaDashVoucher") {
      return true;
    }
    bool normalGameplay = !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack && !Game1.player.swimming.Value && !Game1.player.bathingClothes.Value && !Game1.player.onBridge.Value;
    if (!Game1.player.canMove || __instance.isTemporarilyInvisible || !normalGameplay) {
      __result = false;
      return false;
    }
    if (__instance.QualifiedItemId == "(O)selph.FreshFarmProduceCP.JojaDashVoucher") {
      if (Game1.player.mailReceived.Contains(JojaDashPhoneHandler.JojaDashActive)) {
        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("JojaDash.subscriptionAlreadyActive")) {
          noIcon = true,
        });
        __result = false;
        return false;
      }
      Game1.player.mailReceived.Add(JojaDashPhoneHandler.JojaDashActive);
      Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("JojaDash.subscriptionActive")) {
        noIcon = true,
      });
      Game1.playSound("newRecord");
      __result = true;
      return false;
    }
    if (__instance.QualifiedItemId == "(O)selph.FreshFarmProduceCP.SwagBag") {
      List<Item> missingPerfectionItems = new();
      if (!Game1.player.team.farmPerfect.Value) {
        foreach (ParsedItemData allDatum in
            from p in ItemRegistry.GetObjectTypeDefinition().GetAllData()
            orderby Game1.random.Next()
            select p) {
          string itemId = allDatum.ItemId;
          string qualifiedItemId = allDatum.QualifiedItemId;
          if (SpawnedItems.Contains(qualifiedItemId)) {
            continue;
          }
          ObjectData? objectData = allDatum.RawData as ObjectData;
          var isUncaughtFish = 
            allDatum.ObjectType == "Fish" &&
            !(objectData?.ExcludeFromFishingCollection ?? false) &&
            !Game1.player.fishCaught.ContainsKey(qualifiedItemId) &&
            !(objectData?.ContextTags.Contains("fish_legendary") ?? false);
          // Technically we want to iterate over cookingRecipes for each item and determine whether the item is actually cookable, but ehhh
          // this is good enough
          var isUncookedDish = 
            allDatum.ObjectType == "Cooking" &&
            !Game1.player.recipesCooked.ContainsKey(itemId) &&
            !(new List<string>{"217", "772", "773", "279", "873"}).Contains(itemId);
          var isUnshippedItem =
            SObject.isPotentialBasicShipped(itemId, allDatum.Category, allDatum.ObjectType) &&
            !Game1.player.basicShipped.ContainsKey(itemId);
          var isUndonatedMuseumItem = LibraryMuseum.IsItemSuitableForDonation(qualifiedItemId);
          if (isUncookedDish || isUncaughtFish || isUndonatedMuseumItem || isUnshippedItem) {
            missingPerfectionItems.Add(ItemRegistry.Create(qualifiedItemId));
            SpawnedItems.Add(qualifiedItemId);
            if (isUncaughtFish) {
              Game1.player.fishCaught.Add(qualifiedItemId, new int[3]);
            } else if (isUncookedDish) {
              Game1.player.recipesCooked.Add(itemId, 1);
            }
          }
          if (missingPerfectionItems.Count() > SwagBagCount) {
            break;
          }
        }
      }
      var swagBagContent = new List<Item>{
        ItemRegistry.Create("(O)908", 10),
        ItemRegistry.Create("(O)917", 5),
      };
      if (Game1.random.NextBool(0.1)) {
        swagBagContent.Add(ItemRegistry.Create("(O)341", 1));
      }
      swagBagContent.AddRange(missingPerfectionItems);
      // Replace unneeded stuff with magic rock candy
      if (missingPerfectionItems.Count() < SwagBagCount) {
        swagBagContent.Add(ItemRegistry.Create("(O)279", SwagBagCount - missingPerfectionItems.Count()));
      }
      Game1.player.addItemsByMenuIfNecessary(swagBagContent);
      Game1.playSound("newRecipe");
      __result = true;
      return false;
    }
    // This should not happen
    __result = false;
    return false;
  }

  static void QuestLog_receiveLeftClick_postfix(QuestLog __instance, int x, int y, bool playSound = true) {
    try {
      var specialOrder = Helper.Reflection.GetField<IQuest>(__instance, "_shownQuest").GetValue() as SpecialOrder;
      var questPage = Helper.Reflection.GetField<int>(__instance, "questPage").GetValue();
      if (questPage != -1 && specialOrder?.questKey.Value == FarmCompetitionSpecialOrderId) {
        __instance.exitQuestPage();
        Game1.activeClickableMenu = viewEngine.CreateMenuFromAsset(
            $"Mods/{UniqueId}/Views/CompetitionTracker",
            new CompetitionTrackerViewModel());
        Game1.nextClickableMenu.Add(__instance);
      }
    } catch (Exception e) {
      StaticMonitor.Log($"Error showing custom competition window: {e.Message}", LogLevel.Warn);
    }
  }

  private void AddSpecialOrder(string command, string[] args) {
    Game1.player.team.AddSpecialOrder(FarmCompetitionSpecialOrderId);
  }

  private void RemoveSpecialOrder(string command, string[] args) {
    Game1.player.team.specialOrders.RemoveWhere((SpecialOrder order) => order.questKey.Value == FarmCompetitionSpecialOrderId);
  }

  private void ResetSpecialOrder(string command, string[] args) {
    RemoveSpecialOrder(command, args);
    AddSpecialOrder(command, args);
  }

  private void PrintDiagnostics(string command, string[] args) {
    var specialOrder = Game1.player.team.specialOrders
      .FirstOrDefault((SpecialOrder order) => order.questKey.Value == FarmCompetitionSpecialOrderId, null);
    if (specialOrder is not null) {
      foreach (var objective in specialOrder.objectives) {
        if (objective is ShipPointsObjective shipPointsObjective &&
            competitionDataAssetHandler.data.Categories.TryGetValue(shipPointsObjective.Id.Value, out var categoryData)) {
          StaticMonitor.Log($"Objective {shipPointsObjective.Id}: {shipPointsObjective.GetCount()}/{shipPointsObjective.GetMaxCount()}", LogLevel.Info);
          shipPointsObjective.UpdatePoints();
          foreach (var item in shipPointsObjective.shippedItems.Keys) {
            var value = shipPointsObjective.shippedItems[item];
            int points = value.points.Value;
            int threshold = shipPointsObjective.GetThresholdFor(value);
            int actualPoints = shipPointsObjective.GetPointsFor(value);
            var uniqueFlavors = String.Join(",", value.flavors);
            StaticMonitor.Log($"  {item}: {points}/{threshold} ({actualPoints} actual, flavors: {uniqueFlavors})", LogLevel.Info);
          }
        }
      }
    } else {
      StaticMonitor.Log("Special order not active", LogLevel.Info);
    }
  }

  private void AddWinningItems(string command, string[] args) {
    Game1.player.addItemsByMenuIfNecessary([
      ItemRegistry.Create("(O)250", 999, 4),
      ItemRegistry.Create("(O)613", 999, 4),
      ItemRegistry.Create("(O)595", 999, 4),
      ItemRegistry.Create("(O)422", 999, 4),
      ItemRegistry.Create("(O)174", 999, 4),
      ItemRegistry.Create("(O)186", 999, 4),
      ItemRegistry.Create("(O)440", 999, 4),
      ItemRegistry.Create("(O)698", 999, 4),
      ItemRegistry.Create("(O)348", 999, 4),
      ItemRegistry.Create("(O)226", 999, 4),
      ItemRegistry.Create("(O)72", 999, 4),
    ]);
  }

  static bool AddGlobalFriendshipPoints(string[] args, TriggerActionContext context, out string error){
    if (!ArgUtility.TryGetInt(args, 1, out var points, out error, "int points")) {
      return false;
    }
    Utility.ForEachVillager((NPC n) => {
      Game1.player.changeFriendship(points, n);
      return true;
    });
    return true;
  }

  static bool AddFame(string[] args, TriggerActionContext context, out string error) {
    if (!ArgUtility.TryGetInt(args, 1, out var points, out error, "int points")) {
      return false;
    }
    Utils.AddFame(points);
    return true;
  }

  static bool CompetitionEnabled(string[] query, GameStateQueryContext context) {
    return Config.EnableCompetition;
  }

  static bool HasFame(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGetInt(query, 1, out var minFame, out var error, "int minFame") || !ArgUtility.TryGetOptionalInt(query, 2, out var maxFame, out error, int.MaxValue, "int maxFame")) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    int fame = Utils.GetFame();
    return fame >= minFame && fame <= maxFame;
  }

  static void SObject_getDescription_Postfix(SObject __instance, ref string __result) {
    if (!Config.ShowCategoriesInDescription || !__instance.canBeShipped()) return;
    Utils.ApplyDescription(__instance, ref __result);
  }

  static bool FameDescriptionToken(string[] query, out string replacement, Random random, Farmer player) {
    replacement = Helper.Translation.Get("FameBanner.tooltip",
        new {
      sellPriceIncrease = Math.Round(Utils.GetFameSellPriceModifier() * 100 - 100, 1),
      difficultyIncrease = Math.Round(Utils.GetFameDifficultyModifier() * 100 - 100, 1)}
      );
    return true;
  }

  static bool FameNameToken(string[] query, out string replacement, Random random, Farmer player) {
    replacement = Helper.Translation.Get("FameBanner", new { fame = Utils.GetFame() });
    return true;
  }
}

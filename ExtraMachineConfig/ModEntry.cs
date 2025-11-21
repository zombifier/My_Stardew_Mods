using Force.DeepCloner;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Machines;
using StardewValley.Triggers;
using StardewValley.Delegates;
using StardewValley.Buildings;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Leclair.Stardew.BetterCrafting;
using LoveOfCooking.Objects;

namespace Selph.StardewMods.ExtraMachineConfig;

using SObject = StardewValley.Object;

internal sealed class ModEntry : Mod {
  internal new static IModHelper Helper { get; set; } = null!;

  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static IExtraMachineConfigApi ModApi = null!;
  internal static ExtraOutputAssetHandler extraOutputAssetHandler = null!;
  internal static ExtraCraftingConfigAssetHandler extraCraftingConfigAssetHandler = null!;
  internal static ExtraMachineDataAssetHandler extraMachineDataAssetHandler = null!;
  internal static OutputRulesGlobalModifiersAssetHandler outputRulesGlobalModifiersAssetHandler = null!;
  internal static string UniqueId = null!;

  internal static string JunimoLovedItemContextTag = "junimo_loved_item";

  internal static string BuffStringsAssetName = null!;
  internal static string BuffsIconsAssetName = null!;

  public static string CopyMachineRulesFromKey = null!;

  static Texture2D? buffsIcons = null;
  static Texture2D BuffsIcons() {
    if (buffsIcons is null) {
      buffsIcons = Game1.content.Load<Texture2D>(BuffsIconsAssetName);
    }
    return buffsIcons!;
  }

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    ModApi = new ExtraMachineConfigApi();
    UniqueId = this.ModManifest.UniqueID;
    BuffStringsAssetName = $"{UniqueId}/BuffStrings";
    BuffsIconsAssetName = $"{UniqueId}/BuffsIcons";
    CopyMachineRulesFromKey = $"{UniqueId}.CopyMachineRulesFrom";

    extraOutputAssetHandler = new ExtraOutputAssetHandler();
    extraCraftingConfigAssetHandler = new ExtraCraftingConfigAssetHandler();
    extraMachineDataAssetHandler = new ExtraMachineDataAssetHandler();
    outputRulesGlobalModifiersAssetHandler = new OutputRulesGlobalModifiersAssetHandler();

    var harmony = new Harmony(this.ModManifest.UniqueID);

    MachineHarmonyPatcher.ApplyPatches(harmony);
    SmokedItemHarmonyPatcher.ApplyPatches(harmony);
    CraftingHarmonyPatcher.ApplyPatches(harmony);
    TooltipPatcher.ApplyPatches(harmony);

    extraOutputAssetHandler.RegisterEvents(Helper);
    extraCraftingConfigAssetHandler.RegisterEvents(Helper);
    extraMachineDataAssetHandler.RegisterEvents(Helper);
    outputRulesGlobalModifiersAssetHandler.RegisterEvents(Helper);

    Helper.Events.GameLoop.DayStarted += OnDayStartedJunimoHut;
    Helper.Events.GameLoop.GameLaunched += OnGameLaunchedBetterCrafting;
    Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    Helper.Events.Content.AssetRequested += OnAssetRequested;

    // Register item query
    ItemQueryResolver.Register($"{UniqueId}_FLAVORED_ITEM", flavoredItemQuery);
    // Register triggers
    TriggerActionManager.RegisterTrigger($"{UniqueId}_BuffRemoved");
    TriggerActionManager.RegisterAction($"{UniqueId}_AddItemQuery", addItemQueryAction);
    // Register game state queries
    // See MachineHarmonyPatcher for where the trigger is being raised
    GameStateQuery.Register($"{UniqueId}_BUFF_NAME", BUFF_NAME);
    GameStateQuery.Register($"{UniqueId}_BUFF_ID", BUFF_ID);

    try {
      if (Helper.ModRegistry.IsLoaded("Pathoschild.Automate")) {
        this.Monitor.Log("This mod patches Automate. If you notice issues with Automate, make sure it happens without this mod before reporting it to the Automate page.", LogLevel.Debug);
        AutomatePatcher.ApplyPatches(harmony);
      }
    }
    catch (Exception e) {
      Monitor.Log("Failed patching Automate. Detail: " + e.Message, LogLevel.Error);
    }

    try {
      if (Helper.ModRegistry.IsLoaded("moonslime.CookingSkill")) {
        this.Monitor.Log("This mod patches YetAnotherCookingSkill. If you notice issues with YACS, make sure it happens without this mod before reporting it to the YACS page.", LogLevel.Debug);
        YACSPatcher.ApplyPatches(harmony);
      }
    }
    catch (Exception e) {
      Monitor.Log("Failed patching YACS. Detail: " + e.Message, LogLevel.Error);
    }
  }

  // If a junimo hut has custom loved items, feed them
  public void OnDayStartedJunimoHut(object? sender, DayStartedEventArgs e) {
    foreach (var location in Game1.locations) {
      foreach (var building in location.buildings) {
        if (building is JunimoHut hut &&
            hut.raisinDays.Value == 0 &&
            !Game1.IsWinter) {
          Chest outputChest = hut.GetOutputChest();
          if (Utils.getItemCountInListByTags(outputChest.Items, JunimoLovedItemContextTag) > 0) {
            hut.raisinDays.Value += 7;
            Utils.RemoveItemFromInventoryByTags(outputChest.Items, JunimoLovedItemContextTag, 1, /*probe*/false);
          }
        }
      }
    }
  }

  static string GetMultiplierBuffString(float value, string key) {
    return Game1.content.LoadString(key, $"+{value * 100}%");
  }

  static string GetBuffString(float value, string key) {
    return Game1.content.LoadString(key, $"+{value}");
  }

  public void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    BuffsDisplay.displayAttributes.AddRange(new List<BuffAttributeDisplay> {
            new BuffAttributeDisplay(() => Game1.buffsIcons, 20, (Buff buff) => buff.effects.CombatLevel.Value, (float value) => GetBuffString(value, "Strings/UI:ItemHover_Buff3")),
            new BuffAttributeDisplay(BuffsIcons, 0, (Buff buff) => buff.effects.AttackMultiplier.Value, (float value) => GetMultiplierBuffString(value, $"{BuffStringsAssetName}:ItemHoverBuff0")),
            new BuffAttributeDisplay(BuffsIcons, 1, (Buff buff) => buff.effects.Immunity.Value, (float value) => GetBuffString(value, $"{BuffStringsAssetName}:ItemHoverBuff1")),
            new BuffAttributeDisplay(BuffsIcons, 2, (Buff buff) => buff.effects.KnockbackMultiplier.Value, (float value) => GetMultiplierBuffString(value, $"{BuffStringsAssetName}:ItemHoverBuff2")),
            new BuffAttributeDisplay(BuffsIcons, 3, (Buff buff) => buff.effects.WeaponSpeedMultiplier.Value, (float value) => GetMultiplierBuffString(value, $"{BuffStringsAssetName}:ItemHoverBuff3")),
            new BuffAttributeDisplay(BuffsIcons, 4, (Buff buff) => buff.effects.CriticalChanceMultiplier.Value, (float value) => GetMultiplierBuffString(value, $"{BuffStringsAssetName}:ItemHoverBuff4")),
            new BuffAttributeDisplay(BuffsIcons, 5, (Buff buff) => buff.effects.CriticalPowerMultiplier.Value, (float value) => GetMultiplierBuffString(value, $"{BuffStringsAssetName}:ItemHoverBuff5")),
            new BuffAttributeDisplay(BuffsIcons, 6, (Buff buff) => buff.effects.WeaponPrecisionMultiplier.Value, (float value) => GetMultiplierBuffString(value, $"{BuffStringsAssetName}:ItemHoverBuff6")),
    });
  }

  public void OnGameLaunchedBetterCrafting(object? sender, GameLaunchedEventArgs e) {
    try {
      IBetterCrafting? bcApi = Helper.ModRegistry.GetApi<IBetterCrafting>("leclair.bettercrafting");
      if (bcApi != null) {
        bcApi.PostCraft += OnPostCraft;
      }
    }
    catch (Exception exception) {
      ModEntry.StaticMonitor.Log(exception.Message, LogLevel.Error);
    }
    try {
      ICookingSkillAPI? locApi = Helper.ModRegistry.GetApi<ICookingSkillAPI>("blueberry.LoveOfCooking");
      if (locApi != null) {
        locApi.PostCook += OnPostCook;
      }
    }
    catch (Exception exception) {
      ModEntry.StaticMonitor.Log(exception.Message, LogLevel.Error);
    }
  }

  public void OnPostCraft(IPostCraftEvent e) {
    var recipe = e.Recipe;
    if (e.Item is not null && ModEntry.extraCraftingConfigAssetHandler.data.TryGetValue(recipe.Name, out var craftingConfig)) {
      e.Item = Utils.applyCraftingChanges(e.Item, e.ConsumedItems, craftingConfig);
    }
  }

  public void OnPostCook(IPostCookEvent e) {
    var recipe = e.Recipe;
    if (ModEntry.extraCraftingConfigAssetHandler.data.TryGetValue(recipe.name, out var craftingConfig)) {
      var consumedItemsLists = new Stack<IList<Item>>(e.ConsumedItems);
      var cookedItems = new Stack<SObject>(e.CookedItems);
      var newCookedItems = new List<SObject>();
      // Pop one item at a time from the cook list, process it, and pop them back
      while (cookedItems.TryPop(out var originalCookedItem)) {
        while (originalCookedItem.Stack > 0) {
          var cookedItem = originalCookedItem.getOne();
          cookedItem.Stack = recipe.numberProducedPerCraft;
          originalCookedItem.Stack -= recipe.numberProducedPerCraft;
          newCookedItems.Add((Utils.applyCraftingChanges(cookedItem, consumedItemsLists.TryPop(out var consumedItems) ? consumedItems : new List<Item>(), craftingConfig) as SObject) ?? new SObject("0", 1));
        }
      }
      e.CookedItems = newCookedItems;
    }
  }

  public override object GetApi() {
    return ModApi;
  }

  public static IEnumerable<ItemQueryResult> flavoredItemQuery(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError) {
    string[] array = ItemQueryResolver.Helpers.SplitArguments(arguments);
    if (array.Length < 2) {
      return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "expected at least two arguments in the form <item ID> <flavor ID> [optional price override]");
    }
    string itemId = array[0];
    string flavorId = array[1];
    Item flavorItem = ItemRegistry.Create(flavorId, allowNull: true);
    SObject? flavorObj = flavorItem as SObject;
    Color color = TailoringMenu.GetDyeColor(flavorItem) ?? Color.White;
    ColoredObject outputObj = new ColoredObject(itemId, 1, color);
    outputObj.Name += " " + itemId;
    outputObj.preservedParentSheetIndex.Value = flavorObj?.ItemId ?? (flavorId == "-1" ? flavorId : null);
    var overridePrice = ArgUtility.GetInt(array, 2, -1);
    outputObj.Price = overridePrice > 0 ? overridePrice : flavorObj?.Price ?? outputObj.Price;
    outputObj.Price = (int)(outputObj.Price * ArgUtility.GetFloat(array, 3, 1));
    outputObj.Price += ArgUtility.GetInt(array, 4, 0);

    return new ItemQueryResult[1]
    {
      new ItemQueryResult(outputObj)
    };

  }

  public static bool addItemQueryAction(string[] args, TriggerActionContext context, out string? error) {
    if (args.Length <= 1) {
      error = "No item query IDs provided!";
      return false;
    }
    error = null;
    foreach (var itemQueryId in args.Skip(1)) {
      if (extraOutputAssetHandler.data.TryGetValue(itemQueryId, out var itemQuery)) {
        Item item = ItemQueryResolver.TryResolveRandomItem(itemQuery, new ItemQueryContext());
        if (item != null) {
          Game1.player.addItemByMenuIfNecessary(item);
          error = null;
          return true;
        }
      } else {
        error = $"Warning: Item Query ID {itemQueryId} not defined in asset!";
      }
    }
    return true;
  }

  public void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects")) {
      e.Edit(asset => {
        var data = asset.AsDictionary<string, ObjectData>().Data;
        data[MachineHarmonyPatcher.HolderId] = new ObjectData {
          DisplayName = ModEntry.Helper.Translation.Get("HolderName"),
          Description = ModEntry.Helper.Translation.Get("HolderDescription"),
          Type = "Basic",
        };
      });
    }
    if (e.NameWithoutLocale.IsEquivalentTo(BuffStringsAssetName)) {
      e.LoadFrom(() =>
        new Dictionary<string, string> {
          // Item tooltip display
          ["ItemHoverBuff0"] = ModEntry.Helper.Translation.Get("AttackMultiplierBuff"),
          ["ItemHoverBuff1"] = ModEntry.Helper.Translation.Get("ImmunityBuff"),
          ["ItemHoverBuff2"] = ModEntry.Helper.Translation.Get("KnockbackMultiplierBuff"),
          ["ItemHoverBuff3"] = ModEntry.Helper.Translation.Get("WeaponSpeedMultiplierBuff"),
          ["ItemHoverBuff4"] = ModEntry.Helper.Translation.Get("CriticalChanceMultiplierBuff"),
          ["ItemHoverBuff5"] = ModEntry.Helper.Translation.Get("CriticalPowerMultiplierBuff"),
          ["ItemHoverBuff6"] = ModEntry.Helper.Translation.Get("WeaponPrecisionMultiplierBuff"),
        }, AssetLoadPriority.Medium);
    }
    if (e.NameWithoutLocale.IsEquivalentTo(BuffsIconsAssetName)) {
      e.LoadFromModFile<Texture2D>("assets/BuffsIcons.png", AssetLoadPriority.Medium);
    }

    // Duplicate machine rules
    // The extra machine data asset is also duplicated in ExtraOutputAssetHandler
    if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines")) {
      e.Edit(asset => {
        var data = asset.AsDictionary<string, MachineData>().Data;
        foreach (var (key, value) in data) {
          if (value.CustomFields?.TryGetValue(CopyMachineRulesFromKey, out var copyKey) ?? false) {
            if (data.ContainsKey(copyKey)) {
              data[key] = data[copyKey].DeepClone();
              if (outputRulesGlobalModifiersAssetHandler.data.TryGetValue(key, out var globalModifiersData)) {
                if (globalModifiersData.GlobalCopyQuality
                    || globalModifiersData.GlobalQualityModifiers is not null
                    || globalModifiersData.GlobalStackModifiers is not null) {
                  foreach (var outputRule in data[key].OutputRules ?? []) {
                    foreach (var outputItem in outputRule.OutputItem ?? []) {
                      if (globalModifiersData.GlobalCopyQuality) {
                        outputItem.CopyQuality = true;
                      }
                      if (globalModifiersData.GlobalQualityModifiers is not null) {
                        outputItem.QualityModifiers ??= [];
                        outputItem.QualityModifiers.AddRange(globalModifiersData.GlobalQualityModifiers.Select(m => m.DeepClone()));
                      }
                      if (globalModifiersData.GlobalStackModifiers is not null) {
                        outputItem.StackModifiers ??= [];
                        outputItem.StackModifiers.AddRange(globalModifiersData.GlobalStackModifiers.Select(m => m.DeepClone()));
                      }
                    }
                  }
                }
              }
            } else {
              StaticMonitor.Log($"Error while handling CopyMachineRulesFrom for {key}: {copyKey} does not exist in machine data", LogLevel.Error);
            }
          }
        }
      }, AssetEditPriority.Late + 10);
    }
  }

  public static bool BUFF_ID(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGet(query, 1, out var buffIdToCheck, out var error)) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    if (context.TargetItem is null ||
        !context.TargetItem.modData.TryGetValue($"{UniqueId}_BuffId", out var buffId)) {
      return GameStateQuery.Helpers.ErrorResult(query, "no target item found - called outside BuffRemoved triggers?");
    }
    return buffId == buffIdToCheck;
  }

  public static bool BUFF_NAME(string[] query, GameStateQueryContext context) {
    if (!ArgUtility.TryGet(query, 1, out var buffNameToCheck, out var error)) {
      return GameStateQuery.Helpers.ErrorResult(query, error);
    }
    if (context.TargetItem is null ||
        !context.TargetItem.modData.TryGetValue($"{UniqueId}_BuffName", out var buffName)) {
      return GameStateQuery.Helpers.ErrorResult(query, "no target item found - called outside BuffRemoved triggers?");
    }
    return buffName == buffNameToCheck;
  }
}

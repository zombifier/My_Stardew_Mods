#nullable enable
using HarmonyLib;
using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Buildings;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Leclair.Stardew.BetterCrafting;

namespace Selph.StardewMods.ExtraMachineConfig; 

using SObject = StardewValley.Object;

internal sealed class ModEntry : Mod {
  internal new static IModHelper Helper { get;
    set;
  }

  internal static IMonitor StaticMonitor { get; set; }
  internal static IExtraMachineConfigApi ModApi;
  internal static ExtraOutputAssetHandler extraOutputAssetHandler;
  internal static ExtraCraftingConfigAssetHandler extraCraftingConfigAssetHandler;
  internal static string UniqueId;

  internal static string JunimoLovedItemContextTag = "junimo_loved_item";

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    ModApi = new ExtraMachineConfigApi();
    UniqueId = this.ModManifest.UniqueID;

    extraOutputAssetHandler = new ExtraOutputAssetHandler();
    extraCraftingConfigAssetHandler = new ExtraCraftingConfigAssetHandler();

    var harmony = new Harmony(this.ModManifest.UniqueID);

    MachineHarmonyPatcher.ApplyPatches(harmony);
    SmokedItemHarmonyPatcher.ApplyPatches(harmony);
    CraftingHarmonyPatcher.ApplyPatches(harmony);

    extraOutputAssetHandler.RegisterEvents(Helper);
    extraCraftingConfigAssetHandler.RegisterEvents(Helper);
    Helper.Events.GameLoop.DayStarted += OnDayStartedJunimoHut;
    Helper.Events.GameLoop.GameLaunched += OnGameLaunchedBetterCrafting;

    // Register item query
    ItemQueryResolver.Register($"{UniqueId}_FLAVORED_ITEM", flavoredItemQuery);

    try {
      if (Helper.ModRegistry.IsLoaded("Pathoschild.Automate")) {
        this.Monitor.Log("This mod patches Automate. If you notice issues with Automate, make sure it happens without this mod before reporting it to the Automate page.", LogLevel.Debug);
        AutomatePatcher.ApplyPatches(harmony);
      }
    } catch (Exception e) {
      Monitor.Log("Failed patching Automate. Detail: " + e.Message, LogLevel.Error);
    }

    try {
      if (Helper.ModRegistry.IsLoaded("moonslime.CookingSkill")) {
        this.Monitor.Log("This mod patches YetAnotherCookingSkill. If you notice issues with YACS, make sure it happens without this mod before reporting it to the YACS page.", LogLevel.Debug);
        YACSPatcher.ApplyPatches(harmony);
      }
    } catch (Exception e) {
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

  public void OnGameLaunchedBetterCrafting(object? sender, GameLaunchedEventArgs e) {
    try {
      IBetterCrafting? bcApi = Helper.ModRegistry.GetApi<IBetterCrafting>("leclair.bettercrafting");
      if (bcApi != null) {
        bcApi.PostCraft += OnPostCraft;
      }
    } catch (Exception exception) {
      ModEntry.StaticMonitor.Log(exception.Message, LogLevel.Error);
    }
  }

  public void OnPostCraft(IPostCraftEvent e) {
    var recipe = e.Recipe;
    if (e.Item is not null && ModEntry.extraCraftingConfigAssetHandler.data.TryGetValue(recipe.Name, out var craftingConfig)) {
      e.Item = Utils.applyCraftingChanges(e.Item, e.ConsumedItems, craftingConfig);
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
    outputObj.Price = ArgUtility.GetInt(array, 2, flavorObj?.Price ?? outputObj.Price);

    return new ItemQueryResult[1]
    {
      new ItemQueryResult(outputObj)
    };

  }
}

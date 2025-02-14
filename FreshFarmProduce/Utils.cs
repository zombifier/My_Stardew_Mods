using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.Objects;
using StardewValley.Delegates;
using StardewValley.SpecialOrders;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.FreshFarmProduce;

static class Utils {
  // modData key for a non-fresh item
  public static string NotFreshKey { get => $"{ModEntry.UniqueId}.NotFresh"; }
  // Fresh categories
  static int[] freshCategories = [
      StardewValley.Object.FishCategory,
      StardewValley.Object.EggCategory,
      StardewValley.Object.MilkCategory,
      StardewValley.Object.meatCategory,
      //            StardewValley.Object.syrupCategory,
      StardewValley.Object.VegetableCategory,
      StardewValley.Object.FruitsCategory,
      StardewValley.Object.flowersCategory,
      StardewValley.Object.GreensCategory,
    ];

  public static string FreshContextTag = "fresh_item";

  static bool IsSpoilable(Item? item) {
    if (item is null) return false;
    bool hasSpoilableTag = 
        ItemContextTagManager.DoAnyTagsMatch(
          ModEntry.competitionDataAssetHandler.data.SpoilableContextTags,
          ItemContextTagManager.GetBaseContextTags(item.QualifiedItemId));
    bool hasNonSpoilableTag = 
        ItemContextTagManager.DoAnyTagsMatch(
          ModEntry.competitionDataAssetHandler.data.NonSpoilableContextTags,
          ItemContextTagManager.GetBaseContextTags(item.QualifiedItemId));
    return (freshCategories.Contains(item.Category) && !hasNonSpoilableTag) || hasSpoilableTag;
  }

  public static bool IsFreshItem(Item? item) {
    if (item is null) return false;
    return (!item.modData.ContainsKey(NotFreshKey) || ModEntry.Config.DisableStaleness) && IsSpoilable(item);
  }

  public static bool IsStaleItem(Item? item) {
    if (item is null) return true;
    return (item.modData.ContainsKey(NotFreshKey) && !ModEntry.Config.DisableStaleness) && IsSpoilable(item);
  }

  // Returns true if an item is spoiled
  public static bool SpoilItem(Item? item) {
    if (item is not null && IsFreshItem(item)) {
      item.modData[NotFreshKey] = "true";
      item.MarkContextTagsDirty();
      item.modData.Remove(CachedDescriptionKey);
      return true;
    }
    return false;
  }

  public static void SpoilItemInChest(Chest chest) {
    bool itemSpoiled = false;
    if (chest.GlobalInventoryId is not null) {
      var items = Game1.player.team.GetOrCreateGlobalInventory(chest.GlobalInventoryId);
      foreach (Item item in items) {
        if (item != null && Utils.SpoilItem(item)) {
          itemSpoiled = true;
        }
      }
      if (itemSpoiled) {
        Utility.consolidateStacks(items);
      }
      return;
    }
    chest.ForEachItem((in ForEachItemContext context) => {
      if (Utils.SpoilItem(context.Item)) {
        itemSpoiled = true;
      }
      return true;
    }, null);
    if (itemSpoiled) {
      Utility.consolidateStacks(chest.Items);
    }
  }

  public static bool IsJojaMealItem(Item item) {
    return item.modData.ContainsKey(JojaDashTerminalModel.JojaMealKey);
  }

  public static string CachedDescriptionKey = $"{ModEntry.UniqueId}.CachedDescription";

  public static void ApplyDescription(SObject obj, ref string result) {
    int width = 0;
    try {
      width = ModEntry.Helper.Reflection.GetMethod(obj, "getDescriptionWidth").Invoke<int>();
    } catch (Exception e) {
      ModEntry.StaticMonitor.Log($"Error reflecting into getDescription: {e.Message}");
      // Stop doing it lol
      obj.modData[CachedDescriptionKey] = "";
      return;
    }
    string extraDescription = "";
    if (obj.modData.TryGetValue("CachedDescriptionKey", out string? cachedDescription)) {
      if (String.IsNullOrEmpty(cachedDescription)) return;
      extraDescription = cachedDescription;
    } else {
      var specialOrder = Game1.player.team.specialOrders
        .FirstOrDefault((SpecialOrder? order) => order?.questKey.Value == ModEntry.FarmCompetitionSpecialOrderId, null);
      if (specialOrder is null) return;
      List<string> categoryStrings = new();
      foreach (var objective in specialOrder.objectives) {
        if (objective is ShipPointsObjective shipPointsObjective &&
            ModEntry.competitionDataAssetHandler.data.Categories.TryGetValue(shipPointsObjective.Id.Value, out var categoryData) &&
            shipPointsObjective.CanAcceptThisItem(obj, Game1.player)) {
          categoryStrings.Add(shipPointsObjective.useShipmentValue.Value ?
              ModEntry.Helper.Translation.Get("CategoryDescription.tooltipNoPoints", new { categoryName = categoryData.Name }) :
              ModEntry.Helper.Translation.Get("CategoryDescription.tooltip", new { categoryName = categoryData.Name, points = shipPointsObjective.CalculatePoints(obj) })
          );
        }
      }
      if (categoryStrings.Count > 0) {
        extraDescription =
          ModEntry.Helper.Translation.Get("CategoryDescription.header") +
          string.Join(ModEntry.Helper.Translation.Get("CategoryDescription.tooltipSeparator"), categoryStrings);
      }
    }
    if (String.IsNullOrEmpty(extraDescription)) return;
    obj.modData[CachedDescriptionKey] = extraDescription;
    result += "\n\n" + Game1.parseText(extraDescription, Game1.smallFont, width);
  }

  public static string FameModData = $"{ModEntry.UniqueId}.Fame";

  public static float GetFameSellPriceModifier() {
    return ModEntry.Config.EnableFamePriceIncrease ? Utility.Clamp(1f + GetFame() * 0.002f, 1f, 1.2f) : 1f;
  }

  public static float GetFameDifficultyModifier() {
    return ModEntry.Config.EnableFameDifficultyIncrease ? Utility.Clamp(1f + GetFame() * 0.01f, 1f, 1.5f) : 1f;
  }

  public static float GetRandomDifficultyModifier() {
    return ModEntry.Config.EnableDifficultyRandomization ? 0.5f + (float)Game1.random.NextDouble() : 1f;
  }

  public static int GetFame() {
    if (Game1.MasterPlayer.modData.TryGetValue(FameModData, out var str) &&
        Int32.TryParse(str, out int fame)) {
      return Math.Max(0, fame);
    }
    return 0;
  }

  public static void AddFame(int fameToAdd) {
    int fame = 0;
    if (Game1.MasterPlayer.modData.TryGetValue(FameModData, out var str) &&
        Int32.TryParse(str, out int parsedFame)) {
      fame = parsedFame;
    }
    fame += fameToAdd;
    if (fame < 0) {
      fame = 0;
    }
    Game1.MasterPlayer.modData[FameModData] = fame.ToString();
  }
}

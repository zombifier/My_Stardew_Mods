using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.Objects;
using StardewValley.Delegates;

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

  static string SpoilableContextTag = "spoilable_item";
  static string NonSpoilableContextTag = "non_spoilable_item";
  public static string FreshContextTag = "fresh_item";

  public static bool IsSpoilable(Item? item) {
    if (item is null) return false;
    return (freshCategories.Contains(item.Category) && !ItemContextTagManager.HasBaseTag(item.QualifiedItemId, NonSpoilableContextTag))
      || ItemContextTagManager.HasBaseTag(item.QualifiedItemId, SpoilableContextTag);
  }

  public static bool IsFreshItem(Item? item) {
    if (item is null) return false;
    return !item.modData.ContainsKey(NotFreshKey) && IsSpoilable(item);
  }

  public static bool IsStaleItem(Item? item) {
    if (item is null) return true;
    return item.modData.ContainsKey(NotFreshKey) && IsSpoilable(item);
  }

  // Returns true if an item is spoiled
  public static bool SpoilItem(Item? item) {
    if (item is not null && IsFreshItem(item)) {
      item.modData[NotFreshKey] = "true";
      item.MarkContextTagsDirty();
      return true;
    }
    return false;
  }

  public static void SpoilItemInChest(Chest chest) {
    bool itemSpoiled = false;
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
}

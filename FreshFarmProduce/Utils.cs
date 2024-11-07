using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;

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

  public static bool IsSpoilable(Item item) {
    return (freshCategories.Contains(item.Category) && !ItemContextTagManager.HasBaseTag(item.QualifiedItemId, NonSpoilableContextTag))
      || ItemContextTagManager.HasBaseTag(SpoilableContextTag, item.QualifiedItemId);
  }

  public static bool IsFreshItem(Item item) {
    return !item.modData.ContainsKey(NotFreshKey) && IsSpoilable(item);
  }

  public static bool IsStaleItem(Item item) {
    return item.modData.ContainsKey(NotFreshKey) && IsSpoilable(item);
  }

  public static void SpoilItem(Item item) {
    if (IsFreshItem(item)) {
      item.modData[NotFreshKey] = "true";
      item.MarkContextTagsDirty();
    }
  }
}

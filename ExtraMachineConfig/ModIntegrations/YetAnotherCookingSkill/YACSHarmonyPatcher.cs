using System;
using System.Linq;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Objects;
using HarmonyLib;
using StardewModdingAPI;

namespace Selph.StardewMods.ExtraMachineConfig;

using SObject = StardewValley.Object;

public class YACSPatcher {
  public static void ApplyPatches(Harmony harmony) {
    var eventsType = AccessTools.TypeByName("CookingSkillRedux.Core.Events");

    harmony.Patch(
        original: AccessTools.Method(eventsType, "PostCook"),
        prefix: new HarmonyMethod(typeof(YACSPatcher),
          nameof(YACSPatcher.PostCook_Prefix)));

  }

  static void PostCook_Prefix(ref Item __result, CraftingRecipe recipe, Item item, Dictionary<Item, int> consumed_items, Farmer who, bool betterCrafting = false) {
    // Does not work without better crafting
    if (!betterCrafting) {
      return;
    }

    try {
      if (item is not null && ModEntry.extraCraftingConfigAssetHandler.data.TryGetValue(recipe.name, out var craftingConfig)) {
        var newItem = Utils.applyCraftingChanges(item, consumed_items.Keys.ToList(), craftingConfig);
        var bcHeldItem = ModEntry.Helper.Reflection.GetField<Item>(
            AccessTools.TypeByName("CookingSkill.Utilities"),
            "BetterCraftingTempItem");
        bcHeldItem.SetValue(newItem);
      }
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log("YACS integration failed. Please report to ExtraMachineConfig's bug report page. Detail: " + e.Message, LogLevel.Warn);
      return;
    }
  }
}

using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Inventories;
using StardewValley.GameData.Machines;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace Selph.StardewMods.ExtraMachineConfig; 

using SObject = StardewValley.Object;

sealed class CraftingHarmonyPatcher {
  public static void ApplyPatches(Harmony harmony) {
    harmony.Patch(
        original: AccessTools.Method(typeof(CraftingPage), "clickCraftingRecipe"),
        transpiler: new HarmonyMethod(typeof(CraftingHarmonyPatcher), nameof(CraftingHarmonyPatcher.CraftingPage_clickCraftingRecipe_Transpiler)));
  }

  static IInventory cloneInventory(IInventory inventory) {
    IInventory result = new Inventory();
    foreach (Item item in inventory) {
      if (item is null) {
        result.Add(item);
      } else {
        var copy = item.getOne();
        copy.Stack = item.Stack;
        result.Add(copy);
      }
    }
    return result;
  }

  static Item ApplyChanges(CraftingRecipe craftingRecipe, Item item, List<IInventory?>? materialContainers) {
    if (!ModEntry.extraCraftingConfigAssetHandler.data.TryGetValue(craftingRecipe.name, out var craftingConfig)) {
      return item;
    }
    var oldPlayerInventory = cloneInventory(Game1.player.Items);
    try {
      // Get the ingredients that was used
      // Lord help me...
      var newMaterialContainers = materialContainers?.Select(inventory => inventory is not null ? cloneInventory(inventory) : null).ToList();

      craftingRecipe.consumeIngredients(newMaterialContainers);

      List<IInventory?> oldInventories = new();
      oldInventories.Add(oldPlayerInventory);
      if (materialContainers is not null) {
        oldInventories.AddRange(materialContainers);
      }

      List<IInventory?> newInventories = new();
      newInventories.Add(Game1.player.Items);
      if (newMaterialContainers is not null) {
        newInventories.AddRange(newMaterialContainers);
      }

      var ingredients = getUsedIngredients(oldInventories, newInventories);
      return Utils.applyCraftingChanges(item, ingredients, craftingConfig);
    } finally {
      Game1.player.Items.OverwriteWith(oldPlayerInventory);
    }
  }

  static List<Item> getUsedIngredients(List<IInventory?> oldInventories, List<IInventory?> newInventories) {
    List<Item> result = new();
    if (oldInventories.Count != newInventories.Count) {
      ModEntry.StaticMonitor.Log("Inventories count not matching?", LogLevel.Warn);
      return new();
    }
    for (int i = 0; i < oldInventories.Count; i++) {
      if (oldInventories[i] is null || newInventories[i] is null) {
        continue;
      }
      if (oldInventories[i]!.Count != newInventories[i]!.Count) {
        ModEntry.StaticMonitor.Log("Inventory size not matching?", LogLevel.Warn);
        return new();
      }
      for (int j = 0; j < oldInventories[i]!.Count; j++) {
        if (oldInventories[i]![j] == null) {
          continue;
        }
        if (newInventories[i]![j] == null || oldInventories[i]![j].Stack != newInventories[i]![j].Stack) {
          var itemToAdd = oldInventories[i]![j].getOne();
          itemToAdd.Stack = oldInventories[i]![j].Stack - (newInventories[i]![j]?.Stack ?? 0);
          result.Add(itemToAdd);
        }
      }
    }
    return result;
  }

  static IEnumerable<CodeInstruction> CraftingPage_clickCraftingRecipe_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    var createItemType = AccessTools.DeclaredMethod(typeof(CraftingRecipe), nameof(CraftingRecipe.createItem));
    // Matched code: Item item = craftingRecipe.createItem();
    // Inserted afterwards: item = CraftingHarmonyPatcher.ApplyChanges(craftingRecipe, item, this._materialContainers);
    matcher.MatchEndForward(
        new CodeMatch(OpCodes.Ldloc_0),
        new CodeMatch(OpCodes.Callvirt, createItemType),
        new CodeMatch(OpCodes.Stloc_1)
        )
      .ThrowIfNotMatch($"Could not find entry point for {nameof(CraftingPage_clickCraftingRecipe_Transpiler)}")
      .Advance(1)
      .InsertAndAdvance(
          new CodeInstruction(OpCodes.Ldloc_0),
          new CodeInstruction(OpCodes.Ldloc_1),
          new CodeInstruction(OpCodes.Ldarg_0),
          new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CraftingPage), nameof(CraftingPage._materialContainers))),
          new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CraftingHarmonyPatcher), nameof(CraftingHarmonyPatcher.ApplyChanges))),
          new CodeInstruction(OpCodes.Stloc_1)
          );
    return matcher.InstructionEnumeration();
  }
}

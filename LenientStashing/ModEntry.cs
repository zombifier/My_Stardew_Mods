using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Linq;
using System.Collections.Generic;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.LenientStashing;

internal sealed class ModEntry : Mod {
  static IMonitor StaticMonitor = null!;
  public override void Entry(IModHelper helper) {
    StaticMonitor = Monitor;
    var harmony = new Harmony(this.ModManifest.UniqueID);
    harmony.Patch(
        original: AccessTools.Method(typeof(ItemGrabMenu),
          nameof(ItemGrabMenu.FillOutStacks)),
        // High priority so other mods that process the inventory after the fact like Convenient Inventory can do their logic
        postfix: new HarmonyMethod(AccessTools.Method(typeof(ModEntry),
          nameof(ModEntry.ItemGrabMenu_FillOutStacks_Postfix)), Priority.First));
  }

  // Patch assumes all stackable items are already stacked.
  public static void ItemGrabMenu_FillOutStacks_Postfix(ItemGrabMenu __instance) {
    var farmerInventory = __instance.inventory.actualInventory;
    var chestInventory = __instance.ItemsToGrabMenu.actualInventory;
    var capacity = (__instance.sourceItem as Chest)?.GetActualCapacity() ?? __instance.ItemsToGrabMenu.capacity;
    if (chestInventory.Count >= capacity) {
      StaticMonitor.Log($"Chest full ({chestInventory.Count}/{capacity}), returning.", LogLevel.Info);
      return;
    }
    // TODO:
    // also add to existing stack in case the farmer has split stacks
    // call ondetachedfromparent
  	HashSet<int> chestSlotsToShake = new HashSet<int>();
    
    for (int i = 0; i < farmerInventory.Count; i++) {
      if (farmerInventory[i] is null) continue;
      bool done = false;
      bool shouldBeAdded = false;
      for (int j = 0; j < chestInventory.Count; j++) {
        if (chestInventory[j]?.canStackWith(farmerInventory[i]) ?? false) {
          shouldBeAdded = true;
          var remainingStack = chestInventory[j].addToStack(farmerInventory[i]);
          if (remainingStack <= 0) {
            var itemSprite = new ItemGrabMenu.TransferredItemSprite(farmerInventory[i].getOne(), __instance.inventory.inventory[i].bounds.X, __instance.inventory.inventory[i].bounds.Y);
            __instance._transferredItemSprites.Add(itemSprite);
            farmerInventory[i] = null;
            chestSlotsToShake.Add(j);
            done = true;
            break;
          } else {
            farmerInventory[i].Stack = remainingStack;
          }
        } else if (farmerInventory[i].QualifiedItemId == chestInventory[j]?.QualifiedItemId) {
          shouldBeAdded = true;
        }
      }
      if (done) continue;
      if (shouldBeAdded) {
        farmerInventory[i].onDetachedFromParent();
        chestInventory.Add(farmerInventory[i]);
        chestSlotsToShake.Add(chestInventory.Count - 1);
        var itemSprite = new ItemGrabMenu.TransferredItemSprite(farmerInventory[i].getOne(), __instance.inventory.inventory[i].bounds.X, __instance.inventory.inventory[i].bounds.Y);
				__instance._transferredItemSprites.Add(itemSprite);
        farmerInventory[i] = null;
        // Calculate capacity again since unlimited storage dynamically adjusts it to be "item count + 1"
        capacity = (__instance.sourceItem as Chest)?.GetActualCapacity() ?? __instance.ItemsToGrabMenu.capacity;
        if (chestInventory.Count >= capacity) {
          StaticMonitor.Log($"Chest full ({chestInventory.Count}/{capacity}), returning.", LogLevel.Info);
          break;
        }
      }
    }
    foreach (int slot in chestSlotsToShake) {
      __instance.ItemsToGrabMenu.ShakeItem(slot);
    }
  }
}

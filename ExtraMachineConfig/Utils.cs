using Microsoft.Xna.Framework;
using StardewModdingAPI;
using System;
using System.Linq;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Inventories;
using StardewValley.GameData.Machines;
using System.Collections.Generic;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ExtraMachineConfig; 

static class Utils {
  // Removes items with the specified ID from the inventory, and returns a clone of the removed item
  // If multiple item stacks are removed, return only the first one
  // This differs from ReduceId is that itemId can also be category IDs.
  public static Item? RemoveItemFromInventoryById(IInventory inventory, string itemId, int count, bool probe) {
    return RemoveItemFromInventory(inventory, item => CraftingRecipe.ItemMatchesForCrafting(item, itemId),
        count, probe);
  }

  // Removes items with the specified tags from the inventory.
  public static Item? RemoveItemFromInventoryByTags(IInventory inventory, string itemTags, int count, bool probe) {
    return RemoveItemFromInventory(inventory, item => ItemContextTagManager.DoesTagQueryMatch(itemTags, item.GetContextTags()),
        count, probe);
  }

  public static Item? RemoveItemFromInventory(IInventory inventory, Func<Item, bool> func, int count, bool probe) {
    Item? returnedItem = null;
    for (int index = 0; index < inventory.Count; ++index) {
      if (inventory[index] != null && func(inventory[index])) {
        if (inventory[index].Stack > count) {
          if (!probe) {
            inventory[index].Stack -= count;
          }
          returnedItem ??= inventory[index].getOne();
          return returnedItem;
        }
        if (!probe) {
          count -= inventory[index].Stack;
        }
        returnedItem ??= inventory[index].getOne();
        inventory[index] = null;
      }
      if (count <= 0) {
        return returnedItem;
      }
    }
    return null;
  }

  public static int getItemCountInListByTags(IList<Item> list, string itemTags) {
    int num = 0;
    for (int i = 0; i < list.Count; i++) {
      if (list[i] != null && ItemContextTagManager.DoesTagQueryMatch(itemTags, list[i].GetContextTags())) {
        num += list[i].Stack;
      }
    }
    return num;
  }

  public struct AdditionalFuelSettings {
    public string fuelEntryId;
    public string itemId;
    public int count;
    public float priceMultiplier;
  }

  public static IList<AdditionalFuelSettings> GetExtraRequirementsImpl(MachineItemOutput outputData, bool isContextTag) {
    IList<AdditionalFuelSettings> extraRequirements = new List<AdditionalFuelSettings>();
    if (outputData?.CustomData == null) {
      return extraRequirements;
    }
    foreach (var entry in outputData.CustomData) {
      var match = isContextTag
        ? MachineHarmonyPatcher.RequirementTagsKeyRegex.Match(entry.Key)
        : MachineHarmonyPatcher.RequirementIdKeyRegex.Match(entry.Key);
      if (match.Success) {
        string countKey = MachineHarmonyPatcher.RequirementCountKeyPrefix + "." + match.Groups[1].Value;
        int count = 1;
        if (outputData.CustomData.TryGetValue(countKey, out var countString) &&
            Int32.TryParse(countString, out int parsedCount)) {
          count = parsedCount;
        }

        string addPriceMultiplierKey = MachineHarmonyPatcher.RequirementAddPriceMultiplierKeyPrefix + "." + match.Groups[1].Value;
        float priceMultiplier = 0;
        if (outputData.CustomData.TryGetValue(addPriceMultiplierKey, out var addPriceMultiplierString) &&
            float.TryParse(addPriceMultiplierString, out float parsedAddPriceMultiplier)) {
          priceMultiplier = parsedAddPriceMultiplier;
        }
        extraRequirements.Add(new AdditionalFuelSettings(){fuelEntryId = match.Groups[1].Value, itemId = entry.Value, count = count, priceMultiplier = priceMultiplier});
      }
    }
    return extraRequirements;
  }

  public static string colorToString(Color color) {
    return $"{color.R},{color.G},{color.B}";
  }

  public static Color? stringToColor(string colorStr) {
    var colorValues = colorStr.Split(",").Select(str => Int32.TryParse(str, out var i) ? i : 0).ToList();
    if (colorValues.Count == 3) {
      return new Color(colorValues[0], colorValues[1], colorValues[2]);
    }
    return null;
  }

  // Returns the flavor, or null if not found.
  // 0 is the main flavor
  public static string? getPreserveId(Item item, int preserveIdIndex) {
    if (preserveIdIndex == 0 && item is SObject obj) {
      return obj.preservedParentSheetIndex.Value;
    } else if (item.modData.TryGetValue($"{MachineHarmonyPatcher.ExtraPreserveIdKeyPrefix}.{preserveIdIndex}", out var val)) {
      return val ?? "";
    }
    return null;
  }

  public static Item applyCraftingChanges(Item item, List<Item> ingredients, ExtraCraftingConfig craftingConfig) {
    SObject? obj = item as SObject;
    try {
      if (craftingConfig.IngredientConfigs is not null) {
        foreach (Item ingredient in ingredients) {
          var ingredientConfig = craftingConfig.IngredientConfigs.Find(config => 
              (config.ItemId is not null && CraftingRecipe.ItemMatchesForCrafting(ingredient, config.ItemId)) ||
              (config.ContextTags is not null && ItemContextTagManager.DoesTagQueryMatch(config.ContextTags, ingredient.GetContextTags()))
              );

          if (ingredientConfig is null) {
            continue;
          }

          // Copy color
          var colorIndex = ingredientConfig.OutputColor;
          if (colorIndex is not null) {
            ColoredObject newColoredObject;
            if (item is StardewValley.Objects.ColoredObject coloredObject) {
              newColoredObject = coloredObject;
            } else {
              newColoredObject = new StardewValley.Objects.ColoredObject(
                  item.ItemId,
                  item.Stack,
                  Color.White);
              ModEntry.Helper.Reflection.GetMethod(newColoredObject, "GetOneCopyFrom").Invoke(item);
              newColoredObject.Stack = item.Stack;
              //newColoredObject.heldObject.Value = item.heldObject.Value;
            }
            var color = TailoringMenu.GetDyeColor(ingredient);
            if (color != null) {
              if (colorIndex == 0) {
                newColoredObject.color.Value = (Color)color;
              } else {
                newColoredObject.modData[$"{MachineHarmonyPatcher.ExtraColorKeyPrefix}.{colorIndex}"] =
                  colorToString(color ?? Color.White);
              }
              item = newColoredObject;
              obj = newColoredObject;
            }
          }

          // Copy flavor
          var preserveIdIndex = ingredientConfig.OutputPreserveId;
          if (preserveIdIndex is not null && obj is not null) {
            var inputPreserveId = ingredientConfig.InputPreserveId switch {
              "DROP_IN_PRESERVE" => getPreserveId(ingredient, 0),
              "DROP_IN_ID" => ingredient.ItemId,
              _ => ingredientConfig.InputPreserveId,
            };
            if (preserveIdIndex == 0) {
              obj.preservedParentSheetIndex.Value = inputPreserveId;
            } else {
              item.modData[$"{MachineHarmonyPatcher.ExtraPreserveIdKeyPrefix}.{preserveIdIndex}"] =
                inputPreserveId;
            }
          }

          // Apply price modifiers
          var priceMultiplier = ingredientConfig.OutputPriceMultiplier ?? 0;
          if (priceMultiplier > 0 && obj is not null) {
            obj.Price += (int)(((ingredient as SObject)?.Price ?? 0) * priceMultiplier);
          }
        }

        // Apply misc changes
        if (craftingConfig.ObjectDisplayName is not null && obj is not null) {
          obj.displayNameFormat = craftingConfig.ObjectDisplayName;
        }
        if (craftingConfig.ObjectInternalName is not null && obj is not null) {
          obj.Name = craftingConfig.ObjectInternalName;
          for (int i = 0; ; i++) {
            var itemId = getPreserveId(obj, i);
            if (itemId is not null) {
              obj.Name = obj.Name.Replace($"PRESERVE_ID_{i}", itemId);
            } else {
              break;
            }
          }
        }

        return item;
      }
    } catch {
    }
    return item;
  }
}

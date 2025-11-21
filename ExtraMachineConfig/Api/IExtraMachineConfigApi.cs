using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Inventories;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Selph.StardewMods.ExtraMachineConfig;

public interface IExtraMachineConfigApi {
  // Get a list of extra fuels (by item ID) for this recipe (as tuples of item ID to count)
  IList<(string, int)> GetExtraRequirements(MachineItemOutput outputData);
  // Get a list of extra fuels (by tags) for this recipe (as tuples of comma-separated context tags to counts)
  // For historical reasons, both this function and the above function must be called to get a full list of extra fuels.
  IList<(string, int)> GetExtraTagsRequirements(MachineItemOutput outputData);
  // Get a list of extra output items (as a list of item spawn objects) that this recipe produces
  IList<MachineItemOutput> GetExtraOutputs(MachineItemOutput outputData, MachineData? machineData);
  // Get a list of actual fuel objects that will be consumed by this recipe if it's used with the
  // provided input item and inventory.
  // Returns null if the recipe doesn't have enough fuels.
  IList<Item>? GetFuelsForThisRecipe(MachineItemOutput outputData, Item inputItem, IInventory inventory);
  // Returns the override color for the provided UNqualified item ID.
  // If there are no overrides, returns null.
  public Color? GetColorOverride(string unqualifiedItemId);
}

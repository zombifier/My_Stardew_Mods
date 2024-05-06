using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Inventories;
using StardewValley.GameData.Machines;
using StardewValley.GameData.BigCraftables;
using StardewValley.TokenizableStrings;
using HarmonyLib;
using System.Collections.Generic;

namespace ExtraMachineConfig;

public class ExtraMachineConfigApi : IExtraMachineConfigApi {
  // Extract the additional fuel data from the output data as a list of fuel IDs to fuel count.
  public IList<(string, int)> GetExtraRequirements(MachineItemOutput outputData) {
    IList<(string, int)> extraRequirements = new List<(string, int)>();
    if (outputData?.CustomData == null) {
      return extraRequirements;
    }
    foreach (var entry in outputData.CustomData) {
      var match = ModEntry.RequirementIdKeyRegex.Match(entry.Key);
      if (!match.Success) {
        match = ModEntry.RequirementIdKeyRegex_Legacy.Match(entry.Key);
      }
      if (match.Success) {
        string countKey = ModEntry.RequirementCountKeyPrefix + "." + match.Groups[1].Value;
        string countKey_Legacy = ModEntry.RequirementCountKeyPrefix_Legacy + "." + match.Groups[1].Value;
        string countString;
        if ((outputData.CustomData.TryGetValue(countKey, out countString) ||
              outputData.CustomData.TryGetValue(countKey_Legacy, out countString)) &&
            Int32.TryParse(countString, out int count)) {
          extraRequirements.Add((entry.Value, count));
        } else {
          extraRequirements.Add((entry.Value, 1));
        }
      }
    }
    return extraRequirements;
  }

  // Same as above, but with item category tags instead of IDs
  public IList<(string, int)> GetExtraTagsRequirements(MachineItemOutput outputData) {
    IList<(string, int)> extraRequirements = new List<(string, int)>();
    if (outputData?.CustomData == null) {
      return extraRequirements;
    }
    foreach (var entry in outputData.CustomData) {
      var match = ModEntry.RequirementTagsKeyRegex.Match(entry.Key);
      if (match.Success) {
        string countKey = ModEntry.RequirementCountKeyPrefix + "." + match.Groups[1].Value;
        string countKey_Legacy = ModEntry.RequirementCountKeyPrefix_Legacy + "." + match.Groups[1].Value;
        string countString;
        if ((outputData.CustomData.TryGetValue(countKey, out countString) ||
              outputData.CustomData.TryGetValue(countKey_Legacy, out countString)) &&
            Int32.TryParse(countString, out int count)) {
          extraRequirements.Add((entry.Value, count));
        } else {
          extraRequirements.Add((entry.Value, 1));
        }
      }
    }
    return extraRequirements;
  }

}

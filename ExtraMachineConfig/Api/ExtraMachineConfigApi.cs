using System.Linq;
using StardewValley.GameData.Machines;
using System.Collections.Generic;

namespace Selph.StardewMods.ExtraMachineConfig;

public class ExtraMachineConfigApi : IExtraMachineConfigApi {
  // Extract the additional fuel data from the output data as a list of fuel IDs to fuel count.
  public IList<(string, int)> GetExtraRequirements(MachineItemOutput outputData) {
    return Utils.GetExtraRequirementsImpl(outputData, false).Select(additionalFuelSettings => (additionalFuelSettings.itemId, additionalFuelSettings.count)).ToList();
  }

  // Same as above, but with item category tags instead of IDs
  public IList<(string, int)> GetExtraTagsRequirements(MachineItemOutput outputData) {
    return Utils.GetExtraRequirementsImpl(outputData, true).Select(additionalFuelSettings => (additionalFuelSettings.itemId, additionalFuelSettings.count)).ToList();
  }

  public IList<MachineItemOutput> GetExtraOutputs(MachineItemOutput outputData) {
    IList<MachineItemOutput> extraOutputs = new List<MachineItemOutput>();
    if (outputData.CustomData != null &&
        outputData.CustomData.TryGetValue(MachineHarmonyPatcher.ExtraOutputIdsKey, out var extraOutputIds)) {
      foreach (var extraOutputId in extraOutputIds.Split(',', ' ')) {
        if (ModEntry.extraOutputAssetHandler.data.TryGetValue(extraOutputId, out var extraOutputData) &&
            // Disallow ExtraOutputIdsKey inside the extra rules to avoid recursion
            (!extraOutputData.CustomData?.ContainsKey(MachineHarmonyPatcher.ExtraOutputIdsKey) ?? true)) {
          extraOutputs.Add(extraOutputData);
        }
      }
    }
    return extraOutputs;
  }
}

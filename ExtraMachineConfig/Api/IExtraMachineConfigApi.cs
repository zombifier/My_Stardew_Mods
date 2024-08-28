using StardewValley.GameData.Machines;
using System.Collections.Generic;

namespace Selph.StardewMods.ExtraMachineConfig;

using SObject = StardewValley.Object;

public interface IExtraMachineConfigApi {
  IList<(string, int)> GetExtraRequirements(MachineItemOutput outputData);
  IList<(string, int)> GetExtraTagsRequirements(MachineItemOutput outputData);
  IList<MachineItemOutput> GetExtraOutputs(MachineItemOutput outputData, MachineData? machineData);
}

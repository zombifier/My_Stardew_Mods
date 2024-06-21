using StardewValley.GameData.Machines;
using Selph.StardewMods.Common;

namespace Selph.StardewMods.ExtraMachineConfig;

public sealed class ExtraOutputAssetHandler : DictAssetHandler<MachineItemOutput> {
  public ExtraOutputAssetHandler() : base($"{ModEntry.UniqueId}/ExtraOutputs", ModEntry.StaticMonitor) {}
}

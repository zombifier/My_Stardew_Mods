using StardewValley.GameData.Machines;
using SelphCommon;

namespace ExtraMachineConfig;

public sealed class ExtraOutputAssetHandler : DictAssetHandler<MachineItemOutput> {
  public ExtraOutputAssetHandler() : base($"{ModEntry.UniqueId}/ExtraOutputs", ModEntry.StaticMonitor) {}
}

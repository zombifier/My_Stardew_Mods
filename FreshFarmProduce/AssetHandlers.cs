namespace Selph.StardewMods.FreshFarmProduce;

using Selph.StardewMods.Common;

sealed class CompetitionDataAssetHandler : AssetHandler<CompetitionData> {
  public CompetitionDataAssetHandler() : base($"{ModEntry.UniqueId}/CompetitionData", ModEntry.StaticMonitor) {}
}

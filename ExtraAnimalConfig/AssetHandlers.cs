namespace Selph.StardewMods.ExtraAnimalConfig;

using Selph.StardewMods.Common;

public sealed class AnimalExtensionDataAssetHandler : DictAssetHandler<AnimalExtensionData> {
  public AnimalExtensionDataAssetHandler() : base($"{ModEntry.UniqueId}/AnimalExtensionData", ModEntry.StaticMonitor) {}
}

public sealed class EggExtensionDataAssetHandler : DictAssetHandler<EggExtensionData> {
  public EggExtensionDataAssetHandler() : base($"{ModEntry.UniqueId}/EggExtensionData", ModEntry.StaticMonitor) {}
}

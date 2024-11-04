using StardewValley;

namespace Selph.StardewMods.ExtraAnimalConfig;

using Selph.StardewMods.Common;

public sealed class AnimalExtensionDataAssetHandler : DictAssetHandler<AnimalExtensionData> {
  public AnimalExtensionDataAssetHandler() : base($"{ModEntry.UniqueId}/AnimalExtensionData", ModEntry.StaticMonitor, () => DataLoader.FarmAnimals(Game1.content).Keys) {}
}

public sealed class EggExtensionDataAssetHandler : DictAssetHandler<EggExtensionData> {
  public EggExtensionDataAssetHandler() : base($"{ModEntry.UniqueId}/EggExtensionData", ModEntry.StaticMonitor) {}
}

public sealed class GrassDropExtensionDataAssetHandler : DictAssetHandler<GrassDropExtensionData> {
  public GrassDropExtensionDataAssetHandler() : base($"{ModEntry.UniqueId}/GrassDropExtensionData", ModEntry.StaticMonitor) {}
}

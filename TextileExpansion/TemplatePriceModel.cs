using Selph.StardewMods.Common;

namespace Selph.StardewMods.TextileExpansion;

public class PriceMultiplierConfig {
  public float EmbroideryBaseItemMultiplier = 1f;
  public float EmbroideryAddedMultiplier = 1.5f;
  public float GemstoneBaseItemMultiplier = 1f;
  public float GemstoneAddedMultiplier = 2f;
}
public sealed class PriceMultiplierConfigAssetHandler : AssetHandler<PriceMultiplierConfig> {
  public PriceMultiplierConfigAssetHandler() : base($"{ModEntry.UniqueId}/PriceMultiplierConfig", ModEntry.StaticMonitor) { }
}

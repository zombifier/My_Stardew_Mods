using StardewValley.GameData.Machines;
using Selph.StardewMods.Common;
using System.Collections.Generic;

namespace Selph.StardewMods.ExtraMachineConfig;

public sealed class ExtraOutputAssetHandler : DictAssetHandler<MachineItemOutput> {
  public ExtraOutputAssetHandler() : base($"{ModEntry.UniqueId}/ExtraOutputs", ModEntry.StaticMonitor) {}
}

public class IngredientConfig {
  public string? Id;
  // Match the ingredient being consumed
  public string? ItemId;
  public string? ContextTags;

  public string? InputPreserveId;

  public int? OutputPreserveId;

  public int? OutputColor;
  public float? OutputPriceMultiplier;
}

public class ExtraCraftingConfig {
  public List<IngredientConfig>? IngredientConfigs;
  public string? ObjectDisplayName;
  public string? ObjectInternalName;
}

public sealed class ExtraCraftingConfigAssetHandler : DictAssetHandler<ExtraCraftingConfig> {
  public ExtraCraftingConfigAssetHandler() : base($"{ModEntry.UniqueId}/ExtraCraftingConfig", ModEntry.StaticMonitor) {}
}

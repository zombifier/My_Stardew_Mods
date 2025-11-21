using Force.DeepCloner;
using StardewValley.GameData.Machines;
using StardewValley.GameData;
using Selph.StardewMods.Common;
using System.Collections.Generic;
using StardewModdingAPI.Events;
using StardewValley;

namespace Selph.StardewMods.ExtraMachineConfig;

public sealed class ExtraOutputAssetHandler : DictAssetHandler<MachineItemOutput> {
  public ExtraOutputAssetHandler() : base($"{ModEntry.UniqueId}/ExtraOutputs", ModEntry.StaticMonitor) { }
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
  public ExtraCraftingConfigAssetHandler() : base($"{ModEntry.UniqueId}/ExtraCraftingConfig", ModEntry.StaticMonitor) { }
}

public class ExtraMachineData {
  public List<ReadyEffects> ReadyEffects = new();
}

public class ReadyEffects : MachineEffects {
  public int IncrementMachineParentSheetIndex = 0;
}

public sealed class ExtraMachineDataAssetHandler : DictAssetHandler<ExtraMachineData> {
  public ExtraMachineDataAssetHandler() : base($"{ModEntry.UniqueId}/ExtraMachineData", ModEntry.StaticMonitor) { }
  public override void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    base.OnAssetRequested(sender, e);
    if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath)) {
      e.Edit(asset => {
        var data = asset.AsDictionary<string, ExtraMachineData>().Data;
        foreach (var (key, value) in data) {
          if ((DataLoader.Machines(Game1.content).GetValueOrDefault(key)?.CustomFields?.TryGetValue(ModEntry.CopyMachineRulesFromKey, out var copyKey) ?? false)
              && (data.ContainsKey(copyKey))) {
            data[key] = data[copyKey].DeepClone();
          }
        }
      }, AssetEditPriority.Late + 10);
    }
  }
}

public class OutputRulesGlobalModifiers {
  public List<QuantityModifier>? GlobalStackModifiers;
  public List<QuantityModifier>? GlobalQualityModifiers;
  public bool GlobalCopyQuality = false;
}

public sealed class OutputRulesGlobalModifiersAssetHandler : DictAssetHandler<OutputRulesGlobalModifiers> {
  public OutputRulesGlobalModifiersAssetHandler() : base($"{ModEntry.UniqueId}/OutputRulesGlobalModifiers", ModEntry.StaticMonitor) { }
}

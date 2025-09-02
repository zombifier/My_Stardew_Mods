using HarmonyLib;
using StardewValley.GameData;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;
using System.Collections.Generic;
using Selph.StardewMods.Common;

namespace Selph.StardewMods.MachineTerrainFramework;

public class HarvestSpawnData : GenericSpawnItemDataWithCondition {
  public bool CopyColor = false;
  public bool OverrideQuality = false;
  public bool OverrideStack = false;
}

public class CropExtensionData {
  // Conditional modifiers to crop stuff
  public List<QuantityModifier>? GrowSpeedModifiers;
  public QuantityModifier.QuantityModifierMode GrowSpeedModifierMode;
  public List<QuantityModifier>? RegrowSpeedModifiers;
  public QuantityModifier.QuantityModifierMode RegrowSpeedModifierMode;
  public List<QuantityModifier>? CropQualityModifiers;
  public QuantityModifier.QuantityModifierMode CropQualityModifierMode;
  public List<QuantityModifier>? CropQuantityModifiers;
  public QuantityModifier.QuantityModifierMode CropQuantityModifierMode;
  public List<HarvestSpawnData>? MainDropOverride;
  public List<HarvestSpawnData>? ExtraDrops;
  public List<string>? PlantTriggers;
  public List<string>? DestroyedTriggers;
  public List<string>? DayStartTriggers;
}

public class CropExtensionDataAssetHandler : DictAssetHandler<CropExtensionData> {
  public CropExtensionDataAssetHandler() :
    base($"{ModEntry.UniqueId}/CropExtensionData", ModEntry.StaticMonitor, () => Game1.cropData.Keys.Concat(new[] { "Default", "Empty" })) { }
}

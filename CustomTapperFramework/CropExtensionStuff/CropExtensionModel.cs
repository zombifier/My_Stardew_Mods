using HarmonyLib;
using StardewValley.GameData;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;
using System.Collections.Generic;
using Selph.StardewMods.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Delegates;

namespace Selph.StardewMods.MachineTerrainFramework;

public class HarvestSpawnData : GenericSpawnItemDataWithCondition {
  public bool CopyColor = false;
  public bool OverrideQuality = false;
  public bool OverrideStack = false;
}

public class CropTextureOverride {
  public string OverrideGroupKey = "Default";
  public int? RequiredPhase;
  public string? RequiredTintColor;
  public string? RequiredCondition;
  public string? Texture;
  public List<int>? SpriteIndexList;
  public List<int>? ColoredSpriteIndexList;

  private Color? reqTintColor;
  internal bool Matches(Crop crop) {
    if (string.IsNullOrEmpty(Texture)) {
      return false;
    }
    if (!Game1.content.DoesAssetExist<Texture2D>(Texture)) {
      Texture = null;
      return false;
    }
    if (RequiredTintColor != null) {
      reqTintColor ??= Utility.StringToColor(RequiredTintColor);
      if (crop.tintColor.Value != reqTintColor) {
        return false;
      }
    }
    if (RequiredPhase != null && crop.currentPhase.Value != RequiredPhase) {
      return false;
    }
    if (RequiredCondition != null && !GameStateQuery.CheckConditions(RequiredCondition, location: crop.currentLocation)) {
      return false;
    }
    return true;
  }
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
  public List<string>? HarvestedTriggers;
  public List<int>? HarvestablePhases;
  public Dictionary<string, CropTextureOverride>? CropTextureOverrides;
}

public class CropExtensionDataAssetHandler : DictAssetHandler<CropExtensionData> {
  public CropExtensionDataAssetHandler() :
    base($"{ModEntry.UniqueId}/CropExtensionData", ModEntry.StaticMonitor, () => Game1.cropData.Keys.Concat(new[] { "Default", "Empty" })) { }
}

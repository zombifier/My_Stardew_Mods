using System;
using System.Collections.Generic;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Characters;
using StardewValley;
using StardewValley.Buildings;

namespace Selph.StardewMods.CustomBuilders;

// Key is (builder - building name - skin)
sealed class BuildingOverrideManager {
  public Dictionary<(string, string, string?), int?> BuildCostOverrides = new();
  public Dictionary<(string, string, string?), int?> BuildDaysOverrides = new();
  public Dictionary<(string, string, string?), List<BuildingMaterial>?> BuildMaterialsOverrides = new();
  public Dictionary<string, (int, int, int)?> ConstructAnimation = new();

  public int? GetBuildCostOverrideFor(string builder, BuildingData data, BuildingSkin? skin) {
    if (!BuildCostOverrides.ContainsKey((builder, data.Name, skin?.Name))) {
      var key = $"{ModEntry.UniqueId}_BuildCostFor_{builder}";
      if (skin?.Name is not null) key += $"_{skin.Name}";
      if ((data.CustomFields?.TryGetValue(key, out var value) ?? false) &&
          Int32.TryParse(value, out var buildCost)) {
        BuildCostOverrides[(builder, data.Name, skin?.Name)] = buildCost;
      } else {
        BuildCostOverrides[(builder, data.Name, skin?.Name)] = null;
      }
    }
    return BuildCostOverrides[(builder, data.Name, skin?.Name)];
  }

  public int? GetBuildDaysOverrideFor(string builder, BuildingData data, BuildingSkin? skin) {
    if (!BuildDaysOverrides.ContainsKey((builder, data.Name, skin?.Name))) {
      var key = $"{ModEntry.UniqueId}_BuildDaysFor_{builder}";
      if (skin?.Name is not null) key += $"_{skin.Name}";
      if ((data.CustomFields?.TryGetValue(key, out var value) ?? false) &&
          Int32.TryParse(value, out var buildDays)) {
        BuildDaysOverrides[(builder, data.Name, skin?.Name)] = buildDays;
      } else {
        BuildDaysOverrides[(builder, data.Name, skin?.Name)] = null;
      }
    }
    return BuildDaysOverrides[(builder, data.Name, skin?.Name)];
  }

  public List<BuildingMaterial>? GetBuildMaterialsOverrideFor(string builder, BuildingData data, BuildingSkin? skin) {
    if (!BuildMaterialsOverrides.ContainsKey((builder, data.Name, skin?.Name))) {
      var key = $"{ModEntry.UniqueId}_BuildMaterialsFor_{builder}";
      if (skin?.Name is not null) key += $"_{skin.Name}";
      if (data.CustomFields?.TryGetValue(key, out var value) ?? false) {
        string[] array = ArgUtility.SplitBySpace(value);
        List<BuildingMaterial> buildMaterials = new();
        for (int i = 0; i < array.Length; i += 2) {
          buildMaterials.Add(new BuildingMaterial {
              ItemId = array[i],
              Amount = ArgUtility.GetInt(array, i + 1, 1)
          });
        }
        BuildMaterialsOverrides[(builder, data.Name, skin?.Name)] = buildMaterials;
      } else {
        BuildMaterialsOverrides[(builder, data.Name, skin?.Name)] = null;
      }
    }
    return BuildMaterialsOverrides[(builder, data.Name, skin?.Name)];
  }

  public void Clear() {
    BuildCostOverrides.Clear();
    BuildDaysOverrides.Clear();
    BuildMaterialsOverrides.Clear();
  }

  public (int, int, int)? GetConstructAnimationIndices(string name) {
    if (!ConstructAnimation.ContainsKey(name)) {
      if (Game1.characterData.TryGetValue(name, out var data) &&
          data.CustomFields is not null &&
          data.CustomFields.TryGetValue($"{ModEntry.UniqueId}_ConstructAnimationIdleIndex1", out var str) &&
          Int32.TryParse(str, out var idleIndex1) &&
          data.CustomFields.TryGetValue($"{ModEntry.UniqueId}_ConstructAnimationIdleIndex2", out var str2) &&
          Int32.TryParse(str2, out var idleIndex2) &&
          data.CustomFields.TryGetValue($"{ModEntry.UniqueId}_ConstructAnimationHammerIndex", out var str3) &&
          Int32.TryParse(str3, out var hammerIndex)) {
        ConstructAnimation[name] = (idleIndex1, idleIndex2, hammerIndex);
      } else {
        ConstructAnimation[name] = null;
      }
    }
    return ConstructAnimation[name];
  }

  public void ClearNPC() {
    ConstructAnimation.Clear();
  }
}

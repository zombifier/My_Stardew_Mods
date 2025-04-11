using System.Collections.Generic;

namespace Selph.StardewMods.FreshFarmProduce;

public sealed class ModConfig {
  public const float DefaultEarlyFreshModifierRegular = 1.05f;
  public const float DefaultEarlyFreshModifierSilver = 1.1f;
  public const float DefaultEarlyFreshModifierGold = 1.15f;
  public const float DefaultEarlyFreshModifierIridium = 1.2f;
  public const float DefaultLateFreshModifierRegular = 1.25f;
  public const float DefaultLateFreshModifierSilver = 1.4f;
  public const float DefaultLateFreshModifierGold = 1.6666f;
  public const float DefaultLateFreshModifierIridium = 2f;
  // Fresh configs
  public bool DisableStaleness = false;
  public bool FreshDisplayName = true;
  public bool ShowCategoriesInDescription = true;
  public bool DisableFreshPriceIncrease = false;
  public int FreshDays = 1;
  public int FridgeFreshDays = 3;
  // Fame and competition configs
  public bool EnableCompetition = true;
  public bool EnableFamePriceIncrease = true;
  public bool EnableFameDifficultyIncrease = true;
  public bool EnableDifficultyRandomization = false;
  public bool EnableRandomPresets = false;
  public HashSet<string> DisabledPresets = new();
  public string ForcedPreset = "";
  // Modifier config
  public float EarlyFreshModifierRegular = DefaultEarlyFreshModifierRegular;
  public float EarlyFreshModifierSilver = DefaultEarlyFreshModifierSilver;
  public float EarlyFreshModifierGold = DefaultEarlyFreshModifierGold;
  public float EarlyFreshModifierIridium = DefaultEarlyFreshModifierIridium;
  public float LateFreshModifierRegular = DefaultLateFreshModifierRegular;
  public float LateFreshModifierSilver = DefaultLateFreshModifierSilver;
  public float LateFreshModifierGold = DefaultLateFreshModifierGold;
  public float LateFreshModifierIridium = DefaultLateFreshModifierIridium;
  // Other config
  public bool DisableFlash = false;
  public int SwagBagItemCount = 4;
}

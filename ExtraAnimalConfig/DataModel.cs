using StardewValley.GameData;
using System.Collections.Generic;

namespace Selph.StardewMods.ExtraAnimalConfig;

public class AnimalProduceExtensionData {
  public GenericSpawnItemDataWithCondition? ItemQuery;
  public List<GenericSpawnItemDataWithCondition> ItemQueries = [];
  public string? HarvestTool;
  public string? ProduceTexture;
  public Dictionary<string, string> SkinProduceTexture = new Dictionary<string, string>();
  public bool IgnoreAnimalQuality = false;
}

public class AnimalExtensionData {
  public float MalePercentage = -1;
  public Dictionary<string, AnimalProduceExtensionData> AnimalProduceExtensionData = new Dictionary<string, AnimalProduceExtensionData>();
  public string? FeedItemId;
  public bool OutsideForager = false;
  public List<AnimalSpawnData> AnimalSpawnList = [];
  public List<ExtraProduceSpawnData> ExtraProduceSpawnList = [];
  public List<string> ExtraHouses = [];
  public bool IgnoreRain = false;
  public string? IgnoreRainCondition;
  public bool IgnoreWinter = false;
  public string? IgnoreWinterCondition;
  public string? GlowColor = null;
  public float GlowRadius = 10f;
  public int? SpeedOverride = null;
  public List<AppearanceData> TextureOverrides = [];
  public string? GoOutsideCondition = null;
  // LMAO
  public bool IsAttackAnimal = false;
  public int AttackDamage = 1;
  public int AttackIntervalMs = 5000;
  public int AttackRange = 10;
  public int AttackMaxChaseTimeMs = 10000;
  public string? AttackCondition;
  // Harvest stuff
  public bool IsHarvester = false;
  public int HarvestInterval = 1000;
  public int HarvestRange = 5;
}

public class EggExtensionData {
  public List<AnimalSpawnData> AnimalSpawnList = new List<AnimalSpawnData>();
}

public class AnimalSpawnData {
  public string? Id;
  public string? AnimalId;
  public string? Condition;
}

public class ExtraProduceSpawnData {
  public string? Id;
  public int DaysToProduce = 1;
  public bool SyncWithMainProduce = false;
  public List<ProduceData> ProduceItemIds = [];
}

public class ProduceData {
  public string? Id;
  public string? ItemId;
  public string? Condition;
  public int MinimumFriendship = -1;
}

public class GrassDropExtensionData {
  public float BaseChance = 0;
  public bool EnterInventoryIfSilosFull = false;
}

public enum DefaultTextureEnum {
  Texture,
  HarvestedTexture,
  BabyTexture
}

public class AppearanceData {
  public string? Id;
  public string? Produce = null;
  public string? Skin;
  public string? Condition;
  public string? TextureToUse;
  public DefaultTextureEnum? DefaultTextureToUse;
}

public class ExtraHousesData {
  public string? Id;
  public string? AnimalHouse;
  public string? BuildingOccupantType;
}

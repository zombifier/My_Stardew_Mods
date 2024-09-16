using StardewValley.GameData;
using System.Collections.Generic;

namespace Selph.StardewMods.ExtraAnimalConfig;

public class AnimalProduceExtensionData {
  public GenericSpawnItemData? ItemQuery;
  public string? HarvestTool;
  public string? ProduceTexture;
  public Dictionary<string, string> SkinProduceTexture = new Dictionary<string, string>();
}

public class AnimalExtensionData {
  public float MalePercentage = -1;
  public Dictionary<string, AnimalProduceExtensionData> AnimalProduceExtensionData = new Dictionary<string, AnimalProduceExtensionData>();
  public string? FeedItemId;
  public bool OutsideForager = false;
  public List<AnimalSpawnData>? AnimalSpawnList;
  public List<ExtraProduceSpawnData>? ExtraProduceSpawnList;
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
  public List<ProduceData>? ProduceItemIds = null;
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

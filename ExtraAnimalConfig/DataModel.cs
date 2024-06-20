using StardewValley.GameData;
using System.Collections.Generic;

namespace ExtraAnimalConfig;

public class AnimalProduceExtensionData {
  public GenericSpawnItemData ItemQuery;
  public string HarvestTool;
  public string ProduceTexture;
  public Dictionary<string, string> SkinProduceTexture = new Dictionary<string, string>();
}

public class AnimalExtensionData {
  public float MalePercentage = -1;
  public Dictionary<string, AnimalProduceExtensionData> AnimalProduceExtensionData = new Dictionary<string, AnimalProduceExtensionData>();
  public string FeedItemId;
  public List<AnimalSpawnData> AnimalSpawnList = null;
}

public class EggExtensionData {
  public List<AnimalSpawnData> AnimalSpawnList = new List<AnimalSpawnData>();
}

public class AnimalSpawnData {
  public string Id;
  public string AnimalId;
  public string Condition;
}

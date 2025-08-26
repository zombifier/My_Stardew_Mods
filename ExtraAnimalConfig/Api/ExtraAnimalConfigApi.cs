using System;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using System.Collections.Generic;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ExtraAnimalConfig;

public class ExtraAnimalConfigApi : IExtraAnimalConfigApi {
  public List<GenericSpawnItemDataWithCondition> GetItemQueryOverrides(string animalType, string produceId) {
    var result = new List<GenericSpawnItemDataWithCondition>();
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animalType ?? "", out var animalExtensionData) &&
        animalExtensionData.AnimalProduceExtensionData.TryGetValue(ItemRegistry.QualifyItemId(produceId) ?? produceId, out var animalProduceExtensionData)) {
      if (animalProduceExtensionData.ItemQuery is not null) {
        result.Add(animalProduceExtensionData.ItemQuery);
      } else if (animalProduceExtensionData.ItemQueries is not null) {
        result.AddRange(animalProduceExtensionData.ItemQueries);
      }
    }
    return result;
  }

  public Dictionary<string, List<string>> GetExtraDrops(string animalType) {
    var result = new Dictionary<string, List<string>>();
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animalType ?? "", out var animalExtensionData)) {
      int i = 0;
      foreach (var entry in animalExtensionData.ExtraProduceSpawnList) {
        var list = new List<string>();
        result.Add(entry.Id ?? i.ToString(), list);
        foreach (var produceData in entry.ProduceItemIds) {
          list.Add(produceData.ItemId ?? "0");
        }
        i++;
      }
    }
    return result;
  }

  public event Action<IAnimalProduceCreatedEvent>? AnimalProduceCreated;

  internal void RunAnimalProduceCreatedEvents(FarmAnimal animal, ref SObject produce, ProduceMethod produceMethod, Tool? tool) {
    if (AnimalProduceCreated is null) return;
    var ev = new AnimalProduceCreatedEvent(animal, produce, produceMethod, tool);
    foreach (Action<IAnimalProduceCreatedEvent> del in AnimalProduceCreated.GetInvocationList()) {
      try {
        del.Invoke(ev);
      }
      catch (Exception e) {
        ModEntry.StaticMonitor.Log("Error processing AnimalProduceCreatedEvent: " + e.ToString(), LogLevel.Error);
      }
    }
    produce = ev.produce ?? produce;
    return;
  }

  public IDictionary<string, IFeedInfo> GetModdedFeedInfo() {
    Dictionary<string, IFeedInfo> feedInfo = new();
    HashSet<string> moddedFeedIds = new();
    foreach (var data in Game1.buildingData.Values) {
      moddedFeedIds.UnionWith(SiloUtils.GetModdedFeedFromCustomFields(data.CustomFields));
    }
    foreach (var feedId in moddedFeedIds) {
      feedInfo.Add(feedId, new FeedInfo(feedId));
    }
    return feedInfo;
  }
}

class AnimalProduceCreatedEvent : IAnimalProduceCreatedEvent {
  public FarmAnimal animal { get; }
  public SObject produce { get; set; }
  public ProduceMethod produceMethod { get; }
  public Tool? tool { get; }

  internal AnimalProduceCreatedEvent(FarmAnimal animal, SObject produce, ProduceMethod produceMethod, Tool? tool) {
    this.animal = animal;
    this.produce = produce;
    this.produceMethod = produceMethod;
    this.tool = tool;
  }
}

class FeedInfo : IFeedInfo {
  string qualifiedItemId;
  public FeedInfo(string qualifiedItemId) {
    this.qualifiedItemId = qualifiedItemId;
  }
  public int capacity {
    get {
      int result = 0;
      Utility.ForEachLocation((GameLocation location) => {
        result += SiloUtils.GetFeedCapacityFor(location, qualifiedItemId);
        return true;
      });
      return result;
    }
  }
  public int count {
    get {
      int result = 0;
      Utility.ForEachLocation((GameLocation location) => {
        result += SiloUtils.GetFeedCountFor(location, qualifiedItemId);
        return true;
      });
      return result;
    }
    set {
      var difference = value - count;
      if (difference > 0) {
        SiloUtils.StoreFeedInAnySilo(qualifiedItemId, difference);
      } else if (difference < 0) {
        SiloUtils.GetFeedFromAnySilo(qualifiedItemId, -difference);
      }
    }
  }
}

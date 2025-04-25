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

  public event Action<IAnimalProduceCreatedEvent>? AnimalProduceCreated;

  internal void RunAnimalProduceCreatedEvents(FarmAnimal animal, ref SObject produce, ProduceMethod produceMethod, Tool? tool) {
    if (AnimalProduceCreated is null) return;
    foreach (Action<IAnimalProduceCreatedEvent> del in AnimalProduceCreated.GetInvocationList()) {
      try {
        var ev = new AnimalProduceCreatedEvent(animal, produce, produceMethod, tool);
        del.Invoke(ev);
        produce = ev.produce ?? produce;
      } catch (Exception e) {
        ModEntry.StaticMonitor.Log("Error processing AnimalProduceCreatedEvent: " + e.ToString(), LogLevel.Error);
      }
    }
    return;
  }
}

class AnimalProduceCreatedEvent : IAnimalProduceCreatedEvent {
  public FarmAnimal animal {get; }
  public SObject produce {get; set; }
  public ProduceMethod produceMethod {get; }
  public Tool? tool {get; }

  internal AnimalProduceCreatedEvent(FarmAnimal animal, SObject produce, ProduceMethod produceMethod, Tool? tool) {
    this.animal = animal;
    this.produce = produce;
    this.produceMethod = produceMethod;
    this.tool = tool;
  }
}

using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.GameData;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ExtraAnimalConfig;

public interface IExtraAnimalConfigApi {
  // Get a list of item queries, in order, that can potentially replace the specified output of this animal. Returns an empty list if there are no overrides.
  // animalType: the animal type (ie. the key in Data/FarmAnimals)
  // produceId: the qualified or unqualified ID of the base produce (ie. the value in (Deluxe)ProduceItemIds)
  public List<GenericSpawnItemDataWithCondition> GetItemQueryOverrides(string animalType, string produceId);
  // Events that will fire when a produce is created. You can register an event here to listen for animal produce events, and potentially even change the output item.
  // WARNING: Because of a quirk in vanilla code, this will fire if an autograbber was unable to grab the produce from a tool harvested animal because it is full!
  public event Action<IAnimalProduceCreatedEvent>? AnimalProduceCreated;
}

// The harvest method associated with this animal. Note that (aside from method Tool and null tool) this is not an indicator of whether the produce was autograbbed.
public enum ProduceMethod {
  DropOvernight,
  Tool,
  DigUp,
  Debris
}

public interface IAnimalProduceCreatedEvent {
  public FarmAnimal animal {get; }
  // You can modify this field to change what gets returned.
  // Note that changing the stack count does not work - the stack will get reset to 1 (or 2 with Golden Animal Cracker).
  // To add extra stacks, add a copy to player inventory/world/autograbber/etc.
  public SObject produce {get; set; }
  public ProduceMethod produceMethod {get; }
  // If harvested with tool, the tool that harvested this animal. Will be null if autograbbed or not harvested with tool.
  public Tool? tool {get; }
}

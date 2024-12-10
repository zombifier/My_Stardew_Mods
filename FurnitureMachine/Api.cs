using StardewValley.Objects;

namespace Selph.StardewMods.FurnitureMachine;

public class FurnitureMachineApi : IFurnitureMachineApi {
  public bool IsFurnitureMachine(Furniture furniture) {
    return ModEntry.IsMachineFurniture(furniture);
  }
}

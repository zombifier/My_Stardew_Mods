using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using Pathoschild.Stardew.Automate;

namespace Selph.StardewMods.MachineTerrainFramework;

// Turns the tile of a resource clump with a tapper on it into connectors.
class ResourceClumpConnector : IAutomatable {
  private ResourceClump resourceClump;
  private Vector2 tile;

  public ResourceClumpConnector(ResourceClump rc, Vector2 t) {
    this.resourceClump = rc;
    this.tile = t;
  }

  public GameLocation Location {
    get {
      return resourceClump.Location;
    }
  }

  public Rectangle TileArea {
    get {
      return new Rectangle(
          (int)tile.X,
          (int)tile.Y,
          1, 1
      );
    }
  }
}

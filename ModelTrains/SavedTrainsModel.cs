using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Selph.StardewMods.ModelTrains;

public sealed class SavedLocomotive {
  public string LocomotiveItem;
  public List<SavedWagon> Wagons = new();
  public Vector2 Tile;
  public int FacingDirection;
  public SavedLocomotive(string locomotiveItem, Vector2 tile, int facingDirection) {
    this.LocomotiveItem = locomotiveItem;
    this.Tile = tile;
    this.FacingDirection = facingDirection;
  }
}

public sealed class SavedWagon {
  public string LocomotiveItem;
  public Vector2 Tile;
  public int FacingDirection;
  public SavedWagon(string locomotiveItem, Vector2 tile, int facingDirection) {
    this.LocomotiveItem = locomotiveItem;
    this.Tile = tile;
    this.FacingDirection = facingDirection;
  }
}

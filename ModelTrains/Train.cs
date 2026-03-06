using System;

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Selph.StardewMods.ModelTrains;

class Train : NPC {
  public override Rectangle GetBoundingBox() {
    return new Rectangle(
        (int)this.Tile.X * Game1.tileSize,
        (int)this.Tile.Y * Game1.tileSize,
        Game1.tileSize,
        Game1.tileSize);
  }
}

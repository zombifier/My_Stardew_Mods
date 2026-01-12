using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using Pathoschild.Stardew.Automate;

namespace Selph.StardewMods.ExtraAnimalConfig;

// Modded hoppers
class ModdedSiloMachine : IMachine {
  private Building? silo;
  private AnimalHouse? location;
  private Vector2? tile;

  public string MachineTypeID => $"{ModEntry.UniqueId}_ModdedSilo";

  public ModdedSiloMachine(Building silo) {
    this.silo = silo;
  }
  public ModdedSiloMachine(AnimalHouse location, Vector2 tile) {
    this.location = location;
    this.tile = tile;
  }

  private IList<string> GetModdedFeeds() {
    if (silo is not null) {
      var feedIds = SiloUtils.GetFeedForThisBuilding(silo);
      feedIds.Remove("(O)178");
      ModEntry.StaticMonitor.Log($"{feedIds}", LogLevel.Alert);
      return feedIds;
    }
    if (location is not null) {
      return AnimalUtils.GetAllCustomFeedForThisAnimalHouse(location).ToList();
    }
    return new List<string>();
  }

  public MachineState GetState() {
    foreach (var feedId in GetModdedFeeds()) {
      var feedInfo = ModEntry.ModApi.GetModdedFeedInfo(feedId);
      if (feedInfo.count < feedInfo.capacity) {
        ModEntry.StaticMonitor.Log($"Need {feedId}", LogLevel.Alert);
        return MachineState.Empty;
      }
    }
    ModEntry.StaticMonitor.Log($"Empty", LogLevel.Alert);
    return MachineState.Disabled;
  }

  public ITrackedStack? GetOutput() {
    return null;
  }

  public bool SetInput(IStorage input) {
    bool anyPulled = false;
    foreach (var feedId in GetModdedFeeds()) {
      ModEntry.StaticMonitor.Log($"Storing {feedId}", LogLevel.Alert);
      // try to add hay until full
      foreach (ITrackedStack stack in input.GetItems().Where(p => p.Sample.QualifiedItemId == feedId)) {
        int count = stack.Count;
        int remaining = SiloUtils.StoreFeedInAnySilo(feedId, stack.Count);
        stack.Reduce(count - remaining);
        if (remaining < count) {
          anyPulled = true;
        }
        if (count == remaining)
          break;
      }
    }
    return anyPulled;
  }

  public GameLocation Location {
    get {
      return silo?.GetParentLocation() ?? location as GameLocation ?? Game1.getFarm();
    }
  }

  public Rectangle TileArea {
    get {
      if (silo is not null) {
        return new Rectangle(
            silo.tileX.Value,
            silo.tileY.Value,
            silo.tilesWide.Value,
            silo.tilesHigh.Value
            );
      } else if (tile is not null) {
        return new Rectangle(
            (int)tile.Value.X,
            (int)tile.Value.Y,
            1, 1
            );
      } else {
        return new Rectangle();
      }
    }
  }
}

using Microsoft.Xna.Framework;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using System.Diagnostics.CodeAnalysis;

namespace Selph.StardewMods.CustomBuilders;

static class Utils {
  public static bool TileActionCommon(GameLocation location, string[] args, Farmer farmer, Point point, [NotNullWhen(true)] out string? npcId, out Rectangle? ownerSearchArea, bool skipOwnerPresenceCheck = false) {
    ownerSearchArea = null;
    if (!ArgUtility.TryGet(args, 1, out npcId, out var error) ||
        !ArgUtility.TryGetOptional(args, 2, out var direction, out error, null, allowBlank: true, "string direction") ||
        !ArgUtility.TryGetOptionalInt(args, 3, out var openTime, out error, -1, "int openTime") ||
        !ArgUtility.TryGetOptionalInt(args, 4, out var closeTime, out error, -1, "int closeTime") ||
        !ArgUtility.TryGetOptionalInt(args, 5, out var shopAreaX, out error, -1, "int shopAreaX") ||
        !ArgUtility.TryGetOptionalInt(args, 6, out var shopAreaY, out error, -1, "int shopAreaY") ||
        !ArgUtility.TryGetOptionalInt(args, 7, out var shopAreaWidth, out error, -1, "int shopAreaWidth") ||
        !ArgUtility.TryGetOptionalInt(args, 8, out var shopAreaHeight, out error, -1, "int shopAreaHeight")) {
      ModEntry.StaticMonitor.Log(error, LogLevel.Warn);
      return false;
    }

    // Check NPC is within area
    if (shopAreaX != -1 || shopAreaY != -1 || shopAreaWidth != -1 || shopAreaHeight != -1) {
      if (shopAreaX == -1 || shopAreaY == -1 || shopAreaWidth == -1 || shopAreaHeight == -1) {
        ModEntry.StaticMonitor.Log("when specifying any of the shop area 'x y width height' arguments (indexes 5-8), all four must be specified", LogLevel.Warn);
        return false;
      }
      ownerSearchArea = new(shopAreaX, shopAreaY, shopAreaWidth, shopAreaHeight);
      if (!skipOwnerPresenceCheck) {
        bool foundNpc = false;
        IList<NPC>? npcs = location.currentEvent?.actors;
        npcs ??= location.characters;
        foreach (var npc in npcs) {
          if (npc.Name == npcId && ownerSearchArea.Value.Contains(npc.TilePoint)) {
            foundNpc = true;
          }
        }
        if (!foundNpc) {
          ModEntry.StaticMonitor.Log($"{npcId} not found in area.");
          return false;
        }
      }
    }

    // Check direction
    switch (direction) {
      case "down":
        if (farmer.TilePoint.Y < point.Y) {
          ModEntry.StaticMonitor.Log($"player not down of {npcId}.");
          return false;
        }
        break;
      case "up":
        if (farmer.TilePoint.Y > point.Y) {
          ModEntry.StaticMonitor.Log($"player not up of {npcId}.");
          return false;
        }
        break;
      case "left":
        if (farmer.TilePoint.X > point.X) {
          ModEntry.StaticMonitor.Log($"player not left of {npcId}.");
          return false;
        }
        break;
      case "right":
        if (farmer.TilePoint.X < point.X) {
          ModEntry.StaticMonitor.Log($"player not right of {npcId}.");
          return false;
        }
        break;
    }

    // Check opening and closing times
    if ((openTime >= 0 && Game1.timeOfDay < openTime) || (closeTime >= 0 && Game1.timeOfDay >= closeTime)) {
      ModEntry.StaticMonitor.Log($"{npcId} is closed.");
      return false;
    }

    return true;
  }
}

using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.CrackerExtractor;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper;
  internal static IMonitor StaticMonitor;
  public static string UniqueId;

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;
    var harmony = new Harmony(ModEntry.UniqueId);
    HarmonyPatcher.ApplyPatches(harmony);
  }
}

using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;
using StardewValley.Delegates;
using StardewValley.Menus;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;

using BlueprintEntry = StardewValley.Menus.CarpenterMenu.BlueprintEntry;

namespace Selph.StardewMods.CustomBuilders;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;
  //internal static BuilderDataAssetHandler builderDataAssetHandler = null!;
  internal static BuildingOverrideManager buildingOverrideManager = null!;

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    // Carpenters stuff
    var harmony = new Harmony(this.ModManifest.UniqueID);
    Carpenters.RegisterEvents(helper);
    Carpenters.RegisterCustomTriggers();
    Carpenters.ApplyPatches(harmony);
    // Blacksmiths stuff
    //Blacksmiths.RegisterEvents(helper);
    //Blacksmiths.RegisterCustomTriggers();
    //Blacksmiths.ApplyPatches(harmony);
  }
}

using StardewModdingAPI;
using HarmonyLib;
using System;

namespace ExtraMachineConfig; 

internal sealed class ModEntry : Mod {
  internal new static IModHelper Helper { get;
    set;
  }

  internal static IMonitor StaticMonitor { get; set; }
  internal static IExtraMachineConfigApi ModApi;
  internal static ExtraOutputAssetHandler extraOutputAssetHandler;
  internal static string UniqueId;

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    ModApi = new ExtraMachineConfigApi();
    extraOutputAssetHandler = new ExtraOutputAssetHandler();

    UniqueId = this.ModManifest.UniqueID;

    var harmony = new Harmony(this.ModManifest.UniqueID);

    MachineHarmonyPatcher.ApplyPatches(harmony);
    SmokedItemHarmonyPatcher.ApplyPatches(harmony);
    AnimalDataPatcher.ApplyPatches(harmony);

    Helper.Events.Content.AssetRequested += extraOutputAssetHandler.OnAssetRequested;
    Helper.Events.Content.AssetReady += extraOutputAssetHandler.OnAssetReady;
    Helper.Events.Content.AssetsInvalidated += extraOutputAssetHandler.OnAssetsInvalidated;
    Helper.Events.GameLoop.DayStarted += AnimalDataPatcher.OnDayStartedJunimoHut;
    Helper.Events.GameLoop.GameLaunched += extraOutputAssetHandler.OnGameLaunched;

    try {
      if (Helper.ModRegistry.IsLoaded("Pathoschild.Automate")) {
        this.Monitor.Log("This mod patches Automate. If you notice issues with Automate, make sure it happens without this mod before reporting it to the Automate page.", LogLevel.Debug);
        AutomatePatcher.ApplyPatches(harmony);
      }
    } catch (Exception e) {
      Monitor.Log("Failed patching Automate. Detail: " + e.Message, LogLevel.Error);
    }
  }

  public override object GetApi() {
    return ModApi;
  }
}

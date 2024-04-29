
using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using HarmonyLib;
using StardewValley;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;

using SObject = StardewValley.Object;

namespace CustomTapperFramework;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper;
  internal static IMonitor StaticMonitor;
  public static string UniqueId;
  internal static AssetHandler assetHandler;

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    assetHandler = new AssetHandler();

    helper.Events.Content.AssetRequested += assetHandler.OnAssetRequested;
    helper.Events.Content.AssetReady += assetHandler.OnAssetReady;
    helper.Events.Content.AssetsInvalidated += assetHandler.OnAssetsInvalidated;
    helper.Events.GameLoop.GameLaunched += assetHandler.OnGameLaunched;

    helper.Events.GameLoop.DayStarted += OnDayStarted;
    helper.Events.Input.ButtonPressed += OnButtonPressed;

    // What
    //helper.Events.Display.Rendered += OnRendered;

    var harmony = new Harmony(ModEntry.UniqueId);
    HarmonyPatcher.ApplyPatches(harmony);
    try {
      if (Helper.ModRegistry.IsLoaded("Pathoschild.Automate")) {
        this.Monitor.Log("This mod patches Automate. If you notice issues with Automate, make sure it happens without this mod before reporting it to the Automate page.", LogLevel.Debug);
        AutomatePatcher.ApplyPatches(harmony);
      }
      if (Helper.ModRegistry.IsLoaded("NermNermNerm.Junimatic")) {
        this.Monitor.Log("This mod patches Junimatic. If you notice issues with Junimatic, make sure it happens without this mod before reporting it to the Junimatic page.", LogLevel.Debug);
        JunimaticPatcher.ApplyPatches(harmony);
      }
    } catch (Exception e) {
      Monitor.Log("Failed patching Automate. Detail: " + e.Message, LogLevel.Error);
    }
  }

  public void OnDayStarted(object sender, DayStartedEventArgs e) {
    foreach (var location in Game1.locations) {
      foreach (var tile in location.terrainFeatures.Keys) {
        if (location.objects.TryGetValue(tile, out SObject tapper) &&
            tapper.IsTapper() &&
            tapper.heldObject.Value == null) {
          Utils.UpdateTapperProduct(tapper);
        }
      }
      foreach (var resourceClump in location.resourceClumps) {
        var tile = Utils.GetTapperLocationForClump(resourceClump);
        if (location.objects.TryGetValue(tile, out SObject tapper) &&
            tapper.IsTapper() &&
            tapper.heldObject.Value == null) {
          Utils.UpdateTapperProduct(tapper);
        }
      }
    }
  }

  private bool IsNormalGameplay() {
    return StardewModdingAPI.Context.CanPlayerMove
      && Game1.player != null
      && !Game1.player.isRidingHorse()
      && Game1.currentLocation != null
      && !Game1.eventUp
      && !Game1.isFestival()
      && !Game1.IsFading();
  }

  public void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
    if (!IsNormalGameplay()) return;
    if ((e.Button.IsUseToolButton() || e.Button.IsActionButton()) &&
        Game1.player.ActiveObject is SObject obj) {
      if (obj.IsTapper() &&
        Utils.GetFeatureAt(Game1.currentLocation, e.Cursor.GrabTile, out var feature, out var centerPos) &&
        !Game1.currentLocation.objects.ContainsKey(centerPos) &&
        Utils.GetOutputRules(obj, feature, out bool unused) is var outputRules &&
        outputRules != null) {
        // Place tapper if able
        SObject @object = (SObject)obj.getOne();
        @object.heldObject.Value = null;
        @object.TileLocation = centerPos;
        Game1.currentLocation.objects.Add(centerPos, @object);
        Utils.UpdateTapperProduct(@object);
        Game1.currentLocation.playSound("axe");
        Game1.player.reduceActiveItemByOne();
        Utils.Shake(feature, centerPos);
      }
    }
    // When taking a machine output, also shake the fruit tree for fruits
    if (e.Button.IsActionButton() &&
        Game1.currentLocation.objects.ContainsKey(e.Cursor.GrabTile) &&
        Game1.currentLocation.terrainFeatures.TryGetValue(e.Cursor.GrabTile, out var feature2) &&
        feature2 is FruitTree fruitTree) {
      fruitTree.shake(e.Cursor.GrabTile, false);
    }
  }
}

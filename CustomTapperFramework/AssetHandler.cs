using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using HarmonyLib;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using Pathoschild.Stardew.Automate;
using Microsoft.Xna.Framework.Graphics;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.MachineTerrainFramework;

public class AssetHandler {
  private string dataPath;
  public Dictionary<string, TapperModel> data { get; private set; }

  public AssetHandler() {
    // "selph.CustomTapperFramework/Data"
    dataPath = $"{ModEntry.UniqueId}/Data";
  }

  public void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath)) {
      var dict = new Dictionary<string, TapperModel>();
      // Populate with base game tappers
      dict["(BC)105"] = new TapperModel();
      dict["(BC)105"].AlsoUseBaseGameRules = true;
      dict["(BC)264"] = new TapperModel();
      dict["(BC)264"].AlsoUseBaseGameRules = true;
      e.LoadFrom(() => dict, AssetLoadPriority.Low);
    }

    // Load water planter texture
    if (e.NameWithoutLocale.IsEquivalentTo($"Mods/{ModEntry.UniqueId}/WaterPlanterTexture")) {
      e.LoadFromModFile<Texture2D>("assets/WaterPlanter.png", AssetLoadPriority.Medium);
    }

    // Load water planters
    if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables")) {
      e.Edit(asset => {
        var bigCraftables = asset.AsDictionary<string, BigCraftableData>();
        bigCraftables.Data[WaterIndoorPotUtils.WaterPlanterItemId] = new BigCraftableData {
          Name = WaterIndoorPotUtils.WaterPlanterItemId,
          DisplayName = ModEntry.Helper.Translation.Get($"{WaterIndoorPotUtils.WaterPlanterItemId}.name"),
          Description = ModEntry.Helper.Translation.Get($"{WaterIndoorPotUtils.WaterPlanterItemId}.description"),
          Texture = $"Mods/{ModEntry.UniqueId}/WaterPlanterTexture",
          SpriteIndex = 0,
          ContextTags = ["custom_crab_pot_item"]
        };
        bigCraftables.Data[WaterIndoorPotUtils.WaterPotItemId] = new BigCraftableData {
          Name = WaterIndoorPotUtils.WaterPotItemId,
          DisplayName = ModEntry.Helper.Translation.Get($"{WaterIndoorPotUtils.WaterPotItemId}.name"),
          Description = ModEntry.Helper.Translation.Get($"{WaterIndoorPotUtils.WaterPotItemId}.description"),
          Texture = $"Mods/{ModEntry.UniqueId}/WaterPlanterTexture",
          SpriteIndex = 2,
        };
      });
    }

    if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes")) {
      e.Edit(asset => {
        var craftingRecipes = asset.AsDictionary<string, string>();
        craftingRecipes.Data[WaterIndoorPotUtils.WaterPlanterItemId] =
        $"388 20/Home/{WaterIndoorPotUtils.WaterPlanterItemId}/true/default";
        craftingRecipes.Data[WaterIndoorPotUtils.WaterPotItemId] =
        $"(BC)62 1/Home/{WaterIndoorPotUtils.WaterPotItemId}/true/default";
      });
    }
    if (e.NameWithoutLocale.IsEquivalentTo("rokugin.perfectionexclusions/recipes")) {
      e.Edit(asset => {
        try {
          ModEntry.StaticMonitor.Log($"Perfection Exclusion detected; excluding water planter and pot recipes...", LogLevel.Info);
          var type = AccessTools.TypeByName("PerfectionExclusions.Models.RecipeExclusions");
          if (type is null) {
            ModEntry.StaticMonitor.Log($"Error writing into Perfection Exclusions: cannot find type.", LogLevel.Warn);
            return;
          }
          var model = asset.Data;
          var entry = System.Activator.CreateInstance(type);
          if (entry is null) {
            ModEntry.StaticMonitor.Log($"Error writing into Perfection Exclusions: cannot create new entry.", LogLevel.Warn);
            return;
          }
          var craftingRecipes = ModEntry.Helper.Reflection.GetProperty<List<string>>(entry, "CraftingRecipes").GetValue();
          craftingRecipes.Add(WaterIndoorPotUtils.WaterPlanterItemId);
          craftingRecipes.Add(WaterIndoorPotUtils.WaterPotItemId);
          ModEntry.Helper.Reflection.GetMethod(model, "Add").Invoke(ModEntry.UniqueId, entry);
        }
        catch (Exception e) {
          ModEntry.StaticMonitor.Log($"Error writing into Perfection Exclusions: {e.ToString()}", LogLevel.Warn);
        }
      });
    }
    // Exclude recipes
  }

  public void OnAssetReady(object? sender, AssetReadyEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath)) {
      this.data = Game1.content.Load<Dictionary<string, TapperModel>>(this.dataPath);
      // Just in case
      this.data["(BC)105"].AlsoUseBaseGameRules = true;
      this.data["(BC)264"].AlsoUseBaseGameRules = true;
      ModEntry.StaticMonitor.Log("Loaded custom tapper data with " + data.Count + " entries.", LogLevel.Info);
    }
  }

  public void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
    this.data = Game1.content.Load<Dictionary<string, TapperModel>>(this.dataPath);
    ModEntry.StaticMonitor.Log("Loaded custom tapper data with " + data.Count + " entries.", LogLevel.Info);
    IAutomateAPI? automate = ModEntry.Helper.ModRegistry.GetApi<IAutomateAPI>("Pathoschild.Automate");
    if (automate != null) {
      automate.AddFactory(new ResourceClumpConnectorFactory());
    }
  }

  public void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e) {
    foreach (var name in e.NamesWithoutLocale) {
      if (name.IsEquivalentTo(this.dataPath)) {
        this.data = Game1.content.Load<Dictionary<string, TapperModel>>(this.dataPath);
        this.data["(BC)105"].AlsoUseBaseGameRules = true;
        this.data["(BC)264"].AlsoUseBaseGameRules = true;
        ModEntry.StaticMonitor.Log("Reloaded custom tapper data with " + data.Count + " entries.", LogLevel.Info);
      }
    }
  }

}

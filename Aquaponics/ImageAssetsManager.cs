using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Extensions;
using StardewValley.Buildings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LeFauxMods.Common.Integrations.CustomBush;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Aquaponics;

static class ImageAssetsManager {
  static Dictionary<string, Texture2D?> textures = new();

  static Texture2D GetTexture(string textureName) {
    if (!textures.ContainsKey(textureName)) {
      textures[textureName] = Game1.content.Load<Texture2D>(textureName);
    }
    return textures[textureName]!;
  }

  static string aquaponicsTank = $"Mods/{ModEntry.UniqueId}/AquaponicsTank";
  public static Texture2D AquaponicsTank => GetTexture(aquaponicsTank);
  static string aquaponicsTankFront = $"Mods/{ModEntry.UniqueId}/AquaponicsTankFront";
  public static Texture2D AquaponicsTankFront => GetTexture(aquaponicsTankFront);
  static string aquaponicsTankBack = $"Mods/{ModEntry.UniqueId}/AquaponicsTankBack";
  public static Texture2D AquaponicsTankBack => GetTexture(aquaponicsTankBack);
  static string aquaponicsTankWater = $"Mods/{ModEntry.UniqueId}/AquaponicsTankWater";
  public static Texture2D AquaponicsTankWater => GetTexture(aquaponicsTankWater);
  static string aquaponicsTankWaterHighlights = $"Mods/{ModEntry.UniqueId}/AquaponicsTankWaterHighlights";
  public static Texture2D AquaponicsTankWaterHighlights => GetTexture(aquaponicsTankWaterHighlights);

  public static void RegisterEvents(IModHelper helper) {
    helper.Events.Content.AssetRequested += OnAssetRequested;
    helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
  }

  static void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo(aquaponicsTank)) {
      e.LoadFromModFile<Texture2D>("assets/AquaponicsTank.png", AssetLoadPriority.Medium);
    }
    if (e.NameWithoutLocale.IsEquivalentTo(aquaponicsTankWater)) {
      e.LoadFromModFile<Texture2D>("assets/AquaponicsTankWater.png", AssetLoadPriority.Medium);
    }
    if (e.NameWithoutLocale.IsEquivalentTo(aquaponicsTankFront)) {
      e.LoadFromModFile<Texture2D>("assets/AquaponicsTankFront.png", AssetLoadPriority.Medium);
    }
    if (e.NameWithoutLocale.IsEquivalentTo(aquaponicsTankBack)) {
      e.LoadFromModFile<Texture2D>("assets/AquaponicsTankBack.png", AssetLoadPriority.Medium);
    }
    if (e.NameWithoutLocale.IsEquivalentTo(aquaponicsTankWaterHighlights)) {
      e.LoadFromModFile<Texture2D>("assets/AquaponicsTankWaterHighlights.png", AssetLoadPriority.Medium);
    }
  }

  static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e) {
    foreach (var asset in e.NamesWithoutLocale) {
      if (asset.IsEquivalentTo(aquaponicsTank)) {
        textures.Remove(aquaponicsTank);
      }
      if (asset.IsEquivalentTo(aquaponicsTankFront)) {
        textures.Remove(aquaponicsTankFront);
      }
      if (asset.IsEquivalentTo(aquaponicsTankBack)) {
        textures.Remove(aquaponicsTankBack);
      }
      if (asset.IsEquivalentTo(aquaponicsTankWater)) {
        textures.Remove(aquaponicsTankWater);
      }
      if (asset.IsEquivalentTo(aquaponicsTankWaterHighlights)) {
        textures.Remove(aquaponicsTankWaterHighlights);
      }
    }
  }
}

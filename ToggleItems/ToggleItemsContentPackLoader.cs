using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Inventories;
using StardewValley.GameData.Machines;
using StardewValley.GameData.BigCraftables;
using StardewValley.TokenizableStrings;
using HarmonyLib;
using System.Collections.Generic;

namespace ToggleItems {
  internal class ToggleItemsContentPackLoader {
    const string ToggleItemsConfigJson = "ToggleItemsConfig.json";

    public static IDictionary<string, string> LoadContentPack(IModHelper helper, IMonitor monitor) {
      IDictionary<string, string> result = new Dictionary<string, string>();
      foreach(IContentPack contentPack in helper.ContentPacks.GetOwned()) {
        monitor.Log($"Reading content pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} from {contentPack.DirectoryPath}");
        if (!contentPack.HasFile(ToggleItemsConfigJson)) {
          monitor.Log($"{contentPack.Manifest.Name} does not have {ToggleItemsConfigJson} in {contentPack.DirectoryPath}", LogLevel.Warn);
          continue;
        }
        ToggleItemsContentPack config = contentPack.ReadJsonFile<ToggleItemsContentPack>(ToggleItemsConfigJson); if (config == null || config.entries == null) {
          monitor.Log($"{contentPack.Manifest.Name} cannot read {ToggleItemsConfigJson} in {contentPack.DirectoryPath}", LogLevel.Warn);
          continue;
        }
        foreach (IList<string> list in config.entries) {
          if (list.Count < 2) {
            monitor.Log($"{contentPack.Manifest.Name} - ignoring empty list or list with only one entry.", LogLevel.Warn);
            continue;
          }
          for (int i = 0; i < list.Count; i++) {
            if (result.ContainsKey(list[i])) {
              monitor.Log($"{contentPack.Manifest.Name} - duplicate entry for {list[i]} found - there might be conflicting content packs.", LogLevel.Warn);
            }
            if (i == list.Count - 1) {
              result[list[i]] = list[0];
            } else {
              result[list[i]] = list[i+1];
            }
          }
        }
      }
      return result;
    }
  }
}

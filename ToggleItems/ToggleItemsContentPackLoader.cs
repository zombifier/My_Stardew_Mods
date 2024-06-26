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

    public static IDictionary<string, IList<string>> LoadContentPack(IModHelper helper, IMonitor monitor) {
      IDictionary<string, IList<string>> result = new Dictionary<string, IList<string>>();
      foreach (IContentPack contentPack in helper.ContentPacks.GetOwned()) {
        monitor.Log(
            $"Reading content pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} from {contentPack.DirectoryPath}");
        if (!contentPack.HasFile(ToggleItemsConfigJson)) {
          monitor.Log(
              $"{contentPack.Manifest.Name} does not have {ToggleItemsConfigJson} in {contentPack.DirectoryPath}",
              LogLevel.Warn);
          continue;
        }
        ToggleItemsContentPack config =
            contentPack.ReadJsonFile<ToggleItemsContentPack>(ToggleItemsConfigJson);
        if (config == null || config.entries == null) {
          monitor.Log(
              $"{contentPack.Manifest.Name} cannot read {ToggleItemsConfigJson} in {contentPack.DirectoryPath}",
              LogLevel.Warn);
          continue;
        }
        foreach (IList<string> list in config.entries) {
          if (list.Count < 2) {
            monitor.Log(
                $"{contentPack.Manifest.Name} - ignoring empty list or list with only one entry.",
                LogLevel.Warn);
            continue;
          }
          for (int i = 0; i < list.Count; i++) {
            if (result.ContainsKey(list[i])) {
              monitor.Log(
                  $"{contentPack.Manifest.Name} - duplicate entry for {list[i]} found - appending new entries to existing config.",
                  LogLevel.Warn);
              appendNonDuplicateItems(result[list[i]], list);
            } else {
              result[list[i]] = list;
            }
          }
        }
      }
      return result;
    }
    private static void appendNonDuplicateItems(IList<string> target, IList<string> source) {
      foreach (string item in source) {
        if (!target.Contains(item)) {
          target.Add(item);
        }
      }
    }
  }
}

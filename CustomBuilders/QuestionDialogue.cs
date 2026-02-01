
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.GameData.Shops;
using StardewValley.TokenizableStrings;
using Selph.StardewMods.Common;

namespace Selph.StardewMods.CustomBuilders;

class QuestionDialogueData {
  public string Question = "";
  public List<string> RequiredNpcs = new();
  public List<DialogueEntryData> DialogueEntries = new();
}

class DialogueEntryData {
  public string Id = "";
  public string Name = "";
  public string Action = "";
  public string MessageIfFalse = "";
}

sealed class QuestionDialogueDataAssetHandler : DictAssetHandler<QuestionDialogueData> {
  public QuestionDialogueDataAssetHandler() : base($"{ModEntry.UniqueId}/QuestionDialogues", ModEntry.StaticMonitor) { }
}

static class QuestionDialogue {
  static QuestionDialogueDataAssetHandler data = new();

  public static void RegisterCustomTriggers() {
    GameLocation.RegisterTileAction($"{ModEntry.UniqueId}_QuestionDialogue", OpenQuestionDialogue);
  }

  public static void RegisterEvents(IModHelper helper) {
    data.RegisterEvents(helper);
  }

  public static void ApplyPatches(Harmony harmony) {
  }

  public static bool OpenQuestionDialogue(GameLocation location, string[] args, Farmer farmer, Point point) {
    if (!Utils.TileActionCommon(location, args, farmer, point, out var npcId, out var ownerSearchArea, true)) {
      return false;
    }
    if (!data.data.TryGetValue(npcId, out var questionDialogueData)) {
      ModEntry.StaticMonitor.Log($"{npcId} not found?", LogLevel.Warn);
      return false;
    }
    if (ownerSearchArea is not null && questionDialogueData.RequiredNpcs.Count > 0) {
      bool foundNpc = false;
      IList<NPC>? npcs = location.currentEvent?.actors;
      npcs ??= location.characters;
      foreach (var npc in npcs) {
        if (questionDialogueData.RequiredNpcs.Contains(npc.Name)
            && ownerSearchArea.Value.Contains(npc.TilePoint)) {
          foundNpc = true;
        }
      }
      if (!foundNpc) {
        ModEntry.StaticMonitor.Log($"{npcId} not found in area.");
        return false;
      }
    }
    location.createQuestionDialogue(
        questionDialogueData.Question,
        questionDialogueData.DialogueEntries.Select(dialogueEntry => new Response(dialogueEntry.Id, TokenParser.ParseText(dialogueEntry.Name))).ToArray(),
        (who, whichAnswer) => {
          var dialogueEntry = questionDialogueData.DialogueEntries.FirstOrDefault(entry => entry.Id == whichAnswer);
          if (dialogueEntry is null) {
            ModEntry.StaticMonitor.Log($"{whichAnswer} not found? This should not happen", LogLevel.Error);
            return;
          }
          if (string.IsNullOrEmpty(dialogueEntry.Action)) {
            //Game1.player.forceCanMove();
            //Game1.exitActiveMenu();
            return;
          }
          if (!location.performAction(dialogueEntry.Action, farmer, new xTile.Dimensions.Location(point.X, point.Y))) {
            if (!String.IsNullOrEmpty(dialogueEntry.MessageIfFalse)) {
              Game1.addHUDMessage(new(TokenParser.ParseText(dialogueEntry.MessageIfFalse)) { noIcon = true });
            }
            //Game1.exitActiveMenu();
          }
          //Game1.player.forceCanMove();
        }, Game1.getCharacterFromName(npcId));
    return true;
  }
}

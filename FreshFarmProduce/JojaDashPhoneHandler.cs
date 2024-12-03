using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace Selph.StardewMods.FreshFarmProduce;

class JojaDashPhoneHandler : IPhoneHandler {
  static string JojaDashId { get => $"{ModEntry.UniqueId}.JojaDash"; }
  // This mail flag is cleared at the beginning of every season in the CP component
  public static string JojaDashActive = "selph.FreshFarmProduceCP.JojaDashActive";
  // This mail flag is cleared every day in the CP component
  public static string JojaDashUsed = "selph.FreshFarmProduceCP.JojaDashUsed";

  public static string JojaDashFirstTime = $"{ModEntry.UniqueId}.JojaDashFirstTime";
  public static string JojaDashFirstTimeNotActive = $"{ModEntry.UniqueId}.JojaDashFirstTimeNotActive";
  public static string JojaDashFirstTimeUsed = $"{ModEntry.UniqueId}.JojaDashFirstTimeUsed";

  public string CheckForIncomingCall(Random random) {
    return null!;
  }
  public bool TryHandleIncomingCall(string callId, out Action showDialogue) {
    showDialogue = null!;
    return false;
  }
  public IEnumerable<KeyValuePair<string, string>> GetOutgoingNumbers() {
    return [
      new(JojaDashId, ModEntry.Helper.Translation.Get("JojaDash")),
    ];
  }
  public bool TryHandleOutgoingCall(string callId) {
    if (callId == JojaDashId) {
      var morris = Game1.getCharacterFromName("MorrisTod", false) ?? Game1.getCharacterFromName("Morris", false);
      // Subscription not active or not gone Joja route
      if (!Game1.player.mailReceived.Contains(JojaDashActive) && !Game1.MasterPlayer.eventsSeen.Contains("502261")) {
        if (!Game1.player.mailReceived.Contains(JojaDashFirstTimeNotActive)) {
          Game1.player.mailReceived.Add(JojaDashFirstTimeNotActive);
          Game1.currentLocation.playShopPhoneNumberSounds(JojaDashId);
          Game1.player.freezePause = 4950;
          DelayedAction.functionAfterDelay(() => {
            Game1.DrawDialogue(morris, "Strings/Characters:selph.FreshFarmProduceCP.Morris.notActive");
          }, 4950);
        } else {
          Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("JojaDash.notActive")) {
            noIcon = true,
          });
        }
        return true;
      }
      // Already ordered
      if (Game1.player.mailReceived.Contains(JojaDashUsed)) {
        if (!Game1.player.mailReceived.Contains(JojaDashFirstTimeUsed)) {
          Game1.player.mailReceived.Add(JojaDashFirstTimeUsed);
          Game1.currentLocation.playShopPhoneNumberSounds(JojaDashId);
          Game1.player.freezePause = 4950;
          DelayedAction.functionAfterDelay(() => {
            Game1.DrawDialogue(morris, "Strings/Characters:selph.FreshFarmProduceCP.Morris.used");
          }, 4950);
        } else {
          Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("JojaDash.alreadyUsed")) {
            noIcon = true,
          });
        }
        return true;
      }
      // Order time!
      if (!Game1.player.mailReceived.Contains(JojaDashFirstTime)) {
        Game1.player.mailReceived.Add(JojaDashFirstTime);
        Game1.currentLocation.playShopPhoneNumberSounds(JojaDashId);
        Game1.player.freezePause = 4950;
        DelayedAction.functionAfterDelay(() => {
          Game1.DrawDialogue(morris,
              Game1.MasterPlayer.eventsSeen.Contains("191393") ?
              "Strings/Characters:selph.FreshFarmProduceCP.Morris.firstTimeAfterCc" :
              "Strings/Characters:selph.FreshFarmProduceCP.Morris.firstTime");
          Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate {
            OpenMenu();
          });
        }, 4950);
      } else {
        OpenMenu();
      }
      return true;
    }
    return false;
  }

  void OpenMenu() {
    // This is needed for some reason...
    Game1.player.forceCanMove();
    Game1.activeClickableMenu = ModEntry.viewEngine.CreateMenuFromAsset(
        $"Mods/{ModEntry.UniqueId}/Views/JojaDashTerminal",
      new JojaDashTerminalModel());
  }
}

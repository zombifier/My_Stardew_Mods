using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Objects;

namespace Selph.StardewMods.FreshFarmProduce;

class JojaDashPhoneHandler : IPhoneHandler {
  static string JojaDashId { get => $"{ModEntry.UniqueId}.JojaDash"; }
  // This mail flag is cleared at the beginning of every season in the CP component
  public static string JojaDashActive = "selph.FreshFarmProduceCP.JojaDashActive";
  // This mail flag is cleared every day in the CP component
  public static string JojaDashUsed = "selph.FreshFarmProduceCP.JojaDashUsed";

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
      if (!Game1.player.mailReceived.Contains(JojaDashActive)) {
        Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("JojaDash.notActive")) {
          noIcon = true,
        });
        return true;
      }
      if (Game1.player.mailReceived.Contains(JojaDashUsed)) {
        Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("JojaDash.alreadyUsed")) {
          noIcon = true,
        });
        return true;
      }
      // This is needed for some reason...
      Game1.player.forceCanMove();
      Game1.activeClickableMenu = ModEntry.viewEngine.CreateMenuFromAsset(
          $"Mods/{ModEntry.UniqueId}/Views/JojaDashTerminal",
        new JojaDashTerminalModel());
      return true;
    }
    return false;
  }
}

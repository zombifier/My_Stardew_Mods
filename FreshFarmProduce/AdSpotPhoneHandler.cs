using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace Selph.StardewMods.FreshFarmProduce;

class AdSpotPhoneHandler : IPhoneHandler {
  static string AdSpotId { get => $"{ModEntry.UniqueId}.AdSpot"; }
  public string CheckForIncomingCall(Random random) {
    return null!;
  }
  public bool TryHandleIncomingCall(string callId, out Action showDialogue) {
    showDialogue = null!;
    return false;
  }
  public IEnumerable<KeyValuePair<string, string>> GetOutgoingNumbers() {
    return [
      new(AdSpotId, ModEntry.Helper.Translation.Get("PhoneAdPurchase.name")),
    ];
  }

  const int goldCost = 200000;
  const int fameRequirement = 30;

  public bool TryHandleOutgoingCall(string callId) {
    if (callId == AdSpotId) {
      if (Game1.player.mailReceived.Contains("selph.FreshFarmProduceCP.PrideOfFerngillActive")) {
        Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("PhoneAdPurchase.alreadyHasPride")) {
          noIcon = true,
        });
        return true;
      }
      if (Game1.player.Money < goldCost) {
        Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("PhoneAdPurchase.noMoney", new { gold = goldCost })) {
          noIcon = true,
        });
        return true;
      }
      if (Utils.GetFame() < fameRequirement) {
        Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("PhoneAdPurchase.notEnoughFame", new { fame = fameRequirement })) {
          noIcon = true,
        });
        return true;
      }
      Game1.currentLocation.createQuestionDialogue(
          ModEntry.Helper.Translation.Get("PhoneAdPurchase.description", new { gold = goldCost }),
          [
          new Response("selph.FreshFarmProduce.YesPhoneAd", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes")),
          new Response("selph.FreshFarmProduce.NoPhoneAd", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")),
          ],
          (who, whichAnswer) => {
            if (whichAnswer == "selph.FreshFarmProduce.YesPhoneAd") {
              who.Money -= goldCost;
              Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("PhoneAdPurchase.adPurchased", new { fame = fameRequirement })) {
                noIcon = true,
              });
              Game1.playSound("newRecord");
              Game1.player.mailReceived.Add("selph.FreshFarmProduceCP.PrideOfFerngillActive");
              Game1.player.applyBuff("selph.FreshFarmProduceCP.PrideOfFerngill");
            }
          });
      return true;
    }
    return false;
  }
}

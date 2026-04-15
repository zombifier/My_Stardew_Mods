using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Enchantments;
using HarmonyLib;

namespace Selph.StardewMods.RideableFarmAnimals;

using SObject = StardewValley.Object;

public class HarmonyPatcher {
  public static void ApplyPatches(Harmony harmony) {
    // Patch tools interactions
    harmony.Patch(
        original: AccessTools.Method(typeof(Tool), nameof(Tool.CanAddEnchantment)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(Tool_CanAddEnchantment_prefix))));

    // Animu :>
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmAnimal), nameof(FarmAnimal.pet)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(FarmAnimal_pet_Prefix))));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(FarmAnimal), nameof(FarmAnimal.draw)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(FarmAnimal_draw_Prefix))));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(FarmAnimal), nameof(FarmAnimal.GetBoundingBox)),
        postfix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(FarmAnimal_GetBoundingBox_Postfix))));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Horse), nameof(Horse.draw)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(Horse_draw_Prefix))));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Horse), nameof(Horse.update)),
        postfix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(Horse_update_Postfix))));
    // Handle dismounting
    harmony.Patch(
        original: AccessTools.Method(typeof(Horse), nameof(Horse.checkAction)),
        prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(Horse_checkAction_Prefix))),
        postfix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(Horse_checkAction_Postfix))));
    harmony.Patch(
        original: AccessTools.Method(typeof(Horse), nameof(Horse.dismount)),
        postfix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(Horse_dismount_Postfix))));
    // Hors flut
    harmony.Patch(
        original: AccessTools.Method(typeof(Utility), nameof(Utility.GetHorseWarpRestrictionsForFarmer)),
        postfix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(Utility_GetHorseWarpRestrictionsForFarmer_Postfix))));
    harmony.Patch(
        original: AccessTools.Method(typeof(FarmerTeam), nameof(FarmerTeam.OnRequestHorseWarp)),
        postfix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(FarmerTeam_OnRequestHorseWarp_Postfix))));
    // Speed
    harmony.Patch(
        original: AccessTools.Method(typeof(Farmer), nameof(Farmer.getMovementSpeed)),
        postfix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatcher), nameof(Farmer_getMovementSpeed_Postfix))));
  }

  public static bool IsRein(Tool? tool) {
    return tool?.QualifiedItemId == $"(T){ModEntry.CpUniqueId}_Rein";
  }

  static bool Tool_CanAddEnchantment_prefix(Tool __instance, ref bool __result, BaseEnchantment enchantment) {
    if (!IsRein(__instance)) {
      return true;
    }
    __result = false;
    return false;
  }

  static bool FarmAnimal_pet_Prefix(FarmAnimal __instance, Farmer who, bool is_auto_pet) {
    if (HorseManager.IsHidden(__instance)) return false;
    // Let the normal pet go through
    if (is_auto_pet || !__instance.wasPet.Value || !IsRein(who.CurrentTool) || who.mount is not null) return true;
    HorseManager.RideAnimal(who, __instance);
    return false;
  }

  static bool Horse_checkAction_Prefix(Horse __instance, ref bool __state, Farmer who, GameLocation l) {
    __state = false;
    if (!HorseManager.IsFakeHorse(__instance)) return true;
    //if (who.CurrentItem is Hat || who.CurrentItem?.QualifiedItemId == "(O)Carrot") return false;
    if (__instance.rider is not null) {
      __state = true;
    }
    return true;
  }
  static void Horse_checkAction_Postfix(Horse __instance, bool __state, bool __result, Farmer who, GameLocation l) {
    if (!__state || !__result) return;
    HorseManager.DismountAnimal(__instance, who, l);
  }
  static bool FarmAnimal_draw_Prefix(FarmAnimal __instance, SpriteBatch b) {
    return !HorseManager.IsHidden(__instance);
  }

  static bool Horse_draw_Prefix(Horse __instance, SpriteBatch b) {
    return HorseManager.DrawFakeHorse(__instance, b);
  }
  static void Horse_dismount_Postfix(Horse __instance, bool from_demolish = false) {
    if (HorseManager.IsFakeHorse(__instance)) {
      HorseManager.MaybeReaddHorseAfterDismounting(__instance);
    }
  }

  static void Horse_update_Postfix(Horse __instance, GameTime time, GameLocation location, int ___ridingAnimationDirection) {
    if (!HorseManager.IsFakeHorse(__instance)) return;
    HorseManager.UpdateHorseSprite(__instance, time, location);
  }

  // remove collision for hidden animals
  static void FarmAnimal_GetBoundingBox_Postfix(FarmAnimal __instance, ref Rectangle __result) {
    if (HorseManager.IsHidden(__instance)) {
      __result.Inflate(-__result.Width, -__result.Height);
    }
  }


  static void FarmerTeam_OnRequestHorseWarp_Postfix(long uid) {
    HorseManager.WarpFakeHorses(uid);
  }

  static void Utility_GetHorseWarpRestrictionsForFarmer_Postfix(ref Utility.HorseWarpRestrictions __result, Farmer who) {
    bool shouldRemove = false;
    Utility.ForEachCharacter(npc => {
      if (npc is Horse horse
          && HorseManager.IsFakeHorse(horse)
          && HorseManager.GetOwner(horse) == who.UniqueMultiplayerID.ToString()) {
        shouldRemove = true;
        return false;
      }
      return true;
    });
    if (shouldRemove) {
      __result &= ~Utility.HorseWarpRestrictions.NoOwnedHorse;
    }
  }

  static void Farmer_getMovementSpeed_Postfix(Farmer __instance, ref float __result) {
    if (!__instance.isRidingHorse() || __instance.mount is null || !HorseManager.IsFakeHorse(__instance.mount)) return;
    __result *= HorseManager.GetSpeedModifier(__instance.mount);
  }
}

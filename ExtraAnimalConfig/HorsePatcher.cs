using Netcode;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Network;
using StardewValley.Characters;
using StardewValley.Buildings;
using StardewValley.Events;
using StardewValley.Extensions;
using StardewValley.Tools;
using StardewValley.Triggers;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.FarmAnimals;
using StardewValley.TerrainFeatures;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using xTile.Dimensions;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.ExtraAnimalConfig;

sealed class HorsePatcher {
  static MethodInfo? HorseCheckDelegate = null;
  public static void ApplyPatches(Harmony harmony, IModHelper helper) {

    try {
      harmony.Patch(
          original: AccessTools.Method(typeof(Horse),
            nameof(Horse.checkAction)),
          prefix: new HarmonyMethod(
            AccessTools.Method(typeof(HorsePatcher), nameof(Horse_checkAction_Prefix)),
            before: ["Goldenrevolver.HorseOverhaul"]));
      harmony.Patch(
          original: AccessTools.Method(typeof(Farmer),
            nameof(Farmer.getMovementSpeed)),
          postfix: new HarmonyMethod(typeof(HorsePatcher),
            nameof(Farmer_getMovementSpeed_Postfix)));
      //          transpiler: new HarmonyMethod(typeof(HorsePatcher), nameof(Horse_checkAction_Transpiler)));
      //if (HorseCheckDelegate is not null) {
      //  harmony.Patch(
      //      original: HorseCheckDelegate,
      //      transpiler: new HarmonyMethod(typeof(HorsePatcher), nameof(Horse_checkActionDelegate_Transpiler)));
      //}
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log($"Error patching custom horse carrots. Error detail: {e.ToString()}", LogLevel.Error);
    }
    helper.Events.GameLoop.DayStarted += OnDayStarted;
  }

  static string eatedFoodStr = $"{ModEntry.UniqueId}_EatedFood";

  static bool Horse_checkAction_Prefix(Horse __instance, ref bool __result, Farmer? who, GameLocation l) {
    if (who is null
        || !who.canMove
        || __instance.modData.ContainsKey("aedenthorn.CustomMounts")
        || __instance.munchingCarrotTimer > 0
        || __instance.rider != null
        || __instance.ateCarrotToday
        || who.ActiveObject is not SObject obj
        || !obj.HasContextTag($"{ModEntry.UniqueId}_IsHorseFood")) {
      return true;
    }
    __instance.mutex.RequestLock(() => {
      __instance.Sprite.StopAnimation();
      __instance.Sprite.faceDirection(__instance.FacingDirection);
      Game1.playSound("eat");
      DelayedAction.playSoundAfterDelay("eat", 600);
      DelayedAction.playSoundAfterDelay("eat", 1200);
      if (obj?.HasContextTag($"{ModEntry.UniqueId}_IsNotCarrot") is true) {
        __instance.munchingCarrotTimer = 0;
        __instance.mutex.ReleaseLock();
      } else {
        __instance.munchingCarrotTimer = 1500;
      }
      if (obj is not null) {
        __instance.modData[eatedFoodStr] = obj.ItemId;
      }
      __instance.doEmote(20, 32);
      who.reduceActiveItemByOne();
      __instance.ateCarrotToday = true;
    });
    __result = true;
    return false;
  }

  static void Farmer_getMovementSpeed_Postfix(Farmer __instance, ref float __result) {
    if ((Game1.CurrentEvent == null || Game1.CurrentEvent.playerControlSequence)
        && __instance.isRidingHorse() && __instance.mount.ateCarrotToday &&
        __instance.mount.modData.TryGetValue(eatedFoodStr, out var eatedFood)
        && Game1.objectData.TryGetValue(eatedFood, out var data)
        && data.CustomFields?.TryGetValue($"{ModEntry.UniqueId}_HorseSpeedBuff", out var str) is true
        && float.TryParse(str, out var buff)) {
      __result += buff - 0.4f;
    }
  }

  static void OnDayStarted(object? sender, DayStartedEventArgs e) {
    Utility.ForEachCharacter(c => {
      if (c is Horse) {
        c.modData.Remove(eatedFoodStr);
      }
      return true;
    });
  }

  // Below are fully functioning transpilers that I didn't end up using because a prefix is fineee
  static IEnumerable<CodeInstruction> Horse_checkAction_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Find the delegate that's used in the mutex requestlock
    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(NetMutex), nameof(NetMutex.RequestLock))))
      .MatchStartBackwards(
          new CodeMatch(OpCodes.Ldftn),
          new CodeMatch(OpCodes.Newobj))
      .ThrowIfNotMatch($"Could not find entry point for {nameof(Horse_checkAction_Transpiler)}");

    HorseCheckDelegate = (MethodInfo)matcher.Operand;

    return matcher.InstructionEnumeration();
  }

  static IEnumerable<CodeInstruction> Horse_checkActionDelegate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
    CodeMatcher matcher = new(instructions, generator);

    // Old: @object.QualifiedItemId == "(O)Carrot"
    // New: ... || IsHorseFood(@object)
    matcher.MatchStartForward(
        new CodeMatch(static instr => instr.IsLdloc()),
        new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Item), nameof(Item.QualifiedItemId))),
        new CodeMatch(OpCodes.Ldstr, "(O)Carrot"),
        new CodeMatch(OpCodes.Call),
        new CodeMatch(OpCodes.Brfalse))
      .ThrowIfNotMatch($"Could not find entry point for {nameof(Horse_checkActionDelegate_Transpiler)}")
      .CreateLabelWithOffsets(5, out var labelToJumpTo);
    var loadItemInst = matcher.Instruction;
    matcher.InsertAndAdvance(
        loadItemInst,
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HorsePatcher), nameof(IsHorseFood))),
        new CodeInstruction(OpCodes.Brtrue, labelToJumpTo)
        );

    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldfld),
        new CodeMatch(OpCodes.Ldc_I4),
        new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(Horse), nameof(Horse.munchingCarrotTimer)))
        )
      .ThrowIfNotMatch($"Could not find entry point 2 for {nameof(Horse_checkActionDelegate_Transpiler)}");
    var getHorseInsts = matcher.Instructions(2);
    matcher
      .Advance(4)
      .InsertAndAdvance(getHorseInsts)
      .InsertAndAdvance(
          loadItemInst,
          new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HorsePatcher), nameof(MaybeCancelCarrotAnim)))
          );

    return matcher.InstructionEnumeration();
  }

  static bool IsHorseFood(SObject item) {
    return item.HasContextTag($"{ModEntry.UniqueId}_IsHorseFood");
  }

  static void MaybeCancelCarrotAnim(Horse horse, SObject item) {
    if (item?.HasContextTag($"{ModEntry.UniqueId}_IsNotCarrot") is true) {
      horse.munchingCarrotTimer = 0;
      horse.mutex.ReleaseLock();
    }
  }
}

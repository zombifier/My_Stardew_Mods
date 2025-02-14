using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewValley.Objects;
using StardewValley.Tools;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;
using Netcode;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.FurnitureMachine;

internal sealed class ModEntry : Mod {
  static IMonitor StaticMonitor = null!;
  static Func<SObject, Item, bool, Farmer, bool, bool> SObject_performObjectDropInAction_Call = null!;
  static Func<SObject, Farmer, bool, bool> SObject_checkForAction_Call = null!;
  static Func<SObject, int, bool> SObject_minutesElapsed_Call = null!;
  static Func<SObject, Farmer, bool> SObject_performDropDownAction_Call = null!;
  static Action<SObject, GameTime> SObject_updateWhenCurrentLocation_Call = null!;
  static string UniqueId = null!;

  public override void Entry(IModHelper helper) {
    StaticMonitor = Monitor;
    UniqueId = ModManifest.UniqueID;

    // set base version of methods
    {
      var SObject_performObjectDropInAction = AccessTools.DeclaredMethod(
          typeof(SObject), nameof(SObject.performObjectDropInAction));
      var dm = new DynamicMethod("SObject_performObjectDropInAction",
          typeof(bool),
          new Type[] {typeof(SObject), typeof(Item), typeof(bool), typeof(Farmer), typeof(bool)});
      var gen = dm.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);
      gen.Emit(OpCodes.Ldarg_1);
      gen.Emit(OpCodes.Ldarg_2);
      gen.Emit(OpCodes.Ldarg_3);
      gen.Emit(OpCodes.Ldarg_S, (short)4);
      gen.Emit(OpCodes.Call, SObject_performObjectDropInAction);
      gen.Emit(OpCodes.Ret);
      SObject_performObjectDropInAction_Call =
        dm.CreateDelegate<Func<SObject, Item, bool, Farmer, bool, bool>>();
    }

    {
      var SObject_checkForAction = AccessTools.DeclaredMethod(
          typeof(SObject), nameof(SObject.checkForAction));
      var dm = new DynamicMethod("SObject_checkForAction",
          typeof(bool),
          new Type[] {typeof(SObject), typeof(Farmer), typeof(bool)});
      var gen = dm.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);
      gen.Emit(OpCodes.Ldarg_1);
      gen.Emit(OpCodes.Ldarg_2);
      gen.Emit(OpCodes.Call, SObject_checkForAction);
      gen.Emit(OpCodes.Ret);
      SObject_checkForAction_Call =
        dm.CreateDelegate<Func<SObject, Farmer, bool, bool>>();
    }

    {
      var SObject_minutesElapsed = AccessTools.DeclaredMethod(
          typeof(SObject), nameof(SObject.minutesElapsed));
      var dm = new DynamicMethod("SObject_minutesElapsed",
          typeof(bool),
          new Type[] {typeof(SObject), typeof(int)});
      var gen = dm.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);
      gen.Emit(OpCodes.Ldarg_1);
      gen.Emit(OpCodes.Call, SObject_minutesElapsed);
      gen.Emit(OpCodes.Ret);
      SObject_minutesElapsed_Call =
        dm.CreateDelegate<Func<SObject, int, bool>>();
    }

    {
      var SObject_performDropDownAction = AccessTools.DeclaredMethod(
          typeof(SObject), nameof(SObject.performDropDownAction));
      var dm = new DynamicMethod("SObject_performDropDownAction",
          typeof(bool),
          new Type[] {typeof(SObject), typeof(Farmer)});
      var gen = dm.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);
      gen.Emit(OpCodes.Ldarg_1);
      gen.Emit(OpCodes.Call, SObject_performDropDownAction);
      gen.Emit(OpCodes.Ret);
      SObject_performDropDownAction_Call =
        dm.CreateDelegate<Func<SObject, Farmer, bool>>();
    }

    {
      var SObject_updateWhenCurrentLocation = AccessTools.DeclaredMethod(
          typeof(SObject), nameof(SObject.updateWhenCurrentLocation));
      var dm = new DynamicMethod("SObject_updateWhenCurrentLocation",
          typeof(void),
          new Type[] {typeof(SObject), typeof(GameTime)});
      var gen = dm.GetILGenerator();
      gen.Emit(OpCodes.Ldarg_0);
      gen.Emit(OpCodes.Ldarg_1);
      gen.Emit(OpCodes.Call, SObject_updateWhenCurrentLocation);
      gen.Emit(OpCodes.Ret);
      SObject_updateWhenCurrentLocation_Call =
        dm.CreateDelegate<Action<SObject, GameTime>>();
    }

    var harmony = new Harmony(this.ModManifest.UniqueID);
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Furniture),
          nameof(Furniture.performObjectDropInAction)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Furniture_performObjectDropInAction_Prefix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Furniture),
          nameof(Furniture.checkForAction)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Furniture_checkForAction_Prefix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Furniture),
          nameof(Furniture.minutesElapsed)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Furniture_minutesElapsed_Postfix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Furniture),
          nameof(Furniture.performDropDownAction)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Furniture_performDropDownAction_Postfix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Furniture),
          nameof(Furniture.clicked)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Furniture_clicked_Prefix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Furniture),
          nameof(Furniture.updateWhenCurrentLocation)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Furniture_updateWhenCurrentLocation_Postfix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(GameLocation),
          nameof(GameLocation.LowPriorityLeftClick)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.GameLocation_LowPriorityLeftClick_Prefix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Tool),
          nameof(Tool.DoFunction)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Tool_DoFunction_Postfix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Chest),
          nameof(Chest.CheckAutoLoad)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Chest_CheckAutoLoad_Postfix)));

    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Furniture),
          nameof(Furniture.draw)),
        prefix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Furniture_draw_Prefix)),
        postfix: new HarmonyMethod(typeof(ModEntry),
          nameof(ModEntry.Furniture_draw_Postfix)));
        //transpiler: new HarmonyMethod(typeof(ModEntry),
        //  nameof(ModEntry.Furniture_draw_Transpiler)));

    // Transpilers to fix the "999 furniture instantly used for 1 object" bug
    //harmony.Patch(
    //    original: AccessTools.DeclaredMethod(typeof(SObject),
    //      nameof(SObject.placementAction)),
    //    transpiler: new HarmonyMethod(typeof(ModEntry),
    //      nameof(ModEntry.SObject_placementAction_Transpiler)));

    //harmony.Patch(
    //    original: AccessTools.DeclaredMethod(typeof(Utility),
    //      nameof(Utility.tryToPlaceItem)),
    //    transpiler: new HarmonyMethod(typeof(ModEntry),
    //      nameof(ModEntry.Utility_tryToPlaceItem_Transpiler)));

    //harmony.Patch(
    //    original: AccessTools.DeclaredMethod(typeof(GameLocation),
    //      "removeQueuedFurniture"),
    //    prefix: new HarmonyMethod(typeof(ModEntry),
    //      nameof(ModEntry.GameLocation_removeQueuedFurniture_Prefix)));

    //harmony.Patch(
    //    original: AccessTools.DeclaredMethod(typeof(Furniture),
    //      nameof(Furniture.maximumStackSize)),
    //    postfix: new HarmonyMethod(typeof(ModEntry),
    //      nameof(ModEntry.Furniture_maximumStackSize_Postfix)));
  }

  public override object GetApi() {
    return new FurnitureMachineApi();
  }

  const string contextTag = "furniture_machine";

  // Call Object.performObjectDropInAction instead of the furniture version (handles item input)
  static bool Furniture_performObjectDropInAction_Prefix(Furniture __instance, ref bool __result, Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false) {
    if (__instance.HasContextTag(contextTag)) {
      __result = SObject_performObjectDropInAction_Call(__instance, dropInItem, probe, who, returnFalseIfItemConsumed);
      return false;
    }
    return true;
  }

  // Call Object.checkForAction instead of the furniture version (handles item collection)
  static bool Furniture_checkForAction_Prefix(Furniture __instance, ref bool __result, Farmer who, bool justCheckingForActivity = false) {
    if (__instance.HasContextTag(contextTag)) {
      __result = SObject_checkForAction_Call(__instance, who, justCheckingForActivity);
      return false;
    }
    return true;
  }

  //static void Furniture_maximumStackSize_Postfix(Furniture __instance, ref int __result) {
  //  if (__instance.HasContextTag(contextTag)) {
  //    __result = 999;
  //  }
  //}

  // Prevents one click moving when processing, and allows left clicking to deposit item
  static bool Furniture_clicked_Prefix(Furniture __instance, ref bool __result, Farmer who) {
    if (__instance.HasContextTag(contextTag)) {
      if (who.ActiveObject != null && __instance.performObjectDropInAction(who.ActiveObject, probe: false, who)) {
        __result = true;
        return false;
      }
      if (__instance.heldObject.Value is not null) {
        __result = SObject_checkForAction_Call(__instance, who, false);
        return false;
      }
    }
    return true;
  }

  // Also call Object.minutesElapsed (handles time passage)
  static void Furniture_minutesElapsed_Postfix(Furniture __instance, ref bool __result, int minutes) {
    if (__instance.HasContextTag(contextTag)) {
      __result = SObject_minutesElapsed_Call(__instance, minutes);
    }
  }

  static void Furniture_performDropDownAction_Postfix(Furniture __instance, ref bool __result, Farmer who) {
    if (__instance.HasContextTag(contextTag)) {
      __result = SObject_performDropDownAction_Call(__instance, who);
    }
  }

  // Set the furniture's source rect if it load/working effect is active
  static void Furniture_updateWhenCurrentLocation_Postfix(Furniture __instance, int ____machineAnimationFrame, MachineEffects? ____machineAnimation, GameTime time) {
    if (__instance.HasContextTag(contextTag)) {
      SObject_updateWhenCurrentLocation_Call(__instance, time);
      var data = ItemRegistry.GetData(__instance.QualifiedItemId);
      if (data is null) {
        return;
      }
      (int spriteWidth, int spriteHeight) = GetSpriteWidthAndHeight(__instance);
      int index = data.SpriteIndex;
      if (__instance.showNextIndex.Value &&
          (__instance.GetMachineData()?.CustomFields?.TryGetValue($"{UniqueId}.NextIndexToShow", out var str) ?? false) &&
          Int32.TryParse(str, out var nextIndex)
          ) {
        index = nextIndex;
      }
      if (____machineAnimation is not null) {
        index += Math.Max(0, ____machineAnimationFrame);
      }
      // get the cached index, only update if it differs
      int previousIndex = -1;
      if (__instance.modData.TryGetValue($"{UniqueId}.CachedIndex", out var str2) &&
          Int32.TryParse(str2, out var cachedIndex)) {
        previousIndex = cachedIndex;
      }
      if (index != previousIndex) {
        __instance.modData[$"{UniqueId}.CachedIndex"] = index.ToString();
        var texture = data.GetTexture();
        __instance.sourceRect.Value = new Microsoft.Xna.Framework.Rectangle(
            index * 16 % texture.Width,
            index * 16 / texture.Width * 16,
            spriteWidth * 16,
            spriteHeight * 16);
      }
    }
  }

  // Allows tool to interact with machine furniture
  static bool GameLocation_LowPriorityLeftClick_Prefix(GameLocation __instance, ref bool __result, int x, int y, Farmer who) {
    if (Game1.activeClickableMenu != null) {
      return true;
    }
    foreach (var furniture in __instance.furniture) {
      if (IsMachineFurniture(furniture) &&
          furniture.GetBoundingBox().Contains(x, y) &&
          furniture.heldObject.Value is not null &&
          !furniture.readyForHarvest.Value) {
        __result = false;
        return false;
      }
    }
    return true;
  }

  static void Tool_DoFunction_Postfix(Tool __instance, GameLocation location, int x, int y, int power, Farmer who) {
    if (__instance.isHeavyHitter() && __instance is not MeleeWeapon) {
      foreach (var furniture in location.furniture) {
        if (IsMachineFurnitureWithOutput(furniture) && furniture.GetBoundingBox().Contains(x, y)) {
          if (furniture.readyForHarvest.Value) {
            location.debris.Add(new Debris(furniture.heldObject.Value, who.Position));
          }
          furniture.heldObject.Value = null;
          furniture.ResetParentSheetIndex();
          furniture.readyForHarvest.Value = false;
          furniture.MinutesUntilReady = 0;
          furniture.performRemoveAction();
          location.furniture.Remove(furniture);
          location.debris.Add(new Debris(furniture, who.Position));
          location.playSound("hammer", who.Tile);
          return;
        }
      }
    }
  }

  static void Chest_CheckAutoLoad_Postfix(Chest __instance, Farmer who) {
    GameLocation location = __instance.Location;
    Vector2 tile = __instance.TileLocation;
    if (location is null) return;
    foreach (var furniture in __instance.Location.furniture) {
      if (IsMachineFurniture(furniture) && furniture.boundingBox.Value.Contains(new Vector2(tile.X, tile.Y + 1) * 64f)) {
        furniture.AttemptAutoLoad(who);
      }
    }
  }

  // Draw the ready bubble
  static void Furniture_draw_Prefix(ref SObject? __state, Furniture __instance, NetVector2 ___drawPosition, SpriteBatch spriteBatch, int x, int y, float alpha = 1f) {
    if (!__instance.HasContextTag(contextTag)) {
      return;
    }
    __state = __instance.heldObject.Value;
    if (DontDrawHeldObject(__instance)) {
      __instance.heldObject.Value = null;
    }
  }

  static void Furniture_draw_Postfix(SObject? __state, Furniture __instance, NetVector2 ___drawPosition, SpriteBatch spriteBatch, int x, int y, float alpha = 1f) {
    if (__state is not null) {
      __instance.heldObject.Value = __state;
    }
    if (!__instance.HasContextTag(contextTag) || !__instance.readyForHarvest.Value || __instance.HasContextTag("dont_draw_ready_bubble")) {
      return;
    }
    Vector2 position = ___drawPosition.Value + ((__instance.shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero);
    x = (int)position.X / 64;
    y = (int)position.Y / 64 + 1;
    (int width, int _) = GetSpriteWidthAndHeight(__instance);
    width /= 2;
    float layer = (float)((y + 1) * 64) / 10000f + __instance.TileLocation.X / 50000f + 0.021f;
    float yOffset = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
    spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + width * 32 - 8, (float)(y * 64 - 96 - 16) + yOffset)), new Microsoft.Xna.Framework.Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, layer + 1E-06f);
    if (__instance.heldObject.Value == null) {
      return;
    }
    ParsedItemData heldItemData = ItemRegistry.GetDataOrErrorItem(__instance.heldObject.Value.QualifiedItemId);
    Texture2D texture3 = heldItemData.GetTexture();
    if (__instance.heldObject.Value is ColoredObject coloredObject) {
      coloredObject.drawInMenu(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + width * 32, (float)(y * 64) - 96f - 8f + yOffset)), 1f, 0.75f, layer + 1.1E-05f);
      return;
    }
    spriteBatch.Draw(texture3, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + width * 32 + 32, (float)(y * 64 - 64 - 8) + yOffset)), heldItemData.GetSourceRect(), Color.White * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, layer + 1E-05f);
    if (__instance.heldObject.Value.Stack > 1) {
      __instance.heldObject.Value.DrawMenuIcons(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + width * 32, (float)(y * 64 - 64 - 32) + yOffset - 4f)), 1f, 1f, layer + 1.2E-05f, StackDrawType.Draw, Color.White);
    }
    else if (__instance.heldObject.Value.Quality > 0) {
      __instance.heldObject.Value.DrawMenuIcons(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + width * 32, (float)(y * 64 - 64 - 32) + yOffset - 4f)), 1f, 1f, layer + 1.2E-05f, StackDrawType.HideButShowQuality, Color.White);
    }
  }

  // Don't draw the held object under certain conditions
  //static IEnumerable<CodeInstruction> Furniture_draw_Transpiler(IEnumerable<CodeInstruction> instructions) {
  //  CodeMatcher matcher = new(instructions);
  //  // Old: if (base.heldObject.Value != null)
  //  // New: ... && !DontDrawHeldObject(this)
  //  matcher.MatchEndForward(
  //      new CodeMatch(OpCodes.Ldarg_0),
  //      new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SObject), nameof(SObject.heldObject))),
  //      new CodeMatch(OpCodes.Callvirt),
  //      new CodeMatch(OpCodes.Brfalse)
  //    )
  //  .ThrowIfNotMatch($"Could not find entry point for {nameof(Furniture_draw_Transpiler)}");
  //  var labelToJumpTo = matcher.Operand;
  //  matcher.Advance(1)
  //    .InsertAndAdvance(
  //        new CodeInstruction(OpCodes.Ldarg_0),
  //        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.DontDrawHeldObject))),
  //        new CodeInstruction(OpCodes.Brtrue, labelToJumpTo)
  //        );
  //  return matcher.InstructionEnumeration();
  //}

  // Don't add the item, but a copy of it
  // This is to avoid adding all 999 stacks of a stacked object
  //static IEnumerable<CodeInstruction> SObject_placementAction_Transpiler(IEnumerable<CodeInstruction> instructions) {
  //  // Old: location.furniture.Add(this as Furniture)
  //  // New: location.furniture.Add((this as Furniture).getOne())
  //  CodeMatcher matcher = new(instructions);
  //  matcher.MatchStartForward(
  //      new CodeMatch(OpCodes.Isinst, typeof(Furniture)),
  //      new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(NetCollection<Furniture>), nameof(NetCollection<Furniture>.Add)))
  //    )
  //  .ThrowIfNotMatch($"Could not find entry point for {nameof(SObject_placementAction_Transpiler)}")
  //  .Advance(1)
  //  .InsertAndAdvance(
  //      new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.GetOneIfMachineFurniture)))
  //      );
  //  return matcher.InstructionEnumeration();
  //}

  //// Don't remove the item, but just one
  //// This is to avoid *removing* all 999 stacks of a stacked object
  //static IEnumerable<CodeInstruction> Utility_tryToPlaceItem_Transpiler(IEnumerable<CodeInstruction> instructions) {
  //  // Old: if (item is Furniture)
  //  // New: if (item is Furniture && !IsMachineFurniture(item))
  //  CodeMatcher matcher = new(instructions);
  //  matcher.MatchStartForward(
  //      new CodeMatch(OpCodes.Ldarg_1),
  //      new CodeMatch(OpCodes.Isinst, typeof(Furniture)),
  //      new CodeMatch(OpCodes.Brfalse_S),
  //      new CodeMatch(OpCodes.Call, AccessTools.DeclaredPropertyGetter(typeof(Game1), nameof(Game1.player))),
  //      new CodeMatch(OpCodes.Ldnull),
  //      new CodeMatch(OpCodes.Callvirt, AccessTools.DeclaredPropertySetter(typeof(Farmer), nameof(Farmer.ActiveObject)))
  //    )
  //  .ThrowIfNotMatch($"Could not find entry point for ActiveObject clearing portion of {nameof(Utility_tryToPlaceItem_Transpiler)}")
  //  .Advance(2);
  //  var labelToJumpTo = matcher.Operand;
  //  matcher.Advance(1)
  //  .InsertAndAdvance(
  //      new CodeInstruction(OpCodes.Ldarg_1),
  //      new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.IsMachineFurniture))),
  //      new CodeInstruction(OpCodes.Brtrue_S, labelToJumpTo)
  //    );

  //  matcher.MatchStartForward(
  //      new CodeMatch(OpCodes.Ldarg_1),
  //      new CodeMatch(OpCodes.Isinst, typeof(Furniture)),
  //      new CodeMatch(OpCodes.Stloc_1),
  //      new CodeMatch(OpCodes.Ldloc_1),
  //      new CodeMatch(OpCodes.Brfalse_S),
  //      new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Game1), nameof(Game1.player))),
  //      new CodeMatch(OpCodes.Ldloc_1),
  //      new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Farmer), nameof(Farmer.ActiveObject)))
  //    )
  //  .ThrowIfNotMatch($"Could not find entry point for ActiveObject setting portion of {nameof(Utility_tryToPlaceItem_Transpiler)}")
  //  .Advance(4);
  //  labelToJumpTo = matcher.Operand;
  //  matcher.Advance(1)
  //  .InsertAndAdvance(
  //      new CodeInstruction(OpCodes.Ldarg_1),
  //      new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.IsMachineFurniture))),
  //      new CodeInstruction(OpCodes.Brtrue, labelToJumpTo)
  //    );

  //  return matcher.InstructionEnumeration();
  //}

  //// If stackable machine furniture, don't put them into a new inventory slot when removed
  //static bool GameLocation_removeQueuedFurniture_Prefix(GameLocation __instance, Guid guid) {
  //    if (!__instance.furniture.TryGetValue(guid, out var furniture) || !IsMachineFurniture(furniture)) {
  //      return true;
  //    }
  //    Farmer player = Game1.player;
  //    if (!player.couldInventoryAcceptThisItem(furniture)) {
  //      return false;
  //    }
  //    furniture.performRemoveAction();
  //    __instance.furniture.Remove(guid);
  //      player.addItemToInventory(furniture);
  //    __instance.localSound("coin");
  //    return false;
  //}

  static (int, int) GetSpriteWidthAndHeight(Furniture furniture) {
    string[]? furnitureData = null;
    if (DataLoader.Furniture(Game1.content).TryGetValue(furniture.itemId.Value, out var dataStr)) {
      furnitureData = dataStr.Split('/');
    }
    if (furnitureData is null) {
      return (2, 2);
    }
    string[] array = ArgUtility.SplitBySpace(furnitureData[2]);
    int spriteWidth = Math.Max(1, Convert.ToInt32(array[0]));
    int spriteHeight = Math.Max(1, Convert.ToInt32(array[1]));
    return (spriteWidth, spriteHeight);
  }

  static bool DontDrawHeldObject(Furniture furniture) {
    return ItemContextTagManager.DoAllTagsMatch(
        new[] {contextTag,
        furniture.readyForHarvest.Value ? "dont_draw_held_object_when_ready" : "dont_draw_held_object_while_processing"},
        furniture.GetContextTags());
  }

  public static bool IsMachineFurniture(SObject furniture) {
    return furniture.HasContextTag(contextTag);
  }

  static bool IsMachineFurnitureWithOutput(SObject furniture) {
    return IsMachineFurniture(furniture) && furniture.heldObject.Value is not null;
  }

  static Furniture GetOneIfMachineFurniture(Furniture furniture) {
    if (furniture.HasContextTag(contextTag)) {
      var ret = (Furniture)furniture.getOne();
      ret.performDropDownAction(Game1.player);
      return ret;
    } else return furniture;
  }
}

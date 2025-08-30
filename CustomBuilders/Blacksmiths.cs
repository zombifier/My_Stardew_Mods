using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.GameData.Shops;
using StardewValley.BellsAndWhistles;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.CustomBuilders;

static class Blacksmiths {
  public static void RegisterCustomTriggers() {
    GameLocation.RegisterTileAction($"{ModEntry.UniqueId}_OpenBlacksmithShop", OpenBlacksmithShop);
  }

  public static void RegisterEvents(IModHelper helper) {
    helper.Events.Display.MenuChanged += OnMenuChanged;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
  }

  public static void ApplyPatches(Harmony harmony) {
    harmony.Patch(
        original: AccessTools.Constructor(typeof(ShopMenu), new[] {typeof(string),
          typeof(ShopData),
          typeof(ShopOwnerData),
          typeof(NPC),
          typeof(ShopMenu.OnPurchaseDelegate),
          typeof(Func<ISalable, bool>),
          typeof(bool)}),
        postfix: new HarmonyMethod(typeof(Blacksmiths),
          nameof(ShopMenu_Constructor_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(ShopMenu), nameof(ShopMenu.performHoverAction)),
        prefix: new HarmonyMethod(typeof(Blacksmiths), nameof(ShopMenu_performHoverAction_Prefix)),
        postfix: new HarmonyMethod(typeof(Blacksmiths), nameof(ShopMenu_performHoverAction_Postfix)));
    try {
      harmony.Patch(
          original: AccessTools.DeclaredMethod(typeof(IClickableMenu),
            nameof(IClickableMenu.drawHoverText),
            new Type[] {
          typeof(SpriteBatch), //0
          typeof(StringBuilder),
          typeof(SpriteFont),
          typeof(int),
          typeof(int),
          typeof(int), //5
          typeof(string),
          typeof(int),
          typeof(string[]),
          typeof(Item),
          typeof(int), //10
          typeof(string),
          typeof(int),
          typeof(int),
          typeof(int),
          typeof(float), //15
          typeof(CraftingRecipe),
          typeof(IList<Item>),
          typeof(Texture2D),
          typeof(Microsoft.Xna.Framework.Rectangle?),
          typeof(Color?), //20
          typeof(Color?),
          typeof(float),
          typeof(int),
          typeof(int),
            }),
          transpiler: new HarmonyMethod(AccessTools.Method(typeof(Blacksmiths), nameof(IClickableMenu_drawHoverText_Transpiler)), Priority.High));
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log($"Failed patching in upgrade info for items in blacksmith store: {e.ToString()}. This shouldn't break anything other than visually", LogLevel.Warn);
    }
  }

  static string ReadyDayKey = $"{ModEntry.UniqueId}_ReadyDay";
  static string RequireToolIdKey = $"{ModEntry.UniqueId}_RequireToolId";
  static string BlacksmithNameKey = $"{ModEntry.UniqueId}_BlacksmithName";
  static string BlacksmithInventoryKeyPrefix = $"{ModEntry.UniqueId}_Blacksmith";

  static Inventory GetBlacksmithInventory(string shopId, Farmer player) {
    return player.team.GetOrCreateGlobalInventory($"{BlacksmithInventoryKeyPrefix}_{player.UniqueMultiplayerID}_{shopId}");
  }

  static int GetReadyDay(Item item) {
    if (item.modData.TryGetValue(ReadyDayKey, out var str)
        && Int32.TryParse(str, out var readyDay)) {
      return readyDay;
    }
    return -1;
  }

  static readonly PerScreen<NPC?> shopOwner = new();

  static void ShopMenu_Constructor_Postfix(ShopMenu __instance, string shopId, ShopData shopData, ShopOwnerData ownerData, NPC? owner = null, ShopMenu.OnPurchaseDelegate? onPurchase = null, Func<ISalable, bool>? onSell = null, bool playOpenSound = true) {
    if (shopData.CustomFields?.TryGetValue($"{ModEntry.UniqueId}_IsCustomToolShop", out var _) ?? false) {
      shopOwner.Value = owner;
      __instance.onPurchase = (salable, who, countTaken, stock) => {
        if (salable is not Item item) return false;
        if (item.modData.TryGetValue(RequireToolIdKey, out var requireToolId)) {
          ModEntry.StaticMonitor.Log(requireToolId, LogLevel.Trace);
          item.modData.Remove(RequireToolIdKey);
          Item? originalItem = who.Items.GetById(requireToolId).FirstOrDefault();
          who.removeItemFromInventory(originalItem);
          if (originalItem is Tool originalTool && item is Tool boughtTool) {
            boughtTool.UpgradeFrom(originalTool);
            // add attachments as well
            foreach (var attachment in originalTool.attachments) {
              boughtTool.attach((SObject)attachment.getOne());
            }
          }
        }
        int readyDay = GetReadyDay(item);
        if (readyDay == -1) {
          readyDay = 2;
          item.modData[ReadyDayKey] = "2";
        }
        var blacksmithInventory = GetBlacksmithInventory(shopId, who);
        blacksmithInventory.Add(item);
        Game1.playSound("parry");
        Game1.exitActiveMenu();
        var displayName = item.Stack > 1 ? Lexicon.makePlural(item.DisplayName) : item.DisplayName;
        if (owner is not null) {
          item.modData[BlacksmithNameKey] = owner.Name;
          Game1.DrawDialogue(
              owner,
              readyDay == 1
              ? $"Characters/Dialogue/{owner.Name}:{ModEntry.UniqueId}_Blacksmith_Bought_OneDay"
              : $"Characters/Dialogue/{owner.Name}:{ModEntry.UniqueId}_Blacksmith_Bought",
              displayName, readyDay);
        } else {
          Game1.drawObjectDialogue(ModEntry.Helper.Translation.Get(
                readyDay == 1 ? "GenericBlacksmithNotReadyOneDay" : "GenericBlacksmithNotReady",
                new { displayName = displayName, readyDay = readyDay }
                ));
        }
        return true;
      };
      __instance.canPurchaseCheck = (index) => {
        if (__instance.forSale[index] is Item item
            && item.modData.TryGetValue(RequireToolIdKey, out var requireToolId)
            && !Game1.player.Items.ContainsId(requireToolId)) {
          return false;
        }
        return true;
      };
    }
  }
  static void OnMenuChanged(object? sender, MenuChangedEventArgs e) {
    if (e.NewMenu is not ShopMenu) {
      shopOwner.Value = null;
    }
  }

  public static bool OpenBlacksmithShop(GameLocation location, string[] args, Farmer farmer, Point point) {
    if (!ArgUtility.TryGet(args, 1, out var shopId, out var error) ||
        !ArgUtility.TryGetOptional(args, 2, out var direction, out error, null, allowBlank: true, "string direction") ||
        !ArgUtility.TryGetOptionalInt(args, 3, out var openTime, out error, -1, "int openTime") ||
        !ArgUtility.TryGetOptionalInt(args, 4, out var closeTime, out error, -1, "int closeTime") ||
        !ArgUtility.TryGetOptionalInt(args, 5, out var shopAreaX, out error, -1, "int shopAreaX") ||
        !ArgUtility.TryGetOptionalInt(args, 6, out var shopAreaY, out error, -1, "int shopAreaY") ||
        !ArgUtility.TryGetOptionalInt(args, 7, out var shopAreaWidth, out error, -1, "int shopAreaWidth") ||
        !ArgUtility.TryGetOptionalInt(args, 8, out var shopAreaHeight, out error, -1, "int shopAreaHeight")) {
      ModEntry.StaticMonitor.Log(error, LogLevel.Warn);
      return false;
    }

    if (!DataLoader.Shops(Game1.content).TryGetValue(shopId, out var shopData)
        || shopData.CustomFields?.TryGetValue($"{ModEntry.UniqueId}_IsCustomToolShop", out var _) is false) {
      ModEntry.StaticMonitor.Log($"{shopId} not found?", LogLevel.Warn);
      return false;
    }

    Microsoft.Xna.Framework.Rectangle? ownerSearchArea = null;
    // Check NPC is within area
    if (shopAreaX != -1 || shopAreaY != -1 || shopAreaWidth != -1 || shopAreaHeight != -1) {
      if (shopAreaX == -1 || shopAreaY == -1 || shopAreaWidth == -1 || shopAreaHeight == -1) {
        ModEntry.StaticMonitor.Log("when specifying any of the shop area 'x y width height' arguments (indexes 5-8), all four must be specified", LogLevel.Warn);
        return false;
      }
      ownerSearchArea = new(shopAreaX, shopAreaY, shopAreaWidth, shopAreaHeight);
      //bool foundNpc = false;
      //IList<NPC>? npcs = location.currentEvent?.actors;
      //npcs ??= location.characters;
      //foreach (var npc in npcs) {
      //  if (npc.Name == shopId && ownerSearchArea.Contains(npc.TilePoint)) {
      //    foundNpc = true;
      //  }
      //}
      //if (!foundNpc) {
      //  ModEntry.StaticMonitor.Log($"{shopId} not found in area.");
      //  return false;
      //}
    }

    // Check direction
    switch (direction) {
      case "down":
        if (farmer.TilePoint.Y < point.Y) {
          ModEntry.StaticMonitor.Log($"player not down of {shopId}.");
          return false;
        }
        break;
      case "up":
        if (farmer.TilePoint.Y > point.Y) {
          ModEntry.StaticMonitor.Log($"player not up of {shopId}.");
          return false;
        }
        break;
      case "left":
        if (farmer.TilePoint.X > point.X) {
          ModEntry.StaticMonitor.Log($"player not left of {shopId}.");
          return false;
        }
        break;
      case "right":
        if (farmer.TilePoint.X < point.X) {
          ModEntry.StaticMonitor.Log($"player not right of {shopId}.");
          return false;
        }
        break;
    }

    // Check opening and closing times
    if ((openTime >= 0 && Game1.timeOfDay < openTime) || (closeTime >= 0 && Game1.timeOfDay >= closeTime)) {
      ModEntry.StaticMonitor.Log($"{shopId} is closed.");
      return false;
    }

    var forceOpen = ownerSearchArea is not null;
    if (Utility.TryOpenShopMenu(shopId, location, ownerSearchArea, null, forceOpen)
        && Game1.activeClickableMenu is ShopMenu shopMenu) {
      var blacksmithInventory = GetBlacksmithInventory(shopId, farmer);
      if (blacksmithInventory.Count > 0 && blacksmithInventory[0] is Item item) {
        shopMenu.exitThisMenuNoSound();
        var readyDay = GetReadyDay(item);
        var displayName = item.Stack > 1 ? Lexicon.makePlural(item.DisplayName) : item.DisplayName;
        if (readyDay > 0) {
          if (shopOwner.Value is not null) {
            Game1.DrawDialogue(
                shopOwner.Value,
                readyDay == 1
                ? $"Characters/Dialogue/{shopOwner.Value.Name}:{ModEntry.UniqueId}_Blacksmith_Busy_OneDay"
                : $"Characters/Dialogue/{shopOwner.Value.Name}:{ModEntry.UniqueId}_Blacksmith_Busy",
                displayName, readyDay);
          } else {
            Game1.drawObjectDialogue(ModEntry.Helper.Translation.Get(
                  readyDay == 1 ? "GenericBlacksmithNotReadyOneDay" : "GenericBlacksmithNotReady",
                  new { displayName = displayName, readyDay = readyDay }
                  ));
          }
        } else {
          blacksmithInventory.Remove(item);
          item.modData.Remove(ReadyDayKey);
          farmer.holdUpItemThenMessage(item);
          farmer.addItemByMenuIfNecessary(item);
          blacksmithInventory.RemoveEmptySlots();
        }
      }
    }
    return true;
  }

  static void OnDayStarted(object? sender, DayStartedEventArgs e) {
    if (!Context.IsMainPlayer) return;
    foreach (var pair in Game1.player.team.globalInventories.Pairs) {
      if (pair.Key.StartsWith(BlacksmithInventoryKeyPrefix)) {
        foreach (var item in pair.Value) {
          var readyDay = GetReadyDay(item);
          readyDay--;
          if (readyDay <= 0) {
            item.modData.Remove(ReadyDayKey);
            var displayName = item.Stack > 1 ? Lexicon.makePlural(item.DisplayName) : item.DisplayName;
            if (item.modData.TryGetValue(BlacksmithNameKey, out var blacksmithName)
                && Game1.characterData.TryGetValue(blacksmithName, out var npcData)) {
              Game1.showGlobalMessage(ModEntry.Helper.Translation.Get("ToolReady", new { displayName = displayName, blacksmithName = npcData.DisplayName }));
            } else {
              Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:ToolReady", displayName));
            }
          } else {
            item.modData[ReadyDayKey] = readyDay.ToString();
          }
        }
      }
    }
  }

  public static IEnumerable<CodeInstruction> IClickableMenu_drawHoverText_Transpiler(IEnumerable<CodeInstruction> instructions) {
    CodeMatcher matcher = new(instructions);
    // Find the address of the x and y local var
    matcher.MatchStartForward(
      new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Game1), nameof(Game1.getOldMouseX))))
    .MatchStartForward(
      new CodeMatch(OpCodes.Stloc_S))
    .ThrowIfNotMatch($"Could not find entry point for x portion of {nameof(IClickableMenu_drawHoverText_Transpiler)}");
    var xVar = matcher.Operand;

    matcher.MatchStartForward(
      new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Game1), nameof(Game1.getOldMouseY))))
    .MatchStartForward(
      new CodeMatch(OpCodes.Stloc_S))
    .ThrowIfNotMatch($"Could not find entry point for y portion of {nameof(IClickableMenu_drawHoverText_Transpiler)}");
    var yVar = matcher.Operand;

    matcher
      .End();
    var retLabels = matcher.Labels;
    matcher
      .RemoveInstruction()
      .InsertAndAdvance(
        new CodeInstruction(OpCodes.Ldarg_0).WithLabels(retLabels),
        new CodeInstruction(OpCodes.Ldarg_2),
        new CodeInstruction(OpCodes.Ldarg_S, (short)9),
        new CodeInstruction(OpCodes.Ldloca_S, xVar),
        new CodeInstruction(OpCodes.Ldloca_S, yVar),
        new CodeInstruction(OpCodes.Ldarg_S, (short)20),
        new CodeInstruction(OpCodes.Ldarg_S, (short)21),
        new CodeInstruction(OpCodes.Ldarg_S, (short)16),
        // width
        new CodeInstruction(OpCodes.Ldloc_1),
        new CodeInstruction(OpCodes.Ldarg_S, (short)5),
        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Blacksmiths), nameof(MaybeDrawToolRequirement))),
        new CodeInstruction(OpCodes.Ret)
        );

    return matcher.InstructionEnumeration();
  }

  static void MaybeDrawToolRequirement(SpriteBatch b, SpriteFont font, Item? item, ref int x, ref int y, Color? textColor, Color? textShadowColor, CraftingRecipe? craftingIngredients, int width, int moneyAmountToDisplayAtBottom) {
    if (item is not null
        && item.modData.TryGetValue(RequireToolIdKey, out var requireToolId)) {
      y += 60;
      textColor = textColor ?? Game1.textColor;
      textShadowColor = textShadowColor ?? Game1.textShadowColor;
      if (moneyAmountToDisplayAtBottom == -1) {
        y += 8;
      }
      var dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(requireToolId);
      string displayName2 = dataOrErrorItem2.DisplayName;
      Texture2D texture = dataOrErrorItem2.GetTexture();
      Microsoft.Xna.Framework.Rectangle sourceRect2 = dataOrErrorItem2.GetSourceRect();
      string text10 = Game1.content.LoadString("Strings\\UI:ItemHover_Requirements", 1, displayName2);
      float num12 = Math.Max(font.MeasureString(text10).Y + 21f, 96f);
      //      IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Microsoft.Xna.Framework.Rectangle(0, 256, 60, 60), x, y + 4, num + ((craftingIngredients != null) ? 21 : 0), (int)num12, Color.White);
      IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Microsoft.Xna.Framework.Rectangle(0, 256, 60, 60), x, y + 4, width + ((craftingIngredients != null) ? 21 : 0), (int)num12, Color.White);
      y += 20;
      b.DrawString(font, text10, new Vector2(x + 16, y + 4) + new Vector2(2f, 2f), textShadowColor.Value);
      b.DrawString(font, text10, new Vector2(x + 16, y + 4) + new Vector2(0f, 2f), textShadowColor.Value);
      b.DrawString(font, text10, new Vector2(x + 16, y + 4) + new Vector2(2f, 0f), textShadowColor.Value);
      b.DrawString(Game1.smallFont, text10, new Vector2(x + 16, y + 4), textColor.Value);
      b.Draw(texture, new Vector2(x + 16 + (int)font.MeasureString(text10).X + 21, y), sourceRect2, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
    }
  }

  // Dirty hack to get hover working for unpurchasable tool upgrade items, weh
  static void ShopMenu_performHoverAction_Prefix(ShopMenu __instance, ref Func<int, bool>? __state, int x, int y) {
    if (__instance.ShopData?.CustomFields?.TryGetValue($"{ModEntry.UniqueId}_IsCustomToolShop", out var _) ?? false) {
      __state = __instance.canPurchaseCheck;
      __instance.canPurchaseCheck = null;
    }
  }

  static void ShopMenu_performHoverAction_Postfix(ShopMenu __instance, ref Func<int, bool>? __state, int x, int y) {
    if (__state is not null) {
      __instance.canPurchaseCheck = __state;
    }
  }
}

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
using StardewValley.ItemTypeDefinitions;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.GameData.Shops;
using StardewValley.BellsAndWhistles;
using Selph.StardewMods.Common;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.CustomBuilders;

static class Blacksmiths {
  public static void RegisterCustomTriggers() {
    GameLocation.RegisterTileAction($"{ModEntry.UniqueId}_OpenBlacksmithShop", OpenBlacksmithShop);
  }

  public static void RegisterEvents(IModHelper helper) {
    helper.Events.Display.MenuChanged += OnMenuChanged;
    helper.Events.GameLoop.DayStarted += OnDayStarted;
    helper.Events.Player.InventoryChanged += OnInventoryChanged;
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
        original: AccessTools.DeclaredMethod(typeof(ShopMenu), "tryToPurchaseItem"),
        prefix: new HarmonyMethod(typeof(Blacksmiths), nameof(ShopMenu_tryToPurchaseItem_Prefix)));
    //postfix: new HarmonyMethod(typeof(Blacksmiths), nameof(ShopMenu_tryToPurchaseItem_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.CanBuyItem)),
        postfix: new HarmonyMethod(typeof(Blacksmiths), nameof(Item_CanBuyItem_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.actionWhenPurchased)),
        postfix: new HarmonyMethod(typeof(Blacksmiths), nameof(Item_actionWhenPurchased_Postfix)));
    harmony.Patch(
        original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.GetSalableInstance)),
        postfix: new HarmonyMethod(typeof(Blacksmiths), nameof(Item_GetSalableInstance_Postfix)));
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
      // Give up for now if CSSM :pensivebutt:
      if (!ModEntry.HasCssm) {
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(ShopMenu), nameof(ShopMenu.draw)),
            transpiler: new HarmonyMethod(AccessTools.Method(typeof(Blacksmiths), nameof(ShopMenu_draw_Transpiler)), Priority.High));
      }
    }
    catch (Exception e) {
      ModEntry.StaticMonitor.Log($"Failed patching in draw patches for custom blacksmiths and tool upgrades: {e.ToString()}", LogLevel.Error);
    }
  }

  static string ReadyDayKey = $"{ModEntry.UniqueId}_ReadyDay";
  static string RequireToolIdKey = $"{ModEntry.UniqueId}_RequireToolId";
  static string BlacksmithNameKey = $"{ModEntry.UniqueId}_BlacksmithName";
  static string BlacksmithInventoryKeyPrefix = $"{ModEntry.UniqueId}_Blacksmith";
  static string ExtraRequirements = $"{ModEntry.UniqueId}_ExtraTradeItems";

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

  static List<(string, int)>? GetExtraRequirements(IHaveModData? item) {
    if (item?.modData.TryGetValue(ExtraRequirements, out var str) is true) {
      string[] array = ArgUtility.SplitBySpace(str);
      var result = new List<(string, int)>();
      for (int i = 0; i < array.Length; i += 2) {
        result.Add((array[i], ArgUtility.GetInt(array, i + 1, 1)));
      }
      return result;
    }
    return null;
  }

  static void ShopMenu_Constructor_Postfix(ShopMenu __instance, string shopId, ShopData shopData, ShopOwnerData ownerData, NPC? owner = null, ShopMenu.OnPurchaseDelegate? onPurchase = null, Func<ISalable, bool>? onSell = null, bool playOpenSound = true) {
    __instance.onPurchase += (salable, who, countTaken, stock) => {
      if (salable is not Item item) return false;
      var extraRequirements = GetExtraRequirements(item);
      if (extraRequirements is not null) {
        foreach (var (itemId, count) in extraRequirements) {
          __instance.ConsumeTradeItem(itemId, count * countTaken);
        }
      }
      return false;
    };
    if (shopData.CustomFields?.TryGetValue($"{ModEntry.UniqueId}_IsCustomToolShop", out var _) ?? false) {
      shopOwner.Value = owner;
      __instance.onPurchase += (salable, who, countTaken, stock) => {
        if (salable is not Item origItem) return false;
        var item = origItem.getOne();
        item.Stack = origItem.Stack;
        item.modData.Remove(ExtraRequirements);
        if (item.modData.TryGetValue(RequireToolIdKey, out var requireToolId)) {
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
      //__instance.canPurchaseCheck += (index) => {
      //  if (__instance.forSale[index] is not Item item) return true;
      //  var extraRequirements = GetExtraRequirements(item);
      //  if (extraRequirements is not null) {
      //    foreach (var (itemId, count) in extraRequirements) {
      //      if (!__instance.HasTradeItem(itemId, count)) {
      //        return false;
      //      }
      //    }
      //  }
      //  if (item.modData.TryGetValue(RequireToolIdKey, out var requireToolId)
      //      && !Game1.player.Items.ContainsId(requireToolId)) {
      //    return false;
      //  }
      //  return true;
      //};
    }
  }
  static void OnMenuChanged(object? sender, MenuChangedEventArgs e) {
    if (e.NewMenu is not ShopMenu) {
      shopOwner.Value = null;
    }
  }

  public static bool OpenBlacksmithShop(GameLocation location, string[] args, Farmer farmer, Point point) {
    if (!Utils.TileActionCommon(location, args, farmer, point, out var shopId, out var ownerSearchArea, true)) {
      return false;
    }

    if (!DataLoader.Shops(Game1.content).TryGetValue(shopId, out var shopData)
        || shopData.CustomFields?.TryGetValue($"{ModEntry.UniqueId}_IsCustomToolShop", out var _) is false) {
      ModEntry.StaticMonitor.Log($"{shopId} not found?", LogLevel.Warn);
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
    if (item is null) return;
    if (item.modData.TryGetValue(RequireToolIdKey, out var requireToolId)) {
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
    var extraRequirements = GetExtraRequirements(item);
    if (extraRequirements is not null) {
      foreach (var (itemId, count) in extraRequirements) {
        y += 60;
        textColor = textColor ?? Game1.textColor;
        textShadowColor = textShadowColor ?? Game1.textShadowColor;
        if (moneyAmountToDisplayAtBottom == -1) {
          y += 8;
        }
        var dataOrErrorItem2 = ItemRegistry.GetDataOrErrorItem(itemId);
        string displayName2 = dataOrErrorItem2.DisplayName;
        Texture2D texture = dataOrErrorItem2.GetTexture();
        Microsoft.Xna.Framework.Rectangle sourceRect2 = dataOrErrorItem2.GetSourceRect();
        string text10 = Game1.content.LoadString("Strings\\UI:ItemHover_Requirements", count, displayName2);
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
  }

  // Dirty hack to get hover working for unpurchasable tool upgrade items, weh
  //static void ShopMenu_performHoverAction_Prefix(ShopMenu __instance, ref Func<int, bool>? __state, int x, int y) {
  //  if (__instance.ShopData?.CustomFields?.TryGetValue($"{ModEntry.UniqueId}_IsCustomToolShop", out var _) ?? false) {
  //    __state = __instance.canPurchaseCheck;
  //    __instance.canPurchaseCheck = null;
  //  }
  //}

  //static void ShopMenu_performHoverAction_Postfix(ShopMenu __instance, ref Func<int, bool>? __state, int x, int y) {
  //  if (__state is not null) {
  //    __instance.canPurchaseCheck = __state;
  //  }
  //}

  static bool ShopMenu_tryToPurchaseItem_Prefix(ShopMenu __instance, ref bool __result, ISalable item, ISalable held_item, int stockToBuy, int x, int y) {
    if (item is not Item soldItem) return true;
    bool canBuy = true;
    var extraRequirements = GetExtraRequirements(soldItem);
    if (extraRequirements is not null) {
      foreach (var (itemId, count) in extraRequirements) {
        if (!__instance.HasTradeItem(itemId, count * stockToBuy)) {
          canBuy = false;
        }
      }
    }
    if (soldItem.modData.TryGetValue(RequireToolIdKey, out var requireToolId)
        && !Game1.player.Items.ContainsId(requireToolId)) {
      canBuy = false;
    }
    if (!canBuy) {
      __result = false;
      Game1.playSound("cancel");
      return false;
    }
    return true;
  }

  //static void ShopMenu_tryToPurchaseItem_Postfix(ShopMenu __instance, ref bool __result, ISalable item, ISalable held_item, int stockToBuy, int x, int y) {
  //  (__instance.heldItem as IHaveModData)?.modData.Remove(ExtraRequirements);
  //}

  // This is so the item can still be bought even though the inventory is full
  static void Item_CanBuyItem_Postfix(Item __instance, ref bool __result) {
    if (!__result
        && Game1.activeClickableMenu is ShopMenu shopMenu
        && shopMenu.ShopData.CustomFields?.ContainsKey($"{ModEntry.UniqueId}_IsCustomToolShop") is true) {
      __result = true;
    }
  }

  // This is so the item doesn't try to get added to inventory (and get "inventory full")
  static void Item_actionWhenPurchased_Postfix(Item __instance, ref bool __result, string shopId) {
    if (DataLoader.Shops(Game1.content).TryGetValue(shopId, out var shopData)
        && (shopData.CustomFields?.ContainsKey($"{ModEntry.UniqueId}_IsCustomToolShop") ?? false)) {
      __result = true;
    }
  }

  static void DrawExtraRequirements(
      ShopMenu shopMenu, SpriteBatch b,
      ref int right, int tradeIconDrawY, int tradeTextDrawY, int i) {
    var extraRequirements = GetExtraRequirements(shopMenu.forSale[shopMenu.currentItemIndex + i] as Item);
    if (extraRequirements is not null) {
      ISalable item = shopMenu.forSale[shopMenu.currentItemIndex + i];
      ItemStockInformation stock = shopMenu.itemPriceAndStock[item];
      ShopMenu.ShopCachedTheme visualTheme = shopMenu.VisualTheme;
      bool canPurchaseCheckFailed = shopMenu.canPurchaseCheck != null && !shopMenu.canPurchaseCheck(shopMenu.currentItemIndex + i);
      float originalTradeTextWidth = 0;
      if (stock.TradeItem != null) {
        int requiredItemCount = 5;
        string requiredItem = stock.TradeItem;
        if (requiredItem != null && stock.TradeItemCount.HasValue) {
          requiredItemCount = stock.TradeItemCount.Value;
        }
        originalTradeTextWidth = SpriteText.getWidthOfString("x" + requiredItemCount);
      }
      for (int j = 0; j < extraRequirements.Count; j++) {
        var (requiredItem, requiredItemCount) = extraRequirements[j];
        right -= (int)(originalTradeTextWidth + 88 + 16);
        float textWidth = SpriteText.getWidthOfString("x" + requiredItemCount);
        if (j >= 1) {
          SpriteText.drawString(b, "...", right - (int)textWidth - 16, tradeTextDrawY, 999999, -1, 999999, 1f, 0.88f, junimoText: false, -1, "", visualTheme.ItemRowTextColor);
          return;
        }

        bool hasEnoughToTrade = shopMenu.HasTradeItem(requiredItem, requiredItemCount);
        if (canPurchaseCheckFailed) {
          hasEnoughToTrade = false;
        }
        ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(requiredItem);
        Texture2D texture = dataOrErrorItem.GetTexture();
        Rectangle sourceRect = dataOrErrorItem.GetSourceRect();
        Utility.drawWithShadow(b, texture, new Vector2((float)(right - 88) - textWidth, tradeIconDrawY), sourceRect, Color.White * (hasEnoughToTrade ? 1f : 0.25f), 0f, Vector2.Zero, -1f, flipped: false, -1f, -1, -1, hasEnoughToTrade ? 0.35f : 0f);
        SpriteText.drawString(b, "x" + requiredItemCount, right - (int)textWidth - 16, tradeTextDrawY, 999999, -1, 999999, hasEnoughToTrade ? 1f : 0.5f, 0.88f, junimoText: false, -1, "", visualTheme.ItemRowTextColor);
      }
    }
  }

  static IEnumerable<CodeInstruction> ShopMenu_draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
    CodeMatcher matcher = new(instructions, generator);
    // Find i
    matcher.MatchStartForward(
        new(OpCodes.Ldc_I4_0),
        new(inst => inst.IsStloc()),
        new(OpCodes.Br)
        )
    .ThrowIfNotMatch($"Could not find 0th entry point for {nameof(ShopMenu_draw_Transpiler)}")
    .Advance(1);
    var iStInst = matcher.Instruction.Clone();
    // Find right
    // via: right = forSaleButton.bounds.Right;
    matcher.MatchEndForward(
        new(inst => inst.IsLdloc()),
        new(OpCodes.Ldflda, AccessTools.Field(typeof(ClickableComponent), nameof(ClickableComponent.bounds))),
        new(OpCodes.Call, AccessTools.PropertyGetter(typeof(Rectangle), nameof(Rectangle.Right))),
        new(inst => inst.IsStloc())
        )
    .ThrowIfNotMatch($"Could not find 1st entry point for {nameof(ShopMenu_draw_Transpiler)}");
    var rightStInst = matcher.Instruction.Clone();
    // Find tradeIconDrawY
    // via: tradeIconDrawY = forSaleButton.bounds.Y + 28 - 4;
    matcher.MatchEndForward(
        new(inst => inst.IsLdloc()),
        new(OpCodes.Ldflda, AccessTools.Field(typeof(ClickableComponent), nameof(ClickableComponent.bounds))),
        new(OpCodes.Ldfld, AccessTools.Field(typeof(Rectangle), nameof(Rectangle.Y))),
        new(OpCodes.Ldc_I4_S, (sbyte)28),
        new(OpCodes.Add),
        new(OpCodes.Ldc_I4_4),
        new(OpCodes.Sub),
        new(inst => inst.IsStloc())
        )
    .ThrowIfNotMatch($"Could not find 2nd entry point for {nameof(ShopMenu_draw_Transpiler)}");
    var tradeIconDrawYStInst = matcher.Instruction.Clone();
    // Find tradeTextDrawY
    // via: tradeTextDrawY = forSaleButton.bounds.Y + 44;
    // necessary? not really but eh too lazy for math:tm:
    matcher.MatchEndForward(
        new(inst => inst.IsLdloc()),
        new(OpCodes.Ldflda, AccessTools.Field(typeof(ClickableComponent), nameof(ClickableComponent.bounds))),
        new(OpCodes.Ldfld, AccessTools.Field(typeof(Rectangle), nameof(Rectangle.Y))),
        new(OpCodes.Ldc_I4_S, (sbyte)44),
        new(OpCodes.Add),
        new(inst => inst.IsStloc())
        )
    .ThrowIfNotMatch($"Could not find 3rd entry point for {nameof(ShopMenu_draw_Transpiler)}");
    var tradeTextDrawYStInst = matcher.Instruction.Clone();
    // Insert draw at the end of the loop
    matcher.MatchStartForward(
        new(inst => inst.IsLdloc()),
        new(OpCodes.Ldc_I4_1),
        new(OpCodes.Add),
        new(iStInst)
        );
    matcher
    .InsertAndAdvance(
      new(OpCodes.Ldarg_0),
      new(OpCodes.Ldarg_1),
      rightStInst.StToLd().LdToLda(),
      tradeIconDrawYStInst.StToLd(),
      tradeTextDrawYStInst.StToLd(),
      iStInst.StToLd(),
      new(OpCodes.Call, AccessTools.Method(typeof(Blacksmiths), nameof(DrawExtraRequirements))));
    //foreach (var m in matcher.InstructionEnumeration()) {
    //  ModEntry.StaticMonitor.Log($"{m.opcode} {m.operand}", LogLevel.Info);
    //}
    return matcher.InstructionEnumeration();
  }

  public static void Item_GetSalableInstance_Postfix(ISalable __result) {
    (__result as IHaveModData)?.modData.Remove(ExtraRequirements);
  }

  static void OnInventoryChanged(object? sender, InventoryChangedEventArgs e) {
    foreach (var item in e.Added) {
      item.modData.Remove(ExtraRequirements);
    }
  }
}

using System;
using System.Linq;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PropertyChanged.SourceGenerator;
using System.ComponentModel;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.FreshFarmProduce;

partial class FoodItemModel : INotifyPropertyChanged {
  [Notify] private bool visible = true;
  [Notify] private bool selected;
  public ParsedItemData Data;
  public Item Item;

  public string DisplayName => Data.DisplayName;

  public FoodItemModel(ParsedItemData data) {
    this.Data = data;
    this.Item = ItemRegistry.Create(data.QualifiedItemId);
  }

  public void UpdateVisibility(bool visible) {
    Visible = visible;
  }
  public void UpdateSelected(bool selected) {
    Selected = selected;
  }
}

partial class JojaDashTerminalModel : INotifyPropertyChanged {
  public string HeaderText = ModEntry.Helper.Translation.Get("JojaDash");
  public string OrderText = ModEntry.Helper.Translation.Get("JojaDash.order");
  public string LuckyText = ModEntry.Helper.Translation.Get("JojaDash.lucky");
  public string OrderTooltip =>
    SelectedItem == null ?
    ModEntry.Helper.Translation.Get("JojaDash.orderTooltipNotSelected") :
    ModEntry.Helper.Translation.Get("JojaDash.orderTooltip", new { foodName = SelectedItem.Item.DisplayName });
  public string LuckyTooltip = ModEntry.Helper.Translation.Get("JojaDash.luckyTooltip");

  public FoodItemModel[] FoodItems;
  [Notify] private FoodItemModel? selectedItem = null;

  [Notify] private string filter = "";

  public JojaDashTerminalModel() {
    // Ban magic rock candy, life elixir and oil of garlic
    string[] bannedItems = { "217", "772", "773", "279" };
    this.FoodItems =
      ItemRegistry.GetObjectTypeDefinition().GetAllData()
      .Where(data => data.Category == SObject.CookingCategory && !bannedItems.Contains(data.ItemId))
      .Select(data => new FoodItemModel(data))
      .ToArray();
  }

  private void OnFilterChanged() {
    foreach (var foodItem in FoodItems) {
      foodItem.UpdateVisibility(Filter.Length == 0 || foodItem.Data.DisplayName.Contains(Filter, StringComparison.CurrentCultureIgnoreCase)); ;
    }
  }

  public void ToggleSelect(FoodItemModel item) {
    Game1.playSound("dwop");
    var oldSelectedItem = selectedItem;
    if (item == selectedItem) {
      SelectedItem = null;
    } else {
      SelectedItem = item;
    }
    item.UpdateSelected(selectedItem == item);
    oldSelectedItem?.UpdateSelected(selectedItem == oldSelectedItem);
  }

  public void Order() {
    if (selectedItem is null) return;
    Game1.player.playNearbySoundAll("wand");
    if (!ModEntry.Config.DisableFlash) {
      Game1.flashAlpha = 1f;
    }
    var food = selectedItem.Item.getOne();
    food.modData[JojaMealKey] = "true";
    Game1.player.addItemByMenuIfNecessary(food);
    Game1.player.mailReceived.Add(JojaDashPhoneHandler.JojaDashUsed);
    Game1.activeClickableMenu.exitThisMenu();
  }

  public void OrderRandom() {
    var randomItem = Game1.random.ChooseFrom(FoodItems);
    Game1.player.playNearbySoundAll("wand");
    if (!ModEntry.Config.DisableFlash) {
      Game1.flashAlpha = 1f;
    }
    var food = randomItem.Item.getOne();
    food.modData[JojaMealKey] = "true";
    Game1.player.addItemByMenuIfNecessary(food);
    Game1.player.mailReceived.Add(JojaDashPhoneHandler.JojaDashUsed);
    Game1.activeClickableMenu.exitThisMenu();
  }

  public static string JojaMealKey = $"{ModEntry.UniqueId}.JojaMeal";
}

using System;
using System.Linq;
using System.Collections.Generic;
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

partial class FoodItemModel {
  private JojaDashTerminalModel terminal;
  public ParsedItemData Data;
  public bool Visible =>
    terminal.IsItemVisible(Data);
  public Texture2D? BorderSprite =>
    terminal.IsSelected(Data) ? Game1.content.Load<Texture2D>("Mods/StardewUI/Sprites/ControlBorder") : null;
  public string DisplayName => Data.DisplayName;

  public FoodItemModel(ParsedItemData data, JojaDashTerminalModel terminal) {
    this.Data = data;
    this.terminal = terminal;
  }
}

partial class JojaDashTerminalModel : INotifyPropertyChanged {
  public string HeaderText = ModEntry.Helper.Translation.Get("JojaDash");
  public string OrderText = ModEntry.Helper.Translation.Get("JojaDash.order");
  public string LuckyText = ModEntry.Helper.Translation.Get("JojaDash.lucky");

  public FoodItemModel[] FoodItems;

  [Notify] private string filter = "";
  [Notify] private ParsedItemData? selectedItem = null;

  public JojaDashTerminalModel() {
    this.FoodItems =
      ItemRegistry.GetObjectTypeDefinition().GetAllData()
      .Where(data => data.Category == SObject.CookingCategory && data.QualifiedItemId != "(O)279")
      .Select(data => new FoodItemModel(data, this))
      .ToArray();
  }

  public bool IsItemVisible(ParsedItemData data) {
    return Filter.Length == 0 || data.DisplayName.Contains(Filter, StringComparison.CurrentCultureIgnoreCase);
  }

  public bool IsSelected(ParsedItemData data) {
    return SelectedItem == data;
  }

  public void ToggleSelect(ParsedItemData data) {
    if (SelectedItem == data) {
      SelectedItem = null;
    } else {
      SelectedItem = data;
    }
  }

  public void Order() {
    if (SelectedItem is null) return;
    Game1.player.playNearbySoundAll("wand");
    Game1.flashAlpha = 1f;
    Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create(SelectedItem.QualifiedItemId));
    //Game1.player.mailReceived.Add(JojaDashPhoneHandler.JojaDashUsed);
    Game1.exitActiveMenu();
  }

  public void OrderRandom() {
    var randomItem = Game1.random.ChooseFrom(FoodItems);
    Game1.player.playNearbySoundAll("wand");
    Game1.flashAlpha = 1f;
    Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create(randomItem.Data.QualifiedItemId));
    //Game1.player.mailReceived.Add(JojaDashPhoneHandler.JojaDashUsed);
    Game1.exitActiveMenu();
  }
}

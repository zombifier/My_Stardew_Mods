#nullable enable

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewValley.Menus;
using StardewValley.Inventories;

#if IS_BETTER_CRAFTING

using Leclair.Stardew.Common.Inventory;
using Leclair.Stardew.Common.Crafting;
using Leclair.Stardew.BetterCrafting.DynamicRules;
using Leclair.Stardew.BetterCrafting.Models;

namespace Leclair.Stardew.BetterCrafting;

#else

using StardewValley.Network;

namespace Leclair.Stardew.BetterCrafting;

/// <summary>
/// An <c>IBCInventory</c> represents an item storage that
/// Better Crafting is interacting with, whether by extracting
/// items or inserting them.
/// </summary>
public interface IBCInventory {

  /// <summary>
  /// Optional. If this inventory is associated with an object, that object.
  /// </summary>
  object Object { get; }

  /// <summary>
  /// If this inventory is associated with an object, where that object is located.
  /// </summary>
  GameLocation? Location { get; }

  /// <summary>
  /// If this inventory is associated with a player, the player.
  /// </summary>
  Farmer? Player { get; }

  /// <summary>
  /// If this inventory is managed by a NetMutex, or an object with one,
  /// which should be locked before manipulating the inventory, then
  /// provide it here.
  /// </summary>
  NetMutex? Mutex { get; }

  /// <summary>
  /// Get this inventory as a vanilla IInventory, if possible. May
  /// be null if the inventory is not a vanilla inventory.
  /// </summary>
  IInventory? Inventory { get; }

  /// <summary>
  /// Whether or not the inventory is locked and ready for read/write usage.
  /// </summary>
  bool IsLocked();

  /// <summary>
  /// Whether or not the inventory is a valid inventory.
  /// </summary>
  bool IsValid();

  /// <summary>
  /// Whether or not we can insert items into this inventory.
  /// </summary>
  bool CanInsertItems();

  /// <summary>
  /// Whether or not we can extract items from this inventory.
  /// </summary>
  bool CanExtractItems();

  /// <summary>
  /// For inventories associated with multiple tile regions in a location,
  /// such as a farm house kitchen, this is the region the inventory fills.
  /// Only rectangular shapes are supported. This is used for discovering
  /// connections to nearby inventories.
  /// </summary>
  Rectangle? GetMultiTileRegion();

  /// <summary>
  /// For inventories associated with a tile position in a location, such
  /// as a chest placed in the world.
  /// 
  /// For multi-tile inventories, this should be the primary tile if
  /// one exists.
  /// </summary>
  Vector2? GetTilePosition();

  /// <summary>
  /// Get this inventory as a list of items. May be null if
  /// there is an issue accessing the object's inventory.
  /// </summary>
  IList<Item?>? GetItems();

  /// <summary>
  /// Check to see if a specific item is allowed to be stored in
  /// this inventory.
  /// </summary>
  /// <param name="item">The item we're checking</param>
  bool IsItemValid(Item item);

  /// <summary>
  /// Attempt to clean the object's inventory. This should remove null
  /// entries, and run any other necessary logic.
  /// </summary>
  void CleanInventory();

  /// <summary>
  /// Get the number of item slots in the object's inventory. When adding
  /// items to the inventory, we will never extend the list beyond this
  /// number of entries.
  /// </summary>
  int GetActualCapacity();

}

/// <summary>
/// An <c>IIngredient</c> represents a single ingredient used when crafting a
/// recipe. An ingredient can be an item, a currency, or anything else.
///
/// The API provides methods for getting basic item and currency ingredients,
/// so you need not use this unless you're doing something fancy.
/// </summary>
public interface IIngredient {
  /// <summary>
  /// Whether or not this <c>IIngredient</c> supports quality control
  /// options, including using low quality first and limiting the maximum
  /// quality to use.
  /// </summary>
  bool SupportsQuality { get; }

  // Display
  /// <summary>
  /// The name of this ingredient to be displayed in the menu.
  /// </summary>
  string DisplayName { get; }

  /// <summary>
  /// The texture to use when drawing this ingredient in the menu.
  /// </summary>
  Texture2D Texture { get; }

  /// <summary>
  /// The source rectangle to use when drawing this ingredient in the menu.
  /// </summary>
  Rectangle SourceRectangle { get; }

  #region Quantity

  /// <summary>
  /// The amount of this ingredient required to perform a craft.
  /// </summary>
  int Quantity { get; }

  /// <summary>
  /// Determine how much of this ingredient is available for crafting both
  /// in the player's inventory and in the other inventories.
  /// </summary>
  /// <param name="who">The farmer performing the craft</param>
  /// <param name="items">A list of all available <see cref="Item"/>s across
  /// all available <see cref="IBCInventory"/> instances. If you only support
  /// consuming ingredients from certain <c>IBCInventory</c> types, you should
  /// not use this value and instead iterate over the inventories. Please
  /// note that this does <b>not</b> include the player's inventory.</param>
  /// <param name="inventories">All the available inventories.</param>
  /// <param name="maxQuality">The maximum item quality we are allowed to
  /// count. This cannot be ignored unless <see cref="SupportsQuality"/>
  /// returns <c>false</c>.</param>
  int GetAvailableQuantity(Farmer who, IList<Item?>? items, IList<IBCInventory>? inventories, int maxQuality);

  #endregion

  #region Consumption

  /// <summary>
  /// Consume this ingredient out of the player's inventory and the other
  /// available inventories.
  /// </summary>
  /// <param name="who">The farmer performing the craft</param>
  /// <param name="inventories">All the available inventories.</param>
  /// <param name="maxQuality">The maximum item quality we are allowed to
  /// count. This cannot be ignored unless <see cref="SupportsQuality"/>
  /// returns <c>false</c>.</param>
  /// <param name="lowQualityFirst">Whether or not we should make an effort
  /// to consume lower quality ingredients before consuming higher quality
  /// ingredients.</param>
  void Consume(Farmer who, IList<IBCInventory>? inventories, int maxQuality, bool lowQualityFirst);

  #endregion
}

/// <summary>
/// This event is dispatched by Better Crafting whenever a player performs a
/// craft, and may be fired multiple times in quick succession if a player is
/// performing bulk crafting.
/// </summary>
public interface IPerformCraftEvent {

  /// <summary>
  /// The player performing the craft.
  /// </summary>
  Farmer Player { get; }

  /// <summary>
  /// The item being crafted, may be null depending on the recipe.
  /// </summary>
  Item? Item { get; set; }

  /// <summary>
  /// The <c>BetterCraftingPage</c> menu instance that the player is
  /// crafting from.
  /// </summary>
  IClickableMenu Menu { get; }

  /// <summary>
  /// Cancel the craft, marking it as a failure. The ingredients will not
  /// be consumed and the player will not receive the item.
  /// </summary>
  void Cancel();

  /// <summary>
  /// Complete the craft, marking it as a success. The ingredients will be
  /// consumed and the player will receive the item, if there is one.
  /// </summary>
  void Complete();

}

/// <summary>
/// An extended IPerformCraftEvent subclass that also includes a
/// reference to the recipe being used. This is necessary because
/// adding this to the existing model would break Pintail proxying,
/// for some reason.
/// </summary>
public interface IGlobalPerformCraftEvent : IPerformCraftEvent {

  /// <summary>
  /// The recipe being crafted.
  /// </summary>
  IRecipe Recipe { get; }

}

/// <summary>
/// This event is dispatched by Better Crafting whenever a
/// craft has been completed, and may be used to modify
/// the finished Item, if there is one, before the item is
/// placed into the player's inventory. At this point
/// the craft has been finalized and cannot be canceled.
/// </summary>
public interface IPostCraftEvent {

  /// <summary>
  /// The recipe being crafted.
  /// </summary>
  IRecipe Recipe { get; }

  /// <summary>
  /// The player performing the craft.
  /// </summary>
  Farmer Player { get; }

  /// <summary>
  /// The item being crafted, may be null depending on the recipe.
  /// Can be changed.
  /// </summary>
  Item? Item { get; set; }

  /// <summary>
  /// The <c>BetterCraftingPage</c> menu instance that the player
  /// is crafting from.
  /// </summary>
  IClickableMenu Menu { get; }

  /// <summary>
  /// A list of ingredient items that were consumed during the
  /// crafting process. This may not contain all items.
  /// </summary>
  List<Item> ConsumedItems { get; }

}

/// <summary>
/// An <c>IRecipe</c> represents a single crafting recipe, though it need not
/// be associated with a vanilla <see cref="StardewValley.CraftingRecipe"/>.
/// Recipes usually produce <see cref="Item"/>s, but they are not required
/// to do so.
/// </summary>
public interface IRecipe {

  #region Identity

  /// <summary>
  /// An additional sorting value to apply to recipes in the Better Crafting
  /// menu. Applied before other forms of sorting.
  /// </summary>
  string SortValue { get; }

  /// <summary>
  /// The internal name of the recipe. For standard recipes, this matches the
  /// name of the recipe used in the player's cookingRecipes / craftingRecipes
  /// dictionaries. For non-standard recipes, this can be anything as long as
  /// it's unique, and it's recommended to prefix the names with your mod's
  /// unique ID to ensure uniqueness.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// A name displayed to the user.
  /// </summary>
  string DisplayName { get; }

  /// <summary>
  /// An optional description of the recipe displayed on its tool-tip.
  /// </summary>
  string? Description { get; }

  /// <summary>
  /// Whether or not this recipe can be reversed with recycling.
  /// </summary>
  bool AllowRecycling { get; }

  /// <summary>
  /// Whether or not the player knows this recipe.
  /// </summary>
  /// <param name="who">The player we're asking about</param>
  bool HasRecipe(Farmer who);

  /// <summary>
  /// How many times the player has crafted this recipe. If advanced crafting
  /// information is enabled, and this value is non-zero, it will be
  /// displayed on recipe tool-tips.
  /// </summary>
  /// <param name="who">The player we're asking about.</param>
  int GetTimesCrafted(Farmer who);

  /// <summary>
  /// The vanilla <c>CraftingRecipe</c> instance for this recipe, if one
  /// exists. This may be used for interoperability with some other
  /// mods, but is not required.
  /// </summary>
  CraftingRecipe? CraftingRecipe { get; }

  #endregion

  #region Display

  /// <summary>
  /// The texture to use when drawing this recipe in the menu.
  /// </summary>
  Texture2D Texture { get; }

  /// <summary>
  /// The source rectangle to use when drawing this recipe in the menu.
  /// </summary>
  Rectangle SourceRectangle { get; }

  /// <summary>
  /// How tall this recipe should appear in the menu, in grid squares.
  /// </summary>
  int GridHeight { get; }

  /// <summary>
  /// How wide this recipe should appear in the menu, in grid squares.
  /// </summary>
  int GridWidth { get; }

  #endregion

  #region Cost and Quantity

  /// <summary>
  /// The quantity of item produced every time this recipe is crafted.
  /// </summary>
  int QuantityPerCraft { get; }

  /// <summary>
  /// The ingredients used by this recipe.
  /// </summary>
  IIngredient[]? Ingredients { get; }

  #endregion

  #region Creation

  /// <summary>
  /// Whether or not the item created by this recipe is stackable, and thus
  /// eligible for bulk crafting.
  /// </summary>
  bool Stackable { get; }

  /// <summary>
  /// Check to see if the given player can currently craft this recipe. This
  /// method is suitable for checking external conditions. For example, the
  /// add-on for crafting buildings from the crafting menu uses this to check
  /// that the current <see cref="GameLocation"/> allows building.
  /// </summary>
  /// <param name="who">The player we're asking about.</param>
  bool CanCraft(Farmer who);

  /// <summary>
  /// An optional, extra string to appear on item tool-tips. This can be used
  /// for displaying error messages to the user, or anything else that would
  /// be relevant. For example, the add-on for crafting buildings uses this
  /// to display error messages telling users why they are unable to craft
  /// a building, if they cannot.
  /// </summary>
  /// <param name="who">The player we're asking about.</param>
  string? GetTooltipExtra(Farmer who);

  /// <summary>
  /// Create an instance of the Item this recipe crafts, if this recipe
  /// crafts an item. Returning null is perfectly acceptable.
  /// </summary>
  Item? CreateItem();

  /// <summary>
  /// This method is called when performing a craft, and can be used to
  /// perform asynchronous actions or other additional logic as required.
  /// While crafting is taking place, Better Crafting will hold locks on
  /// every inventory involved. You should ideally do as little work
  /// here as possible.
  /// </summary>
  /// <param name="evt">Details about the event, and methods for telling
  /// Better Crafting when the craft has succeeded or failed.</param>
  void PerformCraft(IPerformCraftEvent evt) {
    evt.Complete();
  }

  #endregion
}

#endif

public interface IBetterCrafting {

  /// <summary>
  /// This event is fired whenever a player crafts an item using
  /// Better Crafting. This fires before <see cref="IRecipe.PerformCraft(IPerformCraftEvent)" />
  /// to allow generic events to cancel before specific events go off.
  /// </summary>
  event Action<IGlobalPerformCraftEvent>? PerformCraft;

  /// <summary>
  /// This event is fired whenever a player crafts an item using
  /// Better Crafting, once the craft is finished but before the
  /// item is given to the player. This happens after
  /// <see cref="IPostCraftEventRecipe.PostCraft(IPostCraftEvent)"/>.
  /// </summary>
  event Action<IPostCraftEvent>? PostCraft;
}

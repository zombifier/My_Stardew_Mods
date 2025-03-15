using StardewValley;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace ItemBags {
  /// <summary>
  /// If you are not referencing the ItemBags dll, then just delete the API calls that require "ItemBag" instances 
  /// and use the alternative API calls that take in the more general "Item" parameters instead.
  /// </summary>
  public interface IItemBagsAPI
  {
    /// <summary>Added in v1.4.3.</summary>
    /// <returns>True if the given item is an ItemBag.</returns>
    bool IsItemBag(Item item);
    /// <summary>Added in v1.4.3. Returns all items in the given <paramref name="source"/> list that are ItemBag instances.</summary>
    /// <param name="source">The set of items to search</param>
    IList<StardewValley.Tools.GenericTool> GetItemBags(IList<Item> source);

    /// <summary>Added in v1.4.3. Returns all <see cref="StardewValley.Object"/> items that are stored inside of the given bag.</summary>
    /// <param name="bag">This Item must be an ItemBag instance. If you are not referencing the ItemBags dll, consider using <see cref="IsItemBag(Item)"/> to check if a particular Item is a bag.</param>
    /// <param name="includeNestedBags">OmniBags are specialized types of bags that can hold other bags inside of them. If includeNestedBags is true, then items inside of bags that are nested within Omnibags will also be included in the result list.</param>
    IList<SObject> GetObjectsInsideBag(Item bag, bool includeNestedBags);

    /// <summary>Added in v1.4.2. Returns all <see cref="StardewValley.Object"/> items that are stored inside of any bags found in the given <paramref name="source"/> list.</summary>
    /// <param name="source">The list of items that will be searched for bags.</param>
    /// <param name="includeNestedBags">OmniBags are specialized types of bags that can hold other bags inside of them. If includeNestedBags is true, then items inside of bags that are nested within Omnibags will also be included in the result list.</param>
    IList<SObject> GetObjectsInsideBags(IList<Item> source, bool includeNestedBags);
  }
}

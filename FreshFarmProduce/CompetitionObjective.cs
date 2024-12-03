using System;
using System.Linq;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Netcode;
using StardewValley;
using StardewValley.Network;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using StardewValley.Inventories;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.FreshFarmProduce;

// Keeps track of any shipped items. The item ID is part of the key.
[XmlType("Mods_selph.FreshFarmProduce_ShippedItemEntry")]
public class ShippedItemEntry : INetObject<NetFields> {
  [XmlIgnore]
  public NetFields NetFields { get; } = new NetFields(nameof(ShippedItemEntry));

  public NetStringHashSet flavors = ["-1"];
  public NetInt points = new(0);

  public ShippedItemEntry() {
    this.NetFields.SetOwner(this);
    this.NetFields
      .AddField(this.flavors, "flavors")
      .AddField(this.points, "points");
  }
}

// An extension of the "ship items" special order objective that keeps track of all shipped items,
// calculates points instead of count/price, and prevents any one item from
// contributing too many points.
[XmlType("Mods_selph.FreshFarmProduce_CompetitionObjective")]
public class ShipPointsObjective : ShipObjective {
  public NetString Id = new NetString();

  // A dictionary of item qualified IDs to their shipped data,
  // including accumulated points and list of unique flavors shipped
  public NetStringDictionary<ShippedItemEntry, NetRef<ShippedItemEntry>> shippedItems = new();

	public override void InitializeNetFields()
	{
		base.InitializeNetFields();
		base.NetFields
      .AddField(this.Id, "Id")
      .AddField(this.shippedItems, "shippedItems");
	}

  public ShipPointsObjective() {}
  public ShipPointsObjective(string id, bool useSalePrice = false) : base() {
    if (ModEntry.competitionDataAssetHandler.data.Categories.TryGetValue(id, out var categoryData)) {
      this.Id.Value = id;
      this.maxCount.Value = (int)(categoryData.TotalPoints * Utils.GetFameDifficultyModifier() * Utils.GetRandomDifficultyModifier());
      this.description.Value = categoryData.Name;
    } else {
      ModEntry.StaticMonitor.Log($"WARNING: Unknown objective ID found: {id}. This may have weird effects.", LogLevel.Warn);
    }
    this.useShipmentValue.Value = useSalePrice;
  }

  // Reduce any points above maxPoints by 75%.
  // For example, if maxPoints = 1000:
  // 600 => 600
  // 1200 => 1050
  static int NormalizePoints(int points, int maxPoints) {
    return Math.Min(points, maxPoints) + Math.Max((points - maxPoints) / 4, 0);
  }

  // For every unique flavors increase the threshold by 0.05
  static int GetThreshold(int threshold, int uniqueFlavors) {
    return (int)(threshold * (1f + 0.05f * (float)Math.Max(0, uniqueFlavors - 1)));
  }

  public int GetPointsFor(ShippedItemEntry entry) {
    return NormalizePoints(entry.points.Value, GetThresholdFor(entry));
  }

  public int GetThresholdFor(ShippedItemEntry entry) {
    if (ModEntry.competitionDataAssetHandler.data.Categories.TryGetValue(this.Id.Value, out var categoryData)) {
      return GetThreshold(categoryData.MaxIndividualPoints, entry.flavors.Count);
    }
    return 0;
  }

  // Duplicating fair logic here
  public int CalculatePoints(Item shippedItem) {
    int points = shippedItem.Quality + 1;
    int stack = shippedItem.Stack;
    var item = shippedItem.getOne();
    int price = item.sellToStorePrice(-1L);
    if (price >= 20) {
      points++;
    }
    if (price >= 90) {
      points++;
    }
    if (price >= 200) {
      points++;
    }
    if (price >= 300 && item.Quality < 2) {
      points++;
    }
    if (price >= 400 && item.Quality < 1) {
      points++;
    }
    points *= stack;
    return points;
  }

  public void UpdatePoints() {
    if (ModEntry.competitionDataAssetHandler.data.Categories.TryGetValue(this.Id.Value, out var categoryData)) {
      this.SetCount(
          this.shippedItems.Values.Sum(item => GetPointsFor(item)));
    }
  }
  
  public bool CanAcceptThisItem(Item shippedItem, Farmer farmer) {
    if (ModEntry.competitionDataAssetHandler.data.Categories.TryGetValue(this.Id.Value, out var categoryData)) {
      return (categoryData.ItemCriterias is null) ||
        (categoryData.ItemCriterias.Any((ItemCriteria criteria) => {
          if (criteria.ItemIds is not null && !criteria.ItemIds.Contains(shippedItem.QualifiedItemId)) {
            return false;
          }
          if (criteria.ContextTags is not null && !ItemContextTagManager.DoAllTagsMatch(criteria.ContextTags, shippedItem.GetContextTags())) {
            return false;
          }
          if (criteria.Condition is not null && !GameStateQuery.CheckConditions(criteria.Condition, player: farmer, targetItem: shippedItem)) {
            return false;
          }
          return true;
        }));
    }
    return false;
  }

  public override void OnItemShipped(StardewValley.Farmer farmer, StardewValley.Item shippedItem, int shipped_price) {
    if (ModEntry.competitionDataAssetHandler.data.Categories.TryGetValue(this.Id.Value, out var categoryData)) {
      if (!CanAcceptThisItem(shippedItem, farmer)) {
        return;
      }
      var addedPoints = this.useShipmentValue.Value ? shipped_price : CalculatePoints(shippedItem);

      if (!shippedItems.ContainsKey(shippedItem.QualifiedItemId)) {
        shippedItems[shippedItem.QualifiedItemId] = new();
      }
      shippedItems[shippedItem.QualifiedItemId].points.Value += addedPoints;
      if (shippedItem is SObject shippedObject && shippedObject.preservedParentSheetIndex.Value is not null) {
        shippedItems[shippedItem.QualifiedItemId].flavors.Add(shippedObject.preservedParentSheetIndex.Value);
      }
      // TODO: Ehh this is not optimized but it's not like it's particularly expensive
      this.UpdatePoints();
    } else {
      ModEntry.StaticMonitor.Log($"WARNING: Unknown objective ID found: {this.Id}. This may have weird effects.", LogLevel.Warn);
    }
  }
}

using System;
using System.Linq;
using System.Collections.Generic;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.FreshFarmProduce;

class ObjectiveEntryModel {
  private ShippedItemEntry entry { get; }
  private ShipPointsObjective objective { get; }
  private string ItemId { get; }

  public ObjectiveEntryModel(ShipPointsObjective objective, string itemId, ShippedItemEntry entry) {
    this.ItemId = itemId;
    this.objective = objective;
    this.entry = entry;
    this.Data = ItemRegistry.GetDataOrErrorItem(ItemId);
  }

  public int TotalPoints { get => objective.GetThresholdFor(entry);  }
  public bool HasThreshold { get => TotalPoints <= 1000000;  }
  public int Points { get => objective.GetPointsFor(entry);  }
  public ParsedItemData Data { get; }
  public string ItemName { get => Data.DisplayName; }
  public Color PointsColor { get => Points >= TotalPoints ? Color.Black : Color.Green; }
}

class ObjectiveModel {
  private CategoryData categoryData;
  private Texture2D texture;
  private ShipPointsObjective objective { get; }

  public int TotalPoints { get => objective.GetMaxCount(); }
  public int Points { get => objective.GetCount(); }
  public string Name { get => categoryData.Name; }
  public string Description { get => categoryData.Description; }
  public Tuple<Texture2D, Rectangle> Sprite {
    get => new(texture, Game1.getSourceRectForStandardTileSheet(texture, categoryData.SpriteIndex, 16, 16));
  }
  public Color BarColor {
    get {
      if (Points >= TotalPoints) {
        return Color.Green;
      } else if (Points >= TotalPoints / 2) {
        return Color.Yellow;
      } else {
        return Color.Red;
      }
    }
  }
  public Color TextColor {
    get {
      if (Points >= TotalPoints) {
        return Color.Green;
      } else if (Points >= TotalPoints / 2) {
        return Color.DarkGreen;
      } else {
        return Color.Black;
      }
    }
  }
  public ObjectiveEntryModel[] Entries { get; }
  public string BarPercentage { get => $"{(float)Points/TotalPoints*100}% stretch"; }
  public Texture2D BarTexture { get => Game1.staminaRect; }

  public ObjectiveModel(ShipPointsObjective objective) {
    this.objective = objective;
    this.categoryData = ModEntry.competitionDataAssetHandler.data.Categories[objective.Id.Value];
    this.texture = Game1.content.Load<Texture2D>(categoryData.Texture);
    this.Entries = objective.shippedItems.Pairs.Select((KeyValuePair<string, ShippedItemEntry> pair) => new ObjectiveEntryModel(objective, pair.Key, pair.Value)).ToArray();
  }
}

internal record CompetitionTrackerViewModel(
    string HeaderText,
    ObjectiveModel[] Objectives) {
    public static CompetitionTrackerViewModel Load() {
        var specialOrder = Game1.player.team.specialOrders.FirstOrDefault((SpecialOrder so) => so.questKey.Value == ModEntry.FarmCompetitionSpecialOrderId, null);
        if (specialOrder is not null) {
          return new(ModEntry.Helper.Translation.Get("CompetitionName"),
                specialOrder.objectives
                .Where((OrderObjective oo) => oo is ShipPointsObjective)
                .Select((OrderObjective oo) => new ObjectiveModel((oo as ShipPointsObjective)!))
                .ToArray());
        } else {
          return new(ModEntry.Helper.Translation.Get("CompetitionName"), []);
        }
    }
}

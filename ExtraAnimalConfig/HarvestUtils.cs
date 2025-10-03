using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Inventories;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Selph.StardewMods.ExtraAnimalConfig;

class VirtualJunimo : JunimoHarvester {
  FarmAnimal animal;
  //public int farmingExp;
  public VirtualJunimo(FarmAnimal animal) {
    this.animal = animal;
    // these fields are required
    this.currentLocation = animal.currentLocation;
    this.position.Value = new(-1, -1);
  }

  public override void tryToAddItemToHut(Item i) {
    ModEntry.StaticMonitor.Log($"Harvesting {i.QualifiedItemId}");
    HarvestUtils.GetAnimalHarvestChest(this.animal).Add(i);
    // No exp for u :(
    //int price = 0;
    //if (i is SObject obj) {
    //  price = obj.Price;
    //}
    //float experience = (float)(16.0 * Math.Log(0.018 * (double)price + 1.0, Math.E));
    //this.farmingExp = (int)experience;
  }
}

class AnimalHarvestData {
  public int ticksSinceHarvest = 0;
  public VirtualJunimo virtualJunimo;
  public AnimalHarvestData(FarmAnimal animal) {
    virtualJunimo = new VirtualJunimo(animal);
  }
}

class HarvestUtils {
  static ConditionalWeakTable<FarmAnimal, AnimalHarvestData> AnimalHarvestDict = new();

  static AnimalHarvestData GetAnimalHarvestData(FarmAnimal animal) {
    return AnimalHarvestDict.GetValue(animal, (a) => new(a));
  }

  public static Inventory GetAnimalHarvestChest(FarmAnimal animal) {
    return Game1.player.team.GetOrCreateGlobalInventory($"{ModEntry.UniqueId}_AnimalInventoryId_{animal.myID.Value}");
  }

  static bool foundCropEndFunction(PathNode currentNode, Point endPoint, GameLocation location, Character c) {
    if (location.terrainFeatures.TryGetValue(new Vector2(currentNode.x, currentNode.y), out var value)) {
      if (location.isCropAtTile(currentNode.x, currentNode.y) && (value as HoeDirt)?.readyForHarvest() is true) {
        return true;
      }
      if (value is Bush bush && bush.readyForHarvest()) {
        return true;
      }
    }
    return false;
  }

  public static void AnimalHarvest(FarmAnimal animal, GameTime time, ref bool result) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
        animalExtensionData.IsHarvester) {
      var animalHarvestData = GetAnimalHarvestData(animal);
      // Still on cooldown; exit
      if (animalHarvestData.ticksSinceHarvest > 0) {
        animalHarvestData.ticksSinceHarvest -= 1;
        return;
      }
      // Begin controller
      if (animal.controller is null) {
        var harvestDistance = animalExtensionData.HarvestRange;
        //var controller = PathFindController(animal, animal.currentLocation, victim.TilePoint, Game1.random.Next(4));
        var controller = new PathFindController(animal, animal.currentLocation,
            (PathNode currentNode, Point endPoint, GameLocation location, Character c) => {
              if (location.terrainFeatures.TryGetValue(new Vector2(currentNode.x, currentNode.y), out var t)) {
                if (t is HoeDirt hoeDirt && hoeDirt.crop is not null && hoeDirt.readyForHarvest()) {
                  return true;
                }
                if (t is Bush bush && bush.readyForHarvest()) {
                  return true;
                }
              }
              return false;
            },
            -1,
            (Character c, GameLocation l) => {
              animalHarvestData.ticksSinceHarvest = (int)(animalExtensionData.HarvestInterval / (1000f / 60));
              if (l.terrainFeatures.TryGetValue(c.Tile, out var t)) {
                bool playAnimation = false;
                if (t is HoeDirt hoeDirt && hoeDirt.crop is not null && hoeDirt.readyForHarvest()) {
                  if (hoeDirt.crop.harvest((int)c.Tile.X, (int)c.Tile.Y, hoeDirt, animalHarvestData.virtualJunimo)) {
                    hoeDirt.destroyCrop(true);
                  }
                  playAnimation = true;
                }
                if (t is Bush bush && bush.readyForHarvest()) {
                  var harvestItem =
                    (ModEntry.cbApi?.TryGetShakeOffItem(bush, out var bushHarvest) ?? false) ? bushHarvest : ItemRegistry.Create("(O)815");
                  animalHarvestData.virtualJunimo.tryToAddItemToHut(harvestItem);
                  playAnimation = true;
                }
                if (playAnimation && animal.currentLocation == Game1.currentLocation) {
                  var animalData = animal.GetAnimalData();
                  int num = 16;
                  if (!animal.Sprite.textureUsesFlippedRightForLeft) {
                    num += 4;
                  }
                  if (animalData?.UseDoubleUniqueAnimationFrames ?? false) {
                    num += 4;
                  }
                  c.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>() {
                      new FarmerSprite.AnimationFrame(num, 100),
                      new FarmerSprite.AnimationFrame(num + 1, 100),
                      new FarmerSprite.AnimationFrame(num + 2, 100),
                      new FarmerSprite.AnimationFrame(num + 3, 100),
                      });
                  c.Sprite.loop = false;
                  animal.currentLocation.playSound("harvest", animal.Tile);
                }
              }
              c.controller = null;
            }, 100, Point.Zero);
        var endPoint = controller.pathToEndPoint?.Last();
        var originForDistance = animal.home is not null
          ? new Vector2(animal.home.tileX.Value + animal.home.animalDoor.X, animal.home.tileY.Value + animal.home.animalDoor.Y)
          : animal.Tile;
        if (endPoint is not null &&
            Vector2.Distance(originForDistance, endPoint.Value.ToVector2()) <= harvestDistance) {
          animal.controller = controller;
        } else {
          // Go on cooldown to avoid expensive calcs
          animalHarvestData.ticksSinceHarvest = 600;
        }
      }
    }
  }

  // returns false if the original function should be skipped
  public static bool DropHarvestIfAvailable(FarmAnimal animal, Farmer who) {
    if (ModEntry.animalExtensionDataAssetHandler.data.TryGetValue(animal.type.Value ?? "", out var animalExtensionData) &&
        animalExtensionData.IsHarvester) {
      var chest = HarvestUtils.GetAnimalHarvestChest(animal);
      if (chest.Count > 0) {
        foreach (var item in chest) {
          Game1.createItemDebris(item, animal.getStandingPosition(), -1, animal.currentLocation);
        }
        chest.Clear();
        return false;
      }
    }
    return true;
  }
}

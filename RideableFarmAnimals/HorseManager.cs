using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Characters;
using System.Runtime.CompilerServices;

namespace Selph.StardewMods.RideableFarmAnimals;

class HorseManager {

  static string IsFakeHorseKey = $"{ModEntry.UniqueId}_IsFakeHorse";
  static string InvisibleKey = $"{ModEntry.UniqueId}_Invisible";
  static string OwnerIdKey = $"{ModEntry.UniqueId}_OwnerId";
  static string SpeedModifierKey = $"{ModEntry.UniqueId}_SpeedModifier";

  static Vector2 HorsePositionOffset = new(32, 56);
  static Vector2 SmallHorsePositionOffset = new(-8, 8);

  public static bool IsFakeHorse(Horse horse) {
    return horse.modData.ContainsKey(IsFakeHorseKey);
  }

  //static ConditionalWeakTable<FarmAnimal, Horse> animalToFakeHorse = new();
  static ConditionalWeakTable<Horse, FarmAnimal> fakeHorseToAnimal = new();
  static ConditionalWeakTable<Horse, AnimatedSprite> fakeHorseToSprite = new();

  public static void RideAnimal(Farmer who, FarmAnimal farmAnimal) {
    var horse = new Horse(Guid.NewGuid(), (int)farmAnimal.Tile.X, (int)farmAnimal.Tile.Y);
    var isSmallAnimal = farmAnimal.Sprite.SpriteWidth <= 16;
    horse.modData[IsFakeHorseKey] = "true";
    horse.hideFromAnimalSocialMenu.Value = true;
    // Don't use ownerId so as to not interfere with "real" horses
    horse.modData[OwnerIdKey] = who.UniqueMultiplayerID.ToString(); ;
    fakeHorseToAnimal.AddOrUpdate(horse, farmAnimal);
    fakeHorseToSprite.AddOrUpdate(horse, farmAnimal.Sprite.Clone());
    horse.Position = farmAnimal.Position + (isSmallAnimal ? SmallHorsePositionOffset : HorsePositionOffset);
    horse.FacingDirection = farmAnimal.FacingDirection;
    //horse.Sprite = farmAnimal.Sprite.Clone();
    if (farmAnimal.Sprite.SpriteWidth <= 16) {
      horse.drawOffset += new Vector2(16f, 0);
    }
    //if (!farmAnimal.currentLocation.characters.Contains(horse)) {
    farmAnimal.currentLocation.characters.Add(horse);
    //}
    if (horse.checkAction(who, who.currentLocation)) {
      Hide(farmAnimal);
    } else {
      ModEntry.StaticMonitor.Log($"Can't mount {farmAnimal.type} for some reason?", LogLevel.Info);
      farmAnimal.currentLocation.characters.Remove(horse);
    }
  }

  public static void DismountAnimal(Horse horse, Farmer who, GameLocation location) {
    if (fakeHorseToAnimal.TryGetValue(horse, out var farmAnimal)) {
      var isSmallAnimal = farmAnimal.Sprite.SpriteWidth <= 16;
      //if (Game1.timeOfDay >= 2400 && farmAnimal.home is not null && location.buildings.Contains(farmAnimal.home)) {
      //  Unhide(farmAnimal);
      //  horse.IsInvisible = true;
      //  Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("HorseReturned", new { name = farmAnimal.Name })) {
      //    noIcon = true,
      //  });
      //} else if (farmAnimal.currentLocation == location) {
      if (farmAnimal.currentLocation == location) {
        Unhide(farmAnimal);
        farmAnimal.Halt();
        farmAnimal.Position = horse.Position - (isSmallAnimal ? SmallHorsePositionOffset : HorsePositionOffset);
        farmAnimal.FacingDirection = horse.FacingDirection;
        horse.IsInvisible = true;
      }
    } else {
      Game1.addHUDMessage(new HUDMessage(ModEntry.Helper.Translation.Get("NoAnimal")) {
        noIcon = true,
      });
    }
  }

  public static void MaybeReaddHorseAfterDismounting(Horse horse) {
    if (!horse.IsInvisible) {
      horse.currentLocation.characters.Add(horse);
    }
  }

  static void Hide(FarmAnimal farmAnimal) {
    farmAnimal.modData[InvisibleKey] = "true";
    farmAnimal.collidesWithOtherCharacters.Value = false;
  }
  static void Unhide(FarmAnimal farmAnimal) {
    farmAnimal.modData.Remove(InvisibleKey);
    farmAnimal.collidesWithOtherCharacters.Value = true;
  }

  public static bool IsHidden(FarmAnimal farmAnimal) {
    return farmAnimal.modData.ContainsKey(InvisibleKey);
  }

  public static void RemoveFakeHorses() {
    //animalToFakeHorse.Clear();
    Utility.ForEachLocation(static l => {
      foreach (var npc in l.characters.ToList()) {
        if (npc is Horse horse && IsFakeHorse(horse)) {
          l.characters.Remove(horse);
        }
      }
      foreach (var animal in l.animals.Values) {
        Unhide(animal);
      }
      return true;
    });
  }

  public static void WarpHomeHiddenAnimals() {
    Utility.ForEachLocation(static l => {
      foreach (var animal in l.animals.Values) {
        if (IsHidden(animal) && !animal.IsHome) {
          animal.warpHome();
        }
      }
      return true;
    });
  }

  // Returns whether the original draw should continue
  public static bool DrawFakeHorse(Horse horse, SpriteBatch b, float alpha = 1) {
    if (!fakeHorseToSprite.TryGetValue(horse, out var sprite)) {
      return true;
    }
    int y = horse.StandingPixel.Y;
    float layerDepth = Math.Max(0f, horse.drawOnTop ? 0.991f : (y / 10000f + 1e-5f + 1e-6f));
    if (horse.FacingDirection == Game1.up) {
      layerDepth -= 1e-4f;
    }
    if (sprite.Texture == null) {
      Vector2 vector = Game1.GlobalToLocal(Game1.viewport, horse.Position);
      Rectangle screenArea = new Rectangle((int)vector.X, (int)vector.Y - sprite.SpriteWidth * 4, sprite.SpriteWidth * 4, sprite.SpriteHeight * 4);
      Utility.DrawErrorTexture(b, screenArea, layerDepth);
    } else if (!horse.IsInvisible && (Utility.isOnScreen(horse.Position, 128) || (horse.EventActor && horse.currentLocation is Summit))) {
      if (horse.swimming.Value) {
        b.Draw(sprite.Texture, horse.getLocalPosition(Game1.viewport) + new Vector2(32f, 80 + horse.yJumpOffset * 2) + ((horse.shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero) - new Vector2(0f, horse.yOffset), new Microsoft.Xna.Framework.Rectangle(sprite.SourceRect.X, sprite.SourceRect.Y, sprite.SourceRect.Width, sprite.SourceRect.Height / 2 - (int)(horse.yOffset / 4f)), Color.White, horse.rotation, new Vector2(32f, 96f) / 4f, Math.Max(0.2f, horse.Scale) * 4f, horse.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
        Vector2 localPosition = horse.getLocalPosition(Game1.viewport);
        b.Draw(Game1.staminaRect, new Rectangle((int)localPosition.X + (int)horse.yOffset + 8, (int)localPosition.Y - 128 + sprite.SourceRect.Height * 4 + 48 + horse.yJumpOffset * 2 - (int)horse.yOffset, sprite.SourceRect.Width * 4 - (int)horse.yOffset * 2 - 16, 4), Game1.staminaRect.Bounds, Color.White * 0.75f, 0f, Vector2.Zero, SpriteEffects.None, (float)y / 10000f + 0.001f);
      } else {
        b.Draw(
                sprite.Texture,
                horse.getLocalPosition(Game1.viewport)
                + new Vector2(
                    horse.GetSpriteWidthForPositioning() * 4 / 2,
                    horse.GetBoundingBox().Height / 2) + ((horse.shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), sprite.SourceRect, Color.White * alpha, horse.rotation, new Vector2(sprite.SpriteWidth / 2, (float)sprite.SpriteHeight * 3f / 4f), Math.Max(0.2f, horse.Scale) * 4f, (horse.flip || (sprite.CurrentAnimation != null && sprite.CurrentAnimation[sprite.currentAnimationIndex].flip)) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth);
      }
      //horse.DrawBreathing(b, alpha);
      //horse.DrawGlow(b);
      if (!Game1.eventUp) {
        horse.DrawEmote(b);
      }
    }
    return false;
  }

  public static void UpdateHorseSprite(Horse horse, GameTime time, GameLocation location) {
    if (fakeHorseToSprite.TryGetValue(horse, out var sprite)) {
      horse.flip = horse.FacingDirection == Game1.left && sprite.textureUsesFlippedRightForLeft;
      bool moving = horse.rider is not null
        && ((horse.rider.movementDirections.Any() && horse.rider.CanMove) || horse.rider.position.Field.IsInterpolating());
      if (!moving) {
        sprite.StopAnimation();
        sprite.faceDirection(horse.FacingDirection);
      } else {
        if (sprite.textureUsesFlippedRightForLeft
            && horse.FacingDirection == Game1.left) {
          sprite.AnimateRight(time);
        } else {
          switch (horse.FacingDirection) {
            case Game1.up:
              sprite.AnimateUp(time);
              break;
            case Game1.right:
              sprite.AnimateRight(time);
              break;
            case Game1.down:
              sprite.AnimateDown(time);
              break;
            case Game1.left:
              sprite.AnimateLeft(time);
              break;
          }
        }
      }
    } else {
      ModEntry.StaticMonitor.Log($"fake horse doesn't have sprite? this should NOT happen.", LogLevel.Warn);
    }
  }

  public static void WarpFakeHorses(long uid) {
    if (!Game1.IsMasterGame) {
      return;
    }
    Farmer? farmer = Game1.GetPlayer(uid);
    if (farmer == null) {
      return;
    }
    List<Horse> horses = new();
    Utility.ForEachCharacter(npc => {
      if (npc is Horse horse && IsFakeHorse(horse) && horse.modData.TryGetValue(OwnerIdKey, out var ownerId) && ownerId == uid.ToString()) {
        horses.Add(horse);
      }
      return true;
    });
    if (horses.Count == 0 || Utility.GetHorseWarpRestrictionsForFarmer(farmer) != 0) {
      return;
    }
    foreach (var horse in horses) {
      horse.mutex.RequestLock(delegate {
        horse.mutex.ReleaseLock();
        GameLocation currentLocation = horse.currentLocation;
        Vector2 tile = horse.Tile;
        for (int i = 0; i < 8; i++) {
          Game1.Multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(10, new Vector2(tile.X + Utility.RandomFloat(-1f, 1f), tile.Y + Utility.RandomFloat(-1f, 0f)) * 64f, Color.White, 8, flipped: false, 50f) {
            layerDepth = 1f,
            motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
          });
        }
        currentLocation.playSound("wand", horse.Tile);
        currentLocation = farmer.currentLocation;
        tile = farmer.Tile;
        currentLocation.playSound("wand", tile);
        for (int j = 0; j < 8; j++) {
          Game1.Multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(10, new Vector2(tile.X + Utility.RandomFloat(-1f, 1f), tile.Y + Utility.RandomFloat(-1f, 0f)) * 64f, Color.White, 8, flipped: false, 50f) {
            layerDepth = 1f,
            motion = new Vector2(Utility.RandomFloat(-0.5f, 0.5f), Utility.RandomFloat(-0.5f, 0.5f))
          });
        }
        Game1.warpCharacter(horse, farmer.currentLocation, tile);
        int num = 0;
        for (int num2 = (int)tile.X + 3; num2 >= (int)tile.X - 3; num2--) {
          Game1.Multiplayer.broadcastSprites(currentLocation, new TemporaryAnimatedSprite(6, new Vector2(num2, tile.Y) * 64f, Color.White, 8, flipped: false, 50f) {
            layerDepth = 1f,
            delayBeforeAnimationStart = num * 25,
            motion = new Vector2(-0.25f, 0f)
          });
          num++;
        }
      });
    }
  }

  public static string? GetOwner(Horse horse) {
    return horse.modData.GetValueOrDefault(OwnerIdKey);
  }

  public static float GetSpeedModifier(Horse horse) {
    if (fakeHorseToAnimal.TryGetValue(horse, out var farmAnimal)) {
      // Get custom if set
      if (farmAnimal.GetAnimalData()?.CustomFields?.TryGetValue(SpeedModifierKey, out var str) is true
          && float.TryParse(str, out var modifier)) {
        return modifier;
      }
      // If there's Horse in name then good enough lmao
      if (farmAnimal.type.Value?.Contains("Horse") is true) return 1f;
      // otherwise slow them down; slow more if smol animu
      if (farmAnimal.Sprite.SpriteWidth <= 16) return 0.5f;
      else return 0.8f;
    }
    return 1f;
  }
}

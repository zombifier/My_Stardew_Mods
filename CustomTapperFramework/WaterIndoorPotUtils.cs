using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using StardewModdingAPI;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.MachineTerrainFramework;

// Contains utils for handling logic specific to water pot. The rest lives in CustomCrabPotUtils.
public static class WaterIndoorPotUtils {
  public static readonly string WaterPlanterItemId = $"{ModEntry.UniqueId}.WaterPlanter";
  public static readonly string WaterPlanterQualifiedItemId = $"(BC){WaterPlanterItemId}";
  public static readonly string WaterPotItemId = $"{ModEntry.UniqueId}.WaterPot";
  public static readonly string WaterPotQualifiedItemId = $"(BC){WaterPotItemId}";

  public static readonly string HoeDirtIsWaterModDataKey = $"{ModEntry.UniqueId}.IsWater";
  public static readonly string HoeDirtIsAmphibiousModDataKey = $"{ModEntry.UniqueId}.IsAmphibious";
  public static readonly string HoeDirtIsWaterPlanterModDataKey = $"{ModEntry.UniqueId}.IsWaterPlanter";

  // Crop custom fields
  public static readonly string CropIsWaterCustomFieldsKey = $"{ModEntry.UniqueId}.IsAquaticCrop";
  public static readonly string CropIsAmphibiousCustomFieldsKey = $"{ModEntry.UniqueId}.IsSemiAquaticCrop";
  public static readonly string CropCustomPotItemKey = $"{ModEntry.UniqueId}.CustomPots";
  //public static readonly string CropCanUseRegularDirtKey = $"{ModEntry.UniqueId}.CanUseRegularDirt";

  // Pot custom fields
  public static readonly string IsCustomPotKey = $"{ModEntry.UniqueId}.IsCustomPot";
  public static readonly string BansRegularCropsKey = $"{ModEntry.UniqueId}.BansRegularCrops";
  public static readonly string CropYOffsetKey = $"{ModEntry.UniqueId}.CropYOffset";
  public static readonly string CropTintColorKey = $"{ModEntry.UniqueId}.CropTintColor";

  public static void transformIndoorPotToItem(IndoorPot indoorPot, string itemId) {
    indoorPot.ItemId = itemId;
    if (Game1.bigCraftableData.TryGetValue(itemId, out var value)) {
      indoorPot.name = value.Name ?? ItemRegistry.GetDataOrErrorItem($"(BC){itemId}").InternalName;
      indoorPot.Price = value.Price;
      indoorPot.Type = "Crafting";
      indoorPot.Category = -9;
      indoorPot.setOutdoors.Value = value.CanBePlacedOutdoors;
      indoorPot.setIndoors.Value = value.CanBePlacedIndoors;
      indoorPot.Fragility = value.Fragility;
      indoorPot.isLamp.Value = value.IsLamp;
    }
    indoorPot.ResetParentSheetIndex();
  }

  public static bool isWaterPlanter(SObject obj) {
    return
      obj.QualifiedItemId == WaterIndoorPotUtils.WaterPlanterQualifiedItemId ||
      obj.QualifiedItemId == WaterIndoorPotUtils.WaterPotQualifiedItemId;
  }

  public static void draw(IndoorPot obj, SpriteBatch spriteBatch, int x, int y, float alpha = 1f) {
    GameLocation location = obj.Location;
    if (location == null) {
      return;
    }
    CrabPotData crabPotData = CustomCrabPotUtils.getCrabPotData(obj);
    float yBob = (float)(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500.0 + (double)(x * 64)) * 8.0 + 8.0);
    float yBobCrops = (float)(Math.Sin((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + 500) / 500.0 + (double)(x * 64)) * 8.0 + 8.0);
    if (yBob <= 0.001f) {
      location.temporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Microsoft.Xna.Framework.Rectangle(0, 0, 64, 64), 150f, 8, 0, crabPotData.directionOffset + new Vector2(x * 64 + 4, y * 64 + 32), flicker: false, Game1.random.NextBool(), 0.001f, 0.01f, Color.White, 0.75f, 0.003f, 0f, 0f));
    }

    Vector2 vector = obj.getScale();
    vector *= 4f;
    Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 64 + (int)yBob));
    ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId);
    Rectangle sourceRectFromAT = new();
    Texture2D? texture2DFromAT = ModEntry.atApi?.GetTextureForObject(obj, out sourceRectFromAT);
    Texture2D texture2D = texture2DFromAT ?? dataOrErrorItem.GetTexture();
    int spriteIndex = dataOrErrorItem.SpriteIndex;

    spriteBatch.Draw(
        texture2D,
        Game1.GlobalToLocal(Game1.viewport,
          crabPotData.directionOffset + new Vector2(x * 64, y * 64 + (int)yBob - 64)) + crabPotData.shake,
        texture2DFromAT is not null ? sourceRectFromAT : SObject.getSourceRectForBigCraftable(texture2D, spriteIndex),
        Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, ((float)(y * 64) + crabPotData.directionOffset.Y + (float)(x % 4)) / 10000f);

    // Old draw code
    //Microsoft.Xna.Framework.Rectangle destinationRectangle =
    //  new Microsoft.Xna.Framework.Rectangle(
    //      (int)(vector2.X - vector.X / 2f) + ((obj.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
    //      (int)(vector2.Y - vector.Y / 2f) + ((obj.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
    //      (int)(64f + vector.X),
    //      (int)(128f + vector.Y / 2f));
    //spriteBatch.Draw(
    //    dataOrErrorItem.GetTexture(),
    //    destinationRectangle,
    //    dataOrErrorItem.GetSourceRect(obj.showNextIndex.Value ? 1 : 0),
    //    Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, Math.Max(0f, (float)((y + 1) * 64 - 24) / 10000f) + (float)x * 1E-05f);
    if (obj.hoeDirt.Value.HasFertilizer()) {
      Microsoft.Xna.Framework.Rectangle fertilizerSourceRect = obj.hoeDirt.Value.GetFertilizerSourceRect();
      fertilizerSourceRect.Width = 13;
      fertilizerSourceRect.Height = 13;
      spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, crabPotData.directionOffset + new Vector2(obj.TileLocation.X * 64f + 4f, obj.TileLocation.Y * 64f + 4f + (int)yBobCrops)), fertilizerSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (obj.TileLocation.Y + 0.65f) * 64f / 10000f + (float)x * 1E-05f);
    }
    obj.hoeDirt.Value.crop?.drawWithOffset(spriteBatch, obj.TileLocation, /*(obj.hoeDirt.Value.isWatered() && (int)obj.hoeDirt.Value.crop.currentPhase.Value == 0 && !obj.hoeDirt.Value.crop.raisedSeeds.Value) ? (new Color(180, 100, 200) * 1f) :*/ Color.White, obj.hoeDirt.Value.getShakeRotation(), crabPotData.directionOffset + new Vector2(32f, 24f + (int)yBobCrops));
    obj.heldObject.Value?.draw(spriteBatch, x * 64, y * 64 - 48, (obj.TileLocation.Y + 0.66f) * 64f / 10000f + (float)x * 1E-05f, 1f);
    obj.bush.Value?.draw(spriteBatch, crabPotData.directionOffset.Y + yBobCrops);
  }

  public static void canPlant(HoeDirt hoeDirt, string itemId, ref bool result) {
    itemId = Crop.ResolveSeedId(itemId, hoeDirt.Location);
    if (Crop.TryGetData(itemId, out var data)) {
      bool isWater = hoeDirt.modData.ContainsKey(HoeDirtIsWaterModDataKey);
      bool isAmphibious = hoeDirt.modData.ContainsKey(HoeDirtIsAmphibiousModDataKey);
      if ((isWater) &&
          (!data.CustomFields?.ContainsKey(CropIsAmphibiousCustomFieldsKey) ?? true) &&
          (!data.CustomFields?.ContainsKey(CropIsWaterCustomFieldsKey) ?? true)) {
        result = false;
      }
      if (!isWater && !isAmphibious &&
          (data.CustomFields?.ContainsKey(CropIsWaterCustomFieldsKey) ?? false)) {
        result = false;
      }
      // Is regular dirt (not needed, keeping for reference)
      //if ((data.CustomFields?.ContainsKey(CropCustomPotItemKey) ?? false) &&
      //    (!data.CustomFields?.ContainsKey(CropCanUseRegularDirtKey) ?? true) &&
      //    (hoeDirt.Pot is null/* || hoeDirt.Pot.QualifiedItemId == "(BC)62"*/)) {
      //  result = false;
      //}
      // Is modded pot
      if (!isWater && hoeDirt.Pot is not null &&
         //          (!data.CustomFields?.ContainsKey(CropCanUseRegularDirtKey) ?? false) &&
         (
           ((!data.CustomFields?.ContainsKey(CropCustomPotItemKey) ?? true) && !AcceptsRegularCrops(hoeDirt.Pot)) ||
           ((data.CustomFields?.TryGetValue(CropCustomPotItemKey, out var customPotStr) ?? false) && !customPotStr.Split(' ', ',').Contains(hoeDirt.Pot.QualifiedItemId)))
         ) {
        result = false;
      }
    }
  }

  // Doesn't contain the water planters/pots for now, those use the old hardcoded API
  public static bool AcceptsRegularCrops(SObject obj) {
    return obj.QualifiedItemId == "(BC)62" ||
      !Game1.bigCraftableData.TryGetValue(obj.ItemId, out var data) ||
      !(data.CustomFields?.ContainsKey(BansRegularCropsKey) ?? false);
  }

  public static bool IsCustomPot(SObject obj) {
    return Game1.bigCraftableData.TryGetValue(obj.ItemId, out var data) &&
     (data.CustomFields?.ContainsKey(IsCustomPotKey) ?? false);
  }

  public static bool GetDrawOverridesForPot(IndoorPot pot, out int? cropYOffset, out Color? cropTintColor) {
    cropYOffset = null;
    cropTintColor = null;
    bool shouldOverrideDraw = false;
    if (Game1.bigCraftableData.TryGetValue(pot.ItemId, out var data) && data.CustomFields is not null) {
      if (data.CustomFields.TryGetValue(CropYOffsetKey, out string? cropYOffsetStr) &&
          Int32.TryParse(cropYOffsetStr, out int parsed)) {
        cropYOffset = parsed;
        shouldOverrideDraw = true;
      }
      if (data.CustomFields.TryGetValue(CropTintColorKey, out string? cropTintColorStr)) {
        cropTintColor = Utility.StringToColor(cropTintColorStr);
        shouldOverrideDraw = true;
      }
    }
    return shouldOverrideDraw;
  }

  public static void drawPotOverride(IndoorPot pot, SpriteBatch spriteBatch, int x, int y, float alpha = 1f, int? cropYOffset = 0, Color? cropTintColor = null) {
    var yOffset = cropYOffset ?? 0;
    Vector2 vector = pot.getScale();
    vector *= 4f;
    Vector2 vector2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 64));
    Microsoft.Xna.Framework.Rectangle destinationRectangle = new Microsoft.Xna.Framework.Rectangle((int)(vector2.X - vector.X / 2f) + ((pot.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(vector2.Y - vector.Y / 2f) + ((pot.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(64f + vector.X), (int)(128f + vector.Y / 2f));
    ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(pot.QualifiedItemId);
    spriteBatch.Draw(dataOrErrorItem.GetTexture(), destinationRectangle, dataOrErrorItem.GetSourceRect(pot.showNextIndex.Value ? 1 : 0), Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, Math.Max(0f, (float)((y + 1) * 64 - 24) / 10000f) + (float)x * 1E-05f);
    if (pot.hoeDirt.Value.HasFertilizer()) {
      Microsoft.Xna.Framework.Rectangle fertilizerSourceRect = pot.hoeDirt.Value.GetFertilizerSourceRect();
      fertilizerSourceRect.Width = 13;
      fertilizerSourceRect.Height = 13;
      spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(pot.TileLocation.X * 64f + 4f, pot.TileLocation.Y * 64f - 12f + yOffset)), fertilizerSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (pot.TileLocation.Y + 0.65f) * 64f / 10000f + (float)x * 1E-05f);
    }
    pot.hoeDirt.Value.crop?.drawWithOffset(spriteBatch, pot.TileLocation, cropTintColor ?? ((pot.hoeDirt.Value.isWatered() && pot.hoeDirt.Value.crop.currentPhase.Value == 0 && !pot.hoeDirt.Value.crop.raisedSeeds.Value) ? (new Color(180, 100, 200) * 1f) : Color.White), pot.hoeDirt.Value.getShakeRotation(), new Vector2(32f, 8f + yOffset));
    pot.heldObject.Value?.draw(spriteBatch, x * 64, y * 64 - 48 + yOffset, (pot.TileLocation.Y + 0.66f) * 64f / 10000f + (float)x * 1E-05f, 1f);
    pot.bush.Value?.draw(spriteBatch, -24f + yOffset);
  }
}

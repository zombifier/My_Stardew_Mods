using System.Collections.Generic;

class ItemCriteria {
  // Recommended for CP patching purposes
  public string? Id = null;
  // If not set any item will match, if set item ID must be in list
  // Takes qualified IDs
  public List<string>? ItemIds = null;
  // All tags must match
  public List<string>? ContextTags = null;
  // A GSQ to check if any. Use on "Target".
  public string? Condition = null;
}

class CategoryData {
  // Informational
  public string Name = "";
  public string Description = "";
  // 16x16 icon
  public string Texture = "Maps/springobjects";
  public int SpriteIndex = 0;
  // Item shipped points
  public int TotalPoints = 1000;
  public int MaxIndividualPoints = 99999999;
  // If set, use the sell price instead of the Fair points
  public bool UseSalePrice = false;
  // An item must satisfy at least one entry in this list to be eligible
  public List<ItemCriteria>? ItemCriterias = null;
}

class CompetitionData {
  public List<string> ActiveCategoryIds = [];
  public Dictionary<string, CategoryData> Categories = new();
}

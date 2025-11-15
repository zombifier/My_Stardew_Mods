using System.Linq;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using BlueprintEntry = StardewValley.Menus.CarpenterMenu.BlueprintEntry;

namespace Selph.StardewMods.TextileExpansion;

class TextileSkill : SpaceCore.Skills.Skill {
  public static TextileProfession Weaver = null!;
  public static TextileProfession Tailor = null!;
  public static TextileProfession Dyer = null!;
  public static TextileProfession Sericulturist = null!;
  public static TextileProfession Couturier = null!;
  public static TextileProfession Outfitter = null!;

  const int CLOTH_PRICE = 470;
  public static string SkillId = $"{ModEntry.UniqueId}_TextileSkill";
  public static string SkillIconTexture = $"{ModEntry.UniqueId}/SkillIcon";
  public static string SkillPageIconTexture = $"{ModEntry.UniqueId}/SkillPageIcon";
  public static string ProfessionIconTexture(TextileProfessionEnum profession) {
    return $"{ModEntry.UniqueId}/ProfessionIcon{(int)profession}";
  }
  public TextileSkill() : base(SkillId) {
    this.Icon = Game1.content.Load<Texture2D>(SkillIconTexture);
    this.SkillsPageIcon = Game1.content.Load<Texture2D>(SkillPageIconTexture);
    this.ExperienceBarColor = new Microsoft.Xna.Framework.Color(214, 214, 214);
    this.ExperienceCurve = [
      CLOTH_PRICE * 5,
      CLOTH_PRICE * 10,
      CLOTH_PRICE * 20,
      CLOTH_PRICE * 40,
      CLOTH_PRICE * 80,
      CLOTH_PRICE * 150,
      CLOTH_PRICE * 220,
      CLOTH_PRICE * 300,
      CLOTH_PRICE * 400,
      CLOTH_PRICE * 600,
    ];
    Weaver = new TextileProfession(this, TextileProfessionEnum.WEAVER);
    Tailor = new TextileProfession(this, TextileProfessionEnum.TAILOR);
    Dyer = new TextileProfession(this, TextileProfessionEnum.DYER);
    Sericulturist = new TextileProfession(this, TextileProfessionEnum.SERICULTURIST);
    Couturier = new TextileProfession(this, TextileProfessionEnum.COUTURIER);
    Outfitter = new TextileProfession(this, TextileProfessionEnum.OUTFITTER);
    this.Professions.Add(Weaver);
    this.Professions.Add(Tailor);
    this.ProfessionsForLevels.Add(new ProfessionPair(5, Weaver, Tailor));
    this.Professions.Add(Dyer);
    this.Professions.Add(Sericulturist);
    this.ProfessionsForLevels.Add(new ProfessionPair(10, Dyer, Sericulturist, Weaver));
    this.Professions.Add(Couturier);
    this.Professions.Add(Outfitter);
    this.ProfessionsForLevels.Add(new ProfessionPair(10, Couturier, Outfitter, Tailor));
  }

  public override string GetName() {
    return ModEntry.Helper.Translation.Get("skill.name");
  }

  public override List<string> GetExtraLevelUpInfo(int level) {
    List<string> result = new();
    if (new int[] { 2, 3, 6, 8, 9 }.Contains(level)) {
      result.Add(ModEntry.Helper.Translation.Get($"skill.perk.level{level}"));
    }
    if (level % 2 == 1) {
      result.Add(ModEntry.Helper.Translation.Get("skill.perk1", new { speedIncrease = 10 }));
    }
    if (level != 2 && level % 2 == 0) {
      result.Add(ModEntry.Helper.Translation.Get("skill.perk2", new { tailorCount = 1 }));
    }
    return result;
  }

  public override string GetSkillPageHoverText(int level) {
    string result =
        ModEntry.Helper.Translation.Get("skill.perk1", new { speedIncrease = 10 * (level + 1) / 2 });
    if (level >= 2) {
      result += "\n"
        + ModEntry.Helper.Translation.Get("skill.perk2", new { tailorCount = 3 + (level) / 2 });
    }
    return result;
  }
}

enum TextileProfessionEnum {
  WEAVER = 0,
  TAILOR = 1,
  DYER = 2,
  SERICULTURIST = 3,
  COUTURIER = 4,
  OUTFITTER = 5,
}

class TextileProfession : SpaceCore.Skills.Skill.Profession {
  TextileProfessionEnum profession;
  public TextileProfession(TextileSkill skill, TextileProfessionEnum profession) : base(skill, $"{ModEntry.UniqueId}_Profession_{profession}") {
    this.profession = profession;
    this.Icon = Game1.content.Load<Texture2D>(TextileSkill.ProfessionIconTexture(profession));
  }
  public override string GetName() {
    return ModEntry.Helper.Translation.Get($"profession.name.{(int)profession}");
  }
  public override string GetDescription() {
#if SDV1615
    if (profession == TextileProfessionEnum.OUTFITTER) {

      return ModEntry.Helper.Translation.Get($"profession.description.5.pre1616");
    }
#endif
    return ModEntry.Helper.Translation.Get($"profession.description.{(int)profession}");
  }
}

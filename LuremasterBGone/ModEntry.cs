using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Selph.StardewMods.LuremasterBGone;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper { get; set; } = null!;
  internal static IMonitor StaticMonitor { get; set; } = null!;
  internal static string UniqueId = null!;
  internal static string AlreadyHasBaitKey = null!;

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;

    AlreadyHasBaitKey = $"{UniqueId}_HasBait";
    if (helper.ModRegistry.IsLoaded("DaLion.Professions")) {
      ModEntry.StaticMonitor.Log("ALERT: Walk of Life detected; this mod will disable itself (WOL already changes Luremaster to something else)", LogLevel.Alert);
      return;
    }
    var harmony = new Harmony(this.ModManifest.UniqueID);
    harmony.Patch(
        original: AccessTools.Method(typeof(CrabPot), nameof(CrabPot.NeedsBait)),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(CrabPot_NeedsBait_Prefix)));
    harmony.Patch(
        original: AccessTools.Method(typeof(CrabPot), nameof(CrabPot.DayUpdate)),
        postfix: new HarmonyMethod(typeof(ModEntry), nameof(CrabPot_DayUpdate_Postfix)));
    helper.Events.Content.AssetRequested += OnAssetRequested;
  }

  static bool CrabPot_NeedsBait_Prefix(CrabPot __instance, ref bool __result) {
    __result = __instance.bait.Value == null;
    return false;
  }

  static void CrabPot_DayUpdate_Postfix(CrabPot __instance) {
    if (!(Game1.GetPlayer(__instance.owner.Value) ?? Game1.player).professions.Contains(Farmer.mariner)
        || __instance.bait.Value is null
        || __instance.heldObject.Value is null
        || __instance.heldObject.Value.modData.ContainsKey(AlreadyHasBaitKey)) {
      return;
    }
    var chest = __instance.heldObject.Value.heldObject.Value as Chest ?? new Chest(false);
    if (__instance.heldObject.Value.heldObject.Value is not Chest) {
      StaticMonitor.Log($"{__instance.heldObject.Value.QualifiedItemId} already has non-chest held object? This should not be possible.", LogLevel.Warn);
    }
    __instance.heldObject.Value.heldObject.Value ??= chest;
    // EMC handles pulling the item from the held chest and put it in the farmer/chest inventory.
    chest.addItem(__instance.bait.Value.getOne());
    __instance.heldObject.Value.modData.Add(AlreadyHasBaitKey, "true");
  }

  static void OnAssetRequested(object? sender, AssetRequestedEventArgs e) {
    if (e.NameWithoutLocale.IsEquivalentTo("Strings/UI")) {
      e.Edit(asset => {
        var data = asset.AsDictionary<string, string>().Data;
        data["LevelUp_ProfessionDescription_Luremaster"] = Helper.Translation.Get("LuremasterDescription");
      });
    }
  }
}

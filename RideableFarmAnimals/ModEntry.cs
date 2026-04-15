using HarmonyLib;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Selph.StardewMods.RideableFarmAnimals;

internal sealed class ModEntry : Mod {
  internal static new IModHelper Helper = null!;
  internal static IMonitor StaticMonitor = null!;
  public static string UniqueId = null!;
  public const string CpUniqueId = "selph.RideableFarmAnimalsCP";

  public override void Entry(IModHelper helper) {
    Helper = helper;
    StaticMonitor = this.Monitor;
    UniqueId = this.ModManifest.UniqueID;
    var harmony = new Harmony(ModEntry.UniqueId);
    HarmonyPatcher.ApplyPatches(harmony);

    Helper.Events.GameLoop.DayEnding += OnDayEnding;
    Helper.Events.GameLoop.TimeChanged += OnTimeChanged;
    Helper.Events.Player.InventoryChanged += OnInventoryChanged;
  }

  static void OnDayEnding(object? sender, DayEndingEventArgs e) {
    HorseManager.RemoveFakeHorses();
  }

  // Warp the invisible farm animals home in case they got locked out
  static void OnTimeChanged(object? sender, TimeChangedEventArgs e) {
    if (e.OldTime < 1900 && e.NewTime >= 1900) {
      HorseManager.WarpHomeHiddenAnimals();
    }
  }

  static void OnInventoryChanged(object? sender, InventoryChangedEventArgs e) {
    foreach (var item in e.Added) {
      if (item is Tool tool && HarmonyPatcher.IsRein(tool)) {
        tool.InstantUse = true;
      }
    }
  }
}

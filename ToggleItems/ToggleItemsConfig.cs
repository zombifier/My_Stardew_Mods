using System.Collections.Generic;
using StardewModdingAPI.Utilities;

namespace ToggleItems {
  internal class ToggleItemsConfig {
    public KeybindList switchKey { get; set; } = KeybindList.Parse("Q");
    public bool priceBalance { get; set; } = false;
  }
}

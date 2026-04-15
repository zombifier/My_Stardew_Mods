using System;
using System.Collections.Generic;
using StardewValley;

namespace VanillaPlusProfessions.Compatibility {
  public interface IVanillaPlusProfessions {
    /// <summary>
    /// A method to find out what talents a player has.
    /// </summary>
    /// <returns>A list of string names of the talents <paramref name="who"/> or current player has.</returns>
    /// <param name="who">The player to get the VPP professions of. If not filled, it'll default to the current player.</param>
    public IEnumerable<string> GetTalentsForPlayer(Farmer? who = null);
  }
}

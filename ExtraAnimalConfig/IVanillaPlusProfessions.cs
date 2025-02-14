using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace VanillaPlusProfessions.Compatibility
{
    public interface IVanillaPlusProfessions
    {
        /// <summary>j
        /// Registers a custom skill talent tree for a custom skill added via SpaceCore.
        /// </summary>
        /// <returns>A list of string names of the professions <paramref name="who"/> or current player has.</returns>
        /// <param name="who">The player to get the VPP professions of. If not filled, it'll default to the current player.</param>
        public IEnumerable<string> GetProfessionsForPlayer(Farmer? who = null);
    }
}

using System.Collections.Generic;
// ReSharper disable UnusedMember.Global

namespace ItemExtensions;

public interface IItemExtensionsApi {
    /// <summary>
    /// Checks custom mixed seeds.
    /// </summary>
    /// <param name="itemId">The 'main seed' ID.</param>
    /// <param name="includeSource">Include the main seed's crop in calculation.</param>
    /// <param name="parseConditions">Whether to pase GSQs before adding to list.</param>
    /// <returns>All possible seeds.</returns>
    List<string>? GetCustomSeeds(string itemId, bool includeSource, bool parseConditions = true);
}

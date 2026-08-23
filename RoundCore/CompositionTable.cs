using System.Collections.Generic;

namespace EmergencyEvents.RoundCore;

/// <summary>
/// D-LRC Round Core 的权威开局编制表。
/// </summary>
internal static class CompositionTable
{
    private static readonly IReadOnlyDictionary<int, RoundComposition> Rows =
        new Dictionary<int, RoundComposition>
        {
            [16] = new RoundComposition(16, PopulationTier.E, 3, 2, 2, 6, 3),
            [17] = new RoundComposition(17, PopulationTier.E, 3, 2, 2, 7, 3),
            [18] = new RoundComposition(18, PopulationTier.E, 3, 2, 2, 7, 4),
            [19] = new RoundComposition(19, PopulationTier.E, 3, 2, 2, 8, 4),
            [20] = new RoundComposition(20, PopulationTier.D, 4, 3, 3, 7, 3),
            [21] = new RoundComposition(21, PopulationTier.D, 4, 3, 3, 7, 4),
            [22] = new RoundComposition(22, PopulationTier.D, 4, 3, 3, 8, 4),
            [23] = new RoundComposition(23, PopulationTier.D, 4, 3, 3, 9, 4),
            [24] = new RoundComposition(24, PopulationTier.D, 4, 3, 3, 9, 5),
            [25] = new RoundComposition(25, PopulationTier.D, 4, 3, 3, 10, 5),
            [26] = new RoundComposition(26, PopulationTier.C, 4, 4, 4, 9, 5),
            [27] = new RoundComposition(27, PopulationTier.C, 4, 4, 4, 10, 5),
            [28] = new RoundComposition(28, PopulationTier.C, 4, 4, 4, 11, 5),
            [29] = new RoundComposition(29, PopulationTier.C, 4, 4, 4, 11, 6),
            [30] = new RoundComposition(30, PopulationTier.C, 5, 4, 4, 11, 6),
            [31] = new RoundComposition(31, PopulationTier.C, 5, 4, 4, 12, 6),
            [32] = new RoundComposition(32, PopulationTier.B, 5, 5, 5, 11, 6),
            [33] = new RoundComposition(33, PopulationTier.B, 5, 5, 5, 12, 6),
            [34] = new RoundComposition(34, PopulationTier.B, 5, 5, 5, 13, 6),
            [35] = new RoundComposition(35, PopulationTier.B, 5, 5, 5, 13, 7),
            [36] = new RoundComposition(36, PopulationTier.B, 6, 5, 5, 13, 7),
            [37] = new RoundComposition(37, PopulationTier.B, 6, 5, 5, 14, 7),
            [38] = new RoundComposition(38, PopulationTier.A, 6, 6, 6, 13, 7),
            [39] = new RoundComposition(39, PopulationTier.A, 6, 6, 6, 14, 7),
            [40] = new RoundComposition(40, PopulationTier.A, 6, 6, 6, 15, 7),
            [41] = new RoundComposition(41, PopulationTier.A, 6, 6, 6, 15, 8),
            [42] = new RoundComposition(42, PopulationTier.A, 6, 6, 6, 16, 8),
            [43] = new RoundComposition(43, PopulationTier.A, 7, 6, 6, 16, 8),
            [44] = new RoundComposition(44, PopulationTier.A, 7, 6, 6, 17, 8),
            [45] = new RoundComposition(45, PopulationTier.A, 7, 6, 6, 17, 9),
        };

    public static bool TryGet(int population, out RoundComposition? composition)
    {
        return Rows.TryGetValue(population, out composition);
    }
}

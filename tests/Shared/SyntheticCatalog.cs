using System;
using System.Collections.Generic;

using QuickMenu.Core.Pins;

namespace QuickMenu.TestData;

/// <summary>
/// Seeded synthetic data for the perf gates and benchmarks (performance.md section 2). Linked into the test and
/// benchmark projects. The search catalog generator (W-1/W-1x) joins when the Core SearchEntry type exists.
/// </summary>
public static class SyntheticCatalog
{
    public const int DefaultSeed = 20261019;

    /// <summary>Largest pin count in the workload model (200 pins, P-16/P-17).</summary>
    public const int PinCount = 200;

    /// <summary>A board of <paramref name="count"/> distinct pins with realistic label and package lengths.</summary>
    public static PinBoard Pins(int count = PinCount, int seed = DefaultSeed)
    {
        var rng = new Random(seed);
        var board = new PinBoard(count);
        for (int i = 0; i < count; i++)
        {
            string def = "Synthetic_Def_" + rng.Next(100_000, 999_999).ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PinKey key = i % 3 == 0
                ? PinKey.Build(PinKeyKind.BuildTerrain, def)
                : PinKey.Build(PinKeyKind.BuildThing, def);
            board.SetSlot(i, new PinEntry(key, "Label " + def, "author.mod" + (i % 8).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        return board;
    }

    /// <summary>The stored-line form of <see cref="Pins"/>.</summary>
    public static List<string> PinLines(int count = PinCount, int seed = DefaultSeed) =>
        PinSerializer.ToLines(Pins(count, seed));
}

using System;

namespace QuickMenu.Core.Layout;

/// <summary>Slots that fit on ring k so adjacent centres stay at least one pitch apart.</summary>
public static class RingCapacity
{
    /// <summary>cap(k) = floor(pi / asin(1 / (2k)) + 1e-9), k >= 1.</summary>
    public static int Of(int ring)
    {
        if (ring < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ring), ring, "Ring index starts at 1.");
        }

        return (int)Math.Floor((Math.PI / Math.Asin(1.0 / (2.0 * ring))) + 1e-9);
    }

    /// <summary>Total slots on rings 1..rings.</summary>
    public static int Total(int rings)
    {
        var sum = 0;
        for (var k = 1; k <= rings; k++)
        {
            sum += Of(k);
        }

        return sum;
    }
}

using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>Pointer hit testing against a computed wheel. Allocation-free, safe to call from OnGUI.</summary>
public static class WheelHitTest
{
    /// <summary>
    /// Index (into <paramref name="slots"/>) of the slot under <paramref name="point"/>, or -1. A slot is hit when the
    /// distance to its centre is at most its <see cref="SlotPlacement.HitRadius"/>; when several qualify the nearest wins.
    /// Only the first <paramref name="count"/> slots are tested.
    /// </summary>
    public static int SlotAt(SlotPlacement[] slots, int count, Vec2 point)
    {
        var best = -1;
        var bestDistance = float.MaxValue;
        var n = count < slots.Length ? count : slots.Length;
        for (var i = 0; i < n; i++)
        {
            var d = slots[i].Center.Distance(point);
            if (d <= slots[i].HitRadius && d < bestDistance)
            {
                best = i;
                bestDistance = d;
            }
        }

        return best;
    }
}

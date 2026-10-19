using System;

using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>Fixed wheel layout (design.md section 1.1): fill order ring 1 first, then ring 2 and so on.</summary>
public static class WheelLayout
{
    public static SlotPlacement[] Compute(WheelParams p)
    {
        if (p.Rings < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(p), p.Rings, "Rings must be at least 1.");
        }

        if (!(p.ButtonSize > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(p), p.ButtonSize, "Button size must be positive.");
        }

        var pitch = LayoutConstants.Pitch(p.ButtonSize);
        var result = new SlotPlacement[RingCapacity.Total(p.Rings)];
        var index = 0;
        for (var k = 1; k <= p.Rings; k++)
        {
            var cap = RingCapacity.Of(k);
            var radius = k * pitch;
            for (var i = 0; i < cap; i++)
            {
                var degrees = -90.0 + (i * 360.0 / cap);
                var rad = degrees * Math.PI / 180.0;
                var center = new Vec2(
                    p.Center.X + (float)(radius * Math.Cos(rad)),
                    p.Center.Y + (float)(radius * Math.Sin(rad)));
                result[index] = new SlotPlacement(index, k, center, radius, (float)degrees);
                index++;
            }
        }

        return result;
    }
}

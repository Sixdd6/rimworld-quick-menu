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

        if (!float.IsFinite(p.Center.X) || !float.IsFinite(p.Center.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(p), p.Center, "Center must be finite.");
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
                result[index] = new SlotPlacement(index, k, center, radius, (float)degrees, pitch / 2f);
                index++;
            }
        }

        return result;
    }

    /// <summary>
    /// Auto wheel (and satellite) slots: <paramref name="count"/> slots on the smallest number of rings that holds
    /// them, inner rings full and the outer ring spread evenly from 12 o'clock. Count 0 writes nothing.
    /// Allocation-free.
    /// </summary>
    public static void ComputeSpread(int count, float buttonSize, Vec2 center, Span<SlotPlacement> destination)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must not be negative.");
        }

        if (!(buttonSize > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(buttonSize), buttonSize, "Button size must be positive.");
        }

        if (destination.Length < count)
        {
            throw new ArgumentException("Destination is too small.", nameof(destination));
        }

        var pitch = LayoutConstants.Pitch(buttonSize);
        var index = 0;
        for (var k = 1; index < count; k++)
        {
            var onRing = Math.Min(RingCapacity.Of(k), count - index);
            var radius = k * pitch;
            var ringStart = index;
            for (var i = 0; i < onRing; i++)
            {
                var degrees = -90.0 + (i * 360.0 / onRing);
                var rad = degrees * Math.PI / 180.0;
                destination[ringStart + i] = new SlotPlacement(
                    ringStart + i,
                    k,
                    new Vec2(center.X + (float)(radius * Math.Cos(rad)), center.Y + (float)(radius * Math.Sin(rad))),
                    radius,
                    (float)degrees,
                    pitch / 2f);
            }

            index += onRing;
        }
    }

    /// <summary>
    /// Places an Auto-sized wheel for <paramref name="count"/> pins: <see cref="Capacity.AutoWheelRings"/> rings, the
    /// slots spread by <see cref="ComputeSpread"/> around the centre, and the disc plus label plate edge-dodged on
    /// <paramref name="screen"/>. Count 0 gives a hub-only wheel of one ring; a count above the Auto maximum is clamped to it.
    /// </summary>
    public static PlacedAutoWheel PlaceAuto(
        int count,
        float buttonSize,
        Vec2 mouse,
        Vec2 screen,
        float plateHeight = LayoutConstants.PlateHeight)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must not be negative.");
        }

        count = Math.Min(count, RingCapacity.Total(LayoutConstants.AutoMaxRings));

        var rings = Capacity.AutoWheelRings(count);
        var center = mouse + EdgeDodger.Fit(MenuBounds.ForWheel(mouse, buttonSize, rings, plateHeight), screen);
        var slots = new SlotPlacement[count];
        ComputeSpread(count, buttonSize, center, slots);
        return new PlacedAutoWheel(center, rings, slots);
    }
}

/// <summary>An Auto wheel placed by <see cref="WheelLayout.PlaceAuto"/>.</summary>
public readonly record struct PlacedAutoWheel(Vec2 Center, int Rings, SlotPlacement[] Slots);

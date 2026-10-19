using System;
using System.Diagnostics;

using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Perf;

/// <summary>P-10: layout for 40+ slots is computed on open only. Gate at 5 x 50 us (performance.md section 4).</summary>
[Trait("Category", "Perf")]
public class LayoutTimingTests
{
    private const double BudgetMicroseconds = 50.0;
    private const double GateFactor = 5.0;
    private const int Batch = 200;

    [Fact]
    public void WheelLayout_and_EdgeDodger_for_40_slots_stay_under_five_times_the_budget()
    {
        const int rings = 4; // 6 + 12 + 18 + 24 = 60 slots, more than the 40 of the workload model
        Assert.True(RingCapacity.Total(rings) >= 40);
        var screen = new Vec2(960f, 540f);
        var center = new Vec2(30f, 500f);
        float sink = 0f;

        void Once()
        {
            SlotPlacement[] slots = WheelLayout.Compute(new WheelParams(56f, rings, center));
            Rect2 bounds = MenuBounds.ForWheel(center, 56f, rings);
            sink += slots[^1].Center.X + EdgeDodger.Fit(bounds, screen).X;
        }

        for (int i = 0; i < Batch; i++)
        {
            Once();
        }

        double[] us = new double[5];
        for (int run = 0; run < us.Length; run++)
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < Batch; i++)
            {
                Once();
            }

            us[run] = Stopwatch.GetElapsedTime(start).TotalMilliseconds * 1000.0 / Batch;
        }

        Array.Sort(us);
        Assert.True(us[2] <= BudgetMicroseconds * GateFactor, $"median {us[2]:F2} us per layout+dodge, limit {BudgetMicroseconds * GateFactor} us");
        Assert.NotEqual(float.NaN, sink);
    }

    private static void AssertMedianUnderGate(Func<float> once, string what)
    {
        float sink = 0f;
        for (int i = 0; i < Batch; i++)
        {
            sink += once();
        }

        double[] us = new double[5];
        for (int run = 0; run < us.Length; run++)
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < Batch; i++)
            {
                sink += once();
            }

            us[run] = Stopwatch.GetElapsedTime(start).TotalMilliseconds * 1000.0 / Batch;
        }

        Array.Sort(us);
        Assert.True(us[2] <= BudgetMicroseconds * GateFactor, $"{what}: median {us[2]:F2} us, limit {BudgetMicroseconds * GateFactor} us");
        Assert.False(float.IsNaN(sink));
    }

    [Fact]
    public void Grid_Place_Auto_and_one_sub_grid_for_40_slots_stay_under_five_times_the_budget()
    {
        const int columns = 7;
        var screen = new Vec2(960f, 540f);
        AssertMedianUnderGate(
            () =>
            {
                int rows = Capacity.AutoGridRows(40, columns);
                bool full = Capacity.IsAutoGridFull(40, columns, 56f, screen, 168f);
                PlacedGrid grid = GridLayout.Place(56f, columns, 7, new Vec2(30f, 500f), screen, 168f);
                PlacedSubGrid sub = SubMenuLayout.PlaceSubGrid(12, 56f, grid.Panel, grid.Center.X, grid.Slots[10].Center, screen, 168f);
                return grid.Slots[^1].Center.X + sub.Cells[^1].Center.Y + rows + (full ? 1 : 0);
            },
            "grid Place + Auto + sub-grid");
    }

    [Fact]
    public void Auto_wheel_and_one_satellite_for_40_slots_stay_under_five_times_the_budget()
    {
        var screen = new Vec2(960f, 540f);
        var center = new Vec2(30f, 500f);
        AssertMedianUnderGate(
            () =>
            {
                int rings = Capacity.AutoWheelRings(40);
                bool full = Capacity.IsAutoWheelFull(40, 56f, screen);
                SlotPlacement[] slots = WheelLayout.Compute(new WheelParams(56f, rings, center));
                Rect2 bounds = MenuBounds.ForWheel(center, 56f, rings);
                PlacedSatellite sat = SubMenuLayout.PlaceSatellite(12, 56f, slots[7].Center, screen);
                return slots[^1].Center.X + EdgeDodger.Fit(bounds, screen).X + sat.Slots[^1].Center.Y + (full ? 1 : 0);
            },
            "Auto wheel + satellite");
    }
}

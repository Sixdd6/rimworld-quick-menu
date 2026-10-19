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
}

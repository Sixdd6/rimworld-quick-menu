using System;

using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Perf;

[Trait("Category", "Perf")]
public class SubMenuLayoutAllocTests
{
    [Fact]
    public void Satellite_slot_computation_allocates_nothing()
    {
        var buffer = new SlotPlacement[60];
        long sum = 0;
        void Body(int i)
        {
            var n = 2 + (i % 59);
            sum += SubMenuLayout.RingsFor(n) + SubMenuLayout.SubGridColumns(n) + (int)SubMenuLayout.Classify(n);
            SubMenuLayout.ComputeSatelliteSlots(n, 56f, new Vec2(500f, 400f), buffer);
        }

        for (var i = 0; i < 1_000; i++)
        {
            Body(i);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            Body(i);
        }

        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.True(sum > 0);
    }
}

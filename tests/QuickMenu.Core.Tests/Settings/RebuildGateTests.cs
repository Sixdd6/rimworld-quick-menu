using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests.Settings;

public class RebuildGateTests
{
    private const long Debounce = RebuildGate.DebounceMs;

    private static RebuildGate Started(long key = 1)
    {
        var gate = new RebuildGate();
        Assert.True(gate.Observe(key, dragging: false, nowMs: 0)); // the first value is built at once
        return gate;
    }

    [Fact]
    public void First_observation_commits_so_the_preview_has_textures()
    {
        var gate = new RebuildGate();
        Assert.True(gate.Observe(5, dragging: true, nowMs: 100));
        Assert.False(gate.Pending);
    }

    [Fact]
    public void Value_changes_while_dragging_never_rebuild()
    {
        RebuildGate gate = Started();
        long now = 10;
        for (long key = 2; key <= 40; key++)
        {
            Assert.False(gate.Observe(key, dragging: true, nowMs: now));
            Assert.True(gate.Pending);
            now += 1000; // even a long pause does not rebuild while the button is held
        }
    }

    [Fact]
    public void Release_rebuilds_once_at_the_final_value()
    {
        RebuildGate gate = Started();
        gate.Observe(2, true, 10);
        gate.Observe(3, true, 20);
        Assert.True(gate.Observe(3, false, 30));
        Assert.False(gate.Pending);
        Assert.False(gate.Observe(3, false, 40));
        Assert.False(gate.Observe(3, false, 40 + (10 * Debounce)));
    }

    [Fact]
    public void Dragging_back_to_the_built_value_rebuilds_nothing()
    {
        RebuildGate gate = Started(1);
        gate.Observe(2, true, 10);
        gate.Observe(1, true, 20);
        Assert.False(gate.Observe(1, false, 30));
        Assert.False(gate.Pending);
    }

    [Fact]
    public void Unchanged_value_never_rebuilds()
    {
        RebuildGate gate = Started();
        for (long t = 1; t < 50; t++)
        {
            Assert.False(gate.Observe(1, false, t * 100));
        }
    }

    [Fact]
    public void A_change_without_a_drag_waits_for_the_quiet_period()
    {
        RebuildGate gate = Started();
        Assert.False(gate.Observe(2, false, 1000));
        Assert.False(gate.Observe(2, false, 1000 + Debounce - 1));
        Assert.True(gate.Observe(2, false, 1000 + Debounce));
    }

    [Fact]
    public void Each_new_change_restarts_the_quiet_period()
    {
        RebuildGate gate = Started();
        Assert.False(gate.Observe(2, false, 1000));
        Assert.False(gate.Observe(3, false, 1000 + Debounce - 10)); // restarts
        Assert.False(gate.Observe(3, false, 1000 + Debounce + 100));
        Assert.True(gate.Observe(3, false, 1000 + (2 * Debounce) - 10));
    }

    [Fact]
    public void Two_separate_drags_rebuild_twice()
    {
        RebuildGate gate = Started();
        int rebuilds = 0;
        long now = 100;
        for (int drag = 0; drag < 2; drag++)
        {
            for (long k = 2; k < 30; k++)
            {
                if (gate.Observe((drag * 100) + k, true, now++))
                {
                    rebuilds++;
                }
            }

            if (gate.Observe((drag * 100) + 29, false, now++))
            {
                rebuilds++;
            }

            now += 5000;
        }

        Assert.Equal(2, rebuilds);
    }
}

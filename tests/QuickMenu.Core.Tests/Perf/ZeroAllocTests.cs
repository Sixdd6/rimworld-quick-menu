using System;

using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Core.Tests.Perf;

/// <summary>
/// Strict zero-allocation gates for the per-frame Core paths (performance.md section 4: P-2, P-3, P-9 Core part).
/// Each loop is warmed up first, then 10,000 calls must allocate exactly 0 bytes.
/// </summary>
[Trait("Category", "Perf")]
public class ZeroAllocTests
{
    private const int Warmup = 1_000;
    private const int Calls = 10_000;

    private static long AllocatedBy(Action<int> body)
    {
        for (int i = 0; i < Warmup; i++)
        {
            body(i);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Calls; i++)
        {
            body(i);
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static PinClickInput Click(RawEventType type, bool key, bool rawCtrl, bool evAlt = false) =>
        new(key, type, 0, false, evAlt, false, rawCtrl, false, false, false, false);

    [Fact]
    public void PinClickClassifier_and_ModifierPolicy_allocate_nothing()
    {
        PinClickInput pin = Click(RawEventType.MouseUp, key: false, rawCtrl: true);
        PinClickInput pass = Click(RawEventType.KeyDown, key: true, rawCtrl: false, evAlt: true);
        long sum = 0;
        long bytes = AllocatedBy(i =>
        {
            PinModifier m = ModifierPolicy.Effective(i % 3 == 0, PinModifier.Shift, i % 2 == 0);
            sum += (int)PinClickClassifier.Classify(pin, m);
            sum += (int)PinClickClassifier.Classify(pass, m);
        });

        Assert.Equal(0L, bytes);
        Assert.True(sum > 0);
    }

    [Fact]
    public void PinClickDeduper_allocates_nothing()
    {
        var deduper = new PinClickDeduper();
        PinClickInput input = Click(RawEventType.MouseUp, key: false, rawCtrl: true);
        int sum = 0;
        long bytes = AllocatedBy(i => sum += deduper.Accept(i / 2, i % 2 == 0 ? PinClickDeduper.NoPress : i / 2, input) ? 1 : 0);

        Assert.Equal(0L, bytes);
        Assert.True(sum >= 0);
    }

    [Fact]
    public void TriggerStateMachine_Handle_allocates_nothing()
    {
        var m = new TriggerStateMachine();
        var origin = new Vec2(100, 100);
        TriggerEvent[] events =
        {
            TriggerEvent.TriggerPressed(origin),
            TriggerEvent.MouseMove(new Vec2(150, 150)),
            TriggerEvent.MouseDown(origin, Hit.Slot(1), 0),
            TriggerEvent.MouseUp(origin, Hit.Slot(1), SlotKind.Submenu, 0),
            TriggerEvent.MouseDown(origin, Hit.HubEdit, 0),
            TriggerEvent.MouseUp(origin, Hit.HubEdit, SlotKind.Normal, 0),
            TriggerEvent.Escape(origin),
            TriggerEvent.MouseDown(origin, Hit.Outside, 1),
            TriggerEvent.MapLost(),
        };

        long bytes = AllocatedBy(i => m.Handle(events[i % events.Length]));

        Assert.Equal(0L, bytes);
    }

    [Fact]
    public void HitTester_Hit_and_WheelHitTest_allocate_nothing()
    {
        var center = new Vec2(500f, 400f);
        SlotPlacement[] slots = WheelLayout.Compute(new WheelParams(56f, 3, center));
        var buttons = new[] { new HitButton(new Rect2(0f, 0f, 10f, 10f), 1) };
        var layout = new HitLayout(center, 56f, LayoutConstants.DiscRadius(56f, 3), slots, slots.Length, new Rect2(0f, 500f, 100f, 30f), buttons, buttons.Length, true);
        int sink = 0;
        long bytes = AllocatedBy(i =>
        {
            var p = new Vec2(400f + (i % 200), 300f + (i % 180));
            sink += (int)HitTester.Hit(p, layout).Kind;
            sink += WheelHitTest.SlotAt(slots, slots.Length, p);
        });

        Assert.Equal(0L, bytes);
        Assert.NotEqual(int.MinValue, sink);
    }

    [Fact]
    public void EdgeDodger_Fit_allocates_nothing()
    {
        var screen = new Vec2(960f, 540f);
        Rect2 near = MenuBounds.ForWheel(new Vec2(10f, 530f), 56f, 2);
        Rect2 inside = MenuBounds.ForWheel(new Vec2(480f, 270f), 56f, 2);
        float sum = 0f;
        long bytes = AllocatedBy(i => sum += EdgeDodger.Fit(i % 2 == 0 ? near : inside, screen).X);

        Assert.Equal(0L, bytes);
        Assert.True(sum > 0f);
    }
}

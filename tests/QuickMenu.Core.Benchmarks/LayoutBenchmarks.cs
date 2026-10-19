using BenchmarkDotNet.Attributes;

using QuickMenu.Core.Geometry;
using QuickMenu.Core.Input;
using QuickMenu.Core.Layout;

namespace QuickMenu.Benchmarks;

/// <summary>P-3 (pin-click fast path), P-9 Core part, P-10 (layout for 40+ slots).</summary>
[MemoryDiagnoser]
public class LayoutBenchmarks
{
    private static readonly Vec2 _screen = new(960f, 540f);
    private static readonly Vec2 _center = new(30f, 500f);

    private SlotPlacement[] _slots = null!;
    private HitLayout _layout;
    private Rect2 _bounds;
    private PinClickInput _fastPath;
    private readonly TriggerStateMachine _machine = new();
    private TriggerEvent _move;
    private WheelParams _wheel;
    private int _i;

    [GlobalSetup]
    public void Setup()
    {
        _wheel = new WheelParams(56f, 4, _center);
        _slots = WheelLayout.Compute(_wheel);
        _bounds = MenuBounds.ForWheel(_center, 56f, 4);
        _layout = new HitLayout(_center, 56f, LayoutConstants.DiscRadius(56f, 4), _slots, _slots.Length);
        _fastPath = new PinClickInput(false, RawEventType.MouseUp, 1, false, false, false, false, false, false, false, false);
        _move = TriggerEvent.MouseMove(new Vec2(150, 150));
    }

    [Benchmark]
    public SlotPlacement[] WheelLayout_60Slots() => WheelLayout.Compute(_wheel);

    [Benchmark]
    public Vec2 EdgeDodger_Fit() => EdgeDodger.Fit(_bounds, _screen);

    [Benchmark]
    public Hit HitTester_Hit() => HitTester.Hit(new Vec2(40f + (_i++ % 100), 440f), _layout);

    [Benchmark]
    public int WheelHitTest_SlotAt() => WheelHitTest.SlotAt(_slots, _slots.Length, new Vec2(40f + (_i++ % 100), 440f));

    [Benchmark]
    public PinClickAction PinClick_FastPath() =>
        PinClickClassifier.Classify(_fastPath, ModifierPolicy.Effective(false, PinModifier.Ctrl, false));

    [Benchmark]
    public MenuCommand TriggerStateMachine_Handle() => _machine.Handle(_move);
}

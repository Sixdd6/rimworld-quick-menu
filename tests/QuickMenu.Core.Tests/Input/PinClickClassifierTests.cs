using System;
using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class PinClickClassifierTests
{
    private static PinClickInput Click(
        RawEventType type = RawEventType.MouseUp,
        int button = 0,
        bool key = false,
        bool evCtrl = false,
        bool evAlt = false,
        bool evShift = false,
        bool rawCtrl = false,
        bool rawAlt = false,
        bool rawShift = false) =>
        new(key, type, button, evCtrl, evAlt, evShift, rawCtrl, rawAlt, rawShift);

    [Theory]
    [InlineData(RawEventType.MouseDown)]
    [InlineData(RawEventType.MouseUp)]
    public void Left_click_with_event_flag_pins(RawEventType type)
    {
        Assert.Equal(PinClickAction.Pin, PinClickClassifier.Classify(Click(type, evCtrl: true), PinModifier.Ctrl));
    }

    [Fact]
    public void Raw_key_alone_pins_dropdown_entry_case()
    {
        Assert.Equal(PinClickAction.Pin, PinClickClassifier.Classify(Click(rawCtrl: true), PinModifier.Ctrl));
        Assert.Equal(PinClickAction.Pin, PinClickClassifier.Classify(Click(rawAlt: true), PinModifier.Alt));
        Assert.Equal(PinClickAction.Pin, PinClickClassifier.Classify(Click(rawShift: true), PinModifier.Shift));
        Assert.Equal(PinClickAction.Pin, PinClickClassifier.Classify(Click(evAlt: true), PinModifier.Alt));
        Assert.Equal(PinClickAction.Pin, PinClickClassifier.Classify(Click(evShift: true), PinModifier.Shift));
    }

    [Fact]
    public void Wrong_modifier_passes_through()
    {
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(Click(rawCtrl: true), PinModifier.Alt));
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(Click(evAlt: true, rawAlt: true), PinModifier.Ctrl));
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(Click(evCtrl: true), PinModifier.Shift));
    }

    [Fact]
    public void No_modifier_passes_through()
    {
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(Click(), PinModifier.Ctrl));
    }

    [Fact]
    public void Key_events_pass_through_even_with_modifier()
    {
        var i = Click(RawEventType.KeyDown, key: true, evCtrl: true, rawCtrl: true);
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(i, PinModifier.Ctrl));
        var j = Click(RawEventType.MouseUp, key: true, rawCtrl: true);
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(j, PinModifier.Ctrl));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Non_left_buttons_pass_through(int button)
    {
        Assert.Equal(
            PinClickAction.PassThrough,
            PinClickClassifier.Classify(Click(button: button, evCtrl: true, rawCtrl: true), PinModifier.Ctrl));
    }

    [Theory]
    [InlineData(RawEventType.Other)]
    [InlineData(RawEventType.KeyDown)]
    [InlineData(RawEventType.KeyUp)]
    public void Non_mouse_raw_types_pass_through(RawEventType type)
    {
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(Click(type, rawCtrl: true), PinModifier.Ctrl));
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void Classify_and_policy_allocate_nothing()
    {
        var pin = Click(rawCtrl: true);
        var pass = Click(RawEventType.KeyDown, key: true);
        long sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += (int)PinClickClassifier.Classify(pin, ModifierPolicy.Effective(false, PinModifier.Alt, false));
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            var m = ModifierPolicy.Effective(i % 3 == 0, PinModifier.Shift, i % 2 == 0);
            sum += (int)PinClickClassifier.Classify(pin, m);
            sum += (int)PinClickClassifier.Classify(pass, m);
        }

        var after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, after - before);
        Assert.True(sum > 0);
    }
}

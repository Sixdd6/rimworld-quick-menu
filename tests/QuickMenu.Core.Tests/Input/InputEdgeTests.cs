using System.Linq;
using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class InputEdgeTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(303)]
    [InlineData(int.MaxValue)]
    public void Key_binding_round_trips_through_text(int code)
    {
        var b = TriggerBinding.Key(code);
        Assert.True(TriggerBinding.TryParse(b.Serialize(), out var back));
        Assert.Equal(b, back);
        Assert.Equal(b.GetHashCode(), back.GetHashCode());
    }

    [Fact]
    public void All_valid_mouse_buttons_round_trip_and_labels_are_distinct_keyed_ids()
    {
        var labels = new string?[3];
        for (int i = 2; i <= 4; i++)
        {
            var b = TriggerBinding.Mouse(i);
            Assert.True(TriggerBinding.TryParse(b.Serialize(), out var back));
            Assert.Equal(b, back);
            labels[i - 2] = b.LabelKey;
            Assert.StartsWith("QuickMenu.Binding.", b.LabelKey);
        }

        Assert.Equal(3, labels.Distinct().Count());
    }

    [Theory]
    [InlineData("mouse:0")]
    [InlineData("mouse:1")]
    [InlineData("mouse:5")]
    [InlineData("mouse:-2")]
    [InlineData("mouse:+2")]
    [InlineData("mouse: 2")]
    [InlineData("mouse:2 ")]
    [InlineData("key:0")]
    [InlineData("key:-1")]
    [InlineData("key:2147483648")]
    [InlineData("key:")]
    [InlineData("KEY:5")]
    [InlineData("None")]
    [InlineData("none ")]
    [InlineData("key:5:6")]
    [InlineData("key:٥")]
    [InlineData("")]
    public void Garbage_binding_text_is_rejected_and_yields_none(string text)
    {
        Assert.False(TriggerBinding.TryParse(text, out var b));
        Assert.Equal(TriggerBinding.None, b);
    }

    [Fact]
    public void Parse_failure_does_not_leave_a_previous_value()
    {
        Assert.False(TriggerBinding.TryParse("mouse:9", out var b));
        Assert.Equal(TriggerBindingKind.None, b.Kind);
        Assert.Equal("QuickMenu.Binding.Unbound", b.LabelKey);
    }

    [Theory]
    [InlineData(false, PinModifier.Shift, false, PinModifier.Ctrl)]
    [InlineData(false, PinModifier.Shift, true, PinModifier.Alt)]
    [InlineData(true, PinModifier.Shift, false, PinModifier.Shift)]
    [InlineData(true, PinModifier.Ctrl, true, PinModifier.Ctrl)]
    [InlineData(true, PinModifier.Alt, false, PinModifier.Alt)]
    public void Modifier_policy_table(bool userSet, PinModifier choice, bool mint, PinModifier expected) =>
        Assert.Equal(expected, ModifierPolicy.Effective(userSet, choice, mint));

    private static PinClickInput Used(bool downFrame, bool upFrame, bool ctrlRaw = false, bool altEvent = false) =>
        new(false, RawEventType.Used, 0, false, altEvent, false, ctrlRaw, false, false, downFrame, upFrame);

    [Fact]
    public void Other_modifiers_do_not_pin_and_wrong_modifier_does_not_pin()
    {
        Assert.Equal(PinClickAction.Pin, PinClickClassifier.Classify(Used(true, false, ctrlRaw: true), PinModifier.Ctrl));
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(Used(true, false, ctrlRaw: true), PinModifier.Alt));
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(Used(true, false, ctrlRaw: true), PinModifier.Shift));
        Assert.Equal(PinClickAction.Pin, PinClickClassifier.Classify(Used(false, true, altEvent: true), PinModifier.Alt));
    }

    [Fact]
    public void Held_button_without_a_transition_and_idle_frames_pass_through()
    {
        var held = new PinClickInput(false, RawEventType.Used, 0, true, false, false, true, false, false, false, false);
        Assert.Equal(PinClickAction.PassThrough, PinClickClassifier.Classify(held, PinModifier.Ctrl));
    }

    private static PinClickInput Down() => Used(true, false, ctrlRaw: true);

    private static PinClickInput Up() => Used(false, true, ctrlRaw: true);

    [Fact]
    public void Deduper_over_many_frames_pins_each_physical_click_once()
    {
        var d = new PinClickDeduper();
        int accepted = 0;
        int frame = 100;
        for (int click = 0; click < 5; click++)
        {
            var downFrame = frame;
            if (d.Accept(frame, frame, Down()))
            {
                accepted++;
            }

            // Re-entry in the same frame, then held frames never reach the deduper.
            Assert.False(d.Accept(frame, frame, Down()));
            frame += 7;
            if (d.Accept(frame, downFrame, Up()))
            {
                accepted++;
            }

            frame += 3;
        }

        Assert.Equal(5, accepted);
    }

    [Fact]
    public void Deduper_handles_frame_counter_at_extremes_and_zero()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(0, 0, Down()));
        Assert.False(d.Accept(1, 0, Up()));
        Assert.True(d.Accept(int.MaxValue, int.MaxValue, Down()));
        Assert.False(d.Accept(int.MaxValue, int.MaxValue, Up()));
        Assert.True(d.Accept(int.MinValue + 1, int.MinValue + 1, Down()));
    }

    [Fact]
    public void Deduper_up_in_the_down_frame_after_down_is_ignored()
    {
        var d = new PinClickDeduper();
        Assert.True(d.Accept(5, 5, Down()));
        Assert.False(d.Accept(5, 5, Up()));
        Assert.False(d.Accept(6, 5, Up()));
    }
}

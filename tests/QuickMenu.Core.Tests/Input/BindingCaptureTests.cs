using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class BindingCaptureTests
{
    [Fact]
    public void Escape_cancels()
    {
        CaptureDecision d = BindingCapture.DecideKey(27);
        Assert.Equal(CaptureOutcome.Cancel, d.Outcome);
    }

    [Theory]
    [InlineData(113)]
    [InlineData(97)]
    [InlineData(32)]
    public void Key_is_accepted(int code)
    {
        CaptureDecision d = BindingCapture.DecideKey(code);
        Assert.Equal(CaptureOutcome.Accept, d.Outcome);
        Assert.Equal(TriggerBinding.Key(code), d.Binding);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(323)]
    [InlineData(329)]
    public void None_and_mouse_key_codes_are_ignored(int code) =>
        Assert.Equal(CaptureOutcome.Ignore, BindingCapture.DecideKey(code).Outcome);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Left_and_right_are_rejected(int button)
    {
        CaptureDecision d = BindingCapture.DecideMouse(button);
        Assert.Equal(CaptureOutcome.RejectLeftRight, d.Outcome);
        Assert.Equal(TriggerBinding.None, d.Binding);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Middle_and_side_buttons_are_accepted(int button)
    {
        CaptureDecision d = BindingCapture.DecideMouse(button);
        Assert.Equal(CaptureOutcome.Accept, d.Outcome);
        Assert.Equal(TriggerBinding.Mouse(button), d.Binding);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(-1)]
    public void Other_buttons_are_ignored(int button) =>
        Assert.Equal(CaptureOutcome.Ignore, BindingCapture.DecideMouse(button).Outcome);
}

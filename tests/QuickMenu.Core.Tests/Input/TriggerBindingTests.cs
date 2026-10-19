using System;
using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class TriggerBindingTests
{
    [Fact]
    public void Labels_are_keyed_ids()
    {
        Assert.Equal("QuickMenu.Binding.Unbound", TriggerBinding.None.LabelKey);
        Assert.Equal("QuickMenu.Binding.MiddleMouse", TriggerBinding.Mouse(2).LabelKey);
        Assert.Equal("QuickMenu.Binding.Mouse4", TriggerBinding.Mouse(3).LabelKey);
        Assert.Equal("QuickMenu.Binding.Mouse5", TriggerBinding.Mouse(4).LabelKey);
    }

    [Fact]
    public void Key_has_no_label_key_and_keeps_code()
    {
        var k = TriggerBinding.Key(113);
        Assert.Null(k.LabelKey);
        Assert.Equal(TriggerBindingKind.Key, k.Kind);
        Assert.Equal(113, k.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(-1)]
    public void Invalid_mouse_buttons_are_rejected(int button)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TriggerBinding.Mouse(button));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Invalid_key_codes_are_rejected(int code)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TriggerBinding.Key(code));
    }

    [Fact]
    public void Default_is_none()
    {
        Assert.Equal(TriggerBinding.None, default(TriggerBinding));
        Assert.Equal(TriggerBindingKind.None, default(TriggerBinding).Kind);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("key:113")]
    [InlineData("mouse:2")]
    [InlineData("mouse:3")]
    [InlineData("mouse:4")]
    public void Serialize_parse_round_trips(string text)
    {
        Assert.True(TriggerBinding.TryParse(text, out var b));
        Assert.Equal(text, b.Serialize());
        Assert.Equal(text, b.ToString());
        Assert.True(TriggerBinding.TryParse(b.Serialize(), out var again));
        Assert.Equal(b, again);
        Assert.True(b == again);
        Assert.False(b != again);
        Assert.Equal(b.GetHashCode(), again.GetHashCode());
        Assert.True(b.Equals((object)again));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("None")]
    [InlineData("mouse:0")]
    [InlineData("mouse:1")]
    [InlineData("mouse:5")]
    [InlineData("mouse:")]
    [InlineData("mouse:-2")]
    [InlineData("mouse:x")]
    [InlineData("key:0")]
    [InlineData("key:-5")]
    [InlineData("key:")]
    [InlineData("key:99999999999")]
    [InlineData("pad:3")]
    public void Invalid_text_fails_and_yields_none(string? text)
    {
        Assert.False(TriggerBinding.TryParse(text, out var b));
        Assert.Equal(TriggerBinding.None, b);
    }

    [Fact]
    public void Different_bindings_are_not_equal()
    {
        Assert.NotEqual(TriggerBinding.Mouse(3), TriggerBinding.Mouse(4));
        Assert.NotEqual(TriggerBinding.Key(3), TriggerBinding.Mouse(3));
        Assert.False(TriggerBinding.None.Equals("x"));
    }
}

using QuickMenu.Core.Render;
using Xunit;

namespace QuickMenu.Core.Tests.Render;

public class HubStyleTests
{
    [Theory]
    [InlineData(HubPartState.Normal, 1f)]
    [InlineData(HubPartState.Hover, 2f)]
    [InlineData(HubPartState.Pressed, 2f)]
    public void Use_mode_rings_are_1_then_2_px(HubPartState state, float width) =>
        Assert.Equal(width, HubStyle.RingWidth(state, editing: false));

    [Theory]
    [InlineData(HubPartState.Normal, 2f)]
    [InlineData(HubPartState.Hover, 3f)]
    [InlineData(HubPartState.Pressed, 3f)]
    public void Edit_mode_rings_are_2_then_3_px(HubPartState state, float width) =>
        Assert.Equal(width, HubStyle.RingWidth(state, editing: true));

    [Fact]
    public void Glyph_is_the_pencil_in_use_mode_and_the_check_mark_in_edit_mode()
    {
        Assert.Equal(MaskId.IconEdit, HubStyle.Glyph(false));
        Assert.Equal(MaskId.IconDone, HubStyle.Glyph(true));
    }

    [Fact]
    public void Edit_tint_is_22_percent() => Assert.Equal(0.22f, HubStyle.EditTintAlpha);
}

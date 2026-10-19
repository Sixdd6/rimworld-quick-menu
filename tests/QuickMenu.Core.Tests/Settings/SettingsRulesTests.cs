using QuickMenu.Core.Layout;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests.Settings;

public class SettingsRulesTests
{
    [Theory]
    [InlineData(-3, 1)]
    [InlineData(0, 1)]
    [InlineData(2, 2)]
    [InlineData(5, 5)]
    [InlineData(99, 5)]
    public void Rings_are_clamped(int input, int expected) => Assert.Equal(expected, SettingsRules.ClampRings(input));

    [Theory]
    [InlineData(-1, 3)]
    [InlineData(4, 5)]
    [InlineData(5, 5)]
    [InlineData(10, 11)]
    [InlineData(12, 11)]
    [InlineData(1000, 11)]
    public void Columns_are_odd_and_clamped(int input, int expected) => Assert.Equal(expected, SettingsRules.ClampColumns(input));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 3)]
    [InlineData(3, 3)]
    [InlineData(6, 7)]
    [InlineData(8, 7)]
    public void Rows_are_odd_and_clamped(int input, int expected) => Assert.Equal(expected, SettingsRules.ClampRows(input));

    [Theory]
    [InlineData(10f, 36f)]
    [InlineData(36f, 36f)]
    [InlineData(57f, 56f)]
    [InlineData(59f, 60f)]
    [InlineData(500f, 96f)]
    [InlineData(float.NaN, 56f)]
    public void Button_size_snaps_to_step(float input, float expected) => Assert.Equal(expected, SettingsRules.ClampButtonSize(input));

    [Fact]
    public void Defaults_match_architecture()
    {
        Assert.Equal(1, SettingsRules.SchemaVersion);
        Assert.Equal(2, SettingsRules.DefaultRings);
        Assert.Equal("mouse:3", SettingsRules.DefaultTrigger);
        Assert.Equal(56, SettingsRules.DefaultButtonSize);
        Assert.Equal(SettingsRules.DefaultColumns, SettingsRules.ClampColumns(SettingsRules.DefaultColumns));
        Assert.Equal(SettingsRules.DefaultRows, SettingsRules.ClampRows(SettingsRules.DefaultRows));
    }

    [Fact]
    public void Enum_definedness()
    {
        Assert.True(SettingsRules.IsDefined(MenuLayout.Grid));
        Assert.False(SettingsRules.IsDefined((SizingMode)7));
    }

    [Fact]
    public void Wheel_capacity_fixed_and_auto()
    {
        Assert.Equal(RingCapacity.Total(2), SettingsRules.PinCapacity(MenuLayout.Wheel, SizingMode.Fixed, 2, 5, 3));
        Assert.Equal(RingCapacity.Total(5), SettingsRules.PinCapacity(MenuLayout.Wheel, SizingMode.Fixed, 50, 5, 3));
        Assert.Equal(RingCapacity.Total(8), SettingsRules.PinCapacity(MenuLayout.Wheel, SizingMode.Auto, 2, 5, 3));
    }

    [Fact]
    public void Grid_capacity_excludes_hub_cell()
    {
        Assert.Equal(14, SettingsRules.PinCapacity(MenuLayout.Grid, SizingMode.Fixed, 2, 5, 3));
        Assert.Equal((5 * 9) - 1, SettingsRules.PinCapacity(MenuLayout.Grid, SizingMode.Auto, 2, 5, 3));
        Assert.Equal((11 * 7) - 1, SettingsRules.PinCapacity(MenuLayout.Grid, SizingMode.Fixed, 2, 99, 99));
    }
}

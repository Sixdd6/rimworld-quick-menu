using System;
using QuickMenu.Core.Layout;
using Xunit;

namespace QuickMenu.Tests.Layout;

public class RingCapacityTests
{
    [Theory]
    [InlineData(1, 6)]
    [InlineData(2, 12)]
    [InlineData(3, 18)]
    [InlineData(4, 25)]
    [InlineData(5, 31)]
    [InlineData(6, 37)]
    [InlineData(7, 43)]
    [InlineData(8, 50)]
    public void Of_MatchesDesignTable(int k, int expected)
    {
        Assert.Equal(expected, RingCapacity.Of(k));
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(2, 18)]
    [InlineData(3, 36)]
    [InlineData(4, 61)]
    [InlineData(5, 92)]
    public void Total_MatchesRingsSetting(int rings, int expected)
    {
        Assert.Equal(expected, RingCapacity.Total(rings));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Of_RejectsRingBelowOne(int k)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RingCapacity.Of(k));
    }
}

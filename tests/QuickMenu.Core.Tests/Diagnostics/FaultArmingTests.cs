using System;
using QuickMenu.Core.Diagnostics;
using Xunit;

namespace QuickMenu.Core.Tests.Diagnostics;

public class FaultArmingTests
{
    [Fact]
    public void NewInstance_HasNothingArmed()
    {
        var arming = new FaultArming();

        Assert.False(arming.Any);
        Assert.False(arming.TryConsume(FaultPoint.PinPrefix));
    }

    [Fact]
    public void Arm_ThenConsume_FiresExactlyOnce()
    {
        var arming = new FaultArming();
        arming.Arm(FaultPoint.PinPrefix);

        Assert.True(arming.Any);
        Assert.True(arming.IsArmed(FaultPoint.PinPrefix));
        Assert.True(arming.TryConsume(FaultPoint.PinPrefix));
        Assert.False(arming.TryConsume(FaultPoint.PinPrefix));
        Assert.False(arming.Any);
    }

    [Fact]
    public void Points_AreIndependent()
    {
        var arming = new FaultArming();
        arming.Arm(FaultPoint.MenuOpen);
        arming.Arm(FaultPoint.SettingsPage);

        Assert.False(arming.TryConsume(FaultPoint.WindowDraw));
        Assert.True(arming.TryConsume(FaultPoint.MenuOpen));
        Assert.True(arming.Any);
        Assert.True(arming.IsArmed(FaultPoint.SettingsPage));
    }

    [Fact]
    public void Clear_DisarmsEverything()
    {
        var arming = new FaultArming();
        foreach (FaultPoint p in Enum.GetValues<FaultPoint>())
        {
            arming.Arm(p);
        }

        arming.Clear();

        Assert.False(arming.Any);
    }

    [Fact]
    public void EveryPoint_FitsTheMask_AndOutOfRangeThrows()
    {
        var arming = new FaultArming();
        Assert.True(FaultArming.PointCount <= 31);
        foreach (FaultPoint p in Enum.GetValues<FaultPoint>())
        {
            arming.Arm(p);
            Assert.True(arming.TryConsume(p));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => arming.Arm((FaultPoint)99));
    }
}

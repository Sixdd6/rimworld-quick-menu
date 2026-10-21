using QuickMenu.Core.Input;
using QuickMenu.Core.Materials;
using Xunit;

namespace QuickMenu.Core.Tests.Materials;

public class StuffSlotPolicyTests
{
    [Theory]
    [InlineData(StuffRingOutcome.None, true, SlotKind.Disabled)]
    [InlineData(StuffRingOutcome.Skip, true, SlotKind.Normal)]
    [InlineData(StuffRingOutcome.Ring, true, SlotKind.Submenu)]
    [InlineData(StuffRingOutcome.None, false, SlotKind.Disabled)]
    [InlineData(StuffRingOutcome.Skip, false, SlotKind.Disabled)]
    [InlineData(StuffRingOutcome.Ring, false, SlotKind.Disabled)]
    public void Kind_follows_outcome_and_usability(StuffRingOutcome outcome, bool usable, SlotKind expected) =>
        Assert.Equal(expected, StuffSlotPolicy.KindFor(outcome, usable));

    [Theory]
    [InlineData(StuffRingOutcome.None, true, true)]
    [InlineData(StuffRingOutcome.None, false, false)]
    [InlineData(StuffRingOutcome.Skip, true, false)]
    [InlineData(StuffRingOutcome.Ring, true, false)]
    public void No_materials_reason_only_for_usable_slots_without_stuff(StuffRingOutcome outcome, bool usable, bool expected) =>
        Assert.Equal(expected, StuffSlotPolicy.IsNoMaterials(outcome, usable));
}

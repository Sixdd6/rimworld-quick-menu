using QuickMenu.Core.Input;

namespace QuickMenu.Core.Materials;

/// <summary>How a stuff build is shown as a slot (FR-12), decided from its material outcome.</summary>
public static class StuffSlotPolicy
{
    /// <summary>
    /// The slot kind of a stuff build: an unusable designator stays <see cref="SlotKind.Disabled"/> whatever the materials;
    /// no materials grey the slot; one material activates directly; two or more open the material ring.
    /// </summary>
    public static SlotKind KindFor(StuffRingOutcome outcome, bool usable)
    {
        if (!usable)
        {
            return SlotKind.Disabled;
        }

        return outcome switch
        {
            StuffRingOutcome.None => SlotKind.Disabled,
            StuffRingOutcome.Ring => SlotKind.Submenu,
            _ => SlotKind.Normal,
        };
    }

    /// <summary>True when the slot is greyed only because no material is available (the tooltip says so).</summary>
    public static bool IsNoMaterials(StuffRingOutcome outcome, bool usable) => usable && outcome == StuffRingOutcome.None;
}

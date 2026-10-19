using QuickMenu.Core.Pins;

namespace QuickMenu.Skeleton;

/// <summary>The two hard-coded pins of the walking skeleton (spike S-6). Replaced by the pin board in T-28.</summary>
internal static class SkeletonPins
{
    internal static PinKey[] Create() => new[]
    {
        PinKey.Order("RimWorld.Designator_Mine"),
        PinKey.Build(PinKeyKind.BuildThing, "Wall"),
    };
}

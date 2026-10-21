namespace QuickMenu.Core.Pins;

/// <summary>Tells the board whether a pin key currently matches an installed designator (FR-14).</summary>
public interface IPinResolver
{
    bool IsResolved(PinKey key);
}

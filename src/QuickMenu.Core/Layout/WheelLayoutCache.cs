using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>
/// What a cached wheel layout depends on: the settings revision, the UI screen size and scale, and the wheel size.
/// Any change rebuilds the cache; nothing else does.
/// </summary>
public readonly record struct WheelLayoutKey(int Revision, int ScreenWidth, int ScreenHeight, float UiScale, float ButtonSize, int Rings);

/// <summary>A wheel placed on screen: the edge-dodged hub centre and slots centred on it.</summary>
public readonly record struct PlacedWheel(Vec2 Center, SlotPlacement[] Slots);

/// <summary>
/// Caches the slot geometry of a Fixed wheel around the origin and places it at the mouse (FR-3): the geometry is
/// computed only when the <see cref="WheelLayoutKey"/> changes; each open translates it and edge-dodges the bounds.
/// Pure, no game types.
/// </summary>
public sealed class WheelLayoutCache
{
    private WheelLayoutKey? _key;
    private SlotPlacement[] _origin = System.Array.Empty<SlotPlacement>();

    /// <summary>How many times the geometry was computed (for tests and diagnostics).</summary>
    public int Rebuilds { get; private set; }

    /// <summary>Places the wheel with its hub on <paramref name="mouse"/>, shifted so the disc and plate fit the screen.</summary>
    public PlacedWheel Place(in WheelLayoutKey key, Vec2 mouse, float plateHeight)
    {
        if (_key != key)
        {
            _origin = WheelLayout.Compute(new WheelParams(key.ButtonSize, key.Rings, default));
            _key = key;
            Rebuilds++;
        }

        Vec2 fit = EdgeDodger.Fit(
            MenuBounds.ForWheel(mouse, key.ButtonSize, key.Rings, plateHeight),
            new Vec2(key.ScreenWidth, key.ScreenHeight));
        Vec2 center = mouse + fit;
        var slots = new SlotPlacement[_origin.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = _origin[i] with { Center = _origin[i].Center + center };
        }

        return new PlacedWheel(center, slots);
    }
}

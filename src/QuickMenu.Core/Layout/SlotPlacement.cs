using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>
/// A wheel slot. <see cref="Ring"/> is 1-based. <see cref="Radius"/> is the ring radius (k*P) from the hub.
/// <see cref="AngleDegrees"/> is measured clockwise from 12 o'clock in screen space (y down) and is offset by -90,
/// so the first slot of a ring is at -90. <see cref="HitRadius"/> is P/2 (design.md 1.1): a slot is hit when the
/// distance from <see cref="Center"/> is at most this.
/// </summary>
public readonly record struct SlotPlacement(int Index, int Ring, Vec2 Center, float Radius, float AngleDegrees, float HitRadius);

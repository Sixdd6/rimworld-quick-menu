using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>A slot centre. <see cref="Ring"/> is 1-based; <see cref="AngleDegrees"/> is clockwise from 12 o'clock offset of -90 (screen space, y down).</summary>
public readonly record struct SlotPlacement(int Index, int Ring, Vec2 Center, float Radius, float AngleDegrees);

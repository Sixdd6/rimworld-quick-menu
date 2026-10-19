using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>Inputs for a Fixed wheel: button diameter B, ring count and the hub centre.</summary>
public readonly record struct WheelParams(float ButtonSize, int Rings, Vec2 Center);

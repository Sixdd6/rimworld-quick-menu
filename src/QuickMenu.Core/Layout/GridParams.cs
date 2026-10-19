using QuickMenu.Core.Geometry;

namespace QuickMenu.Core.Layout;

/// <summary>Inputs for a main grid: button diameter B, odd column and row counts and the hub (centre cell) centre.</summary>
public readonly record struct GridParams(float ButtonSize, int Columns, int Rows, Vec2 Center);

/// <summary>A grid placed on screen: the edge-dodged hub centre, panel, slots (reading order, hub skipped) and the box used for dodging.</summary>
public readonly record struct PlacedGrid(Vec2 Center, Rect2 Panel, SlotPlacement[] Slots, Rect2 Bounds);

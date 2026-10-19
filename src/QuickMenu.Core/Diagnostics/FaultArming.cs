using System;
using System.Threading;

namespace QuickMenu.Core.Diagnostics;

/// <summary>The guarded sites a developer can make fail once, to check the isolation of NFR-6 in game (T-72).</summary>
public enum FaultPoint
{
    TriggerPoller = 0,
    PinPrefix = 1,
    DropdownPostfix = 2,
    MenuOpen = 3,
    WindowDraw = 4,
    SettingsPage = 5,
    SettingsPreview = 6,
    CatalogIdeology = 7,
    TexturesSized = 8,
    Notice = 9,
    FirstRunWindow = 10,
    BindingCaptureWindow = 11,
}

/// <summary>
/// Which <see cref="FaultPoint"/>s are armed. A hot path asks <see cref="Any"/> (one volatile int read, no allocation) and only
/// then <see cref="TryConsume"/>, so an unarmed build costs one compare per guarded call. Thread-safe.
/// </summary>
public sealed class FaultArming
{
    /// <summary>The number of points; the mask is an <see cref="int"/>, so there can be at most 31.</summary>
    public static readonly int PointCount = Enum.GetValues<FaultPoint>().Length;

    private int _mask;

    /// <summary>True when at least one point is armed.</summary>
    public bool Any => Volatile.Read(ref _mask) != 0;

    /// <summary>Arms <paramref name="point"/> so the next <see cref="TryConsume"/> for it answers true.</summary>
    public void Arm(FaultPoint point) => Interlocked.Or(ref _mask, Bit(point));

    /// <summary>True when <paramref name="point"/> is armed.</summary>
    public bool IsArmed(FaultPoint point) => (Volatile.Read(ref _mask) & Bit(point)) != 0;

    /// <summary>Answers true exactly once per <see cref="Arm"/>, and disarms the point.</summary>
    public bool TryConsume(FaultPoint point)
    {
        int bit = Bit(point);
        return (Interlocked.And(ref _mask, ~bit) & bit) != 0;
    }

    /// <summary>Disarms every point.</summary>
    public void Clear() => Volatile.Write(ref _mask, 0);

    private static int Bit(FaultPoint point)
    {
        int index = (int)point;
        if (index < 0 || index >= 31)
        {
            throw new ArgumentOutOfRangeException(nameof(point));
        }

        return 1 << index;
    }
}

using System;
using System.Diagnostics;
using QuickMenu.Core.Diagnostics;
using UnityEngine;

namespace QuickMenu.Diagnostics;

/// <summary>
/// Dev-mode timing and allocation probe: <c>using var _ = new MeasureScope(PerfCounters.Poller);</c>. While
/// <see cref="PerfCounters.Enabled"/> is false the constructor and <see cref="Dispose"/> cost one static bool read each.
/// Never allocates: it is a struct, and results go into preallocated <see cref="MeasureStats"/>.
/// </summary>
internal readonly struct MeasureScope : IDisposable
{
    private readonly MeasureStats? _stats;
    private readonly long _startTicks;
    private readonly long _startBytes;

    internal MeasureScope(MeasureStats stats)
    {
        if (!PerfCounters.Enabled)
        {
            _stats = null;
            _startTicks = 0;
            _startBytes = 0;
            return;
        }

        _stats = stats;
        _startBytes = GC.GetAllocatedBytesForCurrentThread();
        _startTicks = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        MeasureStats? stats = _stats;
        if (stats == null)
        {
            return;
        }

        long ticks = Stopwatch.GetTimestamp() - _startTicks;
        long bytes = GC.GetAllocatedBytesForCurrentThread() - _startBytes;
        stats.Record(ticks, bytes, Time.frameCount);
        PerfCounters.AfterRecord(stats);
    }
}

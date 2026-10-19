using System;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace QuickMenu.Diagnostics;

/// <summary>
/// Dev-mode timing and allocation probe: <c>using var scope = new MeasureScope(StatId.Poller);</c>.
/// While <see cref="PerfCounters.Enabled"/> is false, the constructor reads that one static bool and
/// <see cref="Dispose"/> tests one bool field; the counters are not even created. When enabled it records
/// <c>Stopwatch</c> ticks and <c>GC.GetAllocatedBytesForCurrentThread</c> (main thread) into preallocated
/// <see cref="Core.Diagnostics.MeasureStats"/>, per frame. The sample covers only the scoped body: for the poller that
/// excludes the Harmony postfix wrapper, so P-1 still needs the Waylith Frame Profiler cross-check. A measurement
/// fault turns measuring off and is logged once; it never reaches the caller.
/// </summary>
internal readonly struct MeasureScope : IDisposable
{
    private readonly bool _active;
    private readonly StatId _id;
    private readonly long _startTicks;
    private readonly long _startBytes;

    internal MeasureScope(StatId id)
    {
        _id = id;
        _startTicks = 0;
        _startBytes = 0;
        _active = false;
        if (!PerfCounters.Enabled)
        {
            return;
        }

        if (!Prefs.DevMode)
        {
            PerfCounters.Enabled = false;
            return;
        }

        _active = true;
        _startBytes = GC.GetAllocatedBytesForCurrentThread();
        _startTicks = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        if (!_active)
        {
            return;
        }

        try
        {
            long ticks = Stopwatch.GetTimestamp() - _startTicks;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - _startBytes;
            PerfCounters.Record(_id, ticks, bytes, Time.frameCount);
        }
        catch (Exception ex)
        {
            PerfCounters.Fault(ex);
        }
    }
}

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using LudeonTK;
using QuickMenu.Core.Diagnostics;
using UnityEngine;
using Verse;

namespace QuickMenu.Diagnostics;

/// <summary>
/// Preallocated dev-mode counters (performance.md T-6, P-1/P-2/P-6/P-7a/P-8/P-9/P-18). <see cref="Enabled"/> is the one
/// cached bool that hot paths check; it is false until a Quick Menu debug action (which only exists in dev mode) turns it on.
/// </summary>
internal static class PerfCounters
{
    internal const int ClosedFrameTarget = 10_000;

    /// <summary>Cached switch read by <see cref="MeasureScope"/>. Plain field: main thread only.</summary>
    internal static bool Enabled;

    internal static readonly MeasureStats Poller = new("poller body (closed + open frames)", 16384);
    internal static readonly MeasureStats OpenPath = new("open path (WindowStack.Add of the menu)", 1024);
    internal static readonly MeasureStats Draw = new("QuickMenuWindow.DoWindowContents (per call)", 16384);
    internal static readonly MeasureStats WindowUpdate = new("QuickMenuWindow.WindowUpdate", 16384);
    internal static readonly MeasureStats ControlDraw = new("control window DoWindowContents (per call)", 16384);

    private static readonly MeasureStats[] _all = { Poller, OpenPath, Draw, WindowUpdate, ControlDraw };

    private static long _ctorTicks;
    private static long _patchTicks;
    private static MeasureStats? _limited;

    internal static void NoteStartup(long ctorTicks, long patchTicks)
    {
        _ctorTicks = ctorTicks;
        _patchTicks = patchTicks;
    }

    internal static void ResetAll()
    {
        foreach (MeasureStats s in _all)
        {
            s.Reset();
        }
    }

    /// <summary>Called by <see cref="MeasureScope"/> after recording (enabled path only): completes a limited run.</summary>
    internal static void AfterRecord(MeasureStats stats)
    {
        if (ReferenceEquals(stats, _limited) && stats.Calls >= ClosedFrameTarget)
        {
            _limited = null;
            ClosedMeasureDone();
        }
    }

    private static double Us(long ticks) => ticks * 1_000_000.0 / Stopwatch.Frequency;

    private static string F(double v, string format = "F2") => v.ToString(format, CultureInfo.InvariantCulture);

    internal static void AppendStartup(StringBuilder sb)
    {
        double ctorMs = _ctorTicks * 1000.0 / Stopwatch.Frequency;
        double patchMs = _patchTicks * 1000.0 / Stopwatch.Frequency;
        sb.AppendLine("modCtorMs=" + F(ctorMs) + " (P-18 <= 150 vanilla, <= 300 large list); of which patching (postfix + pin overrides)=" + F(patchMs)
            + "ms; settings load=n/a and Mint/BAM/MP detection=n/a (not implemented yet, T-21..T-45 will add scopes)");
        sb.AppendLine("pin override scan/patch ms (reflection, inside patching)=" + F(Pinning.PinClickPatch.PatchMilliseconds) + " patched=" + Pinning.PinClickPatch.PatchedCount);
    }

    internal static void AppendCounters(StringBuilder sb)
    {
        sb.AppendLine("measuring=" + Enabled + " stopwatchHz=" + Stopwatch.Frequency + " (counters only record while measuring=true; see 'Quick Menu: perf measuring (toggle)')");
        foreach (MeasureStats s in _all)
        {
            SampleSummary sum = s.Ticks.Summarize();
            long frames = Math.Max(1, s.Frames);
            sb.AppendLine(s.Name + ": calls=" + s.Calls + " frames=" + s.Frames
                + " medianUs=" + F(Us(sum.Median)) + " p99Us=" + F(Us(sum.P99)) + " maxUs=" + F(Us(sum.Max))
                + " (ring " + sum.Count + "/" + s.Ticks.Capacity + ")"
                + " msPerFrame=" + F(Us(s.TotalTicks) / 1000.0 / frames, "F4")
                + " allocBytesTotal=" + s.TotalBytes + " allocBytesPerFrame=" + F((double)s.TotalBytes / frames, "F1")
                + " firstCallBytes=" + s.FirstCallBytes + " maxCallBytes=" + s.MaxCallBytes);
        }
    }

    private static void ClosedMeasureDone()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Log.Prefix + "BEGIN closed-frame measurement");
        MeasureStats s = Poller;
        SampleSummary sum = s.Ticks.Summarize();
        sb.AppendLine("frames=" + s.Calls + " dist=" + s.Frames + " medianUs=" + F(Us(sum.Median)) + " p99Us=" + F(Us(sum.P99)) + " maxUs=" + F(Us(sum.Max))
            + " (P-1 <= 10us median, goal <= 2us)");
        sb.AppendLine("allocatedBytesTotal=" + s.TotalBytes + " over " + s.Calls + " frames (P-2 expects 0) maxCallBytes=" + s.MaxCallBytes);
        sb.AppendLine("note: the scope's own Stopwatch.GetTimestamp x2 is included in each sample (about 40-60 ns); measuring stays ON until toggled off");
        sb.AppendLine(Log.Prefix + "END closed-frame measurement");
        Verse.Log.Message(sb.ToString());
    }

    internal static void BeginClosedMeasurement()
    {
        Poller.Reset();
        _limited = Poller;
        Enabled = true;
        Log.Info("Quick Menu: measuring " + ClosedFrameTarget + " frames; leave the map idle and do not open the menu.");
    }

    [DebugAction("Quick Menu", "measure closed frames", allowedGameStates = AllowedGameStates.Playing)]
    private static void MeasureClosedFrames() => BeginClosedMeasurement();

    [DebugAction("Quick Menu", "perf measuring (toggle)", allowedGameStates = AllowedGameStates.Playing)]
    private static void ToggleMeasuring()
    {
        Enabled = !Enabled;
        if (Enabled)
        {
            ResetAll();
        }

        Log.Info("Quick Menu: perf measuring " + (Enabled ? "ON (counters reset)" : "OFF"));
    }

    [DebugAction("Quick Menu", "perf report", allowedGameStates = AllowedGameStates.Playing)]
    private static void Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Log.Prefix + "BEGIN perf report");
        AppendStartup(sb);
        AppendCounters(sb);
        sb.AppendLine("control-run: compare 'QuickMenuWindow.DoWindowContents' with 'control window' (P-9); target <= 512 B/frame over the control");
        sb.AppendLine(Log.Prefix + "END perf report");
        Verse.Log.Message(sb.ToString());
    }
}

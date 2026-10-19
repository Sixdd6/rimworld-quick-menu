using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using LudeonTK;
using QuickMenu.Core.Diagnostics;
using QuickMenu.Core.Settings;
using QuickMenu.Menu;
using Verse;

namespace QuickMenu.Diagnostics;

/// <summary>Which counter a <see cref="MeasureScope"/> feeds.</summary>
internal enum StatId
{
    Poller,
    OpenPath,
    /// <summary>DoWindowContents and WindowUpdate of the menu window share one per-frame bucket (P-8).</summary>
    Window,
    Control,

    /// <summary>The pin click prefix (P-3).</summary>
    PinPrefix,

    /// <summary>Sub-bucket of <see cref="Window"/>: the hover sound call (game code). One sample per frame it ran.</summary>
    WindowSound,

    /// <summary>Sub-bucket of <see cref="Window"/>: <c>TooltipHandler.TipRegion</c> (game code). One sample per hovered frame.</summary>
    WindowTip,

    /// <summary>Sub-bucket of <see cref="Window"/>: the label plate (<c>Widgets.DrawAtlas</c> + <c>Widgets.Label</c>, game code).</summary>
    WindowPlate,

    /// <summary>Rebuilding the wheel after an unpin; kept out of <see cref="Window"/> so P-9 allocations are not polluted.</summary>
    Rebuild,

    /// <summary>Sub-bucket of <see cref="Window"/> (T-95): frames drawn with the main wheel on screen.</summary>
    WindowWheel,

    /// <summary>Sub-bucket of <see cref="Window"/>: frames drawn with the main grid on screen.</summary>
    WindowGrid,

    /// <summary>Sub-bucket of <see cref="Window"/>: frames drawn with a wheel satellite (sub-ring or material ring) open.</summary>
    WindowSatellite,

    /// <summary>Sub-bucket of <see cref="Window"/>: frames drawn with a sub-grid or material grid open.</summary>
    WindowSubGrid,
}

/// <summary>
/// Dev-mode counters (performance.md T-6; P-1, P-2, P-6, P-7a, P-8, P-9, P-18). Nothing is allocated until a debug
/// action enables measuring, so players never pay for the ring buffers. <see cref="Enabled"/> is the one cached bool
/// hot paths check; it is false until a Quick Menu debug action (dev mode only) turns it on.
/// </summary>
internal static class PerfCounters
{
    internal const int ClosedFrameTarget = 10_000;

    // 10,240 longs per array stays under the 85,000-byte large-object-heap threshold and covers a 10,000-frame run.
    private const int FrameRing = 10_240;

    /// <summary>Cached switch read by <see cref="MeasureScope"/>. Plain field: main thread only.</summary>
    internal static bool Enabled;

    /// <summary>Slot count the control window drew, for the report.</summary>
    internal static int ControlSlots;

    private static MeasureStats[]? _stats;
    private static MeasureStats? _limited;
    private static bool _reportPending;

    internal static bool ReportPending => _reportPending;

    private static MeasureStats[] EnsureStats() => _stats ??= new MeasureStats[]
    {
        new("poller body (closed + open frames)", FrameRing),
        new("open path (WindowStack.Add of the menu; one sample per open)", 256),
        new("menu window per frame (DoWindowContents + WindowUpdate)", FrameRing),
        new("control window per frame (DoWindowContents)", FrameRing),
        new("pin click prefix (P-3; one sample per designator ProcessInput call)", 256),
        new("  of window: hover sound call (PlayOneShotOnCamera; frames where hover changed)", 256),
        new("  of window: TooltipHandler.TipRegion (hovered frames)", FrameRing),
        new("  of window: label plate (DrawAtlas + Widgets.Label; frames with a plate)", FrameRing),
        new("rebuilding the wheel after an unpin (one sample per rebuild)", 256),
        new("  P-8 by layout: wheel (main wheel on screen)", FrameRing),
        new("  P-8 by layout: grid (main grid on screen)", FrameRing),
        new("  P-8 by layout: wheel satellite (sub-ring or material ring open)", FrameRing),
        new("  P-8 by layout: sub-grid (sub-grid or material grid open)", FrameRing),
    };

    internal static MeasureStats Get(StatId id) => EnsureStats()[(int)id];

    /// <summary>Allocates the counters on first use and turns measuring on.</summary>
    internal static void Enable()
    {
        EnsureStats();
        Enabled = true;
    }

    /// <summary>Called by <see cref="MeasureScope"/> (enabled path only).</summary>
    internal static void Record(StatId id, long ticks, long bytes, int frame)
    {
        MeasureStats stats = Get(id);
        stats.Record(ticks, bytes, frame);
        if (id == StatId.Window)
        {
            Get(WindowLayoutStat()).Record(ticks, bytes, frame); // the same sample, split by what is on screen
        }

        if (ReferenceEquals(stats, _limited) && stats.Calls >= ClosedFrameTarget)
        {
            _limited = null;
            _reportPending = true; // printed next frame by the poller, outside any measured scope
        }
    }

    /// <summary>Which layout bucket a window sample belongs to; read only while measuring, allocates nothing.</summary>
    private static StatId WindowLayoutStat()
    {
        RingContent? ring = MenuController.ActiveRing;
        if (ring != null)
        {
            return ring.Model.Panel != null ? StatId.WindowSubGrid : StatId.WindowSatellite;
        }

        return QuickMenuMod.Settings.Layout == MenuLayout.Grid ? StatId.WindowGrid : StatId.WindowWheel;
    }

    /// <summary>Called when the menu opens; the next window frame is a first-after-open frame (P-9).</summary>
    internal static void MarkOpen()
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            Get(StatId.Window).MarkOpen();
            Get(StatId.WindowWheel).MarkOpen();
            Get(StatId.WindowGrid).MarkOpen();
            Get(StatId.WindowSatellite).MarkOpen();
            Get(StatId.WindowSubGrid).MarkOpen();
        }
        catch (Exception ex)
        {
            Fault(ex);
        }
    }

    /// <summary>A measurement fault must never disable the trigger or menu: stop measuring and log once.</summary>
    internal static void Fault(Exception ex)
    {
        Enabled = false;
        _limited = null;
        _reportPending = false;
        Log.LogOnce("Perf.Measure", $"Perf measurement failed and was turned off: {ex}");
    }

    internal static void ResetAll()
    {
        AbortClosedRun();
        _reportPending = false;
        if (_stats == null)
        {
            return;
        }

        foreach (MeasureStats s in _stats)
        {
            s.Reset();
        }
    }

    private static void AbortClosedRun()
    {
        if (_limited != null)
        {
            _limited = null;
            Log.Info("closed measurement aborted");
        }
    }

    /// <summary>Poller hook (only when <see cref="Enabled"/>): prints a finished 10,000-frame report.</summary>
    internal static void PrintPendingReport()
    {
        _reportPending = false;
        try
        {
            ClosedMeasureDone();
        }
        catch (Exception ex)
        {
            Fault(ex);
        }
    }

    private static double Us(long ticks) => ticks * 1_000_000.0 / Stopwatch.Frequency;

    private static string F(double v, string format = "F2") => v.ToString(format, CultureInfo.InvariantCulture);

    internal static void AppendCounters(StringBuilder sb)
    {
        sb.AppendLine("measuring=" + Enabled + " stopwatchHz=" + Stopwatch.Frequency
            + " (counters record only while measuring=true; see 'Quick Menu: perf measuring (toggle)'). All timings are per frame (calls in one frame summed); medians, not means.");
        if (_stats == null)
        {
            sb.AppendLine("no counters yet: measuring was never enabled this session");
            return;
        }

        foreach (MeasureStats s in _stats)
        {
            s.Flush();
            SampleSummary t = s.FrameTicks.Summarize();
            SampleSummary b = s.FrameBytes.Summarize();
            sb.AppendLine(s.Name + ": calls=" + s.Calls + " steadyFrames=" + s.Frames + " opens=" + s.Opens
                + " | us/frame median=" + F(Us(t.Median)) + " p99=" + F(Us(t.P99)) + " max=" + F(Us(t.Max))
                + " | bytes/frame median=" + b.Median + " p99=" + b.P99 + " max=" + b.Max
                + " | firstCallUs=" + F(Us(s.FirstCallTicks)) + " firstCallBytes=" + s.FirstCallBytes
                + " | firstFrameAfterOpen maxBytes=" + s.FirstFrameBytesMax + " maxUs=" + F(Us(s.FirstFrameTicksMax))
                + " | totalBytes=" + s.TotalBytes + " (ring " + t.Count + "/" + s.FrameTicks.Capacity + ")");
        }

        sb.AppendLine("sized texture rebuilds this session=" + Textures.SizedRebuilds + " (T-95: at most one per slider drag), last took "
            + F(Textures.LastSizedRebuildMs) + " ms with upload");
        sb.AppendLine("control window slots drawn N=" + ControlSlots + " (live wheel N is in the menu window line: same count and icon path)");
    }

    private static void ClosedMeasureDone()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Log.Prefix + "BEGIN closed-frame measurement");
        MeasureStats s = Get(StatId.Poller);
        s.Flush();
        SampleSummary t = s.FrameTicks.Summarize();
        sb.AppendLine("frames=" + s.Frames + " calls=" + s.Calls + " medianUs=" + F(Us(t.Median)) + " p99Us=" + F(Us(t.P99)) + " maxUs=" + F(Us(t.Max))
            + " (P-1 <= 10us median, goal <= 2us)");
        sb.AppendLine("allocatedBytesTotal=" + s.TotalBytes + " over " + s.Calls + " frames (P-2 expects 0)");
        sb.AppendLine("note: the sample covers only the postfix body between two Stopwatch reads (about 40-60 ns of scope overhead included); the Harmony wrapper is not in it, so cross-check P-1 with the Frame Profiler. Measuring stays ON until toggled off.");
        sb.AppendLine(Log.Prefix + "END closed-frame measurement");
        Verse.Log.Message(sb.ToString());
    }

    [DebugAction("Quick Menu", "measure closed frames", allowedGameStates = AllowedGameStates.Playing)]
    private static void MeasureClosedFrames()
    {
        Enable();
        ResetAll();
        _limited = Get(StatId.Poller);
        Log.Info("measuring " + ClosedFrameTarget + " frames; leave the map idle and do not open the menu");
    }

    [DebugAction("Quick Menu", "perf measuring (toggle)", allowedGameStates = AllowedGameStates.Playing)]
    private static void ToggleMeasuring()
    {
        if (Enabled)
        {
            AbortClosedRun();
            Enabled = false;
        }
        else
        {
            Enable();
            ResetAll();
        }

        Log.Info("perf measuring " + (Enabled ? "ON (counters reset)" : "OFF"));
    }

    [DebugAction("Quick Menu", "perf report", allowedGameStates = AllowedGameStates.Playing)]
    private static void Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Log.Prefix + "BEGIN perf report");
        PerfStartup.Append(sb);
        AppendCounters(sb);
        sb.AppendLine("control-run: compare the menu window line with the control window line (P-9); target <= 512 B/frame over the control");
        sb.AppendLine(Log.Prefix + "END perf report");
        Verse.Log.Message(sb.ToString());
    }
}

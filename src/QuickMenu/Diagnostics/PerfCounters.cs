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

    /// <summary>The pin-marker hook on <c>Command.GizmoOnGUIInt</c> (P-19): one sample per frame, covering every call of that frame.</summary>
    PinMarker,

    /// <summary>The dropdown-entry marker postfixes (P-19, hook site 5): one sample per frame, covering every option draw of that frame.</summary>
    PinMarkerEntry,

    /// <summary>Drawing a pin marker (two <c>GUI.DrawTexture</c> calls in the game's IMGUI backend): only pinned, visible designators. Kept out of <see cref="PinMarker"/> (P-19 budgets the decision).</summary>
    PinMarkerDraw,

    /// <summary>The pin-click action proper (P-5): resolve, toggle on the board, resolved-cache update and marker-set rebuild. Excludes the message and the settings write.</summary>
    PinToggle,

    /// <summary>The pin-click message and tick sound (<c>Translate</c>, <c>Messages.Message</c>, <c>PlayOneShotOnCamera</c>: game code). Not budgeted.</summary>
    PinMessage,

    /// <summary>The settings file write after a pin click (<c>WriteSettings</c>: game Scribe/XML code). Excluded from P-5; P-17 covers its frequency.</summary>
    PinWrite,
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
        new("pin click prefix (P-3 classification only; one sample per designator ProcessInput call, pin actions excluded)", 256),
        new("  of window: hover sound call (PlayOneShotOnCamera; frames where hover changed)", 256),
        new("  of window: TooltipHandler.TipRegion (hovered frames)", FrameRing), // strings-ok: dev-mode perf label
        new("  of window: label plate (DrawAtlas + Widgets.Label; frames with a plate)", FrameRing),
        new("rebuilding the wheel after an unpin (one sample per rebuild)", 256),
        new("  P-8 by layout: wheel (main wheel on screen)", FrameRing),
        new("  P-8 by layout: grid (main grid on screen)", FrameRing),
        new("  P-8 by layout: wheel satellite (sub-ring or material ring open)", FrameRing),
        new("  P-8 by layout: sub-grid (sub-grid or material grid open)", FrameRing),
        new("pin marker hook per frame (P-19; calls = hook calls, calls/steadyFrames = calls per frame; every command gizmo, drawn or not)", FrameRing),
        new("pin marker dropdown-entry hooks per frame (P-19; calls = float-menu option draws while a mapped list exists)", FrameRing),
        new("pin marker draw per frame (P-19, not budgeted: IMGUI texture drawing of the pinned buttons; calls = markers drawn)", FrameRing),
        new("pin click action (P-5: toggle + cache + marker-set rebuild; excludes message and write)", 256),
        new("  of pin click: message + tick sound (Translate, Messages.Message; game code, not budgeted)", 256),
        new("  of pin click: settings write (WriteSettings; game code, excluded from P-5)", 256),
    };

    internal static MeasureStats Get(StatId id) => EnsureStats()[(int)id];

    /// <summary>Allocates the counters on first use and turns measuring on.</summary>
    internal static void Enable()
    {
        EnsureStats();
        Calibrate();
        Enabled = true;
    }

    /// <summary>Median cost in ticks of the timed interval of an empty <see cref="MeasureScope"/> (calibrated by <see cref="Calibrate"/>).</summary>
    internal static long ScopeOverheadTicks { get; private set; }

    /// <summary>
    /// Times 1,000 empty scopes (same call sequence as <see cref="MeasureScope"/>, nothing recorded) and keeps the median of the
    /// timed interval. Per-call sites (P-19) subtract it per call in the report, so the raw numbers stay raw and the corrected ones are shown beside them.
    /// </summary>
    private static void Calibrate()
    {
        const int n = 1000;
        long[] samples = new long[n];
        for (int i = 0; i < n; i++)
        {
            _ = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            long end = Stopwatch.GetTimestamp();
            samples[i] = end - start;
        }

        Array.Sort(samples);
        ScopeOverheadTicks = samples[n / 2];
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
            if (ReferenceEquals(s, Get(StatId.PinMarker)) || ReferenceEquals(s, Get(StatId.PinMarkerEntry)))
            {
                AppendCorrected(sb, s, t);
            }
        }

        sb.AppendLine("sized texture rebuilds this session=" + Textures.SizedRebuilds + " (T-95: at most one per slider drag), last took "
            + F(Textures.LastSizedRebuildMs) + " ms with upload");
        sb.AppendLine("control window slots drawn N=" + ControlSlots + " (live wheel N is in the menu window line: same count and icon path)");
    }

    /// <summary>
    /// Per-call hooks (P-19) wrap every call in a scope, so each call includes the timed part of the scope itself. This prints the
    /// calibrated overhead and the estimate with it removed: per call = frame median / calls per frame - overhead.
    /// </summary>
    private static void AppendCorrected(StringBuilder sb, MeasureStats s, SampleSummary t)
    {
        if (s.Frames == 0)
        {
            return;
        }

        double callsPerFrame = (double)s.Calls / s.Frames;
        double overheadNs = ScopeOverheadTicks * 1e9 / Stopwatch.Frequency;
        double rawPerCallNs = Us(t.Median) * 1000.0 / Math.Max(callsPerFrame, 1.0);
        double correctedPerCallNs = Math.Max(rawPerCallNs - overheadNs, 0.0);
        sb.AppendLine("    P-19 corrected: calls/frame=" + F(callsPerFrame, "F1") + " scopeOverheadNs=" + F(overheadNs, "F1") + " (calibrated, 1000 empty scopes)"
            + " | rawNsPerCall=" + F(rawPerCallNs, "F1") + " correctedNsPerCall=" + F(correctedPerCallNs, "F1")
            + " | correctedUsPerFrame=" + F(Math.Max(Us(t.Median) - (callsPerFrame * overheadNs / 1000.0), 0.0))
            + " (budget <= 100 ns per call and <= 10 us per frame; allocation outside the scope, i.e. the marker draw, is under 'pin marker draw')");
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

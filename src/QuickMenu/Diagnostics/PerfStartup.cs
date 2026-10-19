using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace QuickMenu.Diagnostics;

/// <summary>Startup timings (P-18). Two longs: touching this class allocates nothing, unlike <see cref="PerfCounters"/>.</summary>
internal static class PerfStartup
{
    private static long _ctorTicks;
    private static long _patchTicks;
    private static long _settingsTicks;

    private static long _getSettingsTicks;
    private static long _exposeTicks;
    private static long _postLoadTicks;
    private static long _ownTicks;
    private static System.TimeSpan _jitDuringGet;
    private static long _jitMethodsDuringGet;

    /// <summary>
    /// Breakdown of the settings load. <paramref name="getTicks"/> is the whole <c>GetSettings</c> call (the game's file read
    /// and Scribe, our <c>ExposeData</c> inside it); <paramref name="exposeTicks"/> is the part inside our <c>ExposeData</c>;
    /// <paramref name="postLoadTicks"/> is purely ours; <paramref name="ownTicks"/> is our code after the call (capacity, Mint detection).
    /// The JIT figures cover the <c>GetSettings</c> call on this thread.
    /// </summary>
    internal static void NoteSettings(long getTicks, long exposeTicks, long postLoadTicks, long ownTicks, System.TimeSpan jit, long jitMethods)
    {
        _getSettingsTicks = getTicks;
        _exposeTicks = exposeTicks;
        _postLoadTicks = postLoadTicks;
        _ownTicks = ownTicks;
        _jitDuringGet = jit;
        _jitMethodsDuringGet = jitMethods;
    }

    internal static void Note(long ctorTicks, long patchTicks, long settingsTicks)
    {
        _settingsTicks = settingsTicks;
        _ctorTicks = ctorTicks;
        _patchTicks = patchTicks;
    }

    internal static void Append(StringBuilder sb)
    {
        double ctorMs = _ctorTicks * 1000.0 / Stopwatch.Frequency;
        double patchMs = _patchTicks * 1000.0 / Stopwatch.Frequency;
        sb.AppendLine("modCtorMs=" + ctorMs.ToString("F2", CultureInfo.InvariantCulture) + " (P-18 <= 150 vanilla, <= 300 large list); of which patching (postfix + pin overrides)="
            + patchMs.ToString("F2", CultureInfo.InvariantCulture) + "ms; settings load incl. the game's own file read and Scribe (see breakdown)="
            + (_settingsTicks * 1000.0 / Stopwatch.Frequency).ToString("F2", CultureInfo.InvariantCulture) + "ms; Mint/BAM/MP detection=n/a (not implemented yet, later tasks add scopes)");
        sb.AppendLine("settings breakdown (P-16, ours = postLoad + after): GetSettings total=" + Ms(_getSettingsTicks)
            + "ms; inside it the settings scribe pass=" + Ms(_exposeTicks) + "ms (our parsing + the game's Scribe calls) of which our PostLoad=" + Ms(_postLoadTicks)
            + "ms; our code after GetSettings (capacity, Mint detect)=" + Ms(_ownTicks)
            + "ms; JIT during GetSettings=" + _jitDuringGet.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture) + "ms in " + _jitMethodsDuringGet.ToString(CultureInfo.InvariantCulture) + " methods");
        sb.AppendLine("pin override scan/patch ms (reflection, inside patching)="
            + Pinning.PinClickPatch.PatchMilliseconds.ToString("F2", CultureInfo.InvariantCulture) + " patched=" + Pinning.PinClickPatch.PatchedCount);
    }

    private static string Ms(long ticks) => (ticks * 1000.0 / Stopwatch.Frequency).ToString("F2", CultureInfo.InvariantCulture);
}

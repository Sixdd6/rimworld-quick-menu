using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace QuickMenu.Diagnostics;

/// <summary>Startup timings (P-18). Two longs: touching this class allocates nothing, unlike <see cref="PerfCounters"/>.</summary>
internal static class PerfStartup
{
    private static long _ctorTicks;
    private static long _patchTicks;

    internal static void Note(long ctorTicks, long patchTicks)
    {
        _ctorTicks = ctorTicks;
        _patchTicks = patchTicks;
    }

    internal static void Append(StringBuilder sb)
    {
        double ctorMs = _ctorTicks * 1000.0 / Stopwatch.Frequency;
        double patchMs = _patchTicks * 1000.0 / Stopwatch.Frequency;
        sb.AppendLine("modCtorMs=" + ctorMs.ToString("F2", CultureInfo.InvariantCulture) + " (P-18 <= 150 vanilla, <= 300 large list); of which patching (postfix + pin overrides)="
            + patchMs.ToString("F2", CultureInfo.InvariantCulture) + "ms; settings load=n/a and Mint/BAM/MP detection=n/a (not implemented yet, later tasks add scopes)");
        sb.AppendLine("pin override scan/patch ms (reflection, inside patching)="
            + Pinning.PinClickPatch.PatchMilliseconds.ToString("F2", CultureInfo.InvariantCulture) + " patched=" + Pinning.PinClickPatch.PatchedCount);
    }
}

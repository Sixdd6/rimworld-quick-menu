using System;
using System.Collections.Generic;

namespace QuickMenu.Diagnostics;

/// <summary>
/// Fault isolation for patch bodies and OnGUI entry points (NFR-6). A throwing site is logged once, then
/// disabled for the session so it cannot break the game or spam the log.
/// </summary>
internal static class PatchGuard
{
    private static readonly HashSet<string> _disabled = new();

    /// <summary>Runs <paramref name="body"/> unless the site is disabled. Pass a non-capturing delegate on per-frame paths.</summary>
    internal static void Run(string site, Action body)
    {
        if (_disabled.Contains(site))
        {
            return;
        }

        try
        {
            body();
        }
        catch (Exception ex)
        {
            Fail(site, ex);
        }
    }

    internal static bool IsDisabled(string site) => _disabled.Contains(site);

    /// <summary>Disables the site for the session and logs the exception once.</summary>
    internal static void Fail(string site, Exception ex)
    {
        _disabled.Add(site);
        Log.LogOnce(site, $"{site} failed and was disabled for this session: {ex}");
    }
}

using System.Collections.Generic;

namespace QuickMenu.Diagnostics;

/// <summary>Logging helpers. Errors are logged once per key so a broken frame loop cannot flood the log (NFR-6).</summary>
internal static class Log
{
    internal const string Prefix = "[QuickMenu] ";

    private static readonly HashSet<string> _logged = new();

    /// <summary>Logs an error the first time <paramref name="key"/> is seen; later calls are ignored.</summary>
    /// <returns>True when the message was written.</returns>
    internal static bool LogOnce(string key, string message)
    {
        if (!_logged.Add(key))
        {
            return false;
        }

        Verse.Log.Error(Prefix + message);
        return true;
    }

    /// <summary>Logs a normal informational message with the mod prefix.</summary>
    internal static void Info(string message) => Verse.Log.Message(Prefix + message);
}

using System;

namespace QuickMenu.Diagnostics;

/// <summary>
/// Fault state for one patch site or OnGUI entry point (NFR-6). Keep one static instance per site. Hot paths check
/// <see cref="Disabled"/> (a single field read) and call <see cref="Fail"/> from a catch block; no closure or
/// allocation is needed. A prefix returns <c>true</c> when its site is disabled so the original runs.
/// </summary>
internal sealed class PatchSite
{
    private volatile bool _disabled;

    internal PatchSite(string name)
    {
        Name = name;
    }

    internal string Name { get; }

    internal bool Disabled => _disabled;

    /// <summary>Disables the site for the session and logs the exception once.</summary>
    internal void Fail(Exception ex)
    {
        _disabled = true;
        Log.LogOnce(Name, $"{Name} failed and was disabled for this session: {ex}");
    }
}

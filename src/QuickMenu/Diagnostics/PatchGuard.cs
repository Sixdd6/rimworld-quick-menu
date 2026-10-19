using System;

namespace QuickMenu.Diagnostics;

/// <summary>Helpers around <see cref="PatchSite"/> for places where a delegate is acceptable (not per-frame hot paths).</summary>
internal static class PatchGuard
{
    /// <summary>Runs <paramref name="body"/> unless the site is disabled; a throw is logged once and disables the site.</summary>
    internal static void Run(PatchSite site, Action body)
    {
        if (site.Disabled)
        {
            return;
        }

        try
        {
            body();
        }
        catch (Exception ex)
        {
            site.Fail(ex);
        }
    }
}

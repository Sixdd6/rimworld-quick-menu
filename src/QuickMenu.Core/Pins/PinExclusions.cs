using System;
using System.Collections.Generic;

namespace QuickMenu.Core.Pins;

/// <summary>
/// Designator types that are never pinned (user decision 2026-10-27): architect buttons that are settings rather than tools.
/// More Planning's colour squares pick the plan colour and switch to its own Plan tool, which is the pinnable one.
/// </summary>
public static class PinExclusions
{
    private static readonly HashSet<string> _types = new(StringComparer.Ordinal)
    {
        "MorePlanning.Designators.SelectColorDesignator",
    };

    /// <summary>True when designators of this exact type (full name, case-sensitive) cannot be pinned.</summary>
    public static bool IsExcluded(string? typeFullName) => typeFullName != null && _types.Contains(typeFullName);
}

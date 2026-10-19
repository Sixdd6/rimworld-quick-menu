using System;
using System.Collections.Generic;

namespace QuickMenu.Core.Diagnostics;

/// <summary>Keeps the first N lines of a report section and counts the rest, so a log block stays bounded.</summary>
public sealed class BoundedLines
{
    private readonly List<string> _kept = new();

    public BoundedLines(int limit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);
        Limit = limit;
    }

    public int Limit { get; }

    /// <summary>All lines offered, kept or not.</summary>
    public int Total { get; private set; }

    public int Omitted => Total - _kept.Count;

    public IReadOnlyList<string> Kept => _kept;

    public void Add(string line)
    {
        Total++;
        if (_kept.Count < Limit)
        {
            _kept.Add(line);
        }
    }
}

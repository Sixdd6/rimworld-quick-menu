using System;
using System.Collections.Generic;

namespace QuickMenu.Core.Materials;

/// <summary>What a stuff build should do when activated (FR-12).</summary>
public enum StuffRingOutcome
{
    /// <summary>No material is available: show the slot greyed with "no materials".</summary>
    None,

    /// <summary>Exactly one material: skip the ring and use it.</summary>
    Skip,

    /// <summary>Two or more materials: open the material ring.</summary>
    Ring,
}

/// <summary>The material ids in their fixed order, the decision taken, and the index of the last-used one (-1 if none).</summary>
public readonly record struct StuffOrder(StuffRingOutcome Outcome, IReadOnlyList<string> Ids, int LastUsedIndex = -1);

/// <summary>
/// Decides the material ring outcome. The order is fixed: the input order (the architect's) is kept and never re-sorted to put
/// the last-used material first; the last-used one is only reported by index so the ring can highlight it (FR-12).
/// </summary>
public static class StuffOrdering
{
    public static StuffOrder Order(IReadOnlyList<string> optionIds, string? lastUsedId)
    {
        ArgumentNullException.ThrowIfNull(optionIds);
        if (optionIds.Count == 0)
        {
            return new StuffOrder(StuffRingOutcome.None, Array.Empty<string>());
        }

        int last = -1;
        if (lastUsedId != null)
        {
            for (int i = 0; i < optionIds.Count; i++)
            {
                if (string.Equals(optionIds[i], lastUsedId, StringComparison.Ordinal))
                {
                    last = i;
                    break;
                }
            }
        }

        var ids = new string[optionIds.Count];
        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = optionIds[i];
        }

        return new StuffOrder(optionIds.Count == 1 ? StuffRingOutcome.Skip : StuffRingOutcome.Ring, ids, last);
    }
}

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

/// <summary>The ordered material ids and the decision taken.</summary>
public readonly record struct StuffOrder(StuffRingOutcome Outcome, IReadOnlyList<string> Ids);

/// <summary>Orders material ids for the material ring: last used first, the rest in input order.</summary>
public static class StuffOrdering
{
    public static StuffOrder Order(IReadOnlyList<string> optionIds, string? lastUsedId)
    {
        ArgumentNullException.ThrowIfNull(optionIds);
        if (optionIds.Count == 0)
        {
            return new StuffOrder(StuffRingOutcome.None, Array.Empty<string>());
        }

        if (optionIds.Count == 1)
        {
            return new StuffOrder(StuffRingOutcome.Skip, new[] { optionIds[0] });
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

        var result = new string[optionIds.Count];
        int w = 0;
        if (last >= 0)
        {
            result[w++] = optionIds[last];
        }

        for (int i = 0; i < optionIds.Count; i++)
        {
            if (i != last)
            {
                result[w++] = optionIds[i];
            }
        }

        return new StuffOrder(StuffRingOutcome.Ring, result);
    }
}

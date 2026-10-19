using System;
using System.Collections.Generic;

namespace QuickMenu.Core.Pins;

/// <summary>
/// Loose match of a stored mod-dropdown key against the current candidates (ADR 0004): same type,
/// Jaccard similarity of the child key sets of at least 0.5. The best similarity wins, then a
/// candidate with the same category, then the first. The stored key is never rewritten.
/// </summary>
public static class ModDropdownMatcher
{
    public const double MinSimilarity = 0.5;

    public static PinKey? Match(PinKey stored, IReadOnlyList<PinKey> candidates)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(candidates);
        if (stored.Kind != PinKeyKind.ModDropdown || stored.Children is null)
        {
            return null;
        }

        PinKey? best = null;
        double bestScore = 0;
        bool bestSameCategory = false;
        for (int i = 0; i < candidates.Count; i++)
        {
            PinKey? c = candidates[i];
            if (c is null || c.Kind != PinKeyKind.ModDropdown || c.Children is null ||
                !string.Equals(c.Name, stored.Name, StringComparison.Ordinal))
            {
                continue;
            }

            double score = Jaccard(stored.Children, c.Children);
            if (score < MinSimilarity)
            {
                continue;
            }

            bool sameCategory = string.Equals(c.Category, stored.Category, StringComparison.Ordinal);
            if (best is null || score > bestScore || (score == bestScore && sameCategory && !bestSameCategory))
            {
                best = c;
                bestScore = score;
                bestSameCategory = sameCategory;
            }
        }

        return best;
    }

    /// <summary>Jaccard index of two sorted, unique key lists (as PinKey.Children are).</summary>
    internal static double Jaccard(IReadOnlyList<PinKey> a, IReadOnlyList<PinKey> b)
    {
        int i = 0, j = 0, common = 0;
        while (i < a.Count && j < b.Count)
        {
            int cmp = string.CompareOrdinal(a[i].Value, b[j].Value);
            if (cmp == 0)
            {
                common++;
                i++;
                j++;
            }
            else if (cmp < 0)
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        int union = a.Count + b.Count - common;
        return union == 0 ? 0 : (double)common / union;
    }
}

using System.Collections.Generic;

namespace QuickMenu.Core.Pins;

/// <summary>Why a build is unavailable, in display order (most actionable first). Contains no game types.</summary>
public enum ReasonCause
{
    Research,
    BuildingPrerequisite,
    TechLevel,
    Monolith,
    GravEngine,
    Precept,
    Mechanitor,
    Difficulty,
    PlaceWorker,
    Discovery,
}

/// <summary>One composed reason sentence and the cause it explains.</summary>
public readonly record struct ReasonPart(ReasonCause Cause, string? Text);

/// <summary>
/// Chooses the reason shown for an unavailable pinned item (FR-14): the designator's own text wins, then the requirements
/// the adapter composed (research, tech level, required buildings, and so on), then the fallback. Contains no game types.
/// </summary>
public static class UnavailableReason
{
    /// <summary>Separator between composed requirements.</summary>
    public const string Separator = " ";

    /// <summary>The most sentences shown together; the tooltip stays readable when many causes apply.</summary>
    public const int MaxSentences = 3;

    /// <summary>Sorts parts by cause (stable within a cause), drops blank and duplicate sentences and keeps the first <see cref="MaxSentences"/>.</summary>
    public static List<string?> Order(IReadOnlyList<ReasonPart> parts)
    {
        var sorted = new List<ReasonPart>(parts);
        // Stable: List.Sort is not, so order by (cause, original index).
        var indexed = new List<(ReasonPart Part, int Index)>(sorted.Count);
        for (int i = 0; i < sorted.Count; i++)
        {
            indexed.Add((sorted[i], i));
        }

        indexed.Sort((a, b) => a.Part.Cause != b.Part.Cause ? a.Part.Cause.CompareTo(b.Part.Cause) : a.Index.CompareTo(b.Index));
        var result = new List<string?>(MaxSentences);
        foreach ((ReasonPart part, _) in indexed)
        {
            if (string.IsNullOrWhiteSpace(part.Text) || result.Count >= MaxSentences)
            {
                continue;
            }

            string text = part.Text.Trim();
            if (!result.Contains(text))
            {
                result.Add(text);
            }
        }

        return result;
    }

    /// <summary>As <see cref="Select"/>, over cause-tagged parts.</summary>
    public static string SelectFrom(string? designatorReason, IReadOnlyList<ReasonPart>? parts, string fallback)
    {
        return Select(designatorReason, parts == null ? null : Order(parts), fallback);
    }

    /// <param name="designatorReason">The designator's own disabled reason; blank text counts as none.</param>
    /// <param name="requirements">Composed requirement sentences in display order; blank entries are skipped.</param>
    /// <param name="fallback">Shown when nothing else is known.</param>
    public static string Select(string? designatorReason, IReadOnlyList<string?>? requirements, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(designatorReason))
        {
            return designatorReason.Trim();
        }

        if (requirements != null)
        {
            var parts = new List<string>(requirements.Count);
            foreach (string? r in requirements)
            {
                if (!string.IsNullOrWhiteSpace(r))
                {
                    parts.Add(r.Trim());
                }
            }

            if (parts.Count > 0)
            {
                return string.Join(Separator, parts);
            }
        }

        return fallback;
    }
}

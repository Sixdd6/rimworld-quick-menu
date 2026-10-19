using System.Collections.Generic;

namespace QuickMenu.Core.Pins;

/// <summary>
/// Chooses the reason shown for an unavailable pinned item (FR-14): the designator's own text wins, then the requirements
/// the adapter composed (research, tech level, required buildings, and so on), then the fallback. Contains no game types.
/// </summary>
public static class UnavailableReason
{
    /// <summary>Separator between composed requirements.</summary>
    public const string Separator = " ";

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

namespace QuickMenu.Core.Catalog;

/// <summary>
/// The per-ideoligion part of the <see cref="IdeoSignature"/> fingerprint. Every input may be absent (a non-fluid
/// ideoligion has no development data), and absence must never throw: it just contributes zero.
/// </summary>
public static class IdeoFingerprint
{
    /// <param name="identityHash">Reference identity of the ideoligion (runtime hash code).</param>
    /// <param name="preceptCount">Number of precepts, or null if unknown.</param>
    /// <param name="reformCount">Reform generation count, or null when the ideoligion has no development data.</param>
    public static long Of(int identityHash, int? preceptCount, int? reformCount) =>
        ((long)identityHash * 1_000_003L) + ((preceptCount ?? 0) * 1009L) + (reformCount ?? 0);

    /// <summary>Folds <paramref name="next"/> into <paramref name="accumulated"/> (order-sensitive, overflow wraps).</summary>
    public static long Combine(long accumulated, long next) => unchecked((accumulated * 31) + next);
}

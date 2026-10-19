namespace QuickMenu.Core.Catalog;

/// <summary>
/// Change detector for the Ideology part of the catalog (ADR 0006): the current game, the player's primary ideoligion and
/// its precept count, plus a caller-computed fingerprint of everything else the game derives designators from (minor
/// ideoligions, reform generations), compared by reference and value with no allocation. The adapter passes the game objects as
/// plain references, so Core stays free of game types.
/// </summary>
public sealed class IdeoSignature
{
    private object? _game;
    private object? _ideo;
    private int _preceptCount;
    private long _fingerprint;

    /// <summary>Number of times <see cref="Update"/> reported a change that matters (see <see cref="Update"/>).</summary>
    public int ChangeCount { get; private set; }

    /// <summary>
    /// Records the current state. Returns true when it differs from the last recorded one. The first call (nothing
    /// recorded yet) is a change only when there is something to build, so a main menu with no game costs no rebuild.
    /// </summary>
    /// <param name="fingerprint">
    /// Any further state that changes the designator set without changing the primary ideo or its precept count (a reform
    /// that swaps precepts one for one, a minor ideoligion). Ignored without an ideo.
    /// </param>
    public bool Update(object? game, object? ideo, int preceptCount, long fingerprint = 0)
    {
        int count = ideo == null ? 0 : preceptCount;
        long print = ideo == null ? 0 : fingerprint;
        if (ReferenceEquals(game, _game) && ReferenceEquals(ideo, _ideo) && count == _preceptCount && print == _fingerprint)
        {
            return false;
        }

        _game = game;
        _ideo = ideo;
        _preceptCount = count;
        _fingerprint = print;
        ChangeCount++;
        return true;
    }

    /// <summary>True when the last recorded state has a game and an ideoligion, that is, Ideology entries can exist.</summary>
    public bool HasIdeo => _game != null && _ideo != null;
}

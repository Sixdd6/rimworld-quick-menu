namespace QuickMenu.Core.Catalog;

/// <summary>
/// Change detector for the Ideology part of the catalog (ADR 0006): the current game, the player's primary ideoligion and
/// its precept count, compared by reference and count in O(1) with no allocation. The adapter passes the game objects as
/// plain references, so Core stays free of game types.
/// </summary>
public sealed class IdeoSignature
{
    private object? _game;
    private object? _ideo;
    private int _preceptCount;

    /// <summary>Number of times <see cref="Update"/> reported a change that matters (see <see cref="Update"/>).</summary>
    public int ChangeCount { get; private set; }

    /// <summary>
    /// Records the current state. Returns true when it differs from the last recorded one. The first call (nothing
    /// recorded yet) is a change only when there is something to build, so a main menu with no game costs no rebuild.
    /// </summary>
    public bool Update(object? game, object? ideo, int preceptCount)
    {
        int count = ideo == null ? 0 : preceptCount;
        if (ReferenceEquals(game, _game) && ReferenceEquals(ideo, _ideo) && count == _preceptCount)
        {
            return false;
        }

        _game = game;
        _ideo = ideo;
        _preceptCount = count;
        ChangeCount++;
        return true;
    }

    /// <summary>True when the last recorded state has a game and an ideoligion, that is, Ideology entries can exist.</summary>
    public bool HasIdeo => _game != null && _ideo != null;
}

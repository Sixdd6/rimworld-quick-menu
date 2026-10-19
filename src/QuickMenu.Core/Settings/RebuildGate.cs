namespace QuickMenu.Core.Settings;

/// <summary>
/// Decides when the settings page rebuilds the size-dependent textures (T-95). A slider drag changes the value on
/// every frame, and a rebuild costs up to tens of ms at a large rim, so the page asks this gate once per frame with the
/// current texture key (a packed button size, rings and UI scale). The gate says "rebuild now" at most once per
/// change: when the drag ends, or when the key has been unchanged for <see cref="DebounceMs"/> without a drag
/// (stepper clicks, shape buttons). Time is a parameter; no allocation, no game types.
/// </summary>
public sealed class RebuildGate
{
    /// <summary>Quiet time after the last change before a non-drag change is built.</summary>
    public const long DebounceMs = 150;

    private bool _started;
    private bool _wasDragging;
    private long _seen;
    private long _built;
    private long _lastChangeMs;

    /// <summary>True while the latest key differs from the one last built (the preview shows stretched textures).</summary>
    public bool Pending => _started && _seen != _built;

    /// <summary>Forgets what was built, so the next <see cref="Observe"/> asks for a rebuild (the settings page was reopened).</summary>
    public void Reset() => _started = false;

    /// <summary>Call once per frame. True when the textures must be rebuilt for <paramref name="key"/> now.</summary>
    /// <param name="key">Identity of the texture sizing the preview wants.</param>
    /// <param name="dragging">The mouse is held on a slider.</param>
    /// <param name="nowMs">Any monotonic millisecond clock.</param>
    public bool Observe(long key, bool dragging, long nowMs)
    {
        if (!_started)
        {
            _started = true;
            _seen = key;
            _built = key;
            _lastChangeMs = nowMs;
            _wasDragging = dragging;
            return true;
        }

        if (key != _seen)
        {
            _seen = key;
            _lastChangeMs = nowMs;
        }

        bool released = _wasDragging && !dragging;
        _wasDragging = dragging;
        if (_seen == _built || dragging)
        {
            return false;
        }

        if (released || nowMs - _lastChangeMs >= DebounceMs)
        {
            _built = _seen;
            return true;
        }

        return false;
    }
}

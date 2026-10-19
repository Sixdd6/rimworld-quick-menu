namespace QuickMenu.Pinning;

/// <summary>
/// While the depth is above zero, <see cref="PinClickPatch.Prefix"/> does nothing. The dropdown entry wrapper raises it
/// when it has to run the game's own action after a failed pin check, so a live pin prefix can never pin the whole dropdown.
/// </summary>
internal static class PinSuppress
{
    internal static int Depth { get; private set; }

    internal static Scope Enter()
    {
        Depth++;
        return default;
    }

    internal readonly struct Scope : System.IDisposable
    {
        public void Dispose()
        {
            if (Depth > 0)
            {
                Depth--;
            }
        }
    }
}

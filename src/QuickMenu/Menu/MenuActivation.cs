namespace QuickMenu.Menu;

/// <summary>
/// Non-zero while the quick menu is calling a designator's <c>ProcessInput</c>. The pin prefix (ADR 0005/0006) returns
/// immediately in that case, so a held Ctrl/Alt during activation is never mistaken for a pin gesture.
/// </summary>
internal static class MenuActivation
{
    internal static int Depth { get; private set; }

    internal static void Enter() => Depth++;

    internal static void Exit()
    {
        if (Depth > 0)
        {
            Depth--;
        }
    }
}

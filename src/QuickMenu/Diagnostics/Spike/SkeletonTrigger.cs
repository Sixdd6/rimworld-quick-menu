using LudeonTK;
using RimWorld;
using Verse;

namespace QuickMenu.Diagnostics.Spike;

/// <summary>Walking-skeleton trigger: a mouse button index that a debug action cycles (spike S-2).</summary>
internal static class SkeletonTrigger
{
    // Mouse indices (0-based): 3 = "Mouse 4", 4 = "Mouse 5", 2 = middle.
    private static readonly int[] _indices = { 3, 4, 2 };
    private static readonly string[] _labels = { "Mouse 4", "Mouse 5", "Middle mouse" };
    private static int _current;

    internal static int MouseIndex => _indices[_current];

    internal static string Label => _labels[_current];

    internal static void Cycle() => _current = (_current + 1) % _indices.Length;

    [DebugAction("Quick Menu", "Cycle skeleton trigger", allowedGameStates = AllowedGameStates.Playing)]
    private static void CycleAction()
    {
        Cycle();
        Messages.Message("QuickMenu.Debug.TriggerCycled".Translate(Label), MessageTypeDefOf.SilentInput, false);
    }
}

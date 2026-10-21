using UnityEngine;
using Verse;

namespace QuickMenu.Diagnostics;

/// <summary>
/// Restores <c>GUI.color</c>, <c>Text.Font</c>, <c>Text.Anchor</c> and <c>Text.WordWrap</c> when it is disposed, so a window
/// that throws half-way through drawing cannot leave the game's UI tinted, resized or aligned (NFR-6). A struct: no allocation.
/// </summary>
internal readonly struct GuiStateScope : System.IDisposable
{
    private readonly Color _color;
    private readonly GameFont _font;
    private readonly TextAnchor _anchor;
    private readonly bool _wordWrap;

    private GuiStateScope(Color color, GameFont font, TextAnchor anchor, bool wordWrap)
    {
        _color = color;
        _font = font;
        _anchor = anchor;
        _wordWrap = wordWrap;
    }

    internal static GuiStateScope Save() => new(GUI.color, Text.Font, Text.Anchor, Text.WordWrap);

    public void Dispose()
    {
        GUI.color = _color;
        Text.Font = _font;
        Text.Anchor = _anchor;
        Text.WordWrap = _wordWrap;
    }
}

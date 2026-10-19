using QuickMenu.Core.Geometry;
using QuickMenu.Core.Render;
using UnityEngine;
using Verse;

namespace QuickMenu.Menu;

/// <summary>
/// The two text plates of design.md S5 (T-55): the edit-mode <b>help panel</b> at the top centre of the screen (60 px
/// down, at most 520 wide, <c>plate</c> background) with the Fixed or Auto help text and the capacity line
/// (<c>textMuted</c>; <c>danger</c> with "The menu is full." when full), and the empty-menu <b>hint plate</b>
/// (FR-16), which replaces the label plate and wraps at 360. Geometry is in <see cref="EditVisuals"/>.
/// </summary>
/// <remarks>
/// <para>API for T-57 (edit mode): call <see cref="SetContent"/> when edit mode starts and whenever the pin count,
/// capacity or fullness changes (it does nothing when nothing changed), and <see cref="Draw"/> from the window's
/// repaint while editing, after the menu. The panel lives outside the menu, so entering edit mode never moves or
/// dodges the menu.</para>
/// <para>API for T-58 (empty menu): call <see cref="DrawHint"/> instead of the label plate when there are no visible
/// pins and the menu is not in edit mode.</para>
/// <para>Strings are built only by <see cref="SetContent"/> and <see cref="EditTexts"/>; text is measured only when the
/// content or the UI screen width changes. Drawing allocates nothing.</para>
/// </remarks>
internal static class HelpPanelRenderer
{
    private static bool _hasContent;
    private static bool _auto;
    private static bool _full;
    private static int _used = -1;
    private static int _capacity = -1;
    private static string _help = string.Empty;
    private static string _capacityLine = string.Empty;
    private static float _measuredWidth = -1f;
    private static Rect2 _panel;
    private static float _helpHeight;
    private static float _lineHeight;

    private static string? _measuredHint;
    private static float _hintWidth;
    private static float _hintHeight;

    /// <summary>
    /// Sets what the help panel says. <paramref name="auto"/>: Auto size ("{used} pinned"), else Fixed ("{used} of
    /// {capacity} slots used"). <paramref name="full"/> (from <c>Capacity</c>, T-51) adds "The menu is full." and turns
    /// the line <c>danger</c>. Cheap when nothing changed.
    /// </summary>
    internal static void SetContent(bool auto, int used, int capacity, bool full)
    {
        string help = EditTexts.EditHelp(auto);
        if (_hasContent && auto == _auto && used == _used && capacity == _capacity && full == _full && ReferenceEquals(help, _help))
        {
            return;
        }

        _hasContent = true;
        _auto = auto;
        _used = used;
        _capacity = capacity;
        _full = full;
        _help = help;
        string count = auto
            ? "QuickMenu.Menu.CapacityAuto".Translate(used).Resolve()
            : "QuickMenu.Menu.CapacityFixed".Translate(used, capacity).Resolve();
        _capacityLine = full ? count + ". " + "QuickMenu.Menu.Full".Translate().Resolve() : count;
        _measuredWidth = -1f;
    }

    /// <summary>Forgets the content (edit mode ended); <see cref="Draw"/> then draws nothing until the next <see cref="SetContent"/>.</summary>
    internal static void Clear() => _hasContent = false;

    /// <summary>Draws the help panel at the top centre of the UI screen. Repaint only; the window must cover the UI screen from (0, 0).</summary>
    internal static void Draw()
    {
        if (!_hasContent || Event.current.type != EventType.Repaint)
        {
            return;
        }

        Color oldColor = GUI.color;
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        try
        {
            Text.Font = GameFont.Small;
            float screenWidth = UI.screenWidth;
            if (screenWidth != _measuredWidth)
            {
                Measure(screenWidth);
            }

            if (Textures.Ready)
            {
                GUI.color = QMStyle.Plate;
                Widgets.DrawAtlas(MenuRenderer.ToRect(_panel), Textures.Mask(MaskId.Plate));
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = QMStyle.Text;
            Widgets.Label(MenuRenderer.ToRect(EditVisuals.HelpTextRect(_panel, _helpHeight)), _help);
            GUI.color = _full ? QMStyle.Danger : QMStyle.TextMuted;
            Widgets.Label(MenuRenderer.ToRect(EditVisuals.CapacityLineRect(_panel, _helpHeight, _lineHeight)), _capacityLine);
        }
        finally
        {
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
        }
    }

    /// <summary>
    /// The empty-menu hint plate (S5, FR-16) in place of the label plate of <paramref name="model"/>: same top and centre,
    /// at most 360 wide, wrapped, <c>plate</c> background and <c>text</c>. Repaint only.
    /// </summary>
    internal static void DrawHint(WheelModel model, in RenderTransform transform)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        string hint = EditTexts.EmptyHint;
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        try
        {
            Text.Font = GameFont.Small;
            if (!ReferenceEquals(hint, _measuredHint))
            {
                float width = Text.CalcSize(hint).x;
                float max = EditVisuals.HintTextMaxWidth;
                _hintWidth = width < max ? width : max;
                _hintHeight = Text.CalcHeight(hint, _hintWidth);
                _measuredHint = hint;
            }

            Rect2 anchor = model.PlateRect(0f);
            Rect2 layout = EditVisuals.HintPlateRect(anchor.X + (anchor.Width / 2f), anchor.Y, _hintWidth, _hintHeight);
            Rect rect = MenuRenderer.ToRect(transform.Rect(layout));
            if (Textures.Ready)
            {
                MenuRenderer.Tint(QMStyle.Plate);
                Widgets.DrawAtlas(rect, Textures.Mask(MaskId.Plate));
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            MenuRenderer.Tint(QMStyle.Text);
            Widgets.Label(rect, hint);
        }
        finally
        {
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }
    }

    private static void Measure(float screenWidth)
    {
        float max = EditVisuals.HelpTextMaxWidth(screenWidth);
        float helpWidth = Text.CalcSize(_help).x;
        float lineWidth = Text.CalcSize(_capacityLine).x;
        float content = helpWidth > lineWidth ? helpWidth : lineWidth;
        if (content > max)
        {
            content = max;
        }

        _helpHeight = Text.CalcHeight(_help, content);
        _lineHeight = Text.CalcHeight(_capacityLine, content);
        _panel = EditVisuals.HelpPanelRect(screenWidth, content, _helpHeight, _lineHeight);
        _measuredWidth = screenWidth;
    }
}

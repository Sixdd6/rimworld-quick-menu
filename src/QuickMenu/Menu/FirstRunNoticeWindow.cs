using System;
using QuickMenu.Core.Diagnostics;
using QuickMenu.Core.Input;
using QuickMenu.Diagnostics;
using UnityEngine;
using Verse;

namespace QuickMenu.Menu;

/// <summary>
/// The first-run notice (design.md S8): non-modal, does not pause, does not close on an outside click. Offers the
/// middle-mouse switch because mice do not report side buttons. Shown once.
/// </summary>
internal sealed class FirstRunNoticeWindow : Window
{
    internal static readonly PatchSite Site = new("FirstRunNoticeWindow.DoWindowContents");

    private readonly string _title = "QuickMenu.Notice.FirstRun.Title".Translate();
    private readonly string _body = "QuickMenu.Notice.FirstRun.Body".Translate();
    private readonly string? _mintLine;
    private readonly string _useMiddle = "QuickMenu.Notice.UseMiddle".Translate();
    private readonly string _openSettings = "QuickMenu.Notice.OpenSettings".Translate();
    private readonly string _ok = "QuickMenu.Notice.Ok".Translate();

    internal FirstRunNoticeWindow(bool includeMintLine)
    {
        _mintLine = includeMintLine ? "QuickMenu.Notice.Mint".Translate().ToString() : null;
        forcePause = false;
        absorbInputAroundWindow = false;
        closeOnClickedOutside = false;
        closeOnAccept = false;
        closeOnCancel = false;
        doCloseX = false;
        doCloseButton = false;
        preventCameraMotion = false;
        draggable = true;
    }

    public override Vector2 InitialSize => new(460f, _mintLine == null ? 190f : 260f);

    protected override void SetInitialSizeAndPosition()
    {
        base.SetInitialSizeAndPosition();
        windowRect.x = (UI.screenWidth - windowRect.width) / 2f;
        windowRect.y = 40f;
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (Site.Disabled)
        {
            CloseSafely();
            return;
        }

        using GuiStateScope guiState = GuiStateScope.Save();
        try
        {
            FaultInjection.Check(FaultPoint.FirstRunWindow);
            Draw(inRect);
        }
        catch (Exception ex)
        {
            Site.Fail(ex);
            CloseSafely();
        }
    }

    private void CloseSafely()
    {
        try
        {
            Close();
        }
        catch (Exception ex)
        {
            QuickMenu.Diagnostics.Log.LogOnce("FirstRunNoticeWindow.Close", $"Closing the first-run notice failed: {ex}");
        }
    }

    private void Draw(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), _title);
        Text.Font = GameFont.Small;
        float y = inRect.y + 36f;
        float bodyHeight = Text.CalcHeight(_body, inRect.width);
        Widgets.Label(new Rect(inRect.x, y, inRect.width, bodyHeight), _body);
        y += bodyHeight + 6f;
        if (_mintLine != null)
        {
            float mintHeight = Text.CalcHeight(_mintLine, inRect.width);
            Widgets.Label(new Rect(inRect.x, y, inRect.width, mintHeight), _mintLine);
        }

        const float h = 30f;
        float w = (inRect.width - 16f) / 3f;
        float by = inRect.yMax - h;
        if (Widgets.ButtonText(new Rect(inRect.x, by, w, h), _useMiddle))
        {
            Settings.BindingRow.Set(TriggerBinding.Mouse(2));
            Close();
        }

        if (Widgets.ButtonText(new Rect(inRect.x + w + 8f, by, w, h), _openSettings))
        {
            Close();
            QuickMenuMod.OpenSettings();
        }

        if (Widgets.ButtonText(new Rect(inRect.x + 2 * (w + 8f), by, w, h), _ok))
        {
            Close();
        }
    }
}

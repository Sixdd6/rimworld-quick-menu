using System;
using QuickMenu.Core.Input;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace QuickMenu.Settings;

/// <summary>
/// Small modal that captures the trigger (ADR 0003, design.md S7). Keys come from <c>KeyDown</c> events. Mouse buttons 2..4
/// are polled in <see cref="WindowUpdate"/>, because IMGUI never reports side buttons. The decisions are made in Core
/// (<see cref="BindingCapture"/>).
/// </summary>
internal sealed class BindingCaptureWindow : Window
{
    private readonly Action<TriggerBinding> _onCaptured;
    private readonly string _title = "QuickMenu.Capture.Title".Translate();
    private readonly string _body = "QuickMenu.Capture.Body".Translate();
    private readonly string _rejected = "QuickMenu.Capture.Rejected".Translate();
    private readonly string _cancel = "QuickMenu.Capture.Cancel".Translate();
    private bool _showRejected;

    internal BindingCaptureWindow(Action<TriggerBinding> onCaptured)
    {
        _onCaptured = onCaptured;
        forcePause = false;
        doCloseX = false;
        doCloseButton = false;
        closeOnClickedOutside = false;
        closeOnAccept = false;
        closeOnCancel = false;
        absorbInputAroundWindow = true;
        preventCameraMotion = true;
        draggable = false;
    }

    public override Vector2 InitialSize => new(360f, 150f);

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        for (int button = TriggerBinding.MinMouseButton; button <= TriggerBinding.MaxMouseButton; button++)
        {
            if (UnityEngine.Input.GetMouseButtonDown(button))
            {
                Apply(BindingCapture.DecideMouse(button));
                return;
            }
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), _title);
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(inRect.x, inRect.y + 34f, inRect.width, 44f), _body);
        if (_showRejected)
        {
            GUI.color = Menu.QMStyle.Danger;
            Widgets.Label(new Rect(inRect.x, inRect.y + 62f, inRect.width, 24f), _rejected);
            GUI.color = Color.white;
        }

        var cancelRect = new Rect(inRect.xMax - 100f, inRect.yMax - 30f, 100f, 30f);
        Event ev = Event.current;
        if (ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None)
        {
            Apply(BindingCapture.DecideKey((int)ev.keyCode));
            ev.Use();
        }
        else if (ev.type == EventType.MouseDown && ev.button <= 1 && !cancelRect.Contains(ev.mousePosition))
        {
            Apply(BindingCapture.DecideMouse(ev.button));
            ev.Use();
        }

        if (Widgets.ButtonText(cancelRect, _cancel))
        {
            Close();
        }
    }

    private void Apply(CaptureDecision decision)
    {
        switch (decision.Outcome)
        {
            case CaptureOutcome.Accept:
                Close();
                _onCaptured(decision.Binding);
                break;
            case CaptureOutcome.Cancel:
                Close();
                break;
            case CaptureOutcome.RejectLeftRight:
                _showRejected = true;
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
                break;
        }
    }
}

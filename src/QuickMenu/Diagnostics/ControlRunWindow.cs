using System;
using LudeonTK;
using QuickMenu.Catalog;
using QuickMenu.Core.Geometry;
using QuickMenu.Core.Layout;
using QuickMenu.Menu;
using UnityEngine;
using Verse;

namespace QuickMenu.Diagnostics;

/// <summary>
/// P-9 control run: a full-screen transparent window that makes the same <c>GUI</c> calls per frame as
/// <see cref="Menu.QuickMenuWindow"/> (hub, one box per slot, <c>Designator.DrawIcon</c> on the same cached vanilla
/// designators) with the same slot count N, but no mod logic: no hit test, no activation. Whatever it allocates is
/// the engine/IMGUI baseline. N is the live wheel's count (<see cref="Menu.MenuController.LastSlotCount"/>, else the
/// first catalog entries) and is printed by "perf report". It closes after <see cref="Seconds"/> s, on a click or on Esc.
/// </summary>
internal sealed class ControlRunWindow : Window
{
    private const float ButtonSize = 56f;
    private const float Seconds = 10f;

    private static readonly Color _slotColor = new(0.12f, 0.12f, 0.12f, 0.85f);
    private static readonly Color _hubColor = new(0f, 0f, 0f, 0.4f);

    private readonly float _endTime = Time.realtimeSinceStartup + Seconds;
    private readonly Rect[] _boxes;
    private readonly Rect[] _icons;
    private readonly Designator?[] _designators;
    private readonly Rect _hub;

    internal ControlRunWindow()
    {
        layer = WindowLayer.GameUI;
        forcePause = false;
        doWindowBackground = false;
        drawShadow = false;
        doCloseX = false;
        doCloseButton = false;
        closeOnCancel = true;
        closeOnAccept = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        draggable = false;
        resizeable = false;
        soundAppear = null;
        soundClose = null;

        Vec2 centre = new(UI.screenWidth / 2f, UI.screenHeight / 2f);
        SlotPlacement[] wheel = WheelLayout.Compute(new WheelParams(ButtonSize, 1, centre));
        Designator?[] live = MenuController.LastDesignators ?? Array.Empty<Designator?>();
        int n = MenuController.LastSlotCount > 0 ? MenuController.LastSlotCount : Math.Min(6, wheel.Length);
        n = Math.Min(n, wheel.Length);
        PerfCounters.ControlSlots = n;
        _boxes = new Rect[n];
        _icons = new Rect[n];
        _designators = new Designator?[n];
        for (int i = 0; i < n; i++)
        {
            Vec2 c = wheel[i].Center;
            _boxes[i] = new Rect(c.X - (ButtonSize / 2f), c.Y - (ButtonSize / 2f), ButtonSize, ButtonSize);
            float icon = ButtonSize * 0.75f;
            _icons[i] = new Rect(c.X - (icon / 2f), c.Y - (icon / 2f), icon, icon);
            _designators[i] = i < live.Length ? live[i] : FromCatalog(i);
        }

        _hub = new Rect(centre.X - (ButtonSize / 2f), centre.Y - (ButtonSize / 2f), ButtonSize, ButtonSize);
    }

    // Before the menu has ever been opened there is no live wheel: use the first catalog designators.
    private static Designator? FromCatalog(int i) => MenuController.LastDesignators == null && i < DesignatorCatalog.Entries.Count ? DesignatorCatalog.Entries[i].Designator : null;

    public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);

    protected override float Margin => 0f;

    protected override void SetInitialSizeAndPosition()
    {
        windowRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (Time.realtimeSinceStartup >= _endTime)
        {
            Close();
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        using var scope = new MeasureScope(StatId.Control);
        Event ev = Event.current;
        if (ev.type == EventType.Repaint)
        {
            Color old = GUI.color;
            try
            {
                GUI.color = _hubColor;
                GUI.DrawTexture(_hub, BaseContent.WhiteTex);
                for (int i = 0; i < _boxes.Length; i++)
                {
                    GUI.color = _slotColor;
                    GUI.DrawTexture(_boxes[i], BaseContent.WhiteTex);
                    GUI.color = old;
                    Designator? d = _designators[i];
                    if (d != null)
                    {
                        d.DrawIcon(_icons[i], null, default);
                    }
                }
            }
            finally
            {
                GUI.color = old;
            }
        }
        else if (ev.type == EventType.MouseDown)
        {
            ev.Use();
            Close();
        }
    }

    [DebugAction("Quick Menu", "control window", allowedGameStates = AllowedGameStates.Playing)]
    private static void Open()
    {
        PerfCounters.Enable();
        PerfCounters.Get(StatId.Control).Reset();
        PerfCounters.Get(StatId.Control).MarkOpen();
        Find.WindowStack.Add(new ControlRunWindow());
        Log.Info("Quick Menu: control window open for " + (int)Seconds + " s; run 'perf report' afterwards.");
    }
}

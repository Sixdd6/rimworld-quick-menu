using System;
using System.Globalization;
using System.Text;
using LudeonTK;
using UnityEngine;
using Verse;

namespace QuickMenu.Diagnostics.Spike;

/// <summary>
/// Spike S-2 probe: for 15 s logs every <c>Input.GetMouseButtonDown/Up(0..4)</c>, every <c>KeyDown</c> key (polled)
/// and every IMGUI mouse event seen inside the probe window, each with the frame number. The report shows which
/// physical side button is index 3 and 4, that IMGUI never sees them, and that up events arrive (hold mode).
/// Dev-only; allocation is accepted while the probe runs.
/// </summary>
internal sealed class InputProbe : Window
{
    private const float DurationSeconds = 15f;
    private const int MouseButtonCount = 5;

    private static readonly PatchSite _site = new("InputProbe");
    private static readonly KeyCode[] _keys = Enum.GetValues<KeyCode>();

    private readonly StringBuilder _log = new();
    private readonly int[] _polledDown = new int[MouseButtonCount];
    private readonly int[] _polledUp = new int[MouseButtonCount];
    private readonly int[] _imguiDown = new int[MouseButtonCount];
    private readonly int[] _imguiUp = new int[MouseButtonCount];
    private readonly float _endTime = Time.realtimeSinceStartup + DurationSeconds;
    private readonly int _startFrame = Time.frameCount;
    private int _imguiOther;
    private int _keyDowns;
    private bool _reported;

    public InputProbe()
    {
        doCloseX = true;
        doCloseButton = false;
        draggable = true;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        closeOnClickedOutside = false;
        closeOnAccept = false;
        closeOnCancel = false;
    }

    public override Vector2 InitialSize => new(420f, 220f);

    internal static bool Running { get; private set; }

    [DebugAction("Quick Menu", "input probe (15 s)", allowedGameStates = AllowedGameStates.Playing)]
    private static void Start()
    {
        if (Running)
        {
            return;
        }

        Running = true;
        Find.WindowStack.Add(new InputProbe());
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        if (_site.Disabled || _reported)
        {
            return;
        }

        try
        {
            PollMouse();
            PollKeys();
            if (Time.realtimeSinceStartup >= _endTime)
            {
                Finish();
            }
        }
        catch (Exception ex)
        {
            _site.Fail(ex);
            Finish();
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (_site.Disabled || _reported)
        {
            return;
        }

        try
        {
            var ev = Event.current;
            if (ev.isMouse || ev.rawType == EventType.MouseDown || ev.rawType == EventType.MouseUp)
            {
                RecordImgui(ev);
            }

            var left = Math.Max(0f, _endTime - Time.realtimeSinceStartup);
            Widgets.Label(
                inRect,
                "Quick Menu input probe, " + left.ToString("F0", CultureInfo.InvariantCulture) + " s left.\n"
                + "Press left, right, middle, front side, back side, then one key, about 1 s apart, with the mouse over this window.");
        }
        catch (Exception ex)
        {
            _site.Fail(ex);
            Finish();
        }
    }

    public override void PostClose()
    {
        base.PostClose();
        Finish();
    }

    private void PollMouse()
    {
        for (var b = 0; b < MouseButtonCount; b++)
        {
            if (UnityEngine.Input.GetMouseButtonDown(b))
            {
                _polledDown[b]++;
                Line("Input.GetMouseButtonDown(" + b + ")");
            }

            if (UnityEngine.Input.GetMouseButtonUp(b))
            {
                _polledUp[b]++;
                Line("Input.GetMouseButtonUp(" + b + ")");
            }
        }
    }

    private void PollKeys()
    {
        for (var i = 0; i < _keys.Length; i++)
        {
            var key = _keys[i];
            // KeyCode.Mouse0..6 are also reported by GetKeyDown; they are logged as keys to compare with the button polls.
            if (UnityEngine.Input.GetKeyDown(key))
            {
                _keyDowns++;
                Line("Input.GetKeyDown(" + key + ")");
            }
        }
    }

    private void RecordImgui(Event ev)
    {
        var button = ev.button;
        var kind = ev.type == EventType.Used ? ev.rawType : ev.type;
        var isDown = kind == EventType.MouseDown;
        var isUp = kind == EventType.MouseUp;
        if (button >= 0 && button < MouseButtonCount && isDown)
        {
            _imguiDown[button]++;
        }
        else if (button >= 0 && button < MouseButtonCount && isUp)
        {
            _imguiUp[button]++;
        }
        else
        {
            _imguiOther++;
            if (kind == EventType.MouseMove || kind == EventType.MouseDrag || kind == EventType.Layout)
            {
                return; // not interesting; only the counter moves
            }
        }

        if (isDown || isUp)
        {
            Line("IMGUI " + kind + " button=" + button + " type=" + ev.type + " rawType=" + ev.rawType);
        }
    }

    private void Line(string text)
    {
        _log.Append("frame=").Append(Time.frameCount).Append(" t=")
            .Append((Time.realtimeSinceStartup - (_endTime - DurationSeconds)).ToString("F2", CultureInfo.InvariantCulture))
            .Append(' ').AppendLine(text);
    }

    private void Finish()
    {
        if (_reported)
        {
            return;
        }

        _reported = true;
        Running = false;
        var sb = new StringBuilder();
        sb.AppendLine(Log.Prefix + "BEGIN input probe");
        sb.AppendLine("frames " + _startFrame + ".." + Time.frameCount);
        sb.Append(_log);
        sb.AppendLine("summary (index 0..4):");
        for (var b = 0; b < MouseButtonCount; b++)
        {
            sb.AppendLine("button " + b + ": polled down=" + _polledDown[b] + " up=" + _polledUp[b]
                + " | IMGUI down=" + _imguiDown[b] + " up=" + _imguiUp[b]);
        }

        sb.AppendLine("keyDowns=" + _keyDowns + " imguiOtherMouseEvents=" + _imguiOther);
        sb.AppendLine("Fill in: physical button for index 3 = ?, index 4 = ? (from the press order: left, right, middle, front side, back side)");
        sb.AppendLine(Log.Prefix + "END input probe");
        Verse.Log.Message(sb.ToString());
        if (Find.WindowStack.IsOpen(this))
        {
            Close();
        }
    }
}

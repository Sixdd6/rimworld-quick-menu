using System.Text;
using LudeonTK;
using QuickMenu.Pinning;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuickMenu.Diagnostics.Spike;

/// <summary>
/// Spike S-1 / S-3 probe: logs what <c>Designator.ProcessInput</c> receives and counts invocations per frame.
/// Off by default; allocation and logging happen only while a developer has switched it on.
/// </summary>
internal static class ProcessInputProbe
{
    private static int _startFrame;
    private static int _total;
    private static int _frame = -1;
    private static int _inFrame;
    private static int _maxInFrame;
    private static int _framesWithCalls;
    private static int _firstCallFrame = -1;
    private static int _callsBeforeFirstInput;
    private static int _inputCalls;
    private static int _multiCallFrames;
    private static int _nullEvents;
    private static bool _sawInputEvent;

    internal static bool Enabled { get; private set; }

    internal static void Record(Designator designator, Event? ev)
    {
        var frame = Time.frameCount;
        if (_firstCallFrame < 0)
        {
            _firstCallFrame = frame;
        }

        if (ev == null)
        {
            _nullEvents++;
        }
        else if (ev.isMouse || ev.isKey)
        {
            _sawInputEvent = true;
            _inputCalls++;
        }

        if (!_sawInputEvent)
        {
            _callsBeforeFirstInput++;
        }

        if (frame != _frame)
        {
            _frame = frame;
            _inFrame = 0;
            _framesWithCalls++;
        }

        _inFrame++;
        _total++;
        if (_inFrame == 2)
        {
            _multiCallFrames++;
        }

        if (_inFrame > _maxInFrame)
        {
            _maxInFrame = _inFrame;
        }

        var type = designator.GetType();
        var evText = ev == null
            ? "ev=null\n"
            : "ev.type=" + ev.type + " ev.rawType=" + ev.rawType + " ev.button=" + ev.button + " ev.isKey=" + ev.isKey
            + " ev.isMouse=" + ev.isMouse + " used=" + (ev.type == EventType.Used) + "\n"
            + "ev.control=" + ev.control + " ev.alt=" + ev.alt + " ev.shift=" + ev.shift + "\n";
        Verse.Log.Message(
            Log.Prefix + "BEGIN ProcessInput call\n"
            + "instance=" + type.FullName + " [" + type.Assembly.GetName().Name + "]\n"
            + "frame=" + frame + " callInFrame=" + _inFrame + "\n"
            + evText
            + "Input.GetKey ctrl L/R=" + UnityEngine.Input.GetKey(KeyCode.LeftControl) + "/" + UnityEngine.Input.GetKey(KeyCode.RightControl)
            + " alt L/R=" + UnityEngine.Input.GetKey(KeyCode.LeftAlt) + "/" + UnityEngine.Input.GetKey(KeyCode.RightAlt)
            + " shift L/R=" + UnityEngine.Input.GetKey(KeyCode.LeftShift) + "/" + UnityEngine.Input.GetKey(KeyCode.RightShift) + "\n"
            + Log.Prefix + "END ProcessInput call");
    }

    [DebugAction("Quick Menu", "ProcessInput probe (toggle)", allowedGameStates = AllowedGameStates.Playing)]
    private static void Toggle()
    {
        Enabled = !Enabled;
        if (Enabled)
        {
            _startFrame = Time.frameCount;
            _total = 0;
            _frame = -1;
            _inFrame = 0;
            _maxInFrame = 0;
            _framesWithCalls = 0;
            _firstCallFrame = -1;
            _callsBeforeFirstInput = 0;
            _inputCalls = 0;
            _multiCallFrames = 0;
            _nullEvents = 0;
            _sawInputEvent = false;
        }

        Log.Info("ProcessInput probe " + (Enabled ? "ON from frame " + _startFrame : "OFF"));
    }

    [DebugAction("Quick Menu", "ProcessInput probe report", allowedGameStates = AllowedGameStates.Playing)]
    private static void Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Log.Prefix + "BEGIN ProcessInput report");
        sb.AppendLine("patched=" + PinClickPatch.PatchedCount + " failed=" + PinClickPatch.FailedCount
            + " patchMs=" + PinClickPatch.PatchMilliseconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " siteDisabled=" + PinClickPatch.Site.Disabled);
        foreach (var line in PinClickPatch.Patched)
        {
            sb.AppendLine("patched: " + line);
        }

        foreach (var line in PinClickPatch.Failed)
        {
            sb.AppendLine("FAILED: " + line);
        }

        sb.AppendLine("probeEnabled=" + Enabled + " startFrame=" + _startFrame + " nowFrame=" + Time.frameCount
            + " framesElapsed=" + (Time.frameCount - _startFrame));
        sb.AppendLine("invocations=" + _total + " framesWithCalls=" + _framesWithCalls + " maxPerFrame=" + _maxInFrame);
        sb.AppendLine("firstCallFrame=" + _firstCallFrame + " callsBeforeFirstMouseOrKeyEvent=" + _callsBeforeFirstInput
            + " mouseOrKeyCalls=" + _inputCalls + " nullEvents=" + _nullEvents + " framesWithCallInFrame>1=" + _multiCallFrames);
        sb.AppendLine("idle phase: invocations with no mouse/key event should be 0 when no click was made; run this report again after the click phase.");
        sb.AppendLine(Log.Prefix + "END ProcessInput report");
        Verse.Log.Message(sb.ToString());
    }
}

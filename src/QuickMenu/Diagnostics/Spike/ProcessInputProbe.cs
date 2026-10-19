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

    internal static bool Enabled { get; private set; }

    internal static void Record(Designator designator, Event ev)
    {
        var frame = Time.frameCount;
        if (frame != _frame)
        {
            _frame = frame;
            _inFrame = 0;
            _framesWithCalls++;
        }

        _inFrame++;
        _total++;
        if (_inFrame > _maxInFrame)
        {
            _maxInFrame = _inFrame;
        }

        var type = designator.GetType();
        Verse.Log.Message(
            Log.Prefix + "BEGIN ProcessInput call\n"
            + "instance=" + type.FullName + " [" + type.Assembly.GetName().Name + "]\n"
            + "frame=" + frame + " callInFrame=" + _inFrame + "\n"
            + "ev.type=" + ev.type + " ev.rawType=" + ev.rawType + " ev.button=" + ev.button + " ev.isKey=" + ev.isKey
            + " ev.isMouse=" + ev.isMouse + " used=" + (ev.type == EventType.Used) + "\n"
            + "ev.control=" + ev.control + " ev.alt=" + ev.alt + " ev.shift=" + ev.shift + "\n"
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
        sb.AppendLine(Log.Prefix + "END ProcessInput report");
        Verse.Log.Message(sb.ToString());
    }
}

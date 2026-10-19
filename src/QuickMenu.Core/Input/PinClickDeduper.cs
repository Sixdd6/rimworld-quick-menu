namespace QuickMenu.Core.Input;

/// <summary>
/// Makes sure one physical click pins once. The prefix may run on the frame the left button goes down and
/// again on the frame it goes up; the adapter calls <see cref="Accept"/> with <c>Time.frameCount</c> after
/// the classifier said <see cref="PinClickAction.Pin"/>, plus the frame of the last raw left-button DOWN
/// (recorded by the per-frame poller; <see cref="NoPress"/> if none). The down frame wins; an up is
/// suppressed only when it belongs to the press already accepted, so a later separate up-only click
/// (down not seen by the prefix) is still accepted. A down frame older than the last accepted press is stale and is ignored. A repeated call within one frame is rejected too.
/// Note: the T-26 prefix must still return false (swallow the click) on a suppressed frame of a pinned
/// click, so the original designator never sees the up.
/// </summary>
public sealed class PinClickDeduper
{
    /// <summary>Pass as <c>lastLeftDownFrame</c> when no left press has been recorded.</summary>
    public const int NoPress = int.MinValue;

    private int _lastFrame = int.MinValue;
    private int _acceptedPress = NoPress;

    public bool Accept(int frame, int lastLeftDownFrame, in PinClickInput input)
    {
        if (frame == _lastFrame)
        {
            return false;
        }

        _lastFrame = frame;
        if (input.LeftDownThisFrame || input.RawType == RawEventType.MouseDown)
        {
            _acceptedPress = frame;
            return true;
        }

        if (lastLeftDownFrame != NoPress && lastLeftDownFrame == _acceptedPress)
        {
            return false;
        }

        // A down frame older than the last accepted press is stale (the poller missed a press): it never rejects and
        // never moves the accepted press backwards.
        if (lastLeftDownFrame > _acceptedPress)
        {
            _acceptedPress = lastLeftDownFrame;
        }

        return true;
    }
}

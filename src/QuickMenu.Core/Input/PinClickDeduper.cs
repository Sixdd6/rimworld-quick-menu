namespace QuickMenu.Core.Input;

/// <summary>
/// Makes sure one physical click pins once. The prefix may run on the frame the left button goes down and
/// again on the frame it goes up; the adapter calls <see cref="Accept"/> with <c>Time.frameCount</c> after
/// the classifier said <see cref="PinClickAction.Pin"/>. The down frame wins; a later up frame is ignored,
/// and so is a repeated call within the same frame (base ProcessInput re-entry).
/// </summary>
public sealed class PinClickDeduper
{
    private int _lastFrame = int.MinValue;
    private bool _suppressUp;

    public bool Accept(int frame, in PinClickInput input)
    {
        if (frame == _lastFrame)
        {
            return false;
        }

        if (input.LeftDownThisFrame || input.RawType == RawEventType.MouseDown)
        {
            _lastFrame = frame;
            _suppressUp = true;
            return true;
        }

        if (_suppressUp)
        {
            _suppressUp = false;
            _lastFrame = frame;
            return false;
        }

        _lastFrame = frame;
        return true;
    }
}

namespace QuickMenu.Core.Input;

/// <summary>What the capture dialog does with one input.</summary>
public enum CaptureOutcome
{
    /// <summary>Not a candidate (modifier-less noise, unsupported button): keep waiting.</summary>
    Ignore,

    /// <summary>Store <see cref="CaptureDecision.Binding"/> and close.</summary>
    Accept,

    /// <summary>Close without changing the binding (Esc).</summary>
    Cancel,

    /// <summary>Left or right mouse button: show the inline error and play <c>ClickReject</c>.</summary>
    RejectLeftRight,
}

/// <summary>The decision for one captured input.</summary>
public readonly record struct CaptureDecision(CaptureOutcome Outcome, TriggerBinding Binding);

/// <summary>Pure decision logic of the binding capture dialog (ADR 0003, design.md S7).</summary>
public static class BindingCapture
{
    /// <summary>Unity KeyCode.Escape.</summary>
    public const int EscapeKeyCode = 27;

    /// <summary>A key went down (<c>KeyDown</c> event). Esc cancels. Mouse key codes (323..329) and 0 are ignored: mice are polled.</summary>
    public static CaptureDecision DecideKey(int keyCode)
    {
        if (keyCode == EscapeKeyCode)
        {
            return new CaptureDecision(CaptureOutcome.Cancel, TriggerBinding.None);
        }

        return TriggerBinding.IsValidKeyCode(keyCode)
            ? new CaptureDecision(CaptureOutcome.Accept, TriggerBinding.Key(keyCode))
            : new CaptureDecision(CaptureOutcome.Ignore, TriggerBinding.None);
    }

    /// <summary>A mouse button went down (0 left, 1 right, 2 middle, 3 and 4 side buttons).</summary>
    public static CaptureDecision DecideMouse(int button)
    {
        if (button is 0 or 1)
        {
            return new CaptureDecision(CaptureOutcome.RejectLeftRight, TriggerBinding.None);
        }

        return TriggerBinding.IsValidMouseButton(button)
            ? new CaptureDecision(CaptureOutcome.Accept, TriggerBinding.Mouse(button))
            : new CaptureDecision(CaptureOutcome.Ignore, TriggerBinding.None);
    }
}

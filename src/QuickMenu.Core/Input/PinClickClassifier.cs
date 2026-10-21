namespace QuickMenu.Core.Input;

/// <summary>Allocation-free classification of a designator click (ADR 0005).</summary>
public static class PinClickClassifier
{
    public static PinClickAction Classify(in PinClickInput input, PinModifier modifier)
    {
        if (input.IsKeyEvent
            || input.Button != 0
            || input.RawType == RawEventType.KeyDown
            || input.RawType == RawEventType.KeyUp)
        {
            return PinClickAction.PassThrough;
        }

        bool click = input.LeftDownThisFrame
            || input.LeftUpThisFrame
            || input.RawType == RawEventType.MouseDown
            || input.RawType == RawEventType.MouseUp;
        if (!click)
        {
            return PinClickAction.PassThrough;
        }

        bool held = modifier switch
        {
            PinModifier.Ctrl => input.EventCtrl || input.RawCtrl,
            PinModifier.Alt => input.EventAlt || input.RawAlt,
            _ => input.EventShift || input.RawShift,
        };

        return held ? PinClickAction.Pin : PinClickAction.PassThrough;
    }
}

namespace QuickMenu.Core.Input;

/// <summary>Chooses the effective pin modifier (ADR 0005, FR-8).</summary>
public static class ModifierPolicy
{
    /// <summary>The user's choice if they set one, else Alt when Mint Menus is active, else Ctrl.</summary>
    public static PinModifier Effective(bool userSet, PinModifier userChoice, bool mintActive)
    {
        if (userSet)
        {
            return userChoice;
        }

        return mintActive ? PinModifier.Alt : PinModifier.Ctrl;
    }
}

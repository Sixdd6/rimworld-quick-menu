namespace QuickMenu.Core.Input;

/// <summary>What to tell the player at the first colony map of a session (design.md S8).</summary>
public enum NoticeKind
{
    None,

    /// <summary>The first-run window; it carries the Mint line when <see cref="NoticeDecision.IncludeMintLine"/>.</summary>
    FirstRun,

    /// <summary>The one-time <c>NeutralEvent</c> message that Mint is active.</summary>
    MintMessage,
}

public readonly record struct NoticeDecision(NoticeKind Kind, bool IncludeMintLine, bool SetFirstRunShown, bool SetMintShown);

public static class NoticePolicy
{
    /// <summary>
    /// First run shows the window once (with the Mint line, and the Mint flag set, if Mint is active). After the first run,
    /// Mint appearing shows its message once. Otherwise nothing. <paramref name="userSet"/> suppresses every Mint line.
    /// </summary>
    public static NoticeDecision Decide(bool firstRunShown, bool mintShown, bool mintActive, bool userSet)
    {
        // A player who already chose a modifier needs no Mint hint: mark it shown without showing it.
        bool mintLine = mintActive && !userSet;
        if (!firstRunShown)
        {
            return new NoticeDecision(NoticeKind.FirstRun, mintLine, true, mintActive || mintShown);
        }

        if (mintActive && !mintShown)
        {
            return userSet
                ? new NoticeDecision(NoticeKind.None, false, false, true)
                : new NoticeDecision(NoticeKind.MintMessage, false, false, true);
        }

        return new NoticeDecision(NoticeKind.None, false, false, false);
    }
}

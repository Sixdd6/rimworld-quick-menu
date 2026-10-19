using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class NoticePolicyTests
{
    [Fact]
    public void First_run_without_mint_shows_window_only()
    {
        NoticeDecision d = NoticePolicy.Decide(false, false, false, false);
        Assert.Equal(new NoticeDecision(NoticeKind.FirstRun, false, true, false), d);
    }

    [Fact]
    public void First_run_with_mint_includes_line_and_sets_both_flags()
    {
        NoticeDecision d = NoticePolicy.Decide(false, false, true, false);
        Assert.Equal(NoticeKind.FirstRun, d.Kind);
        Assert.True(d.IncludeMintLine);
        Assert.True(d.SetFirstRunShown);
        Assert.True(d.SetMintShown);
    }

    [Fact]
    public void Mint_after_first_run_shows_message_once()
    {
        NoticeDecision d = NoticePolicy.Decide(true, false, true, false);
        Assert.Equal(new NoticeDecision(NoticeKind.MintMessage, false, false, true), d);
        Assert.Equal(NoticeKind.None, NoticePolicy.Decide(true, true, true, false).Kind);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void Nothing_to_show(bool first, bool mintShown, bool mint)
    {
        Assert.Equal(NoticeKind.None, NoticePolicy.Decide(first, mintShown, mint, false).Kind);
    }

    [Fact]
    public void User_set_modifier_marks_mint_shown_without_showing_it()
    {
        NoticeDecision first = NoticePolicy.Decide(false, false, true, true);
        Assert.Equal(new NoticeDecision(NoticeKind.FirstRun, false, true, true), first);
        NoticeDecision later = NoticePolicy.Decide(true, false, true, true);
        Assert.Equal(new NoticeDecision(NoticeKind.None, false, false, true), later);
    }
}

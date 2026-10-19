using QuickMenu.Core.Input;
using Xunit;

namespace QuickMenu.Core.Tests.Input;

public class NoticePolicyTests
{
    [Fact]
    public void First_run_without_mint_shows_window_only()
    {
        NoticeDecision d = NoticePolicy.Decide(false, false, false);
        Assert.Equal(new NoticeDecision(NoticeKind.FirstRun, false, true, false), d);
    }

    [Fact]
    public void First_run_with_mint_includes_line_and_sets_both_flags()
    {
        NoticeDecision d = NoticePolicy.Decide(false, false, true);
        Assert.Equal(NoticeKind.FirstRun, d.Kind);
        Assert.True(d.IncludeMintLine);
        Assert.True(d.SetFirstRunShown);
        Assert.True(d.SetMintShown);
    }

    [Fact]
    public void Mint_after_first_run_shows_message_once()
    {
        NoticeDecision d = NoticePolicy.Decide(true, false, true);
        Assert.Equal(new NoticeDecision(NoticeKind.MintMessage, false, false, true), d);
        Assert.Equal(NoticeKind.None, NoticePolicy.Decide(true, true, true).Kind);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void Nothing_to_show(bool first, bool mintShown, bool mint)
    {
        Assert.Equal(NoticeKind.None, NoticePolicy.Decide(first, mintShown, mint).Kind);
    }
}

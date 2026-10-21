using QuickMenu.Core.Catalog;
using Xunit;

namespace QuickMenu.Core.Tests.Catalog;

public class IdeoFingerprintTests
{
    [Fact]
    public void No_development_data_does_not_throw_and_is_stable()
    {
        long a = IdeoFingerprint.Of(42, 5, null);
        Assert.Equal(a, IdeoFingerprint.Of(42, 5, null));
        Assert.Equal(a, IdeoFingerprint.Of(42, 5, 0));
    }

    [Fact]
    public void Reform_count_precepts_and_identity_each_change_the_print()
    {
        long baseline = IdeoFingerprint.Of(42, 5, 1);
        Assert.NotEqual(baseline, IdeoFingerprint.Of(42, 5, 2));
        Assert.NotEqual(baseline, IdeoFingerprint.Of(42, 6, 1));
        Assert.NotEqual(baseline, IdeoFingerprint.Of(43, 5, 1));
    }

    [Fact]
    public void Missing_everything_is_just_identity()
    {
        Assert.Equal(IdeoFingerprint.Of(7, 0, 0), IdeoFingerprint.Of(7, null, null));
    }

    [Fact]
    public void Signature_with_a_no_development_print_reports_change_once()
    {
        var s = new IdeoSignature();
        object game = new();
        object ideo = new();
        long print = IdeoFingerprint.Of(1, 3, null);
        Assert.True(s.Update(game, ideo, 3, print));
        Assert.False(s.Update(game, ideo, 3, print));
    }

    [Fact]
    public void Combine_is_order_sensitive()
    {
        Assert.NotEqual(IdeoFingerprint.Combine(IdeoFingerprint.Combine(0, 1), 2), IdeoFingerprint.Combine(IdeoFingerprint.Combine(0, 2), 1));
    }
}

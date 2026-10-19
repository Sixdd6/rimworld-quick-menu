using System;
using System.Runtime.CompilerServices;
using QuickMenu.Core.Catalog;
using Xunit;

namespace QuickMenu.Core.Tests.Catalog;

public class IdeoSignatureTests
{
    private readonly object _game = new();
    private readonly object _ideo = new();

    [Fact]
    public void Same_state_is_not_a_change()
    {
        var s = new IdeoSignature();
        Assert.True(s.Update(_game, _ideo, 5));
        Assert.False(s.Update(_game, _ideo, 5));
        Assert.Equal(1, s.ChangeCount);
    }

    [Fact]
    public void Precept_count_change_is_a_change()
    {
        var s = new IdeoSignature();
        s.Update(_game, _ideo, 5);
        Assert.True(s.Update(_game, _ideo, 6));
    }

    [Fact]
    public void Different_game_or_ideo_is_a_change()
    {
        var s = new IdeoSignature();
        s.Update(_game, _ideo, 5);
        Assert.True(s.Update(new object(), _ideo, 5));
        Assert.True(s.Update(_game, new object(), 5));
    }

    [Fact]
    public void No_game_and_no_ideo_matches_the_initial_state()
    {
        var s = new IdeoSignature();
        Assert.False(s.Update(null, null, 0));
        Assert.False(s.HasIdeo);
        Assert.Equal(0, s.ChangeCount);
    }

    [Fact]
    public void Leaving_the_game_is_a_change_and_clears_HasIdeo()
    {
        var s = new IdeoSignature();
        s.Update(_game, _ideo, 5);
        Assert.True(s.HasIdeo);
        Assert.True(s.Update(null, null, 0));
        Assert.False(s.HasIdeo);
    }

    [Fact]
    public void Precept_count_is_ignored_without_an_ideo()
    {
        var s = new IdeoSignature();
        s.Update(_game, null, 3);
        Assert.False(s.Update(_game, null, 9));
    }

    [Fact]
    public void Same_ideo_same_count_different_reform_generation_is_a_change()
    {
        var s = new IdeoSignature();
        s.Update(_game, _ideo, 5, fingerprint: 1);
        Assert.False(s.Update(_game, _ideo, 5, fingerprint: 1));
        Assert.True(s.Update(_game, _ideo, 5, fingerprint: 2));
    }

    [Fact]
    public void Fingerprint_is_ignored_without_an_ideo()
    {
        var s = new IdeoSignature();
        s.Update(_game, null, 0, fingerprint: 1);
        Assert.False(s.Update(_game, null, 0, fingerprint: 2));
    }

    [Fact]
    public void Does_not_keep_game_or_ideo_alive()
    {
        var s = new IdeoSignature();
        (WeakReference game, WeakReference ideo) = Record(s);
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(game.IsAlive);
        Assert.False(ideo.IsAlive);
        Assert.False(s.HasIdeo);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference, WeakReference) Record(IdeoSignature s)
    {
        var g = new object();
        var i = new object();
        s.Update(g, i, 2);
        return (new WeakReference(g), new WeakReference(i));
    }
}

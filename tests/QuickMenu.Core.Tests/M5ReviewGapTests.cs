using System.Collections.Generic;
using QuickMenu.Core.Input;
using QuickMenu.Core.Pins;
using QuickMenu.Core.Render;
using QuickMenu.Core.Settings;
using Xunit;

namespace QuickMenu.Core.Tests;

/// <summary>M5 milestone review: cheap bounded gaps in the availability, notice, mod-id and rebuild-gate rules.</summary>
public class M5ReviewGapTests
{
    // ---- NoticePolicy: exhaustive invariants ------------------------------------------------------

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, false, true)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, true, true)]
    public void Notice_invariants_hold_for_every_input(bool firstShown, bool mintShown, bool mint, bool userSet)
    {
        NoticeDecision d = NoticePolicy.Decide(firstShown, mintShown, mint, userSet);

        Assert.Equal(!firstShown, d.Kind == NoticeKind.FirstRun);          // the window shows exactly once
        Assert.Equal(!firstShown, d.SetFirstRunShown);
        if (userSet)
        {
            Assert.False(d.IncludeMintLine);                               // a chosen modifier needs no hint
            Assert.NotEqual(NoticeKind.MintMessage, d.Kind);
        }

        if (d.Kind == NoticeKind.MintMessage)
        {
            Assert.True(mint && firstShown && !mintShown && !userSet);
            Assert.True(d.SetMintShown);
        }

        if (d.IncludeMintLine)
        {
            Assert.True(mint && d.Kind == NoticeKind.FirstRun);
        }

        if (mintShown)
        {
            Assert.NotEqual(NoticeKind.MintMessage, d.Kind);               // never twice
        }

        if (!mint)
        {
            Assert.False(d.IncludeMintLine);
            Assert.NotEqual(NoticeKind.MintMessage, d.Kind);
        }
    }

    // ---- ModIds -----------------------------------------------------------------------------------

    [Theory]
    [InlineData("other.mod_steam", false)]            // right suffix, wrong id
    [InlineData("dubwise.dubsmintmenus_steam_steam", false)]
    [InlineData("dubwise.dubsmintmenus_steam_copy", false)]
    [InlineData("_steam", false)]
    [InlineData("DUBWISE.DUBSMINTMENUS_COPY", true)]
    [InlineData(" dubwise.dubsmintmenus", false)]     // no trimming
    [InlineData("dubwise.dubsmintmenus_STEAM", true)]
    public void Matches_mint_edge_variants(string actual, bool expected)
    {
        Assert.Equal(expected, ModIds.Matches(actual, ModIds.Mint));
    }

    [Fact]
    public void Matches_keeps_the_three_ids_apart()
    {
        Assert.False(ModIds.Matches(ModIds.BetterArchitect, ModIds.Mint));
        Assert.True(ModIds.Matches("RWMT.Multiplayer_steam", ModIds.Multiplayer));
        Assert.True(ModIds.Matches("FERNY.betterarchitect_copy", ModIds.BetterArchitect));
    }

    // ---- PinMissingRule ---------------------------------------------------------------------------

    [Fact]
    public void Build_pin_ignores_the_package_id_and_mod_state_entirely()
    {
        PinKey key = PinKey.Build(PinKeyKind.BuildThing, "Wall");
        bool modAsked = false;
        Assert.False(PinMissingRule.IsMissing(key, "gone.mod", _ => true, _ => { modAsked = true; return false; }));
        Assert.False(modAsked);
    }

    [Fact]
    public void Non_build_kinds_never_consult_the_def_lookup()
    {
        bool defAsked = false;
        Assert.True(PinMissingRule.IsMissing(PinKey.Order("A.B"), "gone", _ => { defAsked = true; return true; }, _ => false));
        Assert.False(defAsked);
    }

    [Fact]
    public void Whitespace_package_id_is_treated_as_unknown_not_missing()
    {
        Assert.Equal(string.Empty, PinMissingRule.NormalizePackageId("   "));
        Assert.False(PinMissingRule.IsMissing(PinKey.Dropdown("G"), "   ", _ => true, _ => false));
    }

    [Fact]
    public void Only_a_trailing_steam_suffix_is_dropped()
    {
        Assert.Equal("a_steam.b", PinMissingRule.NormalizePackageId("A_Steam.B"));
        Assert.Equal("a_copy", PinMissingRule.NormalizePackageId("A_copy"));
    }

    // ---- UnavailableReason ------------------------------------------------------------------------

    [Fact]
    public void Blank_parts_do_not_use_up_the_sentence_cap()
    {
        var parts = new List<ReasonPart>
        {
            new(ReasonCause.Research, " "), new(ReasonCause.BuildingPrerequisite, null),
            new(ReasonCause.TechLevel, "a"), new(ReasonCause.Monolith, "b"), new(ReasonCause.GravEngine, "c"),
            new(ReasonCause.Precept, "d"),
        };
        Assert.Equal(["a", "b", "c"], UnavailableReason.Order(parts));
    }

    [Fact]
    public void Duplicates_across_causes_collapse_before_the_cap_and_text_is_trimmed()
    {
        var parts = new List<ReasonPart>
        {
            new(ReasonCause.Research, " x "), new(ReasonCause.TechLevel, "x"), new(ReasonCause.Monolith, "y"),
            new(ReasonCause.Precept, "z"), new(ReasonCause.Discovery, "w"),
        };
        Assert.Equal(["x", "y", "z"], UnavailableReason.Order(parts));
    }

    [Fact]
    public void Order_of_nothing_and_SelectFrom_null_parts_use_the_fallback()
    {
        Assert.Empty(UnavailableReason.Order(new List<ReasonPart>()));
        Assert.Equal("fb", UnavailableReason.SelectFrom(null, null, "fb"));
        Assert.Equal("fb", UnavailableReason.SelectFrom(null, new List<ReasonPart> { new(ReasonCause.Research, "") }, "fb"));
        Assert.Equal("own", UnavailableReason.SelectFrom("own", new List<ReasonPart> { new(ReasonCause.Research, "r") }, "fb"));
    }

    [Fact]
    public void Cause_order_is_the_display_order()
    {
        var parts = new List<ReasonPart>
        {
            new(ReasonCause.Discovery, "5"), new(ReasonCause.Research, "1"), new(ReasonCause.TechLevel, "3"),
        };
        Assert.Equal("1 3 5", UnavailableReason.SelectFrom(null, parts, "fb").Replace("  ", " "));
    }

    // ---- RebuildGate ------------------------------------------------------------------------------

    [Fact]
    public void Release_with_no_net_change_rebuilds_nothing()
    {
        var gate = new RebuildGate();
        Assert.True(gate.Observe(1, false, 0));
        Assert.False(gate.Observe(1, true, 10));
        Assert.False(gate.Observe(1, false, 20));
        Assert.False(gate.Pending);
    }

    [Fact]
    public void A_drag_longer_than_the_debounce_still_waits_for_release()
    {
        var gate = new RebuildGate();
        gate.Observe(1, false, 0);
        for (long t = 10; t < 5000; t += 100)
        {
            Assert.False(gate.Observe(2 + t / 100, true, t));
        }

        Assert.True(gate.Pending);
        Assert.True(gate.Observe(99, false, 5000));
        Assert.False(gate.Observe(99, false, 5001));
    }

    [Fact]
    public void Reset_while_dragging_rebuilds_on_the_next_observe_and_then_settles()
    {
        var gate = new RebuildGate();
        gate.Observe(1, false, 0);
        gate.Reset();
        Assert.True(gate.Observe(7, true, 10));
        Assert.False(gate.Pending);
        Assert.False(gate.Observe(7, false, 20));
    }

    [Fact]
    public void Debounce_boundary_is_inclusive()
    {
        var gate = new RebuildGate();
        gate.Observe(1, false, 0);
        Assert.False(gate.Observe(2, false, 100));
        Assert.False(gate.Observe(2, false, 100 + RebuildGate.DebounceMs - 1));
        Assert.True(gate.Observe(2, false, 100 + RebuildGate.DebounceMs));
    }

    // ---- TextureSizing stretch --------------------------------------------------------------------

    [Fact]
    public void Stretch_is_finite_positive_and_reversible_across_extremes()
    {
        var small = TextureSizing.Create(36f, 1, 1f);
        var large = TextureSizing.Create(96f, 8, 1f);
        float up = TextureSizing.RimStretch(small, large);
        float down = TextureSizing.RimStretch(large, small);
        Assert.True(float.IsFinite(up) && up > 1f);
        Assert.True(float.IsFinite(down) && down > 0f && down < 1f);
        Assert.Equal(1f, up * down, 3);
        Assert.Equal(96f / 36f, TextureSizing.ButtonStretch(small, large), 3);
    }

    // ---- Missing-mod round trip -------------------------------------------------------------------

    private sealed class Resolver(params PinKey[] missing) : IPinResolver
    {
        private readonly HashSet<PinKey> _missing = new(missing);

        public bool IsResolved(PinKey key) => !_missing.Contains(key);
    }

    [Fact]
    public void Forgetting_every_missing_pin_then_reloading_leaves_only_the_resolved_ones()
    {
        var board = new PinBoard(4) { Mode = SizingMode.Fixed };
        PinKey a = PinKey.Build(PinKeyKind.BuildThing, "A");
        PinKey g1 = PinKey.Build(PinKeyKind.BuildThing, "G1");
        PinKey g2 = PinKey.Build(PinKeyKind.BuildTerrain, "G2");
        board.Toggle(g1, "G1", "mod.gone");
        board.Toggle(a, "A", "mod.a");
        board.Toggle(g2, "G2", "mod.gone");

        PinBoard loaded = PinSerializer.FromLines(PinSerializer.ToLines(board)).Board;
        loaded.Capacity = 4;
        loaded.Mode = SizingMode.Fixed;
        var gone = new Resolver(g1, g2);
        Assert.Equal(2, loaded.ForgetUnresolved(gone));

        PinBoard again = PinSerializer.FromLines(PinSerializer.ToLines(loaded)).Board;
        again.Capacity = 4;
        again.Mode = SizingMode.Fixed;
        var all = new Resolver();
        Assert.Equal(1, again.VisibleCount(all));
        Assert.Single(again.Entries, e => e != null && e.Key.Equals(a));
    }
}

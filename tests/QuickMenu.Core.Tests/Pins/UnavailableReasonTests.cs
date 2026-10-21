using System;
using System.Collections.Generic;
using QuickMenu.Core.Pins;
using Xunit;

namespace QuickMenu.Core.Tests.Pins;

public class UnavailableReasonTests
{
    [Fact]
    public void Designator_reason_wins_over_requirements()
    {
        Assert.Equal("No stuff.", UnavailableReason.Select(" No stuff. ", ["Requires research: X."], "fb"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_designator_reason_falls_through_to_requirements(string? own)
    {
        Assert.Equal("Requires research: X. Needs a Y.", UnavailableReason.Select(own, ["Requires research: X.", null, " ", "Needs a Y."], "fb"));
    }

    [Fact]
    public void Nothing_known_gives_the_fallback()
    {
        Assert.Equal("fb", UnavailableReason.Select(null, [], "fb"));
        Assert.Equal("fb", UnavailableReason.Select(null, null, "fb"));
        Assert.Equal("fb", UnavailableReason.Select("", ["", null], "fb"));
    }
}

public class UnavailableReasonComposeTests
{
    [Fact]
    public void Parts_are_ordered_by_cause_not_by_input_order()
    {
        var parts = new[]
        {
            new ReasonPart(ReasonCause.TechLevel, "Tech."),
            new ReasonPart(ReasonCause.Research, "Research."),
            new ReasonPart(ReasonCause.Mechanitor, "Mech."),
            new ReasonPart(ReasonCause.Precept, "Precept."),
        };
        Assert.Equal(["Research.", "Tech.", "Precept."], UnavailableReason.Order(parts));
    }

    [Fact]
    public void Same_cause_keeps_input_order_and_duplicates_collapse()
    {
        var parts = new[]
        {
            new ReasonPart(ReasonCause.Research, "B."),
            new ReasonPart(ReasonCause.Research, "A."),
            new ReasonPart(ReasonCause.Research, "B."),
            new ReasonPart(ReasonCause.PlaceWorker, " "),
        };
        Assert.Equal(["B.", "A."], UnavailableReason.Order(parts));
    }

    [Fact]
    public void Order_caps_the_sentence_count()
    {
        var parts = new List<ReasonPart>();
        foreach (ReasonCause cause in Enum.GetValues<ReasonCause>())
        {
            parts.Add(new ReasonPart(cause, cause.ToString() + "."));
        }

        Assert.Equal(UnavailableReason.MaxSentences, UnavailableReason.Order(parts).Count);
        Assert.Equal("Research.", UnavailableReason.Order(parts)[0]);
    }

    [Fact]
    public void Select_over_parts_prefers_the_games_reason_then_composes_then_falls_back()
    {
        var parts = new[] { new ReasonPart(ReasonCause.GravEngine, "Grav."), new ReasonPart(ReasonCause.Monolith, "Mono.") };
        Assert.Equal("Own.", UnavailableReason.SelectFrom("Own.", parts, "fb"));
        Assert.Equal("Mono. Grav.", UnavailableReason.SelectFrom(null, parts, "fb"));
        Assert.Equal("fb", UnavailableReason.SelectFrom(null, new List<ReasonPart>(), "fb"));
        Assert.Equal("fb", UnavailableReason.SelectFrom(null, null, "fb"));
    }
}

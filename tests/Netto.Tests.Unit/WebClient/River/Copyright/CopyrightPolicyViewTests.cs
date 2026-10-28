using Netto.Core.Copyright;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River.Copyright;

// How the royalty is decided, on the Copyright royalty card (operator, 2026-10-28, reversing the one-place
// decision of 2026-10-23): whether it follows the pay or is a fixed amount, that amount, kept while it
// follows, and why a royalty that follows the pay is none. The example holds 100,000 of profit before pay
// and 50,000 of pay.
public class CopyrightPolicyViewTests
{
    private static readonly RiverInputs Added = RiverInputs.Example.AddToPlan(OptionalModule.Copyright);

    // At 75% of the working time creating, a ruling accepts 11,538.46 beside the example's 50,000 of pay.
    private static readonly RiverInputs Following = Added.WithCreativeShare(0.75m);

    private static CopyrightPolicyView View(RiverInputs inputs) => CopyrightPolicyView.From(inputs, inputs.Run());

    // A royalty added follows the pay, as a default; the amount is the example's none.
    [Fact]
    public void From_royaltyAdded_followsThePayAsADefault()
    {
        var view = View(Added);

        (view.FollowsPay, view.Fixed).ShouldBe((new Input<bool>(true, InputOrigin.Default), new Input<decimal>(0m, InputOrigin.None)));
    }

    [Fact]
    public void From_royaltyTyped_isFixedAtTheAmountAsTheUsers()
    {
        var view = View(Added.WithRoyalty(10_000m));

        (view.FollowsPay, view.Fixed).ShouldBe((new Input<bool>(false, InputOrigin.Yours), new Input<decimal>(10_000m, InputOrigin.Yours)));
    }

    // The amount fixed before stays while the royalty follows the pay, for when it is fixed again.
    [Fact]
    public void From_royaltyFollowingAgain_keepsTheAmountFixedBefore()
    {
        var view = View(Added.WithRoyalty(10_000m).WithRoyaltyFollowingPay());

        (view.FollowsPay.Value, view.Fixed.Value).ShouldBe((true, 10_000m));
    }

    // Fixed from following, the royalty stays what it came to at the pay: the field opens on that amount.
    [Fact]
    public void From_followingRoyaltyFixed_opensOnWhatItCameTo()
    {
        var view = View(Following.WithRoyaltyFixed());

        (view.FollowsPay, view.Fixed, view.WhyNone).ShouldBe((new Input<bool>(false, InputOrigin.Yours), new Input<decimal>(11_538.46m, InputOrigin.Yours), null));
    }

    // While the royalty is refused it keeps its place, the amount typed in its field.
    [Fact]
    public void From_royaltyRefused_keepsTheAmountTyped()
    {
        View(Added.WithRoyalty(-1m)).Fixed.Value.ShouldBe(-1m);
    }

    // Following, the engine says how it set the royalty or why it is none, in the card's own words; set
    // at what a ruling accepts, its figure is among the card's lines and no sentence says it again.
    [Fact]
    public void WhyNone_followingAtWhatARulingAccepts_isNone()
    {
        View(Following).WhyNone.ShouldBeNull();
    }

    [Fact]
    public void WhyNone_followingWithoutAShare_saysTheShareIsNeeded()
    {
        View(Added).WhyNone.ShouldBe("Following_NoCreativeShare");
    }

    [Fact]
    public void WhyNone_followingWithTheAverageAboveTheCap_saysSo()
    {
        View(Following.WithAverageWithinCap(false)).WhyNone.ShouldBe("Following_AverageAboveCap");
    }

    // A fixed royalty is nothing to explain, and neither is a river the engine refuses: there is no year
    // to say why.
    [Fact]
    public void WhyNone_fixedRoyaltyOrRefusedRiver_isNone()
    {
        RiverInputs[] rivers = [Added.WithRoyalty(10_000m), Following.WithReserve(-1m)];

        rivers.Select(river => View(river).WhyNone).ShouldAllBe(why => why == null);
    }
}

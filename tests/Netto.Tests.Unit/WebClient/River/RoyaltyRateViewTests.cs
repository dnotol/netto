using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the royalty does to the smallest pay keeping the reduced corporate rate, as the split's card
// reads it from the engine's condition (YearConditions): the two pays, with the royalty and without it,
// and none where the plan has no such condition.
public class RoyaltyRateViewTests
{
    private static readonly RiverInputs Fixed = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(20_000m);

    [Fact]
    public void Of_aCondition_isItsTwoPays()
    {
        RoyaltyRateView.Of(new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false)).ShouldBe(new RoyaltyRateView(40_000m, 50_000m));
    }

    [Fact]
    public void Of_noPayKeepsTheRateWithoutIt_leavesItsPayNone()
    {
        RoyaltyRateView.Of(new RoyaltyLowersPayKeepingReducedRate(40_000m, null, true)).ShouldBe(new RoyaltyRateView(40_000m, null));
    }

    // The pays are the engine's, read off the year the card draws.
    [Fact]
    public void From_aRoyaltyInThePlan_isTheEnginesConditionOfThatYear()
    {
        var year = Fixed.Run().Year.ShouldNotBeNull();
        var condition = YearConditions.Of(year).OfType<RoyaltyLowersPayKeepingReducedRate>().Single();

        RoyaltyRateView.From(year).ShouldBe(new RoyaltyRateView(condition.With, condition.Without));
    }

    // The royalty lowers the pay that keeps the rate: the pay with it is below the pay without it.
    [Fact]
    public void From_aFixedRoyalty_lowersThePayKeepingTheRate()
    {
        var view = RoyaltyRateView.From(Fixed.Run().Year).ShouldNotBeNull();

        (view.With < view.Without).ShouldBeTrue();
    }

    [Fact]
    public void From_noRoyaltyInThePlan_isNone()
    {
        RoyaltyRateView.From(RiverInputs.Example.Run().Year).ShouldBeNull();
    }

    // A company in its first four tax periods keeps the rate whatever the pay: the royalty plays no part.
    [Fact]
    public void From_firstFourTaxPeriods_isNone()
    {
        RoyaltyRateView.From(Fixed.WithFinancialYear(FinancialYear.First).Run().Year).ShouldBeNull();
    }

    [Fact]
    public void From_noYear_isNone()
    {
        RoyaltyRateView.From(null).ShouldBeNull();
    }
}

using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;
using static Netto.Tests.WebClient.River.PlanFixtures;

namespace Netto.Tests.WebClient.Tabs;

// The panel drawing both pockets on one scale from nothing: the example leaves 61,975.74 in the pocket
// and its best pay 64,526.87, 2,551.13 more (BaselineGainTests).
public class PocketPanelTests
{
    private static readonly RiverInputs BestPay = RiverInputs.Example.WithDirectorPay(20_799m);

    [Fact]
    public void Pocket_twoPlans_drawsEachPocketAsAFractionOfTheLarger()
    {
        var panel = Compare(RiverInputs.Example, BestPay).Pocket.ShouldNotBeNull();

        (panel.Plan.Pocket, panel.Plan.Width, panel.Against.Pocket, panel.Against.Width).ShouldBe((61_975.74m, 61_975.74m / 64_526.87m, 64_526.87m, 1m));
    }

    [Fact]
    public void Pocket_largerPocket_isTheBetterOne()
    {
        var panel = Compare(RiverInputs.Example, BestPay).Pocket.ShouldNotBeNull();

        (panel.Plan.IsBetter, panel.Against.IsBetter).ShouldBe((false, true));
    }

    [Fact]
    public void Pocket_firstPlanLeavesMore_isTheBetterOne()
    {
        var panel = Compare(BestPay, RiverInputs.Example).Pocket.ShouldNotBeNull();

        (panel.Plan.IsBetter, panel.Against.IsBetter, panel.Plan.Width).ShouldBe((true, false, 1m));
    }

    // Two pockets of the same cents: neither is better.
    [Fact]
    public void Pocket_samePockets_areNeitherBetter()
    {
        var panel = Compare(RiverInputs.Example, RiverInputs.Example.WithReserve(0m)).Pocket.ShouldNotBeNull();

        (panel.Plan.IsBetter, panel.Against.IsBetter, panel.Plan.Width, panel.Against.Width).ShouldBe((false, false, 1m, 1m));
    }

    // Nothing in either pocket: no scale to draw them on.
    [Fact]
    public void Pocket_nothingInEitherPocket_hasNoWidth()
    {
        var nothing = RiverInputs.Example.SkipCompany().WithDirectorPay(0m);

        var panel = Compare(nothing, nothing.WithTaxWithheld(0m)).Pocket.ShouldNotBeNull();

        (panel.Plan.Width, panel.Against.Width, panel.Plan.IsBetter, panel.Against.IsBetter).ShouldBe((null, null, false, false));
    }

    // The sentence under the panel is the gain the bar and the Plans panel say.
    [Fact]
    public void Pocket_gain_isTheOneBaselineGainSays()
    {
        Compare(RiverInputs.Example, BestPay).Pocket.ShouldNotBeNull().Gain.ShouldBe(new BaselineGain(GainKind.More, 2_551.13m));
    }

    [Fact]
    public void Pocket_names_areThoseOfThePlansAndSayWhichIsToday()
    {
        var panel = Compare(RiverInputs.Example, BestPay).Pocket.ShouldNotBeNull();

        ((panel.Plan.Name, panel.Plan.IsToday), (panel.Against.Name, panel.Against.IsToday)).ShouldBe((("Plan 1", true), ("Plan 2", false)));
    }

    [Fact]
    public void Pocket_planTheEngineRefuses_isNone()
    {
        CompareView.Of(Saved(RiverInputs.Example, RiverInputs.Example.WithReserve(-1m))).Pocket.ShouldBeNull();
    }
}

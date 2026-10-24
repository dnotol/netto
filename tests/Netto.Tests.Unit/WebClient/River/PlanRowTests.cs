using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The example leaves 61,975.74 in the pocket, 62% of the 100,000; the best pay 64,526.87, 65%,
// 2,551.13 more (BucketsViewTests, BaselineGainTests).
public class PlanRowTests
{
    private static readonly RiverInputs BestPay = RiverInputs.Example.WithDirectorPay(20_799m);

    [Fact]
    public void From_noPlan_isNoRow()
    {
        PlanRow.From(new SavedPlans(new BrowserStorage())).ShouldBeEmpty();
    }

    [Fact]
    public void From_plans_givesEachItsPocketAndShare()
    {
        var plans = Saved(RiverInputs.Example, BestPay);

        PlanRow.From(plans).Select(row => (row.Pocket, row.Reach)).ShouldBe(
            [((decimal?)61_975.74m, (decimal?)0.6197574m), (64_526.87m, 0.6452687m)]);
    }

    [Fact]
    public void From_plans_comparesEachWithTodayButTodayItself()
    {
        var plans = Saved(RiverInputs.Example, BestPay);

        PlanRow.From(plans).Select(row => (row.IsToday, row.Gain)).ShouldBe(
            [(true, (BaselineGain?)null), (false, new BaselineGain(GainKind.More, 2_551.13m))]);
    }

    // A plan with a car is compared with Today's in cash alone, the difference of the two pockets.
    [Fact]
    public void From_planWithACar_comparesItsPocketAloneWithTodays()
    {
        var car = RiverInputs.Example.AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);
        var plans = Saved(RiverInputs.Example, car);

        PlanRow.From(plans)[1].Gain.ShouldBe(new BaselineGain(GainKind.Less, RiverInputs.Example.Run().Year!.NetInPocket - car.Run().Year!.NetInPocket));
    }

    [Fact]
    public void From_noToday_comparesNothing()
    {
        var plans = Saved(RiverInputs.Example, BestPay);
        plans.Delete(plans.Plans[0].Id);

        PlanRow.From(plans).Single().Gain.ShouldBeNull();
    }

    [Fact]
    public void From_plans_marksTheOneTheRiverWasOpenedFrom()
    {
        var plans = Saved(RiverInputs.Example, BestPay);

        PlanRow.From(plans).Select(row => row.IsOpened).ShouldBe([false, true]);
    }

    // A refused plan has no figure, like the bar.
    [Fact]
    public void From_refusedPlan_hasNoFigure()
    {
        var plans = Saved(RiverInputs.Example, RiverInputs.Example.WithReserve(-1m));

        PlanRow.From(plans)[1].ShouldSatisfyAllConditions(
            row => (row.Pocket, row.Reach, row.Gain).ShouldBe((null, null, null)));
    }

    private static SavedPlans Saved(params RiverInputs[] inputs)
    {
        var plans = new SavedPlans(new BrowserStorage());
        foreach (var plan in inputs)
            plans.Save(plan, number => $"Plan {number}");
        return plans;
    }
}

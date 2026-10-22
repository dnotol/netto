using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;

namespace Netto.Tests.Simulation;

// MoveSearchTests' example year, 100,000 of profit before the director and 50,000 of pay, whose director
// spends 75% of their working time creating protected works: the royalty move proposes what ruling
// 2026.0484 accepts, 11,538.46 (CopyrightCalculatorTests).
public class MoveSearchCopyrightTests
{
    private static readonly Scenario Today = new(
        2026, 120_000m, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false), 50_000m, MunicipalRates.Entered(0.07m), 0m,
        WithholdingCategory.VvprbisThirdYearOnwards)
    {
        TaxPrepayments = TaxPrepayments.AsWithheld,
        Copyright = new CopyrightRoyalty(0m) { CreativeShare = 0.75m },
    };

    private static readonly IReadOnlyList<IMoveFinder> RoyaltyOnly = [new CopyrightRoyaltyFinder()];

    [Fact]
    public void Run_creativeShareGiven_proposesTheRoyaltyAtItsCeiling()
    {
        var plan = MoveSearch.Run(Today, RoyaltyOnly).Value.ShouldNotBeNull();

        plan.Moves.ShouldBe([new PlannedMove(new CopyrightRoyaltyMove(11_538.46m), Pocket(WithRoyalty(Today, 11_538.46m)) - Pocket(Today))]);
    }

    // A royalty within its limits beats leaving the money in the company (CopyrightViewTests).
    [Fact]
    public void Run_creativeShareGiven_gainsFromTheRoyalty()
    {
        MoveSearch.Run(Today, RoyaltyOnly).Value.ShouldNotBeNull().Gain.ShouldBeGreaterThan(0m);
    }

    // No share given, no ceiling: the move never appears.
    [Fact]
    public void Run_noCreativeShare_proposesNoRoyalty()
    {
        var noShare = Today with { Copyright = new CopyrightRoyalty(0m) };

        MoveSearch.Run(noShare, RoyaltyOnly).Value.ShouldNotBeNull().Moves.ShouldBeEmpty();
    }

    [Fact]
    public void Run_royaltyAtItsCeiling_proposesNone()
    {
        MoveSearch.Run(WithRoyalty(Today, 11_538.46m), RoyaltyOnly).Value.ShouldNotBeNull().Moves.ShouldBeEmpty();
    }

    // Above what a ruling accepts, the engine still taxes the royalty by the law; the move never lowers
    // it, not even one passing the limits, whose excess lowering would spare.
    [Theory]
    [InlineData(15_000)]
    [InlineData(40_000)]
    public void Find_royaltyAboveItsCeiling_proposesNoLowerOne(int royalty)
    {
        new CopyrightRoyaltyFinder().Find(WithRoyalty(Today, royalty)).ShouldBeNull();
    }

    // Every royalty is taxed as pay when the four previous years averaged above the cap: no move.
    [Fact]
    public void Run_averageAboveTheCap_proposesNoRoyalty()
    {
        var averageAbove = Today with { Copyright = Today.Copyright with { AverageWithinCap = false } };

        MoveSearch.Run(averageAbove, RoyaltyOnly).Value.ShouldNotBeNull().Moves.ShouldBeEmpty();
    }

    // The move sets the royalty alone: the share and the statuses stay the scenario's.
    [Fact]
    public void Apply_royaltyMove_setsTheRoyaltyAndKeepsTheRest()
    {
        var holder = Today with { Copyright = Today.Copyright with { HoldsAttestation = true } };

        new CopyrightRoyaltyMove(11_538.46m).Apply(holder).Copyright.ShouldBe(holder.Copyright with { Royalty = 11_538.46m });
    }

    // The default finders include the royalty: on 22 October, among the pay and the payments.
    [Fact]
    public void Run_everyKind_includesTheRoyalty()
    {
        var plan = MoveSearch.Run(Today, new DateOnly(2026, 10, 22)).Value.ShouldNotBeNull();

        plan.Moves.ShouldContain(planned => planned.Move is CopyrightRoyaltyMove);
    }

    [Fact]
    public void Run_everyKind_sumsItsGainsToThePlanLessToday()
    {
        var plan = MoveSearch.Run(Today, new DateOnly(2026, 10, 22)).Value.ShouldNotBeNull();

        plan.Moves.Sum(move => move.Gain).ShouldBe(plan.PlanInPocket - plan.TodayInPocket);
    }

    // A move after the royalty that lowers the pay would leave the royalty above what a ruling accepts
    // at the new pay: the search does not take it. Here a pay of 30,000 with 20,000 more revenue, which
    // the search takes when no royalty was raised.
    [Fact]
    public void Run_laterMoveLeavingARaisedRoyaltyAboveItsCeiling_isNotTaken()
    {
        var lowerPay = new LowerPayMoreRevenue(30_000m, 140_000m);

        var plan = MoveSearch.Run(Today, [new CopyrightRoyaltyFinder(), new OnceARoyaltyIsIn(lowerPay)]).Value.ShouldNotBeNull();

        plan.Moves.Select(planned => planned.Move).ShouldBe([new CopyrightRoyaltyMove(11_538.46m)]);
    }

    // The same move taken before the royalty is: the royalty then follows the new pay, 75% of 30,000.
    [Fact]
    public void Run_lowerPayBeforeTheRoyalty_isTakenAndTheRoyaltyFollowsIt()
    {
        var lowerPay = new LowerPayMoreRevenue(30_000m, 140_000m);

        var plan = MoveSearch.Run(Today, [new CopyrightRoyaltyFinder(), new Always(lowerPay)]).Value.ShouldNotBeNull();

        plan.Moves.Select(planned => planned.Move).ShouldBe([lowerPay, new CopyrightRoyaltyMove(6_923.07m)]);
    }

    // A royalty Today already holds is the user's: a move never stops on it, whatever its ceiling.
    [Fact]
    public void Run_laterMoveLeavingTodaysRoyaltyAboveItsCeiling_isTaken()
    {
        var lowerPay = new LowerPayMoreRevenue(30_000m, 140_000m);

        var plan = MoveSearch.Run(WithRoyalty(Today, 11_538.46m), [new CopyrightRoyaltyFinder(), new Always(lowerPay)]).Value.ShouldNotBeNull();

        plan.Moves.Select(planned => planned.Move).ShouldBe([lowerPay]);
    }

    // A real case: 80,000 of profit and 24,000 of pay, 75% of the time creating. The royalty, 5,538.46,
    // adds more than the split's best pay, 20,799, so a search that takes it first stops there: with it,
    // the split's best pay is still below 24,000, where the ruling accepts less. The plan never ends
    // above what the ruling accepts at its own pay.
    [Fact]
    public void Run_bestPayBelowTheRoyaltysOnceItIsIn_neverEndsAboveTheCeiling()
    {
        var today = Today with { Revenue = 100_000m, DirectorPay = 24_000m };

        var plan = MoveSearch.Run(today, [new SplitMoveFinder(), new CopyrightRoyaltyFinder()]).Value.ShouldNotBeNull();
        var final = Final(today, plan);

        plan.ShouldSatisfyAllConditions(
            () => SplitSearch.Run(WithRoyalty(today, 5_538.46m)).Value.ShouldNotBeNull().Best.ShouldNotBeNull().Pay.ShouldBeLessThan(24_000m),
            () => final.Copyright.Royalty.ShouldBeLessThanOrEqualTo(CopyrightCalculator.Ceiling(CopyrightRules.IncomeYear2026, final.Copyright, final.DirectorPay)!.Value),
            () => plan.Gain.ShouldBe(Pocket(final) - Pocket(today)));
    }

    // The royalty taken first stops the pay where it is, 50,000, with 16,666.66 for all of the time
    // creating; taken last, it follows the split's best pay, 20,799, with 6,933: that plan leaves more,
    // and is the one kept.
    [Fact]
    public void Run_royaltyLastLeavingMore_keepsThatPlan()
    {
        var today = Today with { Copyright = new CopyrightRoyalty(0m) { CreativeShare = 1m } };

        var plan = MoveSearch.Run(today, [new SplitMoveFinder(), new CopyrightRoyaltyFinder()]).Value.ShouldNotBeNull();

        plan.ShouldSatisfyAllConditions(
            () => plan.Moves.Select(planned => planned.Move).ShouldBe([new PayMove(20_799m), new CopyrightRoyaltyMove(6_933m)]),
            () => plan.Gain.ShouldBeGreaterThan(Pocket(WithRoyalty(today, 16_666.66m)) - Pocket(today)));
    }

    // 200,000 of profit and 40,000 of pay: the royalty first, 13,333.33, then the pay at 51,000, the
    // minimum that keeps the reduced corporate rate, with 17,000, leaves more than the royalty last.
    [Fact]
    public void Run_royaltyFirstLeavingMore_keepsThatPlan()
    {
        var today = Today with { Revenue = 220_000m, DirectorPay = 40_000m, Copyright = new CopyrightRoyalty(0m) { CreativeShare = 1m } };

        var plan = MoveSearch.Run(today, [new SplitMoveFinder(), new CopyrightRoyaltyFinder()]).Value.ShouldNotBeNull();
        var final = Final(today, plan);

        (final.DirectorPay, final.Copyright.Royalty).ShouldBe((51_000m, 17_000m));
    }

    private static Scenario WithRoyalty(Scenario scenario, decimal royalty) => scenario with { Copyright = scenario.Copyright with { Royalty = royalty } };

    private static decimal Pocket(Scenario scenario) => YearSimulation.Run(scenario).Value!.NetInPocket;

    private static Scenario Final(Scenario today, MovePlan plan) => plan.Moves.Aggregate(today, (scenario, planned) => planned.Move.Apply(scenario));

    // A lower pay with more revenue: a move that always adds, and lowers what the ruling accepts.
    private sealed record LowerPayMoreRevenue(decimal Pay, decimal Revenue) : Move
    {
        public override Scenario Apply(Scenario scenario) => scenario with { DirectorPay = Pay, Revenue = Revenue };
    }

    private sealed class Always(Move move) : IMoveFinder
    {
        public Move? Find(Scenario scenario) => move;
    }

    // Proposes its move only once the plan pays a royalty, so it always comes after the royalty's.
    private sealed class OnceARoyaltyIsIn(Move move) : IMoveFinder
    {
        public Move? Find(Scenario scenario) => scenario.Copyright.Any ? move : null;
    }
}

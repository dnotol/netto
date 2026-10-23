using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;

namespace Netto.Tests.Simulation;

// MoveSearchTests' example year, 100,000 of profit before the director and 50,000 of pay, whose director
// spends 75% of their working time creating protected works: letting the royalty follow the pay sets it,
// at that pay, at what ruling 2026.0484 accepts, 11,538.46 (CopyrightCalculatorTests).
public class MoveSearchCopyrightTests
{
    private static readonly Scenario Today = new(
        2026, 120_000m, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false), 50_000m, MunicipalRates.Entered(0.07m), 0m,
        WithholdingCategory.VvprbisThirdYearOnwards)
    {
        TaxPrepayments = TaxPrepayments.AsWithheld,
        Copyright = new CopyrightRoyalty(0m) { CreativeShare = 0.75m },
    };

    private static readonly IReadOnlyList<IMoveFinder> FollowOnly = [new CopyrightFollowsPayFinder()];

    // A royalty within its limits beats leaving the money in the company (CopyrightViewTests).
    [Fact]
    public void Run_creativeShareGiven_gainsFromTheRoyalty()
    {
        MoveSearch.Run(Today, FollowOnly).Value.ShouldNotBeNull().Gain.ShouldBeGreaterThan(0m);
    }

    // No share given, nothing a ruling accepts: the move never appears.
    [Fact]
    public void Run_noCreativeShare_proposesNoRoyalty()
    {
        var noShare = Today with { Copyright = new CopyrightRoyalty(0m) };

        MoveSearch.Run(noShare, FollowOnly).Value.ShouldNotBeNull().Moves.ShouldBeEmpty();
    }

    // The default finders let the royalty follow the pay: on 22 October, among the pay and the payments.
    [Fact]
    public void Run_everyKind_includesLettingTheRoyaltyFollowThePay()
    {
        var plan = MoveSearch.Run(Today, new DateOnly(2026, 10, 22)).Value.ShouldNotBeNull();

        plan.Moves.ShouldContain(planned => planned.Move is CopyrightFollowsPayMove);
    }

    [Fact]
    public void Run_everyKind_sumsItsGainsToThePlanLessToday()
    {
        var plan = MoveSearch.Run(Today, new DateOnly(2026, 10, 22)).Value.ShouldNotBeNull();

        plan.Moves.Sum(move => move.Gain).ShouldBe(plan.PlanInPocket - plan.TodayInPocket);
    }

    // The plan the default finders leave ends with the royalty following the pay, at what a ruling accepts
    // at the plan's own pay: a later move of the pay carries it along.
    [Fact]
    public void Run_everyKind_endsWithTheRoyaltyAtWhatARulingAcceptsAtThePlansPay()
    {
        var plan = MoveSearch.Run(Today, new DateOnly(2026, 10, 22)).Value.ShouldNotBeNull();
        var final = Final(Today, plan);

        YearSimulation.Run(final).Value.ShouldNotBeNull().Copyright.Royalty
            .ShouldBe(CopyrightCalculator.Ceiling(CopyrightRules.IncomeYear2026, final.Copyright, final.DirectorPay).ShouldNotBeNull());
    }

    // Letting the royalty follow the pay sets it, at Today's 50,000, at what a ruling accepts there: the
    // move adds the difference of the two years.
    [Fact]
    public void Run_followFinderCreativeShareGiven_proposesLettingTheRoyaltyFollowThePay()
    {
        var plan = MoveSearch.Run(Today, FollowOnly).Value.ShouldNotBeNull();

        plan.Moves.Select(move => (move.Move, move.Gain)).ShouldBe([(new CopyrightFollowsPayMove(), Pocket(Following(Today)) - Pocket(Today))]);
    }

    // The move lets the royalty follow the pay alone: the pay, the share, the statuses and the amount
    // given stay the scenario's.
    [Fact]
    public void Apply_followMove_letsTheRoyaltyFollowThePayAndKeepsTheRest()
    {
        var holder = Today with { Copyright = Today.Copyright with { Royalty = 5_000m, HoldsAttestation = true } };

        new CopyrightFollowsPayMove().Apply(holder).ShouldBe(holder with { Copyright = holder.Copyright with { FollowsPay = true } });
    }

    // Above the largest pay the company pays together with what a ruling accepts beside it, 81,250 beside
    // 18,750 of the 100,000, the move holds the pay there, as the river holds it.
    [Fact]
    public void Apply_followMoveAtAPayAboveThePayCeilingWithItsRoyalty_holdsThePayThere()
    {
        new CopyrightFollowsPayMove().Apply(Today with { DirectorPay = 90_000m }).DirectorPay.ShouldBe(81_250m);
    }

    // From 90,000 of pay, where what a ruling accepts would pay 110,769.23 out of 100,000, the plan's year
    // is one the company pays, and the move's gain the difference of two such years.
    [Fact]
    public void Run_followFinderPayAboveThePayCeilingWithItsRoyalty_plansAYearTheCompanyPays()
    {
        var today = Today with { DirectorPay = 90_000m };

        var plan = MoveSearch.Run(today, FollowOnly).Value.ShouldNotBeNull();
        var final = Final(today, plan);

        (final.DirectorPay, YearSimulation.Run(final).Value.ShouldNotBeNull().KeptInCompany >= 0m, plan.Moves.Sum(move => move.Gain), plan.Gain)
            .ShouldBe((81_250m, true, plan.Gain, Pocket(final) - Pocket(today)));
    }

    [Fact]
    public void Find_followFinderRoyaltyFixedBelowWhatARulingAccepts_proposesIt()
    {
        new CopyrightFollowsPayFinder().Find(WithRoyalty(Today, 5_000m)).ShouldBe(new CopyrightFollowsPayMove());
    }

    // A royalty at or above what a ruling accepts at the pay is the user's: the move never lowers it.
    [Theory]
    [InlineData(11_538.46)]
    [InlineData(15_000)]
    public void Find_followFinderRoyaltyAtOrAboveWhatARulingAccepts_proposesNone(decimal royalty)
    {
        new CopyrightFollowsPayFinder().Find(WithRoyalty(Today, royalty)).ShouldBeNull();
    }

    [Fact]
    public void Find_followFinderNoCreativeShare_proposesNone()
    {
        new CopyrightFollowsPayFinder().Find(Today with { Copyright = new CopyrightRoyalty(0m) }).ShouldBeNull();
    }

    // Every royalty is taxed as pay when the four previous years averaged above the cap: no move.
    [Fact]
    public void Find_followFinderAverageAboveTheCap_proposesNone()
    {
        new CopyrightFollowsPayFinder().Find(Today with { Copyright = Today.Copyright with { AverageWithinCap = false } }).ShouldBeNull();
    }

    [Fact]
    public void Find_followFinderRoyaltyAlreadyFollowing_proposesNone()
    {
        new CopyrightFollowsPayFinder().Find(Following(Today)).ShouldBeNull();
    }

    public static TheoryData<decimal, decimal, decimal> Setups => new()
    {
        { 120_000m, 50_000m, 0.75m },
        { 120_000m, 50_000m, 1m },
        { 100_000m, 24_000m, 0.75m },
        { 220_000m, 40_000m, 1m },
    };

    // With the split, the plan ends with the royalty following the pay, at what a ruling accepts at the
    // plan's own pay; every gain is the difference of two years, and they sum to the plan less Today.
    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_followAndSplit_endsWithTheRoyaltyAtWhatARulingAcceptsAtThePlansPay(decimal revenue, decimal pay, decimal share)
    {
        var today = Today with { Revenue = revenue, DirectorPay = pay, Copyright = new CopyrightRoyalty(0m) { CreativeShare = share } };

        var plan = MoveSearch.Run(today, [new SplitMoveFinder(), new CopyrightFollowsPayFinder()]).Value.ShouldNotBeNull();
        var final = Final(today, plan);

        (final.Copyright.FollowsPay, YearSimulation.Run(final).Value.ShouldNotBeNull().Copyright.Royalty, plan.Moves.Sum(move => move.Gain), plan.Gain)
            .ShouldBe((true, CopyrightCalculator.Ceiling(CopyrightRules.IncomeYear2026, final.Copyright, final.DirectorPay).ShouldNotBeNull(),
                plan.PlanInPocket - plan.TodayInPocket, Pocket(final) - Pocket(today)));
    }

    // The two plans the raise to the ceiling left before a royalty could follow the pay: taken last, the
    // pay at 20,799 with 6,933; taken first, the pay at 51,000 with 17,000. Following the pay leaves at
    // least as much as each.
    [Theory]
    [InlineData(120_000, 50_000, 20_799, 6_933)]
    [InlineData(220_000, 40_000, 51_000, 17_000)]
    public void Run_followAndSplit_leavesAtLeastWhatTheRaiseInEitherOrderLeft(int revenue, int pay, int raisedPay, int raisedRoyalty)
    {
        var today = Today with { Revenue = revenue, DirectorPay = pay, Copyright = new CopyrightRoyalty(0m) { CreativeShare = 1m } };

        var plan = MoveSearch.Run(today, [new SplitMoveFinder(), new CopyrightFollowsPayFinder()]).Value.ShouldNotBeNull();

        plan.PlanInPocket.ShouldBeGreaterThanOrEqualTo(Pocket(WithRoyalty(today with { DirectorPay = raisedPay }, raisedRoyalty)));
    }

    private static Scenario Following(Scenario scenario) => scenario with { Copyright = scenario.Copyright with { FollowsPay = true } };

    private static Scenario WithRoyalty(Scenario scenario, decimal royalty) => scenario with { Copyright = scenario.Copyright with { Royalty = royalty } };

    private static decimal Pocket(Scenario scenario) => YearSimulation.Run(scenario).Value!.NetInPocket;

    private static Scenario Final(Scenario today, MovePlan plan) => plan.Moves.Aggregate(today, (scenario, planned) => planned.Move.Apply(scenario));
}

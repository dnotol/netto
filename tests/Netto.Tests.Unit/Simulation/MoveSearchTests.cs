using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;

namespace Netto.Tests.Simulation;

// The river's example year as its scenario (RiverInputsTests): 120,000 of revenue and 20,000 of
// running costs, a small company past its first four tax periods, 50,000 of pay withheld on as the key
// formula says, a 7% municipal rate, VVPRbis from the third distribution, nothing prepaid by the
// company and nothing kept. It leaves 61,975.74 in the pocket; the split's best pay, 20,799, leaves
// 64,526.87 (SplitSearchViewTests, BucketsViewTests), 2,551.13 more.
public class MoveSearchTests
{
    private static readonly Scenario Today = new(
        2026, 120_000m, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false), 50_000m, MunicipalRates.Entered(0.07m), 0m,
        WithholdingCategory.VvprbisThirdYearOnwards)
    {
        TaxPrepayments = TaxPrepayments.AsWithheld,
    };

    // Every deadline of 2026 is past: only the split moves.
    private static readonly DateOnly AfterTheYear = new(2027, 1, 1);

    // Only 21 December is still open.
    private static readonly DateOnly On22October = new(2026, 10, 22);

    [Fact]
    public void Run_example_movesThePayToTheBest()
    {
        var plan = MoveSearch.Run(Today, AfterTheYear).Value.ShouldNotBeNull();

        plan.Moves.ShouldBe([new PlannedMove(new PayMove(20_799m), 2_551.13m)]);
    }

    [Fact]
    public void Run_example_sumsItsGainsToThePlanLessToday()
    {
        var plan = MoveSearch.Run(Today, AfterTheYear).Value.ShouldNotBeNull();

        (plan.TodayInPocket, plan.PlanInPocket, plan.Gain, plan.Moves.Sum(move => move.Gain)).ShouldBe((61_975.74m, 64_526.87m, 2_551.13m, 2_551.13m));
    }

    [Fact]
    public void Run_todayAtTheBestPay_hasNoMove()
    {
        var plan = MoveSearch.Run(Today with { DirectorPay = 20_799m }, AfterTheYear).Value.ShouldNotBeNull();

        (plan.Moves.Count, plan.Gain).ShouldBe((0, 0m));
    }

    // One euro of pay from the best is worth less than a euro: not a move.
    [Fact]
    public void Run_gainUnderAEuro_isNoMove()
    {
        var plan = MoveSearch.Run(Today with { DirectorPay = 20_798m }, AfterTheYear).Value.ShouldNotBeNull();

        plan.Moves.ShouldBeEmpty();
    }

    [Fact]
    public void Run_refusedScenario_givesTheEnginesError()
    {
        var (plan, error) = MoveSearch.Run(Today with { Reserve = -1m }, AfterTheYear);

        (plan, error).ShouldBe((null, YearSimulation.Run(Today with { Reserve = -1m }).Error));
    }

    // Two kinds of move: the one that adds is taken, and the other, measured again on top of it, still
    // takes from the pocket, so it is not.
    [Fact]
    public void Run_twoFinders_takesTheBiggerFirstAndMeasuresTheOtherOnTopOfIt()
    {
        var plan = MoveSearch.Run(Today, [new Fixed(new ReserveMove(5_000m)), new Fixed(new PayMove(20_799m))]).Value.ShouldNotBeNull();

        var afterPay = Pocket(Today with { DirectorPay = 20_799m });
        var afterBoth = Pocket(Today with { DirectorPay = 20_799m, Reserve = 5_000m });
        plan.ShouldSatisfyAllConditions(
            () => plan.Moves.Select(move => move.Move).ShouldBe([new PayMove(20_799m)]),
            () => plan.Moves[0].Gain.ShouldBe(afterPay - Pocket(Today)),
            () => afterBoth.ShouldBeLessThan(afterPay));
    }

    // Each finder is asked again on top of the moves taken, so its second proposal is measured there:
    // from no pay, the pocket climbs to the best pay (SplitSearchTests).
    [Fact]
    public void Run_finderProposingAgain_isMeasuredOnTopOfTheMovesTaken()
    {
        var noPay = Today with { DirectorPay = 0m };
        var finder = new Steps(new PayMove(10_000m), new PayMove(20_799m));

        var plan = MoveSearch.Run(noPay, [finder]).Value.ShouldNotBeNull();

        plan.Moves.ShouldBe([
            new PlannedMove(new PayMove(10_000m), Pocket(noPay with { DirectorPay = 10_000m }) - Pocket(noPay)),
            new PlannedMove(new PayMove(20_799m), Pocket(noPay with { DirectorPay = 20_799m }) - Pocket(noPay with { DirectorPay = 10_000m })),
        ]);
    }

    // A finder that always proposes a move that adds is stopped after ten rounds.
    [Fact]
    public void Run_movesThatNeverStop_stopAfterTenRounds()
    {
        var plan = MoveSearch.Run(Today, [new MoreRevenue()]).Value.ShouldNotBeNull();

        plan.Moves.Count.ShouldBe(10);
    }

    // A move that leaves less than the reserve in the company is never taken.
    [Fact]
    public void Run_moveBreakingTheReserve_isNotTaken()
    {
        var plan = MoveSearch.Run(Today with { Reserve = 30_000m, DirectorPay = 20_000m }, [new Fixed(new PayMove(80_000m))]).Value.ShouldNotBeNull();

        plan.Moves.ShouldBeEmpty();
    }

    // On 22 October the split moves first, then the company pays by 21 December what cancels the
    // surcharge on the corporate tax at the new pay, 19,800.25 (BucketsViewTests): 1,336.52 at 6.75%,
    // covered by 4.5% of 29,700.45, rounded up to the cent.
    [Fact]
    public void Run_exampleOn22October_movesThePayThenPaysTheCompanysSurchargeBy21December()
    {
        var plan = MoveSearch.Run(Today, On22October).Value.ShouldNotBeNull();

        var atTheBestPay = Today with { DirectorPay = 20_799m };
        plan.Moves.ShouldBe([
            new PlannedMove(new PayMove(20_799m), 2_551.13m),
            new PlannedMove(new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 29_700.45m),
                Pocket(atTheBestPay with { AdvancePayments = new AdvancePayments(0m, 0m, 0m, 29_700.45m) }) - Pocket(atTheBestPay)),
        ]);
    }

    [Fact]
    public void Run_exampleOn22October_endsOnThePlansOwnPocket()
    {
        var plan = MoveSearch.Run(Today, On22October).Value.ShouldNotBeNull();

        (plan.Moves.Sum(move => move.Gain), plan.PlanInPocket).ShouldBe(
            (plan.Gain, Pocket(Today with { DirectorPay = 20_799m, AdvancePayments = new AdvancePayments(0m, 0m, 0m, 29_700.45m) })));
    }

    // Before 10 April the first deadline is open, where 9% covers the 1,336.52: 14,850.23.
    [Fact]
    public void Run_exampleOnNewYearsDay_paysBy10April()
    {
        var plan = MoveSearch.Run(Today, new DateOnly(2026, 1, 1)).Value.ShouldNotBeNull();

        plan.Moves[1].Move.ShouldBe(new CompanyAdvancePaymentMove(0, new DateOnly(2026, 4, 10), 14_850.23m));
    }

    // A deadline is open until the end of its day.
    [Fact]
    public void Run_exampleOn21December_paysThatDay()
    {
        var plan = MoveSearch.Run(Today, new DateOnly(2026, 12, 21)).Value.ShouldNotBeNull();

        plan.Moves.Select(move => move.Move).OfType<CompanyAdvancePaymentMove>().Single().Date.ShouldBe(new DateOnly(2026, 12, 21));
    }

    [Fact]
    public void Run_exampleAfterTheYear_hasNoPaymentMove()
    {
        var plan = MoveSearch.Run(Today, AfterTheYear).Value.ShouldNotBeNull();

        plan.Moves.Select(move => move.Move).OfType<CompanyAdvancePaymentMove>().ShouldBeEmpty();
    }

    // A small company in its first three financial years owes no surcharge to cancel.
    [Fact]
    public void Run_exemptCompany_hasNoPaymentMove()
    {
        var plan = MoveSearch.Run(Today with { Company = new CompanyStatus(true, true, true) }, On22October).Value.ShouldNotBeNull();

        plan.Moves.Select(move => move.Move).OfType<CompanyAdvancePaymentMove>().ShouldBeEmpty();
    }

    // Nothing withheld on 50,000 of pay: the 486.57 surcharge on its tax (RiverInputsTests) is what
    // 14,317.50 by 21 December avoids (IncomeTaxAdvancePaymentCalculatorTests), all of it reaching the
    // pocket.
    [Fact]
    public void Run_nothingWithheld_paysTheDirectorsSurchargeBy21December()
    {
        var nothingWithheld = Today with { TaxPrepayments = TaxPrepayments.None };

        var plan = MoveSearch.Run(nothingWithheld, [new DirectorAdvancePaymentFinder(On22October)]).Value.ShouldNotBeNull();

        plan.Moves.ShouldBe([new PlannedMove(new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 14_317.50m), 486.57m)]);
    }

    private static decimal Pocket(Scenario scenario) => YearSimulation.Run(scenario).Value!.NetInPocket;

    private sealed record ReserveMove(decimal Reserve) : Move
    {
        public override Scenario Apply(Scenario scenario) => scenario with { Reserve = Reserve };
    }

    // Proposes the same move whatever the scenario.
    private sealed class Fixed(Move move) : IMoveFinder
    {
        public Move? Find(Scenario scenario) => move;
    }

    // Proposes each move in turn, then nothing.
    private sealed class Steps(params Move[] moves) : IMoveFinder
    {
        private int _next;

        public Move? Find(Scenario scenario) => _next < moves.Length ? moves[_next++] : null;
    }

    // Ten thousand more revenue, every time: a move that always adds.
    private sealed class MoreRevenue : IMoveFinder
    {
        public Move? Find(Scenario scenario) => new RevenueMove(scenario.Revenue + 10_000m);
    }

    private sealed record RevenueMove(decimal Revenue) : Move
    {
        public override Scenario Apply(Scenario scenario) => scenario with { Revenue = Revenue };
    }
}

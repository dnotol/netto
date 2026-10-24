using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.Rent;
using Netto.Core.Vouchers;
using Netto.Core.CorporateTax;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// The river's example year as its scenario (RiverInputsTests): 120,000 of revenue, 19,590.70 of
// running costs and 409.30 of INASTI's company contribution, a small company past its first four tax
// periods, 50,000 of pay withheld on as the key formula says, a 7% municipal rate, VVPRbis from the
// third distribution, nothing prepaid by the company and nothing kept. It leaves 61,975.74 in the pocket; the split's best pay, 20,799, leaves
// 64,526.87 (SplitSearchViewTests, BucketsViewTests), 2,551.13 more.
public class MoveSearchTests
{
    private static readonly Scenario Today = RiverExample;

    // The split alone, or with the advance payments on that date: the levers' moves have tests of their own.
    private static readonly IReadOnlyList<IMoveFinder> SplitOnly = [new SplitMoveFinder()];

    private static IReadOnlyList<IMoveFinder> PayAndPayments(DateOnly date) =>
        [new SplitMoveFinder(), new CompanyAdvancePaymentFinder(date), new DirectorAdvancePaymentFinder(date)];

    // Every deadline of 2026 is past: no payment is proposed.
    private static readonly DateOnly AfterTheYear = new(2027, 1, 1);

    // Only 21 December is still open.
    private static readonly DateOnly On22October = new(2026, 10, 22);

    [Fact]
    public void Run_example_movesThePayToTheBest()
    {
        var plan = MoveSearch.Run(Today, SplitOnly).Value.ShouldNotBeNull();

        Lines(plan).ShouldBe([(new PayMove(20_799m), 2_551.13m)]);
    }

    [Fact]
    public void Run_example_sumsItsGainsToThePlanLessToday()
    {
        var plan = MoveSearch.Run(Today, SplitOnly).Value.ShouldNotBeNull();

        (plan.TodayInPocket, plan.PlanInPocket, plan.Gain, plan.Moves.Sum(move => move.Gain)).ShouldBe((61_975.74m, 64_526.87m, 2_551.13m, 2_551.13m));
    }

    [Fact]
    public void Run_todayAtTheBestPay_hasNoMove()
    {
        var plan = MoveSearch.Run(Today with { DirectorPay = 20_799m }, SplitOnly).Value.ShouldNotBeNull();

        (plan.Moves.Count, plan.Gain).ShouldBe((0, 0m));
    }

    // In the upper band the company owes INASTI 613.08 more before the split: the moves are measured from
    // that year, each line between two runs of it, so they still sum to the plan's pocket less Today's.
    [Fact]
    public void Run_upperBand_measuresTheMovesFromThatBandsYear()
    {
        var today = Today with { CompanyContributionBand = CompanyContributionBand.Upper };

        var plan = MoveSearch.Run(today, PayAndPayments(On22October)).Value.ShouldNotBeNull();

        plan.ShouldSatisfyAllConditions(
            () => plan.TodayInPocket.ShouldBe(Run(today).NetInPocket),
            () => plan.Moves.ShouldNotBeEmpty(),
            () => plan.Moves.ShouldAllBe(move => move.Before.CompanyContribution == 1_022.38m && move.After.CompanyContribution == 1_022.38m),
            () => plan.Moves.Sum(move => move.Gain).ShouldBe(plan.Gain));
    }

    // One euro of pay from the best is worth less than a euro: not a move.
    [Fact]
    public void Run_gainUnderAEuro_isNoMove()
    {
        var plan = MoveSearch.Run(Today with { DirectorPay = 20_798m }, SplitOnly).Value.ShouldNotBeNull();

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

    // Each finder is asked again on top of the moves taken. A second move of the same kind replaces
    // the first in its line, which adds both gains: from no pay, the pocket climbs to the best pay
    // (SplitSearchTests).
    [Fact]
    public void Run_finderProposingAgain_replacesItsMoveAndAddsTheGains()
    {
        var noPay = Today with { DirectorPay = 0m };
        var finder = new Steps(new PayMove(10_000m), new PayMove(20_799m));

        var plan = MoveSearch.Run(noPay, [finder]).Value.ShouldNotBeNull();

        Lines(plan).ShouldBe([(new PayMove(20_799m), Pocket(noPay with { DirectorPay = 20_799m }) - Pocket(noPay))]);
    }

    // A payment adds to what is paid by its deadline: a second one by the same deadline joins the
    // first in its line, the two amounts together.
    [Fact]
    public void Run_paymentsByOneDeadline_areOneLineOfBothAmounts()
    {
        var finder = new Steps(new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 10_000m), new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 5_000m));

        var plan = MoveSearch.Run(Today, [finder]).Value.ShouldNotBeNull();

        Lines(plan).ShouldBe([(new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 15_000m),
            Pocket(Today with { AdvancePayments = new AdvancePayments(0m, 0m, 0m, 15_000m) }) - Pocket(Today))]);
    }

    [Fact]
    public void Run_directorPaymentsByOneDeadline_areOneLineOfBothAmounts()
    {
        var nothingWithheld = Today with { TaxPrepayments = TaxPrepayments.None };
        var finder = new Steps(new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 7_000m), new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 7_317.50m));

        var plan = MoveSearch.Run(nothingWithheld, [finder]).Value.ShouldNotBeNull();

        plan.Moves.Single().Move.ShouldBe(new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 14_317.50m));
    }

    // Each line stands between two years of the plan, the year before it was taken and the year after:
    // the first starts on Today's year, the next starts where it ends, and the last ends on the plan's.
    [Fact]
    public void Run_exampleOn22October_eachLineStandsBetweenTheYearsBeforeAndAfterIt()
    {
        var plan = MoveSearch.Run(Today, PayAndPayments(On22October)).Value.ShouldNotBeNull();

        var atTheBestPay = Pocket(Today with { DirectorPay = 20_799m });
        plan.Moves.Select(move => (move.Before.NetInPocket, move.After.NetInPocket))
            .ShouldBe([(plan.TodayInPocket, atTheBestPay), (atTheBestPay, plan.PlanInPocket)]);
    }

    // The years are the ones the line's move changes: the pay, then the surcharge the payment cancels.
    [Fact]
    public void Run_exampleOn22October_eachLinesYearsHoldWhatItsMoveChanges()
    {
        var plan = MoveSearch.Run(Today, PayAndPayments(On22October)).Value.ShouldNotBeNull();

        (plan.Moves[0].Before.Scenario.DirectorPay, plan.Moves[0].After.Scenario.DirectorPay,
                plan.Moves[1].Before.AdvancePayments.Surcharge > 0m, plan.Moves[1].After.AdvancePayments.Surcharge)
            .ShouldBe((50_000m, 20_799m, true, 0m));
    }

    // A line taken in several rounds stands between the year before its first round and the year after
    // its last.
    [Fact]
    public void Run_finderProposingAgain_spansItsLineFromBeforeItsFirstRoundToAfterItsLast()
    {
        var noPay = Today with { DirectorPay = 0m };

        var line = MoveSearch.Run(noPay, [new Steps(new PayMove(10_000m), new PayMove(20_799m))]).Value.ShouldNotBeNull().Moves.Single();

        (line.Before.Scenario.DirectorPay, line.After.Scenario.DirectorPay, line.Before.NetInPocket, line.After.NetInPocket)
            .ShouldBe((0m, 20_799m, Pocket(noPay), Pocket(noPay with { DirectorPay = 20_799m })));
    }

    // A payment by another deadline is another line.
    [Fact]
    public void Run_paymentsByTwoDeadlines_areTwoLines()
    {
        var finder = new Steps(new CompanyAdvancePaymentMove(0, new DateOnly(2026, 4, 10), 3_000m), new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 3_000m));

        var plan = MoveSearch.Run(Today, [finder]).Value.ShouldNotBeNull();

        plan.Moves.Select(move => move.Move).ShouldBe([finder.Moves[0], finder.Moves[1]]);
    }

    // A finder that always proposes a move that adds is stopped after ten rounds: its one line holds
    // the tenth.
    [Fact]
    public void Run_movesThatNeverStop_stopAfterTenRounds()
    {
        var plan = MoveSearch.Run(Today, [new MoreRevenue()]).Value.ShouldNotBeNull();

        plan.Moves.Single().Move.ShouldBe(new RevenueMove(Today.Revenue + 100_000m));
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
    // covered by 4.5% of 29,700.34, 1,336.5153, which rounds to it.
    [Fact]
    public void Run_exampleOn22October_movesThePayThenPaysTheCompanysSurchargeBy21December()
    {
        var plan = MoveSearch.Run(Today, PayAndPayments(On22October)).Value.ShouldNotBeNull();

        var atTheBestPay = Today with { DirectorPay = 20_799m };
        Lines(plan).ShouldBe([
            (new PayMove(20_799m), 2_551.13m),
            (new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 29_700.34m),
                Pocket(atTheBestPay with { AdvancePayments = new AdvancePayments(0m, 0m, 0m, 29_700.34m) }) - Pocket(atTheBestPay)),
        ]);
    }

    [Fact]
    public void Run_exampleOn22October_endsOnThePlansOwnPocket()
    {
        var plan = MoveSearch.Run(Today, PayAndPayments(On22October)).Value.ShouldNotBeNull();

        (plan.Moves.Sum(move => move.Gain), plan.PlanInPocket).ShouldBe(
            (plan.Gain, Pocket(Today with { DirectorPay = 20_799m, AdvancePayments = new AdvancePayments(0m, 0m, 0m, 29_700.34m) })));
    }

    // Before 10 April the first deadline is open, where 9% covers the 1,336.52: 14,850.17.
    [Fact]
    public void Run_exampleOnNewYearsDay_paysBy10April()
    {
        var plan = MoveSearch.Run(Today, PayAndPayments(new DateOnly(2026, 1, 1))).Value.ShouldNotBeNull();

        plan.Moves[1].Move.ShouldBe(new CompanyAdvancePaymentMove(0, new DateOnly(2026, 4, 10), 14_850.17m));
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

        Lines(plan).ShouldBe([(new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 14_317.50m), 486.57m)]);
    }

    // With a company car, its benefit is taxed with the pay: the payment proposed leaves no surcharge
    // on that tax either, and the car itself is never a move.
    [Fact]
    public void Run_nothingWithheldWithACar_paysWhatLeavesNoSurchargeOnTheTaxWithItsBenefit()
    {
        var withCar = Today with { TaxPrepayments = TaxPrepayments.None, Car = new CarFacts(50_000m, new DateOnly(2026, 1, 10), 8_000m, false, 0m) };

        var plan = MoveSearch.Run(withCar, [new DirectorAdvancePaymentFinder(On22October)]).Value.ShouldNotBeNull();
        var move = plan.Moves.Single().Move.ShouldBeOfType<DirectorAdvancePaymentMove>();

        YearSimulation.Run(move.Apply(withCar)).Value.ShouldNotBeNull().DirectorPay.AdvancePayments.Surcharge.ShouldBe(0m);
    }

    // A royalty beyond its limits is taxed as pay that nothing withholds on: the payment the finder
    // proposes is on the tax with that part, and leaves no surcharge.
    [Fact]
    public void Run_nothingWithheldWithARoyaltyBeyondItsLimits_paysWhatLeavesNoSurchargeOnTheTaxWithIt()
    {
        var withRoyalty = Today with { TaxPrepayments = TaxPrepayments.None, Copyright = new Core.Copyright.CopyrightRoyalty(30_000m) };

        var plan = MoveSearch.Run(withRoyalty, [new DirectorAdvancePaymentFinder(On22October)]).Value.ShouldNotBeNull();
        var move = plan.Moves.Single().Move.ShouldBeOfType<DirectorAdvancePaymentMove>();

        YearSimulation.Run(move.Apply(withRoyalty)).Value.ShouldNotBeNull().DirectorPay.AdvancePayments.Surcharge.ShouldBe(0m);
    }

    [Fact]
    public void Run_everyKindWithACar_neverMovesTheCar()
    {
        var withCar = Today with { Car = new CarFacts(50_000m, new DateOnly(2026, 1, 10), 8_000m, false, 0m) };

        var plan = MoveSearch.Run(withCar, On22October).Value.ShouldNotBeNull();

        plan.Moves.Select(move => move.Move.Apply(withCar).Car).ShouldAllBe(car => car == withCar.Car);
    }

    // Without vouchers, each kind is proposed at its yearly maximum, 250 and 100 for 2026.
    [Fact]
    public void Run_noVouchers_grantsEachKindAtItsMaximum()
    {
        var plan = MoveSearch.Run(Today, [new EcoVouchersFinder(), new SportCultureVouchersFinder()]).Value.ShouldNotBeNull();

        plan.Moves.Select(move => move.Move).ShouldBe([new EcoVouchersMove(250m), new SportCultureVouchersMove(100m)], ignoreOrder: true);
    }

    [Fact]
    public void Run_noVouchers_measuresEachOnTheYearItIsAddedTo()
    {
        var plan = MoveSearch.Run(Today, [new EcoVouchersFinder()]).Value.ShouldNotBeNull();

        plan.Moves.Single().Gain.ShouldBe(Pocket(Today with { Vouchers = new VoucherGrants(0, 250m, 0m) }) - Pocket(Today));
    }

    [Fact]
    public void Run_vouchersAtTheirMaximum_hasNoVoucherMove()
    {
        var atMaximum = Today with { Vouchers = new VoucherGrants(0, 250m, 100m) };

        MoveSearch.Run(atMaximum, [new EcoVouchersFinder(), new SportCultureVouchersFinder()]).Value.ShouldNotBeNull().Moves.ShouldBeEmpty();
    }

    // 12% of a cadastral income of 1,200: the 2026 cap is 1,380.00 (RentCalculatorTests).
    [Fact]
    public void Run_rentFactsGiven_rentsAtTheCap()
    {
        var plan = MoveSearch.Run(Today with { Rent = new HomeOfficeRent(1_200m, 0.12m, 0m) }, [new RentFinder()]).Value.ShouldNotBeNull();

        Lines(plan).ShouldBe([(new RentMove(1_380.00m),
            Pocket(Today with { Rent = new HomeOfficeRent(1_200m, 0.12m, 1_380.00m) }) - Pocket(Today))]);
    }

    [Fact]
    public void Run_rentAtTheCap_hasNoRentMove()
    {
        var atTheCap = Today with { Rent = new HomeOfficeRent(1_200m, 0.12m, 1_380.00m) };

        MoveSearch.Run(atTheCap, [new RentFinder()]).Value.ShouldNotBeNull().Moves.ShouldBeEmpty();
    }

    [Fact]
    public void Run_noRentFacts_hasNoRentMove()
    {
        MoveSearch.Run(Today, [new RentFinder()]).Value.ShouldNotBeNull().Moves.ShouldBeEmpty();
    }

    // The engine refuses a rent beside an office allowance: never a move.
    [Fact]
    public void Run_rentBesideAnOfficeAllowance_isNotTaken()
    {
        var withOffice = Today with { Rent = new HomeOfficeRent(1_200m, 0.12m, 0m), Allowances = new ExpenseAllowances(1_000m, 0m, 0m, 0m) };

        MoveSearch.Run(withOffice, [new RentFinder()]).Value.ShouldNotBeNull().Moves.ShouldBeEmpty();
    }

    // Every kind together, on 22 October: the pay, the company's payment, both vouchers and the rent,
    // one line each, the pay's best moving once the rent cuts the credit, the gains still summing to the
    // plan's pocket less Today's.
    [Fact]
    public void Run_everyKindOn22October_sumsItsGainsToThePlanLessToday()
    {
        var plan = MoveSearch.Run(Today with { Rent = new HomeOfficeRent(1_200m, 0.12m, 0m) }, On22October).Value.ShouldNotBeNull();

        (plan.Moves.Count, plan.Moves.Sum(move => move.Gain)).ShouldBe((5, plan.PlanInPocket - plan.TodayInPocket));
    }

    private static decimal Pocket(Scenario scenario) => YearSimulation.Run(scenario).Value!.NetInPocket;

    // Each line's move and what it adds, without the years it stands between.
    private static IReadOnlyList<(Move Move, decimal Gain)> Lines(MovePlan plan) => [.. plan.Moves.Select(move => (move.Move, move.Gain))];

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

        public Move[] Moves => moves;

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

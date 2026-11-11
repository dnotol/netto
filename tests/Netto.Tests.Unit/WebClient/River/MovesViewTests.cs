using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The example leaves 61,975.74 in the pocket; its best pay, 20,799, 2,551.13 more, and on 22 October
// 2026 the company then pays 29,700.34 by 21 December to cancel the surcharge on its corporate tax
// (MoveSearchTests). After the year only the split moves, the example holding no lever.
public class MovesViewTests
{
    private static readonly DateOnly AfterTheYear = new(2027, 1, 1);

    private static readonly DateOnly On22October = new(2026, 10, 22);

    // The example's year, which a move made here stands between: what Apply reads is the move.
    private static readonly YearResult ExampleYear = RiverInputs.Example.Run().Year.ShouldNotBeNull();

    private static readonly PlannedMove BestPay = Planned(new PayMove(20_799m), 2_551.13m);

    private static readonly PlannedMove CompanyPays = Planned(new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 29_700.34m));

    private static readonly PlannedMove DirectorPays = Planned(new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 14_317.50m));

    // The page searches once per plan and date, and the tab draws the view of what was found, searching nothing.
    [Fact]
    public void Of_whatTheSearchFound_isTheViewOfSearchingAgain()
    {
        var found = RiverInputs.Example.FindMoves(On22October);

        var view = MovesView.Of(RiverInputs.Example, found);

        var searched = MovesView.Of(RiverInputs.Example, On22October);
        (view.State, view.FromPay, view.Total, Lines(view).SequenceEqual(Lines(searched))).ShouldBe((searched.State, searched.FromPay, searched.Total, true));
    }

    // Nothing found is a plan that starts at the pay.
    [Fact]
    public void Of_nothingFound_startsAtThePay()
    {
        MovesView.Of(RiverInputs.Example.SkipCompany(), found: null).State.ShouldBe(MovesState.StartsAtPay);
    }

    // Until the page has searched, once painted (MovesSource), the moves are being looked for: no figure
    // and no move.
    [Fact]
    public void Searching_aPlan_drawsNoFigureAndNoMove()
    {
        var view = MovesView.Searching(RiverInputs.Example);

        (view.State, view.Moves.Count, view.Total, view.FromInPocket, view.From).ShouldBe((MovesState.Searching, 0, 0m, 0m, RiverInputs.Example));
    }

    // After the year the pay alone moves: the example holds no voucher, and none is added.
    [Fact]
    public void Of_exampleAfterTheYear_movesThePayAlone()
    {
        var view = MovesView.Of(RiverInputs.Example, AfterTheYear);

        (view.State, view.FromPay).ShouldBe((MovesState.Moves, 50_000m));
        Lines(view).ShouldBe([(BestPay.Move, BestPay.Gain)]);
    }

    [Fact]
    public void Of_exampleOn22October_paysTheCompanysSurchargeAfterThePay()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        view.Moves.Select(move => move.Move).ShouldBe([BestPay.Move, CompanyPays.Move]);
    }

    [Fact]
    public void Of_exampleOn22October_totalsItsMoves()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        view.Total.ShouldBe(view.Moves.Sum(move => move.Gain));
    }

    [Fact]
    public void Of_planSkipsTheCompany_saysTheMovesStartFromIt()
    {
        MovesView.Of(RiverInputs.Example.SkipCompany(), On22October).State.ShouldBe(MovesState.StartsAtPay);
    }

    // A lever out of the plan shows no card, so facts of it the engine refuses refuse nothing: the plan is
    // not refused, and starts from the pocket the river shows.
    public static TheoryData<string, RiverInputs> UnreadableFactsOfLeversOutOfThePlan => new()
    {
        { "a share let beyond the dwelling, Rent out of the plan", RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(1.5m) },
        { "a negative cadastral income, Rent out of the plan", RiverInputs.Example.WithCadastralIncome(-1m).WithShareLet(0.12m) },
        { "a creative share beyond all the time, Copyright royalty out of the plan", RiverInputs.Example.WithCreativeShare(1.5m) },
    };

    [Theory]
    [MemberData(nameof(UnreadableFactsOfLeversOutOfThePlan))]
    public void Of_unreadableFactsOfALeverOutOfThePlan_isNotRefused(string _, RiverInputs plan)
    {
        var view = MovesView.Of(plan, On22October);

        (view.State, view.Error, view.FromInPocket).ShouldBe((MovesState.Moves, null, plan.Run().Year.ShouldNotBeNull().NetInPocket));
    }

    [Fact]
    public void Of_planRefused_givesTheEnginesError()
    {
        var view = MovesView.Of(RiverInputs.Example.WithReserve(-1m), On22October);

        (view.State, view.Error).ShouldBe((MovesState.Refused, RiverInputs.Example.WithReserve(-1m).Run().Error));
    }

    // Plans whose moves read each kind of value the moves set: the example on 22 October, the example with a
    // royalty following the pay at a quarter of the time on 11 November, nothing withheld on the pay, and
    // every lever with a move in the plan, the royalty fixed below what a ruling accepts.
    public static TheoryData<string, RiverInputs, DateOnly> Plans => new()
    {
        { "the example", RiverInputs.Example, On22October },
        { "a royalty following the pay", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.25m).WithRoyaltyFollowingPay(), new DateOnly(2026, 11, 11) },
        { "nothing withheld on the pay", RiverInputs.Example.WithTaxWithheld(0m), On22October },
        { "every lever with a move in the plan", Furnished, On22October },
    };

    // The moves start from the pocket the river shows, the band's.
    [Theory]
    [MemberData(nameof(Plans))]
    public void Of_plan_startsFromTheRiversPocket(string _, RiverInputs plan, DateOnly date)
    {
        MovesView.Of(plan, date).FromInPocket.ShouldBe(plan.Run().Year.ShouldNotBeNull().NetInPocket);
    }

    // Every move applied lands on the plan's optimised pocket, where nothing is left to move.
    [Theory]
    [MemberData(nameof(Plans))]
    public void ApplyAll_plan_landsOnThePlansPocketWithNothingLeftToMove(string _, RiverInputs plan, DateOnly date)
    {
        var view = MovesView.Of(plan, date);

        var optimised = view.ApplyAll(plan);

        (view.State, optimised.Run().Year.ShouldNotBeNull().NetInPocket, MovesView.Of(optimised, date).State)
            .ShouldBe((MovesState.Moves, view.PlanInPocket, MovesState.AlreadyBest));
    }

    // Every kind of move the furnished plan holds, six at least with the royalty following the pay, so Apply
    // all is held across them.
    [Fact]
    public void Of_furnishedPlan_holdsSixKindsOfMoveAtLeast()
    {
        Kinds(MovesView.Of(Furnished, On22October)).ShouldBeGreaterThanOrEqualTo(6);
    }

    // The example with a royalty following the pay, unsaved: the band's 62,624.32, the pay and the company's
    // payment, 3,309.67 more, 65,933.99 once applied.
    // These are the engine's own figures for the example plan, a regression pin matching the band on
    // 2026-11-11, not a Tax-Calc value.
    [Fact]
    public void ApplyAll_royaltyFollowingThePay_landsOn65933_99()
    {
        var plan = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.25m).WithRoyaltyFollowingPay();
        var view = MovesView.Of(plan, new DateOnly(2026, 11, 11));

        (view.FromInPocket, view.Total, view.ApplyAll(plan).Run().Year?.NetInPocket).ShouldBe((62_624.32m, 3_309.67m, 65_933.99m));
    }

    // What the pocket holds after each move is the plan's with the gains up to it: the last is the optimised plan's.
    [Fact]
    public void InPocketAfter_eachMove_addsTheGainsUpToItToThePlansPocket()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        view.Moves.Select((_, index) => view.InPocketAfter(index))
            .ShouldBe(view.Moves.Select((_, index) => view.FromInPocket + view.Moves.Take(index + 1).Sum(move => move.Gain)));
    }

    [Fact]
    public void InPocketAfter_lastMove_isThePlansPocket()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        (view.Moves.Count > 1, view.InPocketAfter(view.Moves.Count - 1)).ShouldBe((true, view.PlanInPocket));
    }

    // Nothing to move: the plan's pocket is the optimised one.
    [Fact]
    public void Of_planAlreadyBest_holdsItsPocketAlone()
    {
        var optimised = MovesView.Of(RiverInputs.Example, On22October).ApplyAll(RiverInputs.Example);

        var best = MovesView.Of(optimised, On22October);

        (best.State, best.FromInPocket, best.PlanInPocket).ShouldBe((MovesState.AlreadyBest, optimised.Run().Year.ShouldNotBeNull().NetInPocket, best.FromInPocket));
    }

    // The plan's pay is the one the river runs: a pay above the profit before pay is that profit.
    [Fact]
    public void Of_payAboveTheProfit_startsFromTheProfit()
    {
        MovesView.Of(RiverInputs.Example.WithDirectorPay(150_000m), AfterTheYear).FromPay.ShouldBe(100_000m);
    }

    [Fact]
    public void Apply_payMove_setsTheRiversPayAsYours()
    {
        View().Apply(BestPay, RiverInputs.Example).ShouldBe(RiverInputs.Example.WithDirectorPay(20_799m));
    }

    // The plan's payment by the deadline, with the move's amount on top.
    [Fact]
    public void Apply_companyPayment_setsTheRiversPaymentToThePlansAndTheMove()
    {
        var plan = RiverInputs.Example.WithAdvancePayment(3, 1_000m);

        var river = View(plan).Apply(CompanyPays, plan.WithDirectorPay(20_799m));

        river.AdvancePayments.Fourth.Value.ShouldBe(30_700.34m);
    }

    [Fact]
    public void Apply_directorPayment_setsTheRiversPaymentOnTheIncomeTax()
    {
        View().Apply(DirectorPays, RiverInputs.Example).TaxPrepayments.Paid.Fourth.Value.ShouldBe(14_317.50m);
    }

    // A voucher move retunes vouchers already in the plan: it sets the amount, and the plan holds what it held.
    [Fact]
    public void Apply_ecoVouchers_setsTheAmountAndAddsNoModule()
    {
        var plan = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);

        var river = View(plan).Apply(Planned(new EcoVouchersMove(250m)), plan);

        (river.Composition, river.Vouchers.EcoVouchers.Value).ShouldBe((plan.Composition, 250m));
    }

    [Fact]
    public void Apply_sportCultureVouchers_setsTheAmount()
    {
        View().Apply(Planned(new SportCultureVouchersMove(100m)), RiverInputs.Example).Vouchers.SportCultureVouchers.Value.ShouldBe(100m);
    }

    // Rent in the plan with its home's facts: the move is the cap, and applying it sets the rent alone.
    [Fact]
    public void Of_rentFactsInThePlan_rentsAtTheCap()
    {
        var plan = RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m).AddToPlan(OptionalModule.Rent);

        MovesView.Of(plan, AfterTheYear).Moves.Select(move => move.Move).ShouldContain(new RentMove(1_380.00m));
    }

    // Rent out of the plan, its home's facts given: no move adds it.
    [Fact]
    public void Of_rentFactsOutOfThePlan_proposesNoRent()
    {
        var plan = RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m);

        MovesView.Of(plan, AfterTheYear).Moves.Select(move => move.Move).OfType<RentMove>().ShouldBeEmpty();
    }

    [Fact]
    public void Apply_rent_setsTheRentAlone()
    {
        var plan = RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m).AddToPlan(OptionalModule.Rent);

        View(plan).Apply(Planned(new RentMove(1_380.00m)), plan).ShouldBe(plan.WithRent(1_380.00m));
    }

    // The royalty in the plan at 75% of the time creating, fixed at nothing: the move lets it follow the pay.
    [Fact]
    public void Of_creativeShareGivenRoyaltyInThePlan_proposesLettingTheRoyaltyFollowThePay()
    {
        var plan = RiverInputs.Example.WithCreativeShare(0.75m).AddToPlan(OptionalModule.Copyright).WithRoyalty(0m);

        MovesView.Of(plan, AfterTheYear).Moves.ShouldContain(move => move.Move is CopyrightFollowsPayMove);
    }

    // The share given, the royalty out of the plan: no move adds it.
    [Fact]
    public void Of_creativeShareGivenRoyaltyOutOfThePlan_proposesNoRoyalty()
    {
        MovesView.Of(RiverInputs.Example.WithCreativeShare(0.75m), AfterTheYear).Moves.ShouldNotContain(move => move.Move is CopyrightFollowsPayMove);
    }

    // Applying it lets the royalty follow the pay; the share, the statuses and the amount given stay.
    [Fact]
    public void Apply_royaltyFollowingThePay_letsItFollowAndKeepsTheRest()
    {
        var plan = RiverInputs.Example.WithCreativeShare(0.75m).WithAttestation(true).AddToPlan(OptionalModule.Copyright).WithRoyalty(2_000m);

        View(plan).Apply(Planned(new CopyrightFollowsPayMove()), plan).ShouldBe(plan.WithRoyaltyFollowingPay());
    }

    // From a pay above what the company pays beside the royalty, the river holds the pay there itself,
    // as the move does: 81,250 beside 18,750 of the 100,000 at 75%.
    [Fact]
    public void Apply_royaltyFollowingFromAPayAboveWhatTheCompanyPays_runsThePayTheMovePlanned()
    {
        var plan = RiverInputs.Example.WithCreativeShare(0.75m).AddToPlan(OptionalModule.Copyright).WithDirectorPay(90_000m);

        var year = View(plan).Apply(Planned(new CopyrightFollowsPayMove()), plan).Run().Year.ShouldNotBeNull();

        (year.DirectorPay.GrossPay, year.Copyright.Royalty).ShouldBe((81_250m, 18_750m));
    }

    // A plan with a payment by December, vouchers, its home's facts and its creative share, every lever with
    // a move in the plan: its moves read each of them.
    private static readonly RiverInputs Furnished = RiverInputs.Example.WithAdvancePayment(3, 1_000m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithCreativeShare(0.75m).WithAttestation(true)
        .AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Rent).AddToPlan(OptionalModule.Copyright).WithRoyalty(0m);

    private static int Kinds(MovesView view) => view.Moves.Select(move => move.Move.GetType()).Distinct().Count();

    private static MovesView View(RiverInputs? plan = null) => MovesView.Of(plan ?? RiverInputs.Example, AfterTheYear);

    private static PlannedMove Planned(Move move, decimal gain = 1m) => new(move, gain, ExampleYear, ExampleYear);

    // Each move and what it adds: two searches run two years, equal in their figures but not the same lists.
    private static IEnumerable<(Move Move, decimal Gain)> Lines(MovesView view) => view.Moves.Select(move => (move.Move, move.Gain));
}

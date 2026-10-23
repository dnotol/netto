using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The example leaves 61,975.74 in the pocket; its best pay, 20,799, 2,551.13 more, and on 22 October
// 2026 the company then pays 29,700.45 by 21 December to cancel the surcharge on its corporate tax
// (MoveSearchTests). After the year only the split moves.
public class MovesViewTests
{
    private static readonly DateOnly AfterTheYear = new(2027, 1, 1);

    private static readonly DateOnly On22October = new(2026, 10, 22);

    private static readonly PlannedMove BestPay = new(new PayMove(20_799m), 2_551.13m);

    private static readonly PlannedMove CompanyPays = new(new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 29_700.45m), 1m);

    private static readonly PlannedMove DirectorPays = new(new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 14_317.50m), 1m);

    [Fact]
    public void Of_noToday_asksForOne()
    {
        MovesView.Of(null, On22October).State.ShouldBe(MovesState.NoToday);
    }

    // The page searches once per Today, and the tab draws the view of what was found, searching nothing.
    [Fact]
    public void Of_whatTheSearchFound_isTheViewOfSearchingAgain()
    {
        var found = RiverInputs.Example.FindMoves(On22October);

        var view = MovesView.Of(RiverInputs.Example, found);

        var searched = MovesView.Of(RiverInputs.Example, On22October);
        (view.State, view.TodayPay, view.Total, view.Moves.SequenceEqual(searched.Moves)).ShouldBe((searched.State, searched.TodayPay, searched.Total, true));
    }

    // Nothing found, from no Today or from one that starts at the pay: two answers, told apart by Today.
    [Fact]
    public void Of_nothingFound_isNoTodayOrStartsAtThePay()
    {
        (MovesView.Of(null, found: null).State, MovesView.Of(RiverInputs.Example.SkipCompany(), found: null).State)
            .ShouldBe((MovesState.NoToday, MovesState.StartsAtPay));
    }

    // The one card of the river that opens by itself: what it says is the answer, and every state
    // of a Today it says something the user has to read (its moves, a refusal, where to start).
    // Without a Today it is folded to what it waits for, like the other cards.
    [Fact]
    public void OpenByDefault_noToday_isFalse()
    {
        MovesView.Of(null, On22October).OpenByDefault.ShouldBeFalse();
    }

    [Fact]
    public void OpenByDefault_todayWithMoves_isTrue()
    {
        MovesView.Of(RiverInputs.Example, On22October).OpenByDefault.ShouldBeTrue();
    }

    [Fact]
    public void OpenByDefault_todayAlreadyBest_isTrue()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var planned = view.Moves.Aggregate(RiverInputs.Example, (river, move) => view.Apply(move, river));

        MovesView.Of(planned, On22October).OpenByDefault.ShouldBeTrue();
    }

    [Fact]
    public void OpenByDefault_todaySkipsTheCompany_isTrue()
    {
        MovesView.Of(RiverInputs.Example.SkipCompany(), On22October).OpenByDefault.ShouldBeTrue();
    }

    [Fact]
    public void OpenByDefault_todayRefused_isTrue()
    {
        MovesView.Of(RiverInputs.Example.WithReserve(-1m), On22October).OpenByDefault.ShouldBeTrue();
    }

    // After the year: the pay first, then the vouchers the example does not grant.
    [Fact]
    public void Of_exampleAfterTheYear_movesThePayThenGrantsTheVouchers()
    {
        var view = MovesView.Of(RiverInputs.Example, AfterTheYear);

        (view.State, view.TodayPay, view.Moves[0]).ShouldBe((MovesState.Moves, 50_000m, BestPay));
        view.Moves.Skip(1).Select(move => move.Move).ShouldBe([new EcoVouchersMove(250m), new SportCultureVouchersMove(100m)], ignoreOrder: true);
    }

    [Fact]
    public void Of_exampleOn22October_paysTheCompanysSurchargeAfterThePay()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        view.Moves.Take(2).Select(move => move.Move).ShouldBe([BestPay.Move, CompanyPays.Move]);
    }

    // Every move applied, nothing is left to move.
    [Fact]
    public void Of_todayWithEveryMoveApplied_isAlreadyBest()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var planned = view.Moves.Aggregate(RiverInputs.Example, (river, move) => view.Apply(move, river));

        MovesView.Of(planned, On22October).State.ShouldBe(MovesState.AlreadyBest);
    }

    [Fact]
    public void Of_exampleOn22October_totalsItsMoves()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        view.Total.ShouldBe(view.Moves.Sum(move => move.Gain));
    }

    [Fact]
    public void Of_todaySkipsTheCompany_saysTheMovesStartFromIt()
    {
        MovesView.Of(RiverInputs.Example.SkipCompany(), On22October).State.ShouldBe(MovesState.StartsAtPay);
    }

    [Fact]
    public void Of_todayRefused_givesTheEnginesError()
    {
        var view = MovesView.Of(RiverInputs.Example.WithReserve(-1m), On22October);

        (view.State, view.Error).ShouldBe((MovesState.Refused, RiverInputs.Example.WithReserve(-1m).Run().Error));
    }

    // Today's pay is the one the river runs: a pay above the profit before pay is that profit.
    [Fact]
    public void Of_todayPayAboveTheProfit_startsFromTheProfit()
    {
        MovesView.Of(RiverInputs.Example.WithDirectorPay(150_000m), AfterTheYear).TodayPay.ShouldBe(100_000m);
    }

    [Fact]
    public void Applied_riverAtTheMovesPay_isTrue()
    {
        View().Applied(BestPay, RiverInputs.Example.WithDirectorPay(20_799m)).ShouldBeTrue();
    }

    [Fact]
    public void Applied_riverElsewhere_isFalse()
    {
        View().Applied(BestPay, RiverInputs.Example).ShouldBeFalse();
    }

    [Fact]
    public void Apply_payMove_setsTheRiversPayAsYours()
    {
        View().Apply(BestPay, RiverInputs.Example).ShouldBe(RiverInputs.Example.WithDirectorPay(20_799m));
    }

    // Today's payment by the deadline, with the move's amount on top.
    [Fact]
    public void Apply_companyPayment_setsTheRiversPaymentToTodaysAndTheMove()
    {
        var today = RiverInputs.Example.WithAdvancePayment(3, 1_000m);

        var river = View(today).Apply(CompanyPays, today.WithDirectorPay(20_799m));

        river.AdvancePayments.Fourth.Value.ShouldBe(30_700.45m);
    }

    [Fact]
    public void Applied_companyPaymentInTheRiver_isTrue()
    {
        var today = RiverInputs.Example.WithAdvancePayment(3, 1_000m);

        View(today).Applied(CompanyPays, today.WithAdvancePayment(3, 30_700.45m)).ShouldBeTrue();
    }

    [Fact]
    public void Applied_companyPaymentNotInTheRiver_isFalse()
    {
        View().Applied(CompanyPays, RiverInputs.Example).ShouldBeFalse();
    }

    [Fact]
    public void Apply_directorPayment_setsTheRiversPaymentOnTheIncomeTax()
    {
        var river = View().Apply(DirectorPays, RiverInputs.Example);

        (river.TaxPrepayments.Paid.Fourth.Value, View().Applied(DirectorPays, river)).ShouldBe((14_317.50m, true));
    }

    // Applying a voucher move adds Vouchers to the plan, which the example does not hold.
    [Fact]
    public void Apply_ecoVouchers_addsVouchersToThePlan()
    {
        var river = View().Apply(new PlannedMove(new EcoVouchersMove(250m), 1m), RiverInputs.Example);

        (river.InPlan(OptionalModule.Vouchers), river.Vouchers.EcoVouchers.Value).ShouldBe((true, 250m));
    }

    [Fact]
    public void Applied_ecoVouchersWithVouchersOutOfThePlan_isFalse()
    {
        var river = RiverInputs.Example.WithEcoVouchers(250m);

        View().Applied(new PlannedMove(new EcoVouchersMove(250m), 1m), river).ShouldBeFalse();
    }

    [Fact]
    public void Apply_sportCultureVouchers_isApplied()
    {
        var move = new PlannedMove(new SportCultureVouchersMove(100m), 1m);

        View().Applied(move, View().Apply(move, RiverInputs.Example)).ShouldBeTrue();
    }

    // Rent out of Today's plan with its home's facts kept: the move is the cap, and applying it brings
    // the home's facts, the rent and the Rent module into the river.
    [Fact]
    public void Of_rentFactsOutOfThePlan_rentsAtTheCap()
    {
        var today = RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m);

        MovesView.Of(today, AfterTheYear).Moves.Select(move => move.Move).ShouldContain(new RentMove(1_380.00m));
    }

    [Fact]
    public void Apply_rent_bringsTodaysHomeAndAddsRentToThePlan()
    {
        var today = RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m);
        var move = new PlannedMove(new RentMove(1_380.00m), 1m);

        var river = View(today).Apply(move, RiverInputs.Example);

        (river.InPlan(OptionalModule.Rent), river.Rent.CadastralIncome.Value, river.Rent.ShareLet.Value, river.Rent.Rent.Value, View(today).Applied(move, river))
            .ShouldBe((true, 1_200m, 0.12m, 1_380.00m, true));
    }

    // Today gives 75% of the time creating, its royalty out of the plan: the move lets the royalty follow
    // the pay, what a ruling accepts beside the pay the moves set.
    [Fact]
    public void Of_creativeShareGiven_proposesLettingTheRoyaltyFollowThePay()
    {
        var today = RiverInputs.Example.WithCreativeShare(0.75m);

        MovesView.Of(today, AfterTheYear).Moves.ShouldContain(move => move.Move is CopyrightFollowsPayMove);
    }

    // No share, no royalty move, and nothing said about it.
    [Fact]
    public void Of_noCreativeShare_proposesNoRoyalty()
    {
        MovesView.Of(RiverInputs.Example, AfterTheYear).Moves.ShouldNotContain(move => move.Move is CopyrightFollowsPayMove);
    }

    // Applying it brings Today's share and statuses, with their origins, lets the royalty follow the pay
    // and adds it to the plan; the amount given before stays.
    [Fact]
    public void Apply_royaltyFollowingThePay_bringsTodaysShareAndStatusesAndAddsTheRoyaltyToThePlan()
    {
        var today = RiverInputs.Example.WithCreativeShare(0.75m).WithAttestation(true);
        var move = new PlannedMove(new CopyrightFollowsPayMove(), 1m);

        var river = View(today).Apply(move, RiverInputs.Example.WithRoyalty(2_000m));

        (river.InPlan(OptionalModule.Copyright), river.Copyright, View(today).Applied(move, river))
            .ShouldBe((true, today.Copyright with { Royalty = new Input<decimal>(2_000m, InputOrigin.Yours), FollowsPay = new Input<bool>(true, InputOrigin.Yours) }, true));
    }

    // From a pay above what the company pays beside the royalty, the river holds the pay there itself,
    // as the move does: 81,250 beside 18,750 of the 100,000 at 75%.
    [Fact]
    public void Apply_royaltyFollowingFromAPayAboveWhatTheCompanyPays_runsThePayTheMovePlanned()
    {
        var today = RiverInputs.Example.WithCreativeShare(0.75m);
        var move = new PlannedMove(new CopyrightFollowsPayMove(), 1m);

        var year = View(today).Apply(move, RiverInputs.Example.WithDirectorPay(90_000m)).Run().Year.ShouldNotBeNull();

        (year.DirectorPay.GrossPay, year.Copyright.Royalty).ShouldBe((81_250m, 18_750m));
    }

    [Fact]
    public void Applied_royaltyOutOfThePlan_isNot()
    {
        var move = new PlannedMove(new CopyrightFollowsPayMove(), 1m);

        View().Applied(move, RiverInputs.Example.WithCreativeShare(0.75m)).ShouldBeFalse();
    }

    [Fact]
    public void Applied_fixedRoyaltyInThePlan_isNot()
    {
        var move = new PlannedMove(new CopyrightFollowsPayMove(), 1m);

        View().Applied(move, RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(5_000m)).ShouldBeFalse();
    }

    private static MovesView View(RiverInputs? today = null) => MovesView.Of(today ?? RiverInputs.Example, AfterTheYear);
}

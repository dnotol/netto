using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The example leaves 61,975.74 in the pocket; its best pay, 20,799, 2,551.13 more, and on 22 October
// 2026 the company then pays 29,700.34 by 21 December to cancel the surcharge on its corporate tax
// (MoveSearchTests). After the year only the split moves.
public class MovesViewTests
{
    private static readonly DateOnly AfterTheYear = new(2027, 1, 1);

    private static readonly DateOnly On22October = new(2026, 10, 22);

    // The example's year, which a move made here stands between: what Apply and Applied read is the move.
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

    // Nothing found is a plan that starts at the pay, Today or the plan in the river: there is always a plan
    // the moves are from, the plan in the river while none is starred Today (MovesSource).
    [Fact]
    public void Of_nothingFound_startsAtThePay()
    {
        (MovesView.Of(RiverInputs.Example.SkipCompany(), found: null).State, MovesView.Of(new MovesBaseline(RiverInputs.Example.SkipCompany(), IsToday: false), found: null).State)
            .ShouldBe((MovesState.StartsAtPay, MovesState.StartsAtPay));
    }

    // After the year: the pay first, then the vouchers the example does not grant.
    [Fact]
    public void Of_exampleAfterTheYear_movesThePayThenGrantsTheVouchers()
    {
        var view = MovesView.Of(RiverInputs.Example, AfterTheYear);

        (view.State, view.FromPay, view.Moves[0].Move, view.Moves[0].Gain).ShouldBe((MovesState.Moves, 50_000m, BestPay.Move, BestPay.Gain));
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

    // Today's pocket is the one the river shows for Today: the search's scenario keeps the home's facts
    // while Rent is out of the plan, and the royalty's share and statuses while Copyright royalty is, and
    // counts neither.
    public static TheoryData<string, RiverInputs> Todays => new()
    {
        { "the example", RiverInputs.Example },
        { "the home's facts kept, Rent out of the plan", RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m) },
        { "the royalty's share and attestation kept, Copyright royalty out of the plan", RiverInputs.Example.WithCreativeShare(0.75m).WithAttestation(true) },
    };

    [Theory]
    [MemberData(nameof(Todays))]
    public void Of_today_startsFromTheRiversPocketForToday(string _, RiverInputs today)
    {
        MovesView.Of(today, On22October).FromInPocket.ShouldBe(today.Run().Year.ShouldNotBeNull().NetInPocket);
    }

    // The plan's pocket is what the river leaves once every move is applied to Today.
    [Theory]
    [MemberData(nameof(Todays))]
    public void Of_today_endsOnThePocketOfTodayWithEveryMoveApplied(string _, RiverInputs today)
    {
        var view = MovesView.Of(today, On22October);

        var planned = view.Moves.Aggregate(today, (river, move) => view.Apply(move, river));

        view.PlanInPocket.ShouldBe(planned.Run().Year.ShouldNotBeNull().NetInPocket);
    }

    [Fact]
    public void Of_today_isMeasuredFromToday()
    {
        MovesView.Of(RiverInputs.Example, On22October).From.ShouldBe(new MovesBaseline(RiverInputs.Example, IsToday: true));
    }

    // While no plan is starred Today, the moves are measured from the plan in the river as it was opened
    // or saved: the same moves Today would have, were that plan starred.
    [Theory]
    [MemberData(nameof(Todays))]
    public void Of_planInTheRiver_hasTheMovesTodayWouldHaveForTheSamePlan(string _, RiverInputs plan)
    {
        var fromPlan = MovesView.Of(new MovesBaseline(plan, IsToday: false), On22October);

        var fromToday = MovesView.Of(plan, On22October);
        (fromPlan.State, fromPlan.FromPay, Lines(fromPlan).SequenceEqual(Lines(fromToday)), fromPlan.Total, fromPlan.FromInPocket, fromPlan.PlanInPocket)
            .ShouldBe((fromToday.State, fromToday.FromPay, true, fromToday.Total, fromToday.FromInPocket, fromToday.PlanInPocket));
    }

    // The view says which plan its moves are from, for the tab to word them.
    [Fact]
    public void Of_planInTheRiver_isNotToday()
    {
        var view = MovesView.Of(new MovesBaseline(RiverInputs.Example, IsToday: false, "Plan 2"), On22October);

        (view.From.IsToday, view.From.Name).ShouldBe((false, "Plan 2"));
    }

    [Fact]
    public void Of_planInTheRiverWithoutTheCompany_startsAtThePay()
    {
        MovesView.Of(new MovesBaseline(RiverInputs.Example.SkipCompany(), IsToday: false), On22October).State.ShouldBe(MovesState.StartsAtPay);
    }

    [Fact]
    public void Of_planInTheRiverRefused_givesTheEnginesError()
    {
        var plan = RiverInputs.Example.WithReserve(-1m);

        var view = MovesView.Of(new MovesBaseline(plan, IsToday: false), On22October);

        (view.State, view.Error).ShouldBe((MovesState.Refused, plan.Run().Error));
    }

    [Fact]
    public void Of_planInTheRiverWithEveryMoveApplied_isAlreadyBest()
    {
        var view = MovesView.Of(new MovesBaseline(RiverInputs.Example, IsToday: false), On22October);
        var planned = view.Moves.Aggregate(RiverInputs.Example, (river, move) => view.Apply(move, river));

        MovesView.Of(new MovesBaseline(planned, IsToday: false), On22October).State.ShouldBe(MovesState.AlreadyBest);
    }

    // Each move of the plan in the river sets the river as the same move from Today would: the plan's
    // payment by December with the move's amount on top, its home's facts with the rent, its creative share
    // and statuses with the royalty. Applied to the example, which holds none of them, so they can only
    // come from the plan; five kinds of move at least, so the list cannot be empty.
    [Fact]
    public void Apply_fromThePlanInTheRiver_setsTheRiverAsFromToday()
    {
        var fromPlan = MovesView.Of(new MovesBaseline(Furnished, IsToday: false), On22October);
        var fromToday = MovesView.Of(Furnished, On22October);

        var applied = fromPlan.Moves.Select(move => fromPlan.Apply(move, RiverInputs.Example)).ToList();

        (Kinds(fromPlan) >= 5, applied.SequenceEqual(fromToday.Moves.Select(move => fromToday.Apply(move, RiverInputs.Example))))
            .ShouldBe((true, true));
    }

    // Whether the river holds a move reads it against the plan in the river as against Today: the river
    // before the move, and after Today's.
    [Fact]
    public void Applied_fromThePlanInTheRiver_readsTheRiverAsFromToday()
    {
        var fromPlan = MovesView.Of(new MovesBaseline(Furnished, IsToday: false), On22October);
        var fromToday = MovesView.Of(Furnished, On22October);
        List<RiverInputs> rivers = [RiverInputs.Example, .. fromToday.Moves.Select(move => fromToday.Apply(move, RiverInputs.Example))];

        var applied = fromPlan.Moves.SelectMany(move => rivers.Select(river => fromPlan.Applied(move, river))).ToList();

        (Kinds(fromPlan) >= 5, applied.Count(held => held), applied.SequenceEqual(fromToday.Moves.SelectMany(move => rivers.Select(river => fromToday.Applied(move, river)))))
            .ShouldBe((true, fromPlan.Moves.Count, true));
    }

    // The river moved away from the plan in it as it was opened or saved, which the moves are still from:
    // the tab says so, while it shows the moves or that the plan is already the best.
    [Fact]
    public void RiverChanged_riverMovedFromThePlanInTheRiver_isTrue()
    {
        var view = MovesView.Of(new MovesBaseline(RiverInputs.Example, IsToday: false), On22October);

        (view.State, view.RiverChanged(RiverInputs.Example.WithDirectorPay(40_000m))).ShouldBe((MovesState.Moves, true));
    }

    [Fact]
    public void RiverChanged_riverAsThePlanInTheRiver_isFalse()
    {
        MovesView.Of(new MovesBaseline(RiverInputs.Example, IsToday: false), On22October).RiverChanged(RiverInputs.Example).ShouldBeFalse();
    }

    [Fact]
    public void RiverChanged_planInTheRiverAlreadyBest_isTrueOnceTheRiverMoves()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var planned = view.Moves.Aggregate(RiverInputs.Example, (river, move) => view.Apply(move, river));

        var best = MovesView.Of(new MovesBaseline(planned, IsToday: false), On22October);

        (best.State, best.RiverChanged(RiverInputs.Example)).ShouldBe((MovesState.AlreadyBest, true));
    }

    // Today is the user's current setup, which the river moves away from to be measured against it.
    [Fact]
    public void RiverChanged_fromToday_isFalse()
    {
        MovesView.Of(RiverInputs.Example, On22October).RiverChanged(RiverInputs.Example.WithDirectorPay(40_000m)).ShouldBeFalse();
    }

    // A plan starting at the pay or refused already says to save the river to see the moves.
    public static TheoryData<string, RiverInputs> PlansWithoutMoves => new()
    {
        { "starting at the pay", RiverInputs.Example.SkipCompany() },
        { "refused", RiverInputs.Example.WithReserve(-1m) },
    };

    [Theory]
    [MemberData(nameof(PlansWithoutMoves))]
    public void RiverChanged_noMovesFromThePlanInTheRiver_isFalse(string _, RiverInputs plan)
    {
        MovesView.Of(new MovesBaseline(plan, IsToday: false), On22October).RiverChanged(RiverInputs.Example).ShouldBeFalse();
    }

    // What the pocket holds after each move is Today's with the gains up to it: the last is the plan's.
    [Fact]
    public void InPocketAfter_eachMove_addsTheGainsUpToItToTodaysPocket()
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

    // Nothing to move: Today's pocket is the plan's.
    [Fact]
    public void Of_todayAlreadyBest_holdsTodaysPocketAlone()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var planned = view.Moves.Aggregate(RiverInputs.Example, (river, move) => view.Apply(move, river));

        var best = MovesView.Of(planned, On22October);

        (best.State, best.FromInPocket, best.PlanInPocket).ShouldBe((MovesState.AlreadyBest, planned.Run().Year.ShouldNotBeNull().NetInPocket, best.FromInPocket));
    }

    // Today's pay is the one the river runs: a pay above the profit before pay is that profit.
    [Fact]
    public void Of_todayPayAboveTheProfit_startsFromTheProfit()
    {
        MovesView.Of(RiverInputs.Example.WithDirectorPay(150_000m), AfterTheYear).FromPay.ShouldBe(100_000m);
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

        river.AdvancePayments.Fourth.Value.ShouldBe(30_700.34m);
    }

    [Fact]
    public void Applied_companyPaymentInTheRiver_isTrue()
    {
        var today = RiverInputs.Example.WithAdvancePayment(3, 1_000m);

        View(today).Applied(CompanyPays, today.WithAdvancePayment(3, 30_700.34m)).ShouldBeTrue();
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
        var river = View().Apply(Planned(new EcoVouchersMove(250m)), RiverInputs.Example);

        (river.InPlan(OptionalModule.Vouchers), river.Vouchers.EcoVouchers.Value).ShouldBe((true, 250m));
    }

    [Fact]
    public void Applied_ecoVouchersWithVouchersOutOfThePlan_isFalse()
    {
        var river = RiverInputs.Example.WithEcoVouchers(250m);

        View().Applied(Planned(new EcoVouchersMove(250m)), river).ShouldBeFalse();
    }

    [Fact]
    public void Apply_sportCultureVouchers_isApplied()
    {
        var move = Planned(new SportCultureVouchersMove(100m));

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
        var move = Planned(new RentMove(1_380.00m));

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
        var move = Planned(new CopyrightFollowsPayMove());

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
        var move = Planned(new CopyrightFollowsPayMove());

        var year = View(today).Apply(move, RiverInputs.Example.WithDirectorPay(90_000m)).Run().Year.ShouldNotBeNull();

        (year.DirectorPay.GrossPay, year.Copyright.Royalty).ShouldBe((81_250m, 18_750m));
    }

    [Fact]
    public void Applied_royaltyOutOfThePlan_isNot()
    {
        var move = Planned(new CopyrightFollowsPayMove());

        View().Applied(move, RiverInputs.Example.WithCreativeShare(0.75m)).ShouldBeFalse();
    }

    [Fact]
    public void Applied_fixedRoyaltyInThePlan_isNot()
    {
        var move = Planned(new CopyrightFollowsPayMove());

        View().Applied(move, RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(5_000m)).ShouldBeFalse();
    }

    // A plan with a payment by December, its home's facts and its creative share given, Rent and
    // Copyright royalty out of its plan: its moves read each of them.
    private static readonly RiverInputs Furnished = RiverInputs.Example.WithAdvancePayment(3, 1_000m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithCreativeShare(0.75m).WithAttestation(true);

    private static int Kinds(MovesView view) => view.Moves.Select(move => move.Move.GetType()).Distinct().Count();

    private static MovesView View(RiverInputs? today = null) => MovesView.Of(today ?? RiverInputs.Example, AfterTheYear);

    private static PlannedMove Planned(Move move, decimal gain = 1m) => new(move, gain, ExampleYear, ExampleYear);

    // Each move and what it adds: two searches run two years, equal in their figures but not the same lists.
    private static IEnumerable<(Move Move, decimal Gain)> Lines(MovesView view) => view.Moves.Select(move => (move.Move, move.Gain));
}

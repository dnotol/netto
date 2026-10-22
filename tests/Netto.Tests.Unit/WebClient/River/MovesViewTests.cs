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

    [Fact]
    public void Of_exampleAfterTheYear_movesThePayToTheBest()
    {
        var view = MovesView.Of(RiverInputs.Example, AfterTheYear);

        (view.State, view.TodayPay, view.Total).ShouldBe((MovesState.Moves, 50_000m, 2_551.13m));
        view.Moves.ShouldBe([BestPay]);
    }

    [Fact]
    public void Of_exampleOn22October_paysTheCompanysSurchargeToo()
    {
        var view = MovesView.Of(RiverInputs.Example, On22October);

        view.Moves.Select(move => move.Move).ShouldBe([BestPay.Move, CompanyPays.Move]);
    }

    [Fact]
    public void Of_todayAtTheBestPay_isAlreadyBest()
    {
        MovesView.Of(RiverInputs.Example.WithDirectorPay(20_799m), AfterTheYear).State.ShouldBe(MovesState.AlreadyBest);
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

    private static MovesView View(RiverInputs? today = null) => MovesView.Of(today ?? RiverInputs.Example, AfterTheYear);
}

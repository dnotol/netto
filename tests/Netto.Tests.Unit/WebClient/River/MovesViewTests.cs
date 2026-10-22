using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The example leaves 61,975.74 in the pocket; its best pay, 20,799, 2,551.13 more (MoveSearchTests).
public class MovesViewTests
{
    private static readonly PlannedMove BestPay = new(new PayMove(20_799m), 2_551.13m);

    [Fact]
    public void Of_noToday_asksForOne()
    {
        MovesView.Of(null).State.ShouldBe(MovesState.NoToday);
    }

    [Fact]
    public void Of_example_movesThePayToTheBest()
    {
        var view = MovesView.Of(RiverInputs.Example);

        (view.State, view.TodayPay, view.Total).ShouldBe((MovesState.Moves, 50_000m, 2_551.13m));
        view.Moves.ShouldBe([BestPay]);
    }

    [Fact]
    public void Of_todayAtTheBestPay_isAlreadyBest()
    {
        MovesView.Of(RiverInputs.Example.WithDirectorPay(20_799m)).State.ShouldBe(MovesState.AlreadyBest);
    }

    [Fact]
    public void Of_todaySkipsTheCompany_saysTheMovesStartFromIt()
    {
        MovesView.Of(RiverInputs.Example.SkipCompany()).State.ShouldBe(MovesState.StartsAtPay);
    }

    [Fact]
    public void Of_todayRefused_givesTheEnginesError()
    {
        var view = MovesView.Of(RiverInputs.Example.WithReserve(-1m));

        (view.State, view.Error).ShouldBe((MovesState.Refused, RiverInputs.Example.WithReserve(-1m).Run().Error));
    }

    // Today's pay is the one the river runs: a pay above the profit before pay is that profit.
    [Fact]
    public void Of_todayPayAboveTheProfit_startsFromTheProfit()
    {
        MovesView.Of(RiverInputs.Example.WithDirectorPay(150_000m)).TodayPay.ShouldBe(100_000m);
    }

    [Fact]
    public void Applied_riverAtTheMovesPay_isTrue()
    {
        MovesView.Applied(BestPay, RiverInputs.Example.WithDirectorPay(20_799m)).ShouldBeTrue();
    }

    [Fact]
    public void Applied_riverElsewhere_isFalse()
    {
        MovesView.Applied(BestPay, RiverInputs.Example).ShouldBeFalse();
    }

    [Fact]
    public void Apply_payMove_setsTheRiversPayAsYours()
    {
        var river = MovesView.Apply(BestPay, RiverInputs.Example);

        river.ShouldBe(RiverInputs.Example.WithDirectorPay(20_799m));
    }
}

using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What each tab says after its name, from the views its content draws: the moves' total on Optimisation,
// over the plan starred Today or the plan in the river, the next advance payment's date on When to pay,
// and on a tab holding an amount the engine refuses, that refusal, which wins over a date, and the number
// of plans to compare on Compare plans.
public class TabBadgesTests
{
    // The deadline still open on this date is December's.
    private static readonly DateOnly On23October = new(2026, 10, 23);

    private static readonly DateOnly AfterTheYear = new(2027, 1, 1);

    [Fact]
    public void Of_riverRefused_marksThePlan()
    {
        var river = RiverInputs.Example.WithReserve(-1m);

        Badges(river, today: null, On23October).GetValueOrDefault(PlanTab.Plan).ShouldBe(new TabBadge(TabBadgeKind.Refused));
    }

    [Fact]
    public void Of_riverRunning_badgesNoPlan()
    {
        Badges(RiverInputs.Example, today: null, On23October).ContainsKey(PlanTab.Plan).ShouldBeFalse();
    }

    // The gain of every move together, what the tab adds to Today.
    [Fact]
    public void Of_movesFromToday_badgesOptimisationWithTheirTotal()
    {
        var total = MovesView.Of(RiverInputs.Example, On23October).Total;

        Badges(RiverInputs.Example, RiverInputs.Example, On23October).GetValueOrDefault(PlanTab.Optimisation)
            .ShouldBe(new TabBadge(TabBadgeKind.MoreThanToday, Amount: total));
    }

    // While no plan is starred Today, the moves are from the plan in the river: what they add, they add to
    // that plan.
    [Fact]
    public void Of_movesFromThePlanInTheRiver_badgesOptimisationWithTheirTotalOverThePlan()
    {
        var moves = MovesView.Of(new MovesBaseline(RiverInputs.Example, IsToday: false), On23October);
        var run = RiverInputs.Example.Run();

        TabBadges.Of(run, moves, CalendarView.From(RiverInputs.Example, run, On23October), 0).GetValueOrDefault(PlanTab.Optimisation)
            .ShouldBe(new TabBadge(TabBadgeKind.MoreThanPlan, Amount: moves.Total));
    }

    // No move adds a euro: nothing to say beside the tab's name.
    [Fact]
    public void Of_todayAlreadyTheBest_badgesNoOptimisation()
    {
        var view = MovesView.Of(RiverInputs.Example, On23October);
        var planned = view.Moves.Aggregate(RiverInputs.Example, (river, move) => view.Apply(move, river));

        Badges(planned, planned, On23October).ContainsKey(PlanTab.Optimisation).ShouldBeFalse();
    }

    [Fact]
    public void Of_todayRefused_badgesNoOptimisation()
    {
        Badges(RiverInputs.Example, RiverInputs.Example.WithReserve(-1m), On23October).ContainsKey(PlanTab.Optimisation).ShouldBeFalse();
    }

    // The example prepays nothing by the company, so its next payment is the one that cancels the
    // surcharge on its corporate tax, by 21 December.
    [Fact]
    public void Of_paymentStillToMake_badgesWhenToPayWithItsDeadline()
    {
        Badges(RiverInputs.Example, today: null, On23October).GetValueOrDefault(PlanTab.WhenToPay)
            .ShouldBe(new TabBadge(TabBadgeKind.NextBy, Date: new DateOnly(2026, 12, 21)));
    }

    [Fact]
    public void Of_everyDeadlinePast_badgesNoWhenToPay()
    {
        Badges(RiverInputs.Example, today: null, AfterTheYear).ContainsKey(PlanTab.WhenToPay).ShouldBeFalse();
    }

    // What the fund bills is refused: the tab holding the field says so, rather than the date.
    [Fact]
    public void Of_billedAmountRefused_marksWhenToPay()
    {
        var river = RiverInputs.Example.WithBilledContributions(-1m);

        Badges(river, today: null, On23October).GetValueOrDefault(PlanTab.WhenToPay).ShouldBe(new TabBadge(TabBadgeKind.Refused));
    }

    // The river refused, there is no calendar: the refusal is the Plan tab's.
    [Fact]
    public void Of_riverRefused_badgesNoWhenToPay()
    {
        Badges(RiverInputs.Example.WithReserve(-1m), today: null, On23October).ContainsKey(PlanTab.WhenToPay).ShouldBeFalse();
    }

    // Compare plans says how many saved plans the viewer can compare, once there are two.
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void Of_twoPlansOrMoreToCompare_badgesCompareWithTheirCount(int plans)
    {
        Badges(RiverInputs.Example, today: null, On23October, plans).GetValueOrDefault(PlanTab.Compare).ShouldBe(new TabBadge(TabBadgeKind.SavedPlans, Count: plans));
    }

    // Nothing to compare yet: with none, or one plan, the tab has no badge.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Of_lessThanTwoPlansToCompare_badgesNoCompare(int plans)
    {
        Badges(RiverInputs.Example, RiverInputs.Example, On23October, plans).ContainsKey(PlanTab.Compare).ShouldBeFalse();
    }

    // The plans are the tab's own, whatever the river does: a refused river leaves the count.
    [Fact]
    public void Of_riverRefused_stillBadgesCompare()
    {
        Badges(RiverInputs.Example.WithReserve(-1m), today: null, On23October, plans: 3).GetValueOrDefault(PlanTab.Compare)
            .ShouldBe(new TabBadge(TabBadgeKind.SavedPlans, Count: 3));
    }

    // Without a plan starred Today, the moves are from the river as a plan, as the page opens on it.
    private static IReadOnlyDictionary<PlanTab, TabBadge> Badges(RiverInputs river, RiverInputs? today, DateOnly date, int plans = 0)
    {
        var run = river.Run();
        var moves = MovesView.Of(today is null ? new MovesBaseline(river, IsToday: false) : MovesBaseline.Today(today), date);
        return TabBadges.Of(run, moves, CalendarView.From(river, run, date), plans);
    }
}

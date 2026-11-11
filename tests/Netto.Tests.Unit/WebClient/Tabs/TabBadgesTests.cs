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

        Badges(river, On23October).GetValueOrDefault(PlanTab.Plan).ShouldBe(new TabBadge(TabBadgeKind.Refused));
    }

    [Fact]
    public void Of_riverRunning_badgesNoPlan()
    {
        Badges(RiverInputs.Example, On23October).ContainsKey(PlanTab.Plan).ShouldBeFalse();
    }

    // The gain of every move together, what the tab adds to the plan on screen, whatever plan is starred
    // Today: Today feeds the band's gain alone.
    [Fact]
    public void Of_moves_badgesOptimisationWithTheirTotalOverThePlanOnScreen()
    {
        var total = MovesView.Of(RiverInputs.Example, On23October).Total;

        Badges(RiverInputs.Example, On23October).GetValueOrDefault(PlanTab.Optimisation)
            .ShouldBe(new TabBadge(TabBadgeKind.MoreThanPlan, Amount: total));
    }

    // While the page looks for the moves, once painted or after an edit, the tab says so.
    [Fact]
    public void Of_movesStillLookedFor_badgesOptimisationAsSearching()
    {
        var run = RiverInputs.Example.Run();
        var moves = MovesView.Searching(RiverInputs.Example);

        TabBadges.Of(run, moves, CalendarView.From(RiverInputs.Example, run, On23October), 0).GetValueOrDefault(PlanTab.Optimisation)
            .ShouldBe(new TabBadge(TabBadgeKind.Searching));
    }

    // No move adds a euro: nothing to say beside the tab's name.
    [Fact]
    public void Of_planAlreadyTheBest_badgesNoOptimisation()
    {
        var planned = MovesView.Of(RiverInputs.Example, On23October).ApplyAll(RiverInputs.Example);

        Badges(planned, On23October).ContainsKey(PlanTab.Optimisation).ShouldBeFalse();
    }

    [Fact]
    public void Of_planRefused_badgesNoOptimisation()
    {
        Badges(RiverInputs.Example.WithReserve(-1m), On23October).ContainsKey(PlanTab.Optimisation).ShouldBeFalse();
    }

    // The example prepays nothing by the company, so its next payment is the one that cancels the
    // surcharge on its corporate tax, by 21 December.
    [Fact]
    public void Of_paymentStillToMake_badgesWhenToPayWithItsDeadline()
    {
        Badges(RiverInputs.Example, On23October).GetValueOrDefault(PlanTab.WhenToPay)
            .ShouldBe(new TabBadge(TabBadgeKind.NextBy, Date: new DateOnly(2026, 12, 21)));
    }

    [Fact]
    public void Of_everyDeadlinePast_badgesNoWhenToPay()
    {
        Badges(RiverInputs.Example, AfterTheYear).ContainsKey(PlanTab.WhenToPay).ShouldBeFalse();
    }

    // What the fund bills is refused: the tab holding the field says so, rather than the date.
    [Fact]
    public void Of_billedAmountRefused_marksWhenToPay()
    {
        var river = RiverInputs.Example.WithBilledContributions(-1m);

        Badges(river, On23October).GetValueOrDefault(PlanTab.WhenToPay).ShouldBe(new TabBadge(TabBadgeKind.Refused));
    }

    // The river refused, there is no calendar: the refusal is the Plan tab's.
    [Fact]
    public void Of_riverRefused_badgesNoWhenToPay()
    {
        Badges(RiverInputs.Example.WithReserve(-1m), On23October).ContainsKey(PlanTab.WhenToPay).ShouldBeFalse();
    }

    // Compare plans says how many saved plans the viewer can compare, once there are two.
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void Of_twoPlansOrMoreToCompare_badgesCompareWithTheirCount(int plans)
    {
        Badges(RiverInputs.Example, On23October, plans).GetValueOrDefault(PlanTab.Compare).ShouldBe(new TabBadge(TabBadgeKind.SavedPlans, Count: plans));
    }

    // Nothing to compare yet: with none, or one plan, the tab has no badge.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Of_lessThanTwoPlansToCompare_badgesNoCompare(int plans)
    {
        Badges(RiverInputs.Example, On23October, plans).ContainsKey(PlanTab.Compare).ShouldBeFalse();
    }

    // The plans are the tab's own, whatever the river does: a refused river leaves the count.
    [Fact]
    public void Of_riverRefused_stillBadgesCompare()
    {
        Badges(RiverInputs.Example.WithReserve(-1m), On23October, plans: 3).GetValueOrDefault(PlanTab.Compare)
            .ShouldBe(new TabBadge(TabBadgeKind.SavedPlans, Count: 3));
    }

    // The moves from the plan on screen.
    private static IReadOnlyDictionary<PlanTab, TabBadge> Badges(RiverInputs river, DateOnly date, int plans = 0)
    {
        var run = river.Run();
        var moves = MovesView.Of(river, date);
        return TabBadges.Of(run, moves, CalendarView.From(river, run, date), plans);
    }
}

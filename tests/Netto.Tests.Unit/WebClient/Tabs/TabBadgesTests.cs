using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What each tab says after its name, from the views its content draws: on Optimisation the moves' total, over
// the plan on screen, then what its When to pay section says, the next advance payment's date, or that what
// the fund bills is refused, which wins over a date; on My plan that the river is refused.
public class TabBadgesTests
{
    // The deadline still open on this date is December's.
    private static readonly DateOnly On23October = new(2026, 10, 23);

    private static readonly DateOnly AfterTheYear = new(2027, 1, 1);

    [Fact]
    public void Of_riverRefused_marksThePlan()
    {
        var river = RiverInputs.Example.WithReserve(-1m);

        Badges(river, On23October).GetValueOrDefault(PlanTab.Plan).ShouldBe([new TabBadge(TabBadgeKind.Refused)]);
    }

    [Fact]
    public void Of_riverRunning_marksNoRefusal()
    {
        Badges(RiverInputs.Example, On23October).GetValueOrDefault(PlanTab.Plan).ShouldBeNull();
    }

    // The gain of every move together, what the tab adds to the plan on screen, whatever plan is starred
    // Today: Today feeds the band's gain alone.
    [Fact]
    public void Of_moves_badgesOptimisationWithTheirTotalOverThePlanOnScreen()
    {
        var total = MovesView.Of(RiverInputs.Example, On23October).Total;

        Badges(RiverInputs.Example, On23October).GetValueOrDefault(PlanTab.Optimisation).ShouldNotBeNull()[0]
            .ShouldBe(new TabBadge(TabBadgeKind.MoreThanPlan, Amount: total));
    }

    // While the page looks for the moves, once painted or after an edit, the tab says so.
    [Fact]
    public void Of_movesStillLookedFor_badgesOptimisationAsSearching()
    {
        var run = RiverInputs.Example.Run();
        var moves = MovesView.Searching(RiverInputs.Example);

        TabBadges.Of(run, moves, CalendarView.From(RiverInputs.Example, run, On23October)).GetValueOrDefault(PlanTab.Optimisation).ShouldNotBeNull()[0]
            .ShouldBe(new TabBadge(TabBadgeKind.Searching));
    }

    // No move adds a euro: no gain beside the tab's name.
    [Fact]
    public void Of_planAlreadyTheBest_badgesNoGain()
    {
        var planned = MovesView.Of(RiverInputs.Example, On23October).ApplyAll(RiverInputs.Example);

        Badges(planned, On23October).GetValueOrDefault(PlanTab.Optimisation).ShouldNotBeNull().ShouldNotContain(badge => badge.Kind == TabBadgeKind.MoreThanPlan);
    }

    [Fact]
    public void Of_planRefused_badgesNoOptimisation()
    {
        Badges(RiverInputs.Example.WithReserve(-1m), On23October).ContainsKey(PlanTab.Optimisation).ShouldBeFalse();
    }

    // The example prepays nothing by the company, so its next payment is the one that cancels the
    // surcharge on its corporate tax, by 21 December: Optimise's badge after its gain, the deadline staying in
    // the band, a link to the section that says it.
    [Fact]
    public void Of_paymentStillToMake_badgesOptimisationWithItsDeadlineAfterTheGain()
    {
        var total = MovesView.Of(RiverInputs.Example, On23October).Total;

        Badges(RiverInputs.Example, On23October).GetValueOrDefault(PlanTab.Optimisation)
            .ShouldBe([new TabBadge(TabBadgeKind.MoreThanPlan, Amount: total), new TabBadge(TabBadgeKind.NextBy, Date: new DateOnly(2026, 12, 21))]);
    }

    [Fact]
    public void Of_paymentStillToMake_leavesMyPlanWithoutIt()
    {
        Badges(RiverInputs.Example, On23October).ContainsKey(PlanTab.Plan).ShouldBeFalse();
    }

    [Fact]
    public void Of_everyDeadlinePast_badgesNoDeadline()
    {
        Badges(RiverInputs.Example, AfterTheYear).GetValueOrDefault(PlanTab.Optimisation)?.ShouldNotContain(badge => badge.Kind == TabBadgeKind.NextBy);
    }

    // What the fund bills is refused, a field of the section: Optimise says so, rather than the date, and
    // My plan, whose river runs, says nothing.
    [Fact]
    public void Of_billedAmountRefused_marksOptimiseOverItsDeadline()
    {
        var river = RiverInputs.Example.WithBilledContributions(-1m);

        var badges = Badges(river, On23October);

        badges.GetValueOrDefault(PlanTab.Optimisation).ShouldNotBeNull().Select(badge => badge.Kind).ShouldBe([TabBadgeKind.MoreThanPlan, TabBadgeKind.BilledRefused]);
        badges.ContainsKey(PlanTab.Plan).ShouldBeFalse();
    }

    // The river refused, there is no calendar and no deadline: the refusal alone.
    [Fact]
    public void Of_riverRefused_badgesTheRefusalAlone()
    {
        Badges(RiverInputs.Example.WithReserve(-1m), On23October).GetValueOrDefault(PlanTab.Plan)
            .ShouldBe([new TabBadge(TabBadgeKind.Refused)]);
    }

    // The moves from the plan on screen.
    private static IReadOnlyDictionary<PlanTab, IReadOnlyList<TabBadge>> Badges(RiverInputs river, DateOnly date)
    {
        var run = river.Run();
        var moves = MovesView.Of(river, date);
        return TabBadges.Of(run, moves, CalendarView.From(river, run, date));
    }
}

using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What each tab says after its name, from the views its content draws: the moves' total on Optimisation,
// over the plan on screen, the next advance payment's date on My plan, which the by-date lens shows, and
// there too an amount the engine refuses, which wins over a date, and on Pistes how many pistes are priced
// and how many questions wait, once something supplies them.
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
    public void Of_riverRunning_marksNoRefusal()
    {
        Badges(RiverInputs.Example, On23October).GetValueOrDefault(PlanTab.Plan)?.Kind.ShouldNotBe(TabBadgeKind.Refused);
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

        TabBadges.Of(run, moves, CalendarView.From(RiverInputs.Example, run, On23October), null).GetValueOrDefault(PlanTab.Optimisation)
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
    // surcharge on its corporate tax, by 21 December: My plan's badge, the deadline staying in the band.
    [Fact]
    public void Of_paymentStillToMake_badgesMyPlanWithItsDeadline()
    {
        Badges(RiverInputs.Example, On23October).GetValueOrDefault(PlanTab.Plan)
            .ShouldBe(new TabBadge(TabBadgeKind.NextBy, Date: new DateOnly(2026, 12, 21)));
    }

    [Fact]
    public void Of_everyDeadlinePast_badgesNoMyPlan()
    {
        Badges(RiverInputs.Example, AfterTheYear).ContainsKey(PlanTab.Plan).ShouldBeFalse();
    }

    // What the fund bills is refused, a field of the by-date lens: My plan says so, rather than the date.
    [Fact]
    public void Of_billedAmountRefused_marksMyPlanOverItsDeadline()
    {
        var river = RiverInputs.Example.WithBilledContributions(-1m);

        Badges(river, On23October).GetValueOrDefault(PlanTab.Plan).ShouldBe(new TabBadge(TabBadgeKind.Refused));
    }

    // The river refused, there is no calendar and no deadline: the refusal alone.
    [Fact]
    public void Of_riverRefused_badgesTheRefusalAlone()
    {
        Badges(RiverInputs.Example.WithReserve(-1m), On23October).GetValueOrDefault(PlanTab.Plan)
            .ShouldBe(new TabBadge(TabBadgeKind.Refused));
    }

    // Nothing supplies the pistes yet: Pistes has no badge. Once something does, it says how many are
    // priced and how many questions wait.
    [Fact]
    public void Of_noPistesCount_badgesNoPistes()
    {
        Badges(RiverInputs.Example, On23October).ContainsKey(PlanTab.Pistes).ShouldBeFalse();
    }

    [Fact]
    public void Of_pistesToSay_badgesPistesWithBothCounts()
    {
        Badges(RiverInputs.Example, On23October, new PistesCount(1, 3)).GetValueOrDefault(PlanTab.Pistes)
            .ShouldBe(new TabBadge(TabBadgeKind.Pistes, Count: 1, Questions: 3));
    }

    [Fact]
    public void Of_noPisteAndNoQuestion_badgesNoPistes()
    {
        Badges(RiverInputs.Example, On23October, new PistesCount(0, 0)).ContainsKey(PlanTab.Pistes).ShouldBeFalse();
    }

    // A question without a priced piste is still something to say.
    [Fact]
    public void Of_onlyQuestions_badgesPistes()
    {
        Badges(RiverInputs.Example, On23October, new PistesCount(0, 2)).GetValueOrDefault(PlanTab.Pistes)
            .ShouldBe(new TabBadge(TabBadgeKind.Pistes, Count: 0, Questions: 2));
    }

    // While an idea is still being measured, the badge says so, as Optimise's does for the moves, and no count
    // that would change once it is found.
    [Fact]
    public void Of_pistesBeingMeasured_badgesPistesAsBeingMeasured()
    {
        Badges(RiverInputs.Example, On23October, new PistesCount(1, 3), pistesSearching: true).GetValueOrDefault(PlanTab.Pistes)
            .ShouldBe(new TabBadge(TabBadgeKind.PistesSearching));
    }

    // The moves from the plan on screen.
    private static IReadOnlyDictionary<PlanTab, TabBadge> Badges(RiverInputs river, DateOnly date, PistesCount? pistes = null, bool pistesSearching = false)
    {
        var run = river.Run();
        var moves = MovesView.Of(river, date);
        return TabBadges.Of(run, moves, CalendarView.From(river, run, date), pistes, pistesSearching);
    }
}

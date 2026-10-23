using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What each tab says after its name, from the views its content draws: the moves' total on Optimisation
// once a plan is starred Today, the next advance payment's date on When to pay, and on a tab holding an
// amount the engine refuses, that refusal, which wins over a date.
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

    [Fact]
    public void Of_noToday_badgesNoOptimisation()
    {
        Badges(RiverInputs.Example, today: null, On23October).ContainsKey(PlanTab.Optimisation).ShouldBeFalse();
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

    [Fact]
    public void Of_compare_hasNoBadgeYet()
    {
        Badges(RiverInputs.Example, RiverInputs.Example, On23October).ContainsKey(PlanTab.Compare).ShouldBeFalse();
    }

    private static IReadOnlyDictionary<PlanTab, TabBadge> Badges(RiverInputs river, RiverInputs? today, DateOnly date)
    {
        var run = river.Run();
        return TabBadges.Of(run, MovesView.Of(today, date), CalendarView.From(river, run, date));
    }
}

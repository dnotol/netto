using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Which card a link to My plan arrives at (plan?card=<name>): the page asks on every set of its
// parameters, and opens and brings into view the card it is given, once per arrival.
public class CardArrivalTests
{
    [Fact]
    public void Take_planTabNamingACard_arrivesAtIt()
    {
        new CardArrival().Take(PlanTab.Plan, "split").ShouldBe("split");
    }

    [Fact]
    public void Take_planTabNamingNoCard_arrivesNowhere()
    {
        new CardArrival().Take(PlanTab.Plan, null).ShouldBeNull();
    }

    // Only the river's tab has cards.
    [Fact]
    public void Take_otherTab_arrivesNowhere()
    {
        new CardArrival().Take(PlanTab.Pistes, "split").ShouldBeNull();
    }

    // The same address again, as Blazor may set the page's parameters again while it stays there: the
    // viewer has moved on from the card, which stays where they left it.
    [Fact]
    public void Take_sameCardAgain_arrivesNowhereMore()
    {
        var arrival = new CardArrival();
        arrival.Take(PlanTab.Plan, "split");

        arrival.Take(PlanTab.Plan, "split").ShouldBeNull();
    }

    // Back on the Optimisation tab and following the same link again is a new arrival.
    [Fact]
    public void Take_sameCardAfterAnotherTab_arrivesAgain()
    {
        var arrival = new CardArrival();
        arrival.Take(PlanTab.Plan, "split");
        arrival.Take(PlanTab.Optimisation, null);

        arrival.Take(PlanTab.Plan, "split").ShouldBe("split");
    }

    [Fact]
    public void Take_anotherCard_arrivesAtIt()
    {
        var arrival = new CardArrival();
        arrival.Take(PlanTab.Plan, "split");

        arrival.Take(PlanTab.Plan, "vouchers").ShouldBe("vouchers");
    }
}

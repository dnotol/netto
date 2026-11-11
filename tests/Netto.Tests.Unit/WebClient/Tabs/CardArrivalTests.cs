using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Which card a link to My plan arrives at (plan?card=<name>), the by-bucket lens being where the cards are: the page asks on every set of its
// parameters, and opens and brings into view the card it is given, once per arrival.
public class CardArrivalTests
{
    [Fact]
    public void Take_planTabNamingACard_arrivesAtIt()
    {
        new CardArrival().Take(PlanTab.Plan, PlanLens.ByBucket, "split").ShouldBe("split");
    }

    [Fact]
    public void Take_planTabNamingNoCard_arrivesNowhere()
    {
        new CardArrival().Take(PlanTab.Plan, PlanLens.ByBucket, null).ShouldBeNull();
    }

    // Only the river's tab has cards.
    [Fact]
    public void Take_otherTab_arrivesNowhere()
    {
        new CardArrival().Take(PlanTab.Pistes, PlanLens.ByBucket, "split").ShouldBeNull();
    }

    // My plan by date draws no map and no card: a card named there is not one to arrive at.
    [Fact]
    public void Take_myPlanByDate_arrivesNowhere()
    {
        new CardArrival().Take(PlanTab.Plan, PlanLens.ByDate, "split").ShouldBeNull();
    }

    // Read by bucket after the lens, the same link is a new arrival: its card was never drawn.
    [Fact]
    public void Take_cardNamedByDateThenByBucket_arrivesWhenTheMapIsDrawn()
    {
        var arrival = new CardArrival();
        arrival.Take(PlanTab.Plan, PlanLens.ByDate, "split");

        arrival.Take(PlanTab.Plan, PlanLens.ByBucket, "split").ShouldBe("split");
    }

    // The same address again, as Blazor may set the page's parameters again while it stays there: the
    // viewer has moved on from the card, which stays where they left it.
    [Fact]
    public void Take_sameCardAgain_arrivesNowhereMore()
    {
        var arrival = new CardArrival();
        arrival.Take(PlanTab.Plan, PlanLens.ByBucket, "split");

        arrival.Take(PlanTab.Plan, PlanLens.ByBucket, "split").ShouldBeNull();
    }

    // Back on the Optimisation tab and following the same link again is a new arrival.
    [Fact]
    public void Take_sameCardAfterAnotherTab_arrivesAgain()
    {
        var arrival = new CardArrival();
        arrival.Take(PlanTab.Plan, PlanLens.ByBucket, "split");
        arrival.Take(PlanTab.Optimisation, PlanLens.ByBucket, null);

        arrival.Take(PlanTab.Plan, PlanLens.ByBucket, "split").ShouldBe("split");
    }

    [Fact]
    public void Take_anotherCard_arrivesAtIt()
    {
        var arrival = new CardArrival();
        arrival.Take(PlanTab.Plan, PlanLens.ByBucket, "split");

        arrival.Take(PlanTab.Plan, PlanLens.ByBucket, "vouchers").ShouldBe("vouchers");
    }
}

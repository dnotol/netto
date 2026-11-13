using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Which sub-tab of Optimise the address arrives at, asked on every set of the page's parameters: the page
// brings that sub-tab's panel into focus once per arrival. Arriving at Optimise on its moves, the view's title
// takes the focus (TabHead), so that is no arrival at a sub-tab.
public class OptimiseArrivalTests
{
    [Theory]
    [InlineData("pay", OptimiseSection.Pay)]
    [InlineData("when", OptimiseSection.When)]
    public void Take_optimiseNamingASubTab_arrivesAtIt(string show, OptimiseSection section)
    {
        new OptimiseArrival().Take(PlanTab.Optimisation, show).ShouldBe(section);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("bogus")]
    public void Take_optimiseOnItsMoves_arrivesAtNoSubTab(string? show)
    {
        new OptimiseArrival().Take(PlanTab.Optimisation, show).ShouldBeNull();
    }

    // The query is Optimise's: it means nothing on another view.
    [Fact]
    public void Take_otherView_arrivesNowhere()
    {
        new OptimiseArrival().Take(PlanTab.Plan, "when").ShouldBeNull();
    }

    // The same address again, as Blazor may set the page's parameters again while it stays there: the viewer
    // has moved on.
    [Fact]
    public void Take_sameAddressAgain_arrivesNowhereMore()
    {
        var arrival = new OptimiseArrival();
        arrival.Take(PlanTab.Optimisation, "when");

        arrival.Take(PlanTab.Optimisation, "when").ShouldBeNull();
    }

    // From one sub-tab to another within Optimise, by Back, Forward or a link: the moves too are arrived at.
    [Theory]
    [InlineData("pay", null, OptimiseSection.Moves)]
    [InlineData("when", "pay", OptimiseSection.Pay)]
    [InlineData(null, "when", OptimiseSection.When)]
    public void Take_anotherSubTabWithinOptimise_arrivesAtIt(string? from, string? to, OptimiseSection section)
    {
        var arrival = new OptimiseArrival();
        arrival.Take(PlanTab.Optimisation, from);

        arrival.Take(PlanTab.Optimisation, to).ShouldBe(section);
    }

    [Fact]
    public void Take_sameLinkAfterAnotherSubTab_arrivesAgain()
    {
        var arrival = new OptimiseArrival();
        arrival.Take(PlanTab.Optimisation, "when");
        arrival.Take(PlanTab.Optimisation, null);

        arrival.Take(PlanTab.Optimisation, "when").ShouldBe(OptimiseSection.When);
    }

    // Back at Optimise from another view, on its moves: the title takes the focus, as on any arrival.
    [Fact]
    public void Take_movesAfterAnotherView_arrivesAtNoSubTab()
    {
        var arrival = new OptimiseArrival();
        arrival.Take(PlanTab.Optimisation, "pay");
        arrival.Take(PlanTab.Plan, null);

        arrival.Take(PlanTab.Optimisation, null).ShouldBeNull();
    }
}

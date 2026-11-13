using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Whether a link arrives at the When to pay section of Optimise (/plan/optimisation?show=when): the page asks
// on every set of its parameters, and brings the section into focus once per arrival.
public class SectionArrivalTests
{
    [Fact]
    public void Take_optimiseNamingTheSection_arrivesAtIt()
    {
        new SectionArrival().Take(PlanTab.Optimisation, "when").ShouldBeTrue();
    }

    [Fact]
    public void Take_optimiseNamingNoSection_arrivesNowhere()
    {
        new SectionArrival().Take(PlanTab.Optimisation, null).ShouldBeFalse();
    }

    // The section is Optimise's: the query means nothing on another view.
    [Theory]
    [InlineData(PlanTab.Plan)]
    public void Take_otherView_arrivesNowhere(PlanTab tab)
    {
        new SectionArrival().Take(tab, "when").ShouldBeFalse();
    }

    // The same address again, as Blazor may set the page's parameters again while it stays there: the
    // viewer has moved on from the section.
    [Fact]
    public void Take_sameAddressAgain_arrivesNowhereMore()
    {
        var arrival = new SectionArrival();
        arrival.Take(PlanTab.Optimisation, "when");

        arrival.Take(PlanTab.Optimisation, "when").ShouldBeFalse();
    }

    [Fact]
    public void Take_sameLinkAfterAnotherViewOrNoSection_arrivesAgain()
    {
        var arrival = new SectionArrival();
        arrival.Take(PlanTab.Optimisation, "when");
        arrival.Take(PlanTab.Optimisation, null);

        arrival.Take(PlanTab.Optimisation, "when").ShouldBeTrue();
    }
}

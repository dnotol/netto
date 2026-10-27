using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The plan page's tabs and the URL of each: /plan for the river, /plan/<segment> for the others, so a
// tab can be opened directly, kept by a reload and reached with back and forward.
public class PlanTabRoutesTests
{
    [Fact]
    public void Order_isThePlanThenOptimisationThenWhenToPayThenCompare()
    {
        PlanTabRoutes.Order.ShouldBe([PlanTab.Plan, PlanTab.Optimisation, PlanTab.WhenToPay, PlanTab.Compare]);
    }

    [Fact]
    public void Order_holdsEveryTabOnce()
    {
        PlanTabRoutes.Order.ShouldBe(Enum.GetValues<PlanTab>(), ignoreOrder: true);
    }

    [Theory]
    [InlineData(PlanTab.Plan, "plan")]
    [InlineData(PlanTab.Optimisation, "plan/optimisation")]
    [InlineData(PlanTab.WhenToPay, "plan/when-to-pay")]
    [InlineData(PlanTab.Compare, "plan/compare")]
    public void Href_eachTab_isItsUrlFromTheBase(PlanTab tab, string href)
    {
        PlanTabRoutes.Href(tab).ShouldBe(href);
    }

    // /plan has no segment: the river.
    [Fact]
    public void From_noSegment_isThePlan()
    {
        PlanTabRoutes.From(null).ShouldBe(PlanTab.Plan);
    }

    [Fact]
    public void From_eachTabsOwnSegment_isThatTab()
    {
        PlanTabRoutes.Order.Select(tab => PlanTabRoutes.From(Segment(tab))).ShouldBe(PlanTabRoutes.Order.Cast<PlanTab?>());
    }

    // A URL typed by hand keeps its case: the router matches it whatever the case, and so does the tab.
    [Theory]
    [InlineData("OPTIMISATION", PlanTab.Optimisation)]
    [InlineData("When-To-Pay", PlanTab.WhenToPay)]
    public void From_segmentInAnotherCase_isTheTab(string segment, PlanTab tab)
    {
        PlanTabRoutes.From(segment).ShouldBe(tab);
    }

    // Any other segment is a page Netto does not have, the plan's own name and the map's, which is the
    // plan since 2026-10-27, included.
    [Theory]
    [InlineData("plan")]
    [InlineData("map")]
    [InlineData("optimization")]
    [InlineData("")]
    public void From_anyOtherSegment_isNone(string segment)
    {
        PlanTabRoutes.From(segment).ShouldBeNull();
    }

    // The segment after /plan/, none for the river.
    private static string? Segment(PlanTab tab) => PlanTabRoutes.Href(tab) is var href && href.Contains('/') ? href[(href.IndexOf('/') + 1)..] : null;
}

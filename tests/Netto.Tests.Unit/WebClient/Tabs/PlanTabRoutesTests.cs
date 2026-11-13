using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The plan page's tabs and the URL of each: /plan for the river, /plan/<segment> for the others, so a
// tab can be opened directly, kept by a reload and reached with back and forward.
public class PlanTabRoutesTests
{
    [Fact]
    public void Order_isMyPlanThenOptimisation()
    {
        PlanTabRoutes.Order.ShouldBe([PlanTab.Plan, PlanTab.Optimisation]);
    }

    [Fact]
    public void Order_holdsEveryTabOnce()
    {
        PlanTabRoutes.Order.ShouldBe(Enum.GetValues<PlanTab>(), ignoreOrder: true);
    }

    [Theory]
    [InlineData(PlanTab.Plan, "plan")]
    [InlineData(PlanTab.Optimisation, "plan/optimisation")]
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
    [InlineData("when-to-pay")]
    [InlineData("compare")]
    [InlineData("pistes")]
    [InlineData("PISTES")]
    [InlineData("")]
    public void From_anyOtherSegment_isNone(string segment)
    {
        PlanTabRoutes.From(segment).ShouldBeNull();
    }

    // The plan page's own addresses, as NavigationManager.ToBaseRelativePath gives them: the router matches
    // whatever their case and ignores a trailing slash, a query or a fragment.
    [Theory]
    [InlineData("plan")]
    [InlineData("Plan/")]
    [InlineData("plan/optimisation")]
    [InlineData("plan/Pistes/")]
    [InlineData("plan/pistes")]
    [InlineData("plan?by=date")]
    [InlineData("plan/optimisation?show=when")]
    [InlineData("plan/when-to-pay")]
    [InlineData("Plan/Compare/")]
    [InlineData("plan?card=split")]
    [InlineData("plan/optimisation#tab-content")]
    public void IsAt_aTabsAddress_isThePlanPage(string path)
    {
        PlanTabRoutes.IsAt(path).ShouldBeTrue();
    }

    // A segment Netto has no tab for is the page not found, drawn at that address; the front page and the
    // root, which replaces itself with /plan, are pages of their own.
    [Theory]
    [InlineData("")]
    [InlineData("about")]
    [InlineData("plan/map")]
    [InlineData("plan/optimisation/more")]
    [InlineData("planner")]
    [InlineData("not-found")]
    public void IsAt_anyOtherAddress_isNot(string path)
    {
        PlanTabRoutes.IsAt(path).ShouldBeFalse();
    }

    // The addresses the plan page once had for the views it folded in, and where each now is, as a link
    // writes it: the When to pay section of Optimise, and the Plans panel opened on its compare mode.
    [Theory]
    [InlineData("when-to-pay", "plan/optimisation?show=when")]
    [InlineData("When-To-Pay", "plan/optimisation?show=when")]
    [InlineData("compare", "plan?plans=compare")]
    [InlineData("COMPARE", "plan?plans=compare")]
    [InlineData("pistes", "plan")]
    [InlineData("Pistes", "plan")]
    public void Moved_aFoldedViewsOldSegment_isItsNewPlace(string segment, string href)
    {
        PlanTabRoutes.Moved(segment).ShouldBe(href);
    }

    [Theory]
    [InlineData("optimisation")]
    [InlineData("when")]
    [InlineData("")]
    [InlineData(null)]
    public void Moved_anyOtherSegment_isNone(string? segment)
    {
        PlanTabRoutes.Moved(segment).ShouldBeNull();
    }

    // When to pay is a section of Optimise, named by a query as a card is (plan?card=), never a fragment.
    [Fact]
    public void WhenToPayHref_isTheOptimiseViewAskingForTheSection()
    {
        PlanTabRoutes.WhenToPayHref.ShouldBe("plan/optimisation?show=when");
    }

    [Theory]
    [InlineData("when", true)]
    [InlineData("WHEN", true)]
    [InlineData("nonsense", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void OpensWhenToPay_aQueryValue_isWhetherItNamesTheSection(string? show, bool opens)
    {
        PlanTabRoutes.OpensWhenToPay(show).ShouldBe(opens);
    }

    // A bookmark to My plan read by date goes to the section; any other value of the old query stays My plan.
    [Theory]
    [InlineData("date", "plan/optimisation?show=when")]
    [InlineData("DATE", "plan/optimisation?show=when")]
    [InlineData("bucket", null)]
    [InlineData("nonsense", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void MovedFromLens_aQueryValue_isTheSectionForByDateAndNothingElse(string? by, string? href)
    {
        PlanTabRoutes.MovedFromLens(by).ShouldBe(href);
    }

    // The segment after /plan/, none for the river.
    private static string? Segment(PlanTab tab) => PlanTabRoutes.Href(tab) is var href && href.Contains('/') ? href[(href.IndexOf('/') + 1)..] : null;
}

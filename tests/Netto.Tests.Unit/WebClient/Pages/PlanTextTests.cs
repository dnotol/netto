using System.Globalization;
using Netto.Web.Client.Pages;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The river page's texts that a test holds to its .resx: a misspelt key would show on screen as the
// key itself.
public class PlanTextTests
{
    // The name IStringLocalizer<Plan> reads Pages/Plan.resx under.
    private static readonly ResxText Resources = ResxText.For<Plan>();

    // The keyboard's first stop on the page, past the bar and its tabs to the tab's own content: the map, the
    // moves.
    [Theory]
    [InlineData(PlanTab.Plan, "Skip to the map")]
    [InlineData(PlanTab.Optimisation, "Skip to your moves")]
    public void PlanResx_skipLink_saysWhereItGoesOnEachView(PlanTab tab, string expected)
    {
        Resources.Get(PlanText.SkipTo(tab)).ShouldBe(expected);
    }

    // The window's title names the view, as a new page would.
    [Theory]
    [InlineData(PlanTab.Plan, "My plan: Netto")]
    [InlineData(PlanTab.Optimisation, "Optimise: Netto")]
    public void PlanResx_pageTitle_namesEachView(PlanTab tab, string expected)
    {
        Resources.Get(PlanText.PageTitle(tab)).ShouldBe(expected);
    }

    // A view added later fails here until it has both, in English and in French.
    [Fact]
    public void PlanResx_everyView_hasATitleAndASkipLinkInBothLanguages()
    {
        var french = ResxText.French<Plan>();
        var keys = Enum.GetValues<PlanTab>()
            .SelectMany(tab => new[] { PlanText.PageTitle(tab), PlanText.SkipTo(tab) })
            .Append(PlanText.MyPlanTitle).Distinct();

        keys.Where(key => string.IsNullOrWhiteSpace(Resources.Get(key)) || string.IsNullOrWhiteSpace(french.Get(key))).ShouldBeEmpty();
    }

    // The head of My plan, the page's h1, drawn by the page.
    [Fact]
    public void PlanResx_myPlanTitle_isTheViewsName()
    {
        (Resources.Get(PlanText.MyPlanTitle), ResxText.French<Plan>().Get(PlanText.MyPlanTitle)).ShouldBe(("My plan", "Mon plan"));
    }

    // The two notes under the river fold to one line that counts what they hold, from the lists the
    // page draws, so a line added to a list is counted and a key missing from the .resx fails here.
    [Fact]
    public void PlanResx_assumptions_areEveryAssumedLineOfTheResx()
    {
        var inResx = ResxKeys("Assumed").Except([PlanText.AssumedTitle, PlanText.AssumedSummary]);

        PlanText.Assumptions.ShouldBe(inResx, ignoreOrder: true);
    }

    // The short, general assumptions first, the long one about a partner's withholding last.
    [Fact]
    public void Assumptions_openOnWalloniaAndEndOnThePartnersWithholding()
    {
        PlanText.Assumptions.ShouldBe([
            "AssumedWallonia", "AssumedMainOccupation", "AssumedNoSpecialContribution", "AssumedLowIncomeCredit",
            "AssumedCalendarYear", "AssumedSteadyState", "AssumedFinalContributions", "AssumedPartnerWithholding",
        ]);
    }

    [Fact]
    public void PlanResx_limits_areEveryLimitLineOfTheResxButTheNote()
    {
        var inResx = ResxKeys("Limit").Except([PlanText.LimitsTitle, PlanText.LimitsSummary, PlanText.LimitTaxCalcNote]);

        PlanText.Limits.ShouldBe(inResx, ignoreOrder: true);
    }

    [Fact]
    public void PlanResx_everyLineOfTheNotes_hasItsText()
    {
        PlanText.Assumptions.Concat(PlanText.Limits).Concat([PlanText.LimitTaxCalcNote])
            .Where(key => string.IsNullOrWhiteSpace(Resources.Get(key)))
            .ShouldBeEmpty();
    }

    // Where a viewer's fold of each note is remembered: kept from one version to the next, or the
    // folds are forgotten.
    [Fact]
    public void NoteFolds_keepTheirNames()
    {
        (PlanText.AssumedFold, PlanText.LimitsFold).ShouldBe(("assumed", "not-counted"));
    }

    [Fact]
    public void PlanResx_assumedSummary_countsTheAssumptions()
    {
        var summary = Resources.Format(PlanText.AssumedSummary, PlanText.Assumptions.Count);

        summary.ShouldBe($"{PlanText.Assumptions.Count} assumptions Netto makes");
    }

    [Fact]
    public void PlanResx_limitsSummary_countsWhatIsNotCounted()
    {
        var summary = Resources.Format(PlanText.LimitsSummary, PlanText.Limits.Count);

        summary.ShouldBe($"{PlanText.Limits.Count} things the figures leave out");
    }

    // The household is a module of the plan, partner and dependants: the notes say what Netto assumes while it
    // is out, how the withholding takes the partner while in, and what of the partner's own and of the
    // dependants the figures leave out; the partner's limit no longer says the dependants are left out.
    [Fact]
    public void PlanResx_notes_sayWhatTheyAssumeAndLeaveOutOfTheHousehold()
    {
        Resources.ShouldSatisfyAllConditions(
            () => PlanText.Assumptions.ShouldNotContain("AssumedSingle"),
            () => Resources.Get("AssumedSingle").ShouldBeNull(),
            () => ResxText.French<Plan>().Get("AssumedSingle").ShouldBeNull(),
            () => PlanText.Assumptions.ShouldContain("AssumedPartnerWithholding"),
            () => Resources.Get("AssumedPartnerWithholding").ShouldNotBeNull().ShouldContain("dependants"),
            () => Resources.Get("AssumedPartnerWithholding").ShouldNotBeNull().ShouldStartWith("With a partner ticked in your household"),
            () => PlanText.Limits.ShouldContain("LimitPartner"),
            () => Resources.Get("LimitPartner").ShouldNotBeNull().ShouldNotContain("dependants"),
            () => PlanText.Limits.ShouldContain("LimitDependants"));
    }

    // The company car's non-deductible share is a disallowed expense the figures do count, as the vouchers' is: the
    // line leaves out only what no lever counts.
    [Fact]
    public void PlanResx_limitDisallowed_namesTheCompanyCarsNonDeductibleShareAmongWhatIsCounted()
    {
        (Resources.Get("LimitDisallowed"), ResxText.French<Plan>().Get("LimitDisallowed")).ShouldBe((
            "Disallowed expenses other than the vouchers' and the company car's non-deductible share, and losses carried forward from earlier years.",
            "Les dépenses non admises autres que celles des titres-repas et chèques et que la part non déductible de la voiture de société, et les pertes reportées des années précédentes."));
    }

    private static IEnumerable<string> ResxKeys(string prefix) =>
        Resources.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal));

    // The cards under the split say what runs down each side: no line between them says it again.
    [Theory]
    [InlineData("Fork")]
    [InlineData("Fork_Royalty")]
    public void PlanResx_forkUnderTheSplit_isGone(string key)
    {
        Resources.Get(key).ShouldBeNull();
    }
}

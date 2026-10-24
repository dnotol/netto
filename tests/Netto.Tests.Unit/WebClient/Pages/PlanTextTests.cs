using System.Globalization;
using System.Text.RegularExpressions;
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

    // The keyboard's first stop on the page, past the bar and its tabs to the tab's own content.
    [Theory]
    [InlineData(PlanTab.Plan, "Skip to the river")]
    [InlineData(PlanTab.Optimisation, "Skip to your moves")]
    [InlineData(PlanTab.WhenToPay, "Skip to the payments")]
    [InlineData(PlanTab.Compare, "Skip to the comparison")]
    public void PlanResx_skipLink_saysWhereItGoesOnEachTab(PlanTab tab, string expected)
    {
        Resources.Get(PlanText.SkipTo(tab)).ShouldBe(expected);
    }

    // The window's title names the tab, as a new page would.
    [Theory]
    [InlineData(PlanTab.Plan, "Your year: Netto")]
    [InlineData(PlanTab.Optimisation, "Optimisation: Netto")]
    [InlineData(PlanTab.WhenToPay, "When to pay: Netto")]
    [InlineData(PlanTab.Compare, "Compare plans: Netto")]
    public void PlanResx_pageTitle_namesEachTab(PlanTab tab, string expected)
    {
        Resources.Get(PlanText.PageTitle(tab)).ShouldBe(expected);
    }

    // A tab added later fails here until it has both.
    [Fact]
    public void PlanResx_everyTab_hasATitleAndASkipLink()
    {
        Enum.GetValues<PlanTab>()
            .SelectMany(tab => new[] { PlanText.PageTitle(tab), PlanText.SkipTo(tab) })
            .Where(key => string.IsNullOrWhiteSpace(Resources.Get(key)))
            .ShouldBeEmpty();
    }

    // The two notes under the river fold to one line that counts what they hold, from the lists the
    // page draws, so a line added to a list is counted and a key missing from the .resx fails here.
    [Fact]
    public void PlanResx_assumptions_areEveryAssumedLineOfTheResx()
    {
        var inResx = ResxKeys("Assumed").Except([PlanText.AssumedTitle, PlanText.AssumedSummary]);

        PlanText.Assumptions.ShouldBe(inResx, ignoreOrder: true);
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

    private static IEnumerable<string> ResxKeys(string prefix) =>
        Resources.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal));

    // The river's lede says what the cards and the bar do, in two sentences at most: how a card opens is
    // its chevron's to say, and where the example comes from is Company year's, in its bubble.
    [Fact]
    public void PlanResx_lede_isAtMostTwoSentences()
    {
        Sentences(Text("Lede")).Count.ShouldBeLessThanOrEqualTo(2);
    }

    [Theory]
    [InlineData("fold")]
    [InlineData("open")]
    [InlineData("example")]
    public void PlanResx_lede_namesNeitherHowACardOpensNorTheExample(string word)
    {
        Text("Lede").ShouldNotContain(word, Case.Insensitive);
    }

    // Half the 66 words it held before the story.
    [Fact]
    public void PlanResx_lede_isAtMostHalfItsWords()
    {
        Words(Text("Lede")).ShouldBeLessThanOrEqualTo(33);
    }

    // While the plan holds no optional module, one short line under Company year names them, its button
    // saying where they are added.
    [Fact]
    public void PlanResx_addHint_isAtMostTwelveWords()
    {
        Words(Text("AddHint")).ShouldBeLessThanOrEqualTo(12);
    }

    // The cards under the split say what runs down each side: no line between them says it again.
    [Theory]
    [InlineData("Fork")]
    [InlineData("Fork_Royalty")]
    public void PlanResx_forkUnderTheSplit_isGone(string key)
    {
        Resources.Get(key).ShouldBeNull();
    }

    private static string Text(string key) => Resources.Get(key) ?? "";

    // A sentence ends on its full stop, or the text's end.
    private static List<string> Sentences(string text) => [.. Regex.Split(text.Trim(), @"(?<=[.!?])\s+").Where(sentence => sentence.Length > 0)];

    private static int Words(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(word => word.Any(char.IsLetterOrDigit));
}

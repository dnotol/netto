using System.Globalization;
using System.Resources;
using Netto.Web.Client.Pages;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The river page's texts that a test holds to its .resx: a misspelt key would show on screen as the
// key itself.
public class PlanTextTests
{
    // The name IStringLocalizer<Plan> reads Pages/Plan.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Pages.Plan", typeof(Plan).Assembly);

    // The keyboard's first stop on the page.
    [Fact]
    public void PlanResx_skipLink_saysWhereItGoes()
    {
        var text = Resources.GetString(PlanText.SkipToRiver, CultureInfo.InvariantCulture);

        text.ShouldBe("Skip to the river");
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
            .Where(key => string.IsNullOrWhiteSpace(Resources.GetString(key, CultureInfo.InvariantCulture)))
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
        var summary = string.Format(CultureInfo.InvariantCulture, Resources.GetString(PlanText.AssumedSummary, CultureInfo.InvariantCulture)!, PlanText.Assumptions.Count);

        summary.ShouldBe($"{PlanText.Assumptions.Count} assumptions Netto makes");
    }

    [Fact]
    public void PlanResx_limitsSummary_countsWhatIsNotCounted()
    {
        var summary = string.Format(CultureInfo.InvariantCulture, Resources.GetString(PlanText.LimitsSummary, CultureInfo.InvariantCulture)!, PlanText.Limits.Count);

        summary.ShouldBe($"{PlanText.Limits.Count} things the figures leave out");
    }

    private static IEnumerable<string> ResxKeys(string prefix) =>
        Resources.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)!
            .Cast<System.Collections.DictionaryEntry>()
            .Select(entry => (string)entry.Key)
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal));

    // Under the split, the royalty runs down the director's side with the pay once it is in the plan.
    [Theory]
    [InlineData(false, "↓ Your pay on one side, the company's profit on the other ↓")]
    [InlineData(true, "↓ Your pay and royalty on one side, the company's profit on the other ↓")]
    public void PlanResx_fork_saysWhatRunsDownEachSide(bool royalty, string expected)
    {
        Resources.GetString(PlanText.Fork(royalty), CultureInfo.InvariantCulture).ShouldBe(expected);
    }
}

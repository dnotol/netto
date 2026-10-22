using System.Globalization;
using System.Resources;
using Netto.Core.Copyright;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River.Copyright;

// The copyright card builds resource keys from the royalty and what it compares. A key without a text
// in its .resx would show on screen as the key itself.
public class CopyrightCardTextTests
{
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.Copyright.CopyrightCard", typeof(CopyrightCard).Assembly);

    [Fact]
    public void CopyrightCardResx_everyLimitThatSetsThePartWithin_hasAText()
    {
        Missing(Enum.GetValues<CopyrightBound>().Select(bound => CopyrightCardText.Limits(CopyrightResult.None with { Bound = bound }))).ShouldBeEmpty();
    }

    // No royalty yet, a royalty within its limits, and one beyond them.
    [Fact]
    public void CopyrightCardResx_everySummary_hasAText()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright);
        var views = new[] { inputs, inputs.WithRoyalty(10_000m), inputs.WithRoyalty(30_000m) }.Select(river => CopyrightView.From(river.Run().Year!));

        views.Select(CopyrightCardText.Summary).ShouldBe(["SummaryNoRoyalty", "Summary", "Summary_AsPay"]);
    }

    [Fact]
    public void CopyrightCardResx_everySummaryKey_hasAText()
    {
        Missing(["SummaryNoRoyalty", "Summary", "Summary_AsPay"]).ShouldBeEmpty();
    }

    // What the royalty leaves against the plan without it, each way it can fall.
    [Fact]
    public void CopyrightCardResx_everyGain_hasAText()
    {
        var comparisons = new[]
        {
            new CopyrightComparison(40_000m, 38_000m, 0m, 0m, 0m, 0m), new CopyrightComparison(38_000m, 40_000m, 0m, 0m, 0m, 0m),
            new CopyrightComparison(40_000m, 40_000m, 0m, 0m, 0m, 0m),
        };

        comparisons.Select(CopyrightCardText.Gain).ShouldBe(["Gain_More", "Gain_Less", "Gain_None"]);
        Missing(comparisons.Select(CopyrightCardText.Gain)).ShouldBeEmpty();
    }

    // The words the card says whatever the royalty.
    [Fact]
    public void CopyrightCardResx_everyFixedText_hasAText()
    {
        string[] keys =
        [
            "Title", "Hint", "Waiting", "Scope", "RoyaltyLabel", "RoyaltyHint", "AverageLabel", "AverageHint", "NoRoyaltyYet", "TaxedApart", "OnTheGross",
            "TaxedAsPay", "Readings", "ComparisonTitle", "WithRoyalty", "WithoutRoyalty", "DetailsLabel", "SourcesTitle", "Remove",
        ];

        Missing(keys).ShouldBeEmpty();
    }

    private static IEnumerable<string> Missing(IEnumerable<string> keys) => keys.Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}

using Netto.Core.Copyright;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River.Copyright;

// The copyright card builds resource keys from the royalty, what it compares and the lines it draws. A key
// without a text in its .resx would show on screen as the key itself.
public class CopyrightCardTextTests
{
    private static readonly ResxText Resources = ResxText.For<CopyrightCard>();

    private static readonly CopyrightRules Rules = CopyrightRules.IncomeYear2026;

    [Fact]
    public void CopyrightCardResx_everyLimitThatSetsThePartWithin_hasAText()
    {
        Resources.Missing(Enum.GetValues<CopyrightBound>().Select(bound => CopyrightCardText.Limits(CopyrightResult.None with { Bound = bound }))).ShouldBeEmpty();
    }

    // No royalty yet, a royalty within its limits, one beyond them, and one whose four previous years
    // averaged above the cap.
    [Fact]
    public void CopyrightCardResx_everySummary_hasAText()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright);
        var views = new[] { inputs, inputs.WithRoyalty(10_000m), inputs.WithRoyalty(30_000m), inputs.WithRoyalty(10_000m).WithAverageWithinCap(false) }
            .Select(river => CopyrightView.From(river.Run().Year!));

        views.Select(CopyrightCardText.Summary).ShouldBe(["SummaryNoRoyalty", "Summary", "Summary_AsPay", "Summary_AllAsPay"]);
    }

    // The card's row in the river's outline glances at the summary's first figure, in its words: what
    // reaches the pocket after the tax apart, nothing before there is a royalty.
    [Fact]
    public void Glance_everySummary_isItsFirstFigure()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright);
        var views = new[] { inputs, inputs.WithRoyalty(10_000m), inputs.WithRoyalty(30_000m) }.Select(river => CopyrightView.From(river.Run().Year!)).ToList();

        views.Select(CopyrightCardText.Glance).ShouldBe(
        [
            null,
            new CardGlance(GlanceKind.ToYou, Netto.Web.Client.Formats.Euros(views[1].AfterTax)),
            new CardGlance(GlanceKind.ToYou, Netto.Web.Client.Formats.Euros(views[2].AfterTax)),
        ]);
    }

    // Four previous years above the cap leave nothing within the limits: the summary says all of it is
    // taxed as pay, and so does the glance, with the summary's figure and no pocket's icon, since it
    // reaches the pocket only through the pay.
    [Fact]
    public void Glance_nothingWithinTheLimits_isAllOfItTaxedAsPay()
    {
        var view = CopyrightView.From(RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m).WithAverageWithinCap(false).Run().Year!);
        var summary = Resources.Format(CopyrightCardText.Summary(view),
            Netto.Web.Client.Formats.Euros(view.AfterTax), Netto.Web.Client.Formats.Euros(view.Result.TaxedAsPay));

        var glance = CopyrightCardText.Glance(view).ShouldNotBeNull();

        (glance.Kind, summary.Contains(glance.Figure), glance.Bucket).ShouldBe((GlanceKind.TaxedAsPay, true, (string?)null));
    }

    [Fact]
    public void CopyrightCardResx_everySummaryKey_hasAText()
    {
        Resources.Missing(["SummaryNoRoyalty", "Summary", "Summary_AsPay", "Summary_AllAsPay"]).ShouldBeEmpty();
    }

    // Figures with their labels: what reaches the pocket after the tax apart, then what is taxed as pay
    // once there is some, or all of it taxed as pay.
    [Theory]
    [InlineData("SummaryNoRoyalty", "")]
    [InlineData("Summary", "€8,000.00")]
    [InlineData("Summary_AsPay", "€8,000.00 €2,000.00")]
    [InlineData("Summary_AllAsPay", "€2,000.00")]
    public void Summary_eachWay_isItsFiguresWithTheirLabels(string key, string amounts)
    {
        var summary = Resources.Format(key, "€8,000.00", "€2,000.00");

        summary.ShouldSatisfyAllConditions(
            () => string.Join(' ', CardHtml.Amounts(summary)).ShouldBe(amounts),
            () => CardHtml.SummaryFaults(summary).ShouldBeEmpty());
    }

    // What the royalty leaves against the plan without it, each way it can fall.
    [Fact]
    public void CopyrightCardResx_everyDifferenceLine_hasALabel()
    {
        var comparisons = new[]
        {
            new CopyrightComparison(40_000m, 38_000m, 0m, 0m, 0m, 0m), new CopyrightComparison(38_000m, 40_000m, 0m, 0m, 0m, 0m),
            new CopyrightComparison(40_000m, 40_000m, 0m, 0m, 0m, 0m),
        };

        comparisons.Select(CopyrightCardText.Gain).ShouldBe(["Figure_Gain_More", "Figure_Gain_Less", "Figure_Gain_None"]);
        Resources.Missing(comparisons.Select(CopyrightCardText.Gain)).ShouldBeEmpty();
    }

    // Every line the royalty's figures draw, and what its bubble says, on royalties that take each of them:
    // within its limits, beyond them, with an attestation, all taxed as pay; and the plan with it and
    // without it.
    [Fact]
    public void CopyrightCardResx_everyLineOfTheFiguresAndItsBubble_hasAText()
    {
        CopyrightRoyalty[] royalties =
        [
            new(10_000m), new(20_000m), new(10_000m) { HoldsAttestation = true }, new(10_000m) { AverageWithinCap = false },
        ];
        var comparison = new CopyrightComparison(40_000m, 38_000m, 20_000m, 18_000m, 10_000m, 14_000m);
        var lines = royalties.SelectMany(royalty => CopyrightFigures.Royalty(CopyrightCalculator.Compute(Rules, royalty, 30_000m).Value.ShouldNotBeNull(), Rules))
            .Concat(CopyrightFigures.With(comparison)).Concat(CopyrightFigures.Without(comparison)).Concat(CopyrightFigures.Difference(comparison))
            .ToList();

        Resources.Missing(lines.Select(line => line.Key).Concat(lines.Select(line => line.Help).OfType<string>()).Distinct()).ShouldBeEmpty();
    }

    // The words the card says whatever the royalty.
    [Fact]
    public void CopyrightCardResx_everyFixedText_hasAText()
    {
        string[] keys =
        [
            "Title", "Hint", "AverageLabel", "AverageHint", "AttestationLabel", "AttestationHint", "CreativeShareLabel", "CreativeShareHint", "NoRoyaltyYet",
            "Readings", "WithTitle", "WithoutTitle", "Remove",
        ];

        Resources.Missing(keys).ShouldBeEmpty();
    }

    // A royalty all within its limits says the 30% limit with its amount, a bound of the royalty that stays
    // in sight; the bubble of « Within the limits » explains it whole.
    [Fact]
    public void Limits_allWithin_saysThe30PercentLimitWithItsAmount()
    {
        var limits = Resources.Format("Limits_None",
            "€10,000.00", "€10,000.00", "€18,000.00", "€77,220.00", "€0.00", 2026, "30%");

        CardHtml.Amounts(limits).ShouldBe(["30%", "€18,000.00"]);
    }

    // A rate the rules set is never typed in a text that states it: the rule's own is passed in.
    [Theory]
    [InlineData("Limits_None")]
    [InlineData("Limits_Relative")]
    [InlineData("Help_WithinLimits")]
    [InlineData("Help_FlatRateCosts")]
    [InlineData("Figure_Withheld")]
    public void CopyrightCardResx_textStatingARulesRate_typesNone(string key)
    {
        Resources.Get(key).ShouldNotBeNull().ShouldNotMatch("[0-9]%");
    }

    // The line of « Your copyright situation »: the statuses as assumed and as said, and the share
    // given, needed by a royalty that follows the pay, or not given beside a fixed one.
    [Fact]
    public void CopyrightCardResx_everyPhraseOfItsSettingsLine_hasAText()
    {
        var royalty = RiverInputs.Example.AddToPlan(OptionalModule.Copyright);
        RiverInputs[] variations = [royalty, royalty.WithRoyalty(10_000m), royalty.WithAverageWithinCap(false).WithAttestation(true).WithCreativeShare(0.4m)];
        var phrases = variations.SelectMany(inputs => CopyrightSettingsView.From(inputs, inputs.Run()).Situation);

        Resources.Missing(phrases.Select(phrase => phrase.Key).Append("SituationTitle")).ShouldBeEmpty();
    }
}

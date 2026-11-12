using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;
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

    private static readonly IStringLocalizer Texts = new CardTexts();

    [Fact]
    public void CopyrightCardResx_everyLimitThatSetsThePartWithin_hasAText()
    {
        Resources.Missing(Enum.GetValues<CopyrightBound>().Select(bound => CopyrightCardText.Limits(CopyrightResult.None with { Bound = bound }))).ShouldBeEmpty();
    }

    // No royalty yet, a royalty within its limits, one beyond them, and one whose four previous years
    // averaged above the cap.
    // The summary says what the royalty adds to the pocket or takes from it, the plan with it less the
    // plan without, or that there is no royalty yet: one all taxed as pay, its four previous years above the
    // cap on average, leaves less than the company keeping it as profit would.
    [Fact]
    public void CopyrightCardResx_everySummary_hasAText()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright);
        var keys = new[] { inputs, inputs.WithRoyalty(10_000m), inputs.WithRoyalty(30_000m), inputs.WithRoyalty(10_000m).WithAverageWithinCap(false) }
            .Select(river => CopyrightCardText.Summary(CopyrightView.From(river.Run().Year!), LeverGain.Of(river, river.Run(), OptionalModule.Copyright)!.Gain))
            .ToList();

        keys.ShouldSatisfyAllConditions(
            () => keys.ShouldBe(["SummaryNoRoyalty", "Summary_More", "Summary_More", "Summary_Less"]),
            () => Resources.Missing(["SummaryNoRoyalty", "Summary_More", "Summary_Less", "Summary_None"]).ShouldBeEmpty());
    }

    // Figures with their labels: what the royalty adds to the pocket or takes from it, or none.
    [Theory]
    [InlineData("SummaryNoRoyalty", "")]
    [InlineData("Summary_More", "€8,000.00")]
    [InlineData("Summary_Less", "€8,000.00")]
    [InlineData("Summary_None", "")]
    public void Summary_eachWay_isItsFiguresWithTheirLabels(string key, string amounts)
    {
        var summary = Resources.Format(key, "€8,000.00");

        summary.ShouldSatisfyAllConditions(
            () => string.Join(' ', CardHtml.Amounts(summary)).ShouldBe(amounts),
            () => CardHtml.SummaryFaults(summary).ShouldBeEmpty());
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
            .Concat(CopyrightFigures.With(comparison)).Concat(CopyrightFigures.Without(comparison))
            .ToList();

        Resources.Missing(lines.Select(line => line.Key).Concat(lines.Select(line => line.Help).OfType<string>()).Distinct()).ShouldBeEmpty();
    }

    // The note under which the comparison runs, said only while the pay set is held below: it names the
    // one pay both plans run at, and no rate the rules set.
    [Fact]
    public void PayHeld_note_namesTheOnePayBothPlansRunAt()
    {
        Resources.Missing(["PayHeld"]).ShouldBeEmpty();

        var note = Resources.Format("PayHeld", "€81,250.00");

        note.ShouldSatisfyAllConditions(
            () => CardHtml.Amounts(note).ShouldBe(["€81,250.00"]),
            () => note.ShouldNotMatch("[0-9]%"));
    }

    // The words the card says whatever the royalty.
    [Fact]
    public void CopyrightCardResx_everyFixedText_hasAText()
    {
        string[] keys =
        [
            "Title", "Hint", "AverageLabel", "AverageHint", "AttestationLabel", "AttestationHint", "CreativeShareLabel", "CreativeShareHint", "NoRoyaltyYet",
            "Readings", "WithTitle", "WithoutTitle", "Remove", "PolicyLabel", "PolicyHint", "FixedRoyaltyLabel", "FixedRoyaltyHint", "YearsRate",
        ];

        Resources.Missing(keys).ShouldBeEmpty();
    }

    // How the royalty is decided, on its card: its two ways, one chip each, and why a royalty that follows
    // the pay is none.
    [Fact]
    public void CopyrightCardResx_everyTextOfHowTheRoyaltyIsDecided_hasAText()
    {
        Resources.Missing([CopyrightCardText.Policy(true), CopyrightCardText.Policy(false), .. Enum.GetValues<CopyrightFollowing>().Select(CopyrightCardText.Following).OfType<string>()])
            .ShouldBeEmpty();
    }

    [Fact]
    public void Policy_eachWay_isItsChip()
    {
        (CopyrightCardText.Policy(true), CopyrightCardText.Policy(false)).ShouldBe(("Policy_Follows", "Policy_Fixed"));
    }

    // A royalty that follows the pay says why it is none; set at what a ruling accepts, its figure is among
    // the card's lines, and no sentence says it again.
    [Theory]
    [InlineData(CopyrightFollowing.AtCeiling, null)]
    [InlineData(CopyrightFollowing.NoCreativeShare, "Following_NoCreativeShare")]
    [InlineData(CopyrightFollowing.AverageAboveCap, "Following_AverageAboveCap")]
    [InlineData(CopyrightFollowing.NoEnvelopeRate, "Following_NoEnvelopeRate")]
    public void Following_saysOnlyWhyTheRoyaltyIsNone(CopyrightFollowing following, string? key)
    {
        CopyrightCardText.Following(following).ShouldBe(key);
    }

    // The sentence reads the income year alone, its placeholders counted from 0.
    [Fact]
    public void CopyrightCardResx_followingSentence_readsOnlyTheIncomeYear()
    {
        InterfaceTexts.Of(typeof(CopyrightCard)).Where(text => text.Key.StartsWith("Following_", StringComparison.Ordinal) && InterfaceTexts.Placeholders(text.Value).Any(index => index >= 1))
            .Select(text => text.Key).ShouldBeEmpty();
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

    // A rate the rules set is never typed in a text of the card, a hint or a sentence: the rule's own is
    // passed in (Rate).
    [Fact]
    public void CopyrightCardResx_everyText_typesNoPercentage()
    {
        Resources.Entries.Where(text => Regex.IsMatch(text.Value, "[0-9]%")).Select(text => text.Key).ShouldBeEmpty();
    }

    // A rate a text states is the rules' own as the card words it; where the year has none, or no rules, the
    // year's rate, as the cap is « the year's cap ».
    [Fact]
    public void Rate_aRuleAmount_isItsOwnPercentage()
    {
        (CopyrightCardText.Rate(Texts, Rules.EnvelopeRate), CopyrightCardText.Rate(Texts, Rules.WithholdingRate), CopyrightCardText.Rate(Texts, Rules.TaxedApartRate),
                CopyrightCardText.Rate(Texts, Rules.RelativeLimit))
            .ShouldBe(("25%", "15%", "15%", "30%"));
    }

    // The words come from the card's .resx (YearsRate), never from the helper.
    [Fact]
    public void Rate_noRateForTheYear_isTheYearsRateOfTheResx()
    {
        var yearsRate = Resources.Get("YearsRate").ShouldNotBeNull();

        (CopyrightCardText.Rate(Texts, CopyrightRules.IncomeYear2025.EnvelopeRate), CopyrightCardText.Rate(Texts, null)).ShouldBe((yearsRate, yearsRate));
        yearsRate.ShouldBe("the year's rate");
    }

    // The placeholders each text reads, counted from 0: the card passes one argument for each.
    [Theory]
    [InlineData("Hint", 2)]
    [InlineData("PolicyHint", 1)]
    [InlineData("FixedRoyaltyHint", 1)]
    [InlineData("CreativeShareHint", 1)]
    [InlineData("Readings", 3)]
    public void CopyrightCardResx_textStatingRulesRates_readsOnePlaceholderForEachArgument(string key, int arguments)
    {
        InterfaceTexts.Placeholders(Resources.Get(key).ShouldNotBeNull()).Distinct().Order().ShouldBe(Enumerable.Range(0, arguments));
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

    // The card's texts, read from its .resx as IStringLocalizer<CopyrightCard> reads them.
    private sealed class CardTexts : IStringLocalizer
    {
        public LocalizedString this[string name] =>
            Resources.Get(name) is { } text ? new(name, text) : new(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] =>
            Resources.Get(name) is { } text ? new(name, string.Format(CultureInfo.InvariantCulture, text, arguments)) : this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Resources.Entries.Select(entry => new LocalizedString(entry.Key, entry.Value));
    }
}

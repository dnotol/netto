using Netto.Core.Copyright;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River.Copyright;

// The royalty's figures, line by line, as its card shows them before where its money goes
// (CopyrightFigures): keys of the card's texts with their amounts and their labels' arguments, on
// CopyrightCalculatorTests' made-up royalties of income year 2026.
public class CopyrightFiguresTests
{
    private static readonly CopyrightRules Rules = CopyrightRules.IncomeYear2026;

    // 10,000 beside 50,000 of pay stays within 30% of 60,000: all of it taxed apart, 15% withheld.
    [Fact]
    public void Royalty_withinTheLimits_isAllWithinThemWithWhatIsWithheld()
    {
        Lines(CopyrightFigures.Royalty(Compute(10_000m, 50_000m), Rules)).ShouldBe(
        [
            ("Figure_Royalty", 10_000m, ""),
            ("Figure_WithinLimits", 10_000m, ""),
            ("Figure_Withheld", 1_500m, "15%"),
        ]);
    }

    // 20,000 beside 30,000 of pay passes 30% of 50,000: 15,000 within, 2,250 withheld on it, and 5,000
    // taxed as pay, 30% withheld on it (Tax-Calc T18's figures).
    [Fact]
    public void Royalty_beyondTheLimits_isThePartWithinThePartTaxedAsPayAndWhatIsWithheldOnEach()
    {
        Lines(CopyrightFigures.Royalty(Compute(20_000m, 30_000m), Rules)).ShouldBe(
        [
            ("Figure_Royalty", 20_000m, ""),
            ("Figure_WithinLimits", 15_000m, ""),
            ("Figure_TaxedAsPay", 5_000m, ""),
            ("Figure_Withheld", 2_250m, "15%"),
            ("Figure_Withheld", 1_500m, "30%"),
        ]);
    }

    // With an attestation, half of the 10,000 within comes off as flat-rate costs, a line that takes from
    // what is taxed: the 15% is withheld on the 5,000 left.
    [Fact]
    public void Royalty_withAnAttestation_takesItsFlatRateCostsOff()
    {
        Lines(CopyrightFigures.Royalty(Compute(10_000m, 50_000m, attestation: true), Rules)).ShouldBe(
        [
            ("Figure_Royalty", 10_000m, ""),
            ("Figure_WithinLimits", 10_000m, ""),
            ("Figure_FlatRateCosts", -5_000m, ""),
            ("Figure_Withheld", 750m, "15%"),
        ]);
    }

    // Four previous years above the cap leave nothing within: all of it is taxed as pay, 30% withheld.
    [Fact]
    public void Royalty_averageAboveTheCap_isAllTaxedAsPay()
    {
        Lines(CopyrightFigures.Royalty(Compute(10_000m, 50_000m, averageWithinCap: false), Rules)).ShouldBe(
        [
            ("Figure_Royalty", 10_000m, ""),
            ("Figure_TaxedAsPay", 10_000m, ""),
            ("Figure_Withheld", 3_000m, "30%"),
        ]);
    }

    [Fact]
    public void Royalty_none_isNone()
    {
        CopyrightFigures.Royalty(CopyrightResult.None, Rules).ShouldBeEmpty();
    }

    // What a label cannot say is in its bubble: the limits with their amounts, how the flat-rate costs
    // are counted, and that the assessment credits what is withheld.
    [Fact]
    public void Royalty_linesNeedingExplaining_carryTheirBubbles()
    {
        var lines = CopyrightFigures.Royalty(Compute(20_000m, 30_000m, attestation: true), Rules);

        lines.Select(line => (line.Key, line.Help, string.Join("|", line.HelpArgs))).ShouldBe(
        [
            ("Figure_Royalty", null, ""),
            ("Figure_WithinLimits", "Help_WithinLimits", "€15,000.00|€77,220.00|2026"),
            ("Figure_TaxedAsPay", null, ""),
            ("Figure_FlatRateCosts", "Help_FlatRateCosts", "€20,590.00|€41,180.00"),
            ("Figure_Withheld", "Help_Withheld", ""),
            ("Figure_Withheld", "Help_Withheld", ""),
        ]);
    }

    // The plan with its royalty and the same plan without it, each by what reaches the pocket and what
    // goes to the taxes on pay and on profit; without it, the company keeps the royalty as profit, which
    // its bubble says.
    [Fact]
    public void Comparison_withAndWithout_areThePocketAndTheTaxesOfEach()
    {
        var comparison = new CopyrightComparison(40_000m, 38_000m, 20_000m, 18_000m, 10_000m, 14_000m);

        comparison.ShouldSatisfyAllConditions(
            () => Lines(CopyrightFigures.With(comparison)).ShouldBe([("Figure_Pocket", 40_000m, ""), ("Figure_TaxesOnPay", 20_000m, ""), ("Figure_TaxesOnProfit", 10_000m, "")]),
            () => Lines(CopyrightFigures.Without(comparison)).ShouldBe([("Figure_Pocket", 38_000m, ""), ("Figure_TaxesOnPay", 18_000m, ""), ("Figure_TaxesOnProfit", 14_000m, "")]),
            () => CopyrightFigures.Without(comparison).Select(line => line.Help).ShouldBe([null, null, "Help_KeptAsProfit"]));
    }

    // What the royalty leaves in the pocket against the plan without it, each way it can fall, the amount
    // being the difference itself.
    public static TheoryData<decimal, decimal, string, decimal> Differences => new()
    {
        { 40_000m, 38_000m, "Figure_Gain_More", 2_000m },
        { 38_000m, 40_000m, "Figure_Gain_Less", 2_000m },
        { 40_000m, 40_000m, "Figure_Gain_None", 0m },
    };

    [Theory]
    [MemberData(nameof(Differences))]
    public void Difference_eachWay_isItsLineWithTheDifference(decimal pocketWith, decimal pocketWithout, string key, decimal difference)
    {
        var line = CopyrightFigures.Difference(new CopyrightComparison(pocketWith, pocketWithout, 0m, 0m, 0m, 0m)).Single();

        (line.Key, line.Amount, decimal.IsNegative(line.Amount)).ShouldBe((key, difference, false));
    }

    private static CopyrightResult Compute(decimal royalty, decimal grossPay, bool averageWithinCap = true, bool attestation = false) =>
        CopyrightCalculator.Compute(Rules, new CopyrightRoyalty(royalty) { AverageWithinCap = averageWithinCap, HoldsAttestation = attestation }, grossPay)
            .Value.ShouldNotBeNull();

    // Each line as its key, its amount and its label's arguments, its bubble aside.
    private static List<(string Key, decimal Amount, string Args)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, string.Join("|", line.Args)))];
}

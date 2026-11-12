using Netto.Core.Copyright;
using Netto.Core.Rules;
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

    // 10,000 beside 50,000 of pay stays within 30% of 60,000: the royalty alone, all of it within, the
    // sentence under the lines saying so; what the company withholds on it is the tax apart, its drain line.
    [Fact]
    public void Royalty_withinTheLimits_isTheRoyaltyAlone()
    {
        Lines(CopyrightFigures.Royalty(Compute(10_000m, 50_000m), Rules)).ShouldBe([("Figure_Royalty", 10_000m, "")]);
    }

    // 20,000 beside 30,000 of pay passes 30% of 50,000: 15,000 within, and 30% withheld on the 5,000 taxed as
    // pay, which the tax on pay credits (Tax-Calc T18's figures). The 5,000 itself is the drain line on to
    // Director pay.
    [Fact]
    public void Royalty_beyondTheLimits_isThePartWithinAndWhatIsWithheldOnThePartBeyond()
    {
        Lines(CopyrightFigures.Royalty(Compute(20_000m, 30_000m), Rules)).ShouldBe(
        [
            ("Figure_Royalty", 20_000m, ""),
            ("Figure_WithinLimits", 15_000m, ""),
            ("Figure_WithheldBeyond", 1_500m, "30%"),
        ]);
    }

    // With an attestation, half of the 10,000 within comes off as flat-rate costs, a line that takes from
    // what is taxed.
    [Fact]
    public void Royalty_withAnAttestation_takesItsFlatRateCostsOff()
    {
        Lines(CopyrightFigures.Royalty(Compute(10_000m, 50_000m, attestation: true), Rules)).ShouldBe(
        [
            ("Figure_Royalty", 10_000m, ""),
            ("Figure_FlatRateCosts", -5_000m, ""),
        ]);
    }

    // Four previous years above the cap leave nothing within: all of it is taxed as pay, 30% withheld.
    [Fact]
    public void Royalty_averageAboveTheCap_isTheRoyaltyAndWhatIsWithheldOnIt()
    {
        Lines(CopyrightFigures.Royalty(Compute(10_000m, 50_000m, averageWithinCap: false), Rules)).ShouldBe(
        [
            ("Figure_Royalty", 10_000m, ""),
            ("Figure_WithheldBeyond", 3_000m, "30%"),
        ]);
    }

    // No line says what is withheld on the part within the limits: it is the tax apart, never a second figure.
    [Fact]
    public void Royalty_anyRoyalty_saysNothingWithheldWithinTheLimits()
    {
        new[] { Compute(10_000m, 50_000m), Compute(20_000m, 30_000m), Compute(10_000m, 50_000m, attestation: true) }
            .SelectMany(royalty => CopyrightFigures.Royalty(royalty, Rules)).Select(line => line.Key).ShouldNotContain("Figure_Withheld");
    }

    // A rate reads as its rule set states it, decimals kept: a withholding of 30.5% is no 31%.
    [Fact]
    public void Royalty_withholdingRate_readsAsItsRuleStatesIt()
    {
        var rules = Rules with { BeyondLimitsWithholdingRate = new RuleAmount("copyright_beyond_limits_withholding_rate", 0.305m, Rules.BeyondLimitsWithholdingRate.Source) };

        CopyrightFigures.Royalty(Compute(20_000m, 30_000m), rules).Single(line => line.Key == "Figure_WithheldBeyond").Args.ShouldBe(["30.5%"]);
    }

    [Fact]
    public void Royalty_none_isNone()
    {
        CopyrightFigures.Royalty(CopyrightResult.None, Rules).ShouldBeEmpty();
    }

    // What a label cannot say is in its bubble: the limits with their rate and their amounts, and how the
    // flat-rate costs are counted, at the rules' rates.
    [Fact]
    public void Royalty_linesNeedingExplaining_carryTheirBubbles()
    {
        var lines = CopyrightFigures.Royalty(Compute(20_000m, 30_000m, attestation: true), Rules);

        lines.Select(line => (line.Key, line.Help, string.Join("|", line.HelpArgs))).ShouldBe(
        [
            ("Figure_Royalty", null, ""),
            ("Figure_WithinLimits", "Help_WithinLimits", "30%|€15,000.00|€77,220.00|2026"),
            ("Figure_FlatRateCosts", "Help_FlatRateCosts", "50%|€20,590.00|25%|€41,180.00|15%"),
            ("Figure_WithheldBeyond", null, ""),
        ]);
    }

    // The plan with its royalty and the same plan without it, each by what reaches the pocket and what
    // goes to the taxes on pay and on profit; without it, the company keeps the royalty as profit, which
    // its bubble says. The difference is the card's summary.
    [Fact]
    public void Comparison_withAndWithout_areThePocketAndTheTaxesOfEach()
    {
        var comparison = new CopyrightComparison(40_000m, 38_000m, 20_000m, 18_000m, 10_000m, 14_000m);

        comparison.ShouldSatisfyAllConditions(
            () => Lines(CopyrightFigures.With(comparison)).ShouldBe([("Figure_Pocket", 40_000m, ""), ("Figure_TaxesOnPay", 20_000m, ""), ("Figure_TaxesOnProfit", 10_000m, "")]),
            () => Lines(CopyrightFigures.Without(comparison)).ShouldBe([("Figure_Pocket", 38_000m, ""), ("Figure_TaxesOnPay", 18_000m, ""), ("Figure_TaxesOnProfit", 14_000m, "")]),
            () => CopyrightFigures.Without(comparison).Select(line => line.Help).ShouldBe([null, null, "Help_KeptAsProfit"]));
    }

    private static CopyrightResult Compute(decimal royalty, decimal grossPay, bool averageWithinCap = true, bool attestation = false) =>
        CopyrightCalculator.Compute(Rules, new CopyrightRoyalty(royalty) { AverageWithinCap = averageWithinCap, HoldsAttestation = attestation }, grossPay)
            .Value.ShouldNotBeNull();

    // Each line as its key, its amount and its label's arguments, its bubble aside.
    private static List<(string Key, decimal Amount, string Args)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, string.Join("|", line.Args)))];
}

using Netto.Core.Copyright;
using Shouldly;

namespace Netto.Tests.Copyright;

// Art. 37, 171 and 269 CIR 92 (revenus 2026 and 2025), as they stand: 2026 for the river, 2025 for the
// Tax-Calc tests.
public class CopyrightRulesTests
{
    public static TheoryData<int, decimal> CapByYear => new()
    {
        { 2026, 77_220m },
        { 2025, 75_360m },
    };

    [Theory]
    [MemberData(nameof(CapByYear))]
    public void For_year_holdsItsCap(int incomeYear, decimal cap)
    {
        var rules = CopyrightRules.For(incomeYear).ShouldNotBeNull();

        (rules.IncomeYear, rules.Cap.Value).ShouldBe((incomeYear, cap));
    }

    // Art. 37 al. 2: 30%, no phase-in left (art. 551); art. 171, 2°bis: 15% apart; art. 269 §1: 15% on
    // the first tranche, 30% on movable income §1 sets no other rate for.
    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    public void For_year_holdsTheRates(int incomeYear)
    {
        var rules = CopyrightRules.For(incomeYear).ShouldNotBeNull();

        (rules.RelativeLimit.Value, rules.TaxedApartRate.Value, rules.WithholdingRate.Value, rules.BeyondLimitsWithholdingRate.Value)
            .ShouldBe((0.30m, 0.15m, 0.15m, 0.30m));
    }

    [Theory]
    [InlineData(2024)]
    [InlineData(2027)]
    public void For_incomeYearWithoutPublishedAmounts_hasNoRules(int incomeYear)
    {
        CopyrightRules.For(incomeYear).ShouldBeNull();
    }

    [Fact]
    public void IncomeYear2026_eachAmountCitesItsArticle()
    {
        var rules = CopyrightRules.IncomeYear2026;

        new[] { rules.RelativeLimit, rules.Cap, rules.TaxedApartRate, rules.WithholdingRate, rules.BeyondLimitsWithholdingRate }
            .Select(amount => amount.Source)
            .ShouldBe([rules.Limits, rules.Limits, rules.TaxedApart, rules.Withholding, rules.Withholding]);
    }

    // AR/CIR 92 art. 4 with the indexation notices for tax years 2027 and 2026: 50% of the first band and
    // 25% up to the second.
    public static TheoryData<int, decimal, decimal> BandsByYear => new()
    {
        { 2026, 20_590m, 41_180m },
        { 2025, 20_100m, 40_190m },
    };

    [Theory]
    [MemberData(nameof(BandsByYear))]
    public void For_year_holdsItsFlatRateCostBands(int incomeYear, decimal first, decimal second)
    {
        var costs = CopyrightRules.For(incomeYear).ShouldNotBeNull().FlatRateCosts;

        (costs.FirstBandRate.Value, costs.FirstBandCeiling.Value, costs.SecondBandRate.Value, costs.SecondBandCeiling.Value).ShouldBe((0.50m, first, 0.25m, second));
    }

    // From 2026 the flat-rate costs are an attestation holder's alone (programme law of 30.05.2026, art.
    // 10 to 12, in AR/CIR 92 art. 3 al. 2 and art. 4 al. 2); in 2025 every author took them.
    [Theory]
    [InlineData(2026, true)]
    [InlineData(2025, false)]
    public void For_year_saysWhetherTheFlatRateCostsNeedAnAttestation(int incomeYear, bool needsAttestation)
    {
        CopyrightRules.For(incomeYear).ShouldNotBeNull().FlatRateCosts.NeedsAttestation.ShouldBe(needsAttestation);
    }

    // The bands' ceilings are the indexation notice's; Fisconetplus's text of art. 4 still reads a second
    // band « de 20.100 euros » (story decision 6).
    [Fact]
    public void IncomeYear2026_bandsCiteArticle4AndTheNotice()
    {
        var costs = CopyrightRules.IncomeYear2026.FlatRateCosts;

        new[] { costs.FirstBandRate, costs.FirstBandCeiling, costs.SecondBandRate, costs.SecondBandCeiling }.Select(amount => amount.Source.Document).ShouldBe([
            "personal-tax/spf-finances_arcir92-art-4-revenus-2026_2026-09-17.html",
            "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html",
            "personal-tax/spf-finances_arcir92-art-4-revenus-2026_2026-09-17.html",
            "personal-tax/spf-finances_avis-indexation-exercice-2027_2026-08-03.html",
        ]);
    }

    // The SDA's ruling 2026.0484 of 18.08.2026, points 11 and 31: a royalty of at most 25% of the yearly
    // envelope, director pay plus royalty, times the share of working time spent creating.
    [Fact]
    public void IncomeYear2026_holdsTheShareOfTheEnvelopeARulingAccepts()
    {
        var rate = CopyrightRules.IncomeYear2026.EnvelopeRate.ShouldNotBeNull();

        (rate.Key, rate.Value, rate.Source.Document).ShouldBe(("copyright_envelope_rate", 0.25m, "personal-tax/spf-finances_decision-anticipee-2026-0484_2026-09-17.html"));
    }

    // The ruling is of 2026; the 2025 rules, for the Tax-Calc tests, hold none.
    [Fact]
    public void IncomeYear2025_holdsNoEnvelope()
    {
        CopyrightRules.IncomeYear2025.EnvelopeRate.ShouldBeNull();
    }

    // The conditions the card lists, each with the texts it comes from: works the director created and
    // transferred, for communication to the public, public performance or reproduction, effectively
    // exploited (art. 17 §1, 5°), at a price within professional needs (art. 53, 10°), the 281.45 fiche
    // (art. 57), the royalty declared (art. 313), and, for the flat-rate costs, the attestation du
    // travail des arts (programme law of 30.05.2026, AR/CIR 92 art. 3 al. 2 and art. 4 al. 2).
    [Fact]
    public void IncomeYear2026_listsTheConditionsWithTheirTexts()
    {
        CopyrightRules.IncomeYear2026.Conditions
            .Select(condition => (condition.Key, Labels: string.Join(", ", condition.Sources.Select(source => source.Label))))
            .ShouldBe(
            [
                ("works_you_created", "CIR 92, art. 17"),
                ("for_the_public", "CIR 92, art. 17"),
                ("effectively_exploited", "CIR 92, art. 17"),
                ("arm_length", "CIR 92, art. 53"),
                ("fiche_281_45", "CIR 92, art. 57"),
                ("declared", "CIR 92, art. 313"),
                ("attestation", "Programme law of 30.05.2026, AR/CIR 92, art. 3, AR/CIR 92, art. 4"),
            ]);
    }
}

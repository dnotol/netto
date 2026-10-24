using Netto.Core.Copyright;
using Netto.Core.Rules;
using Netto.Tests.Rules;
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
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public void For_incomeYearWithoutPublishedAmounts_hasNoRules(int incomeYear)
    {
        CopyrightRules.For(incomeYear).ShouldBeNull();
    }

    [Fact]
    public void IncomeYear2026_eachAmountCitesItsArticle()
    {
        var rules = CopyrightRules.IncomeYear2026;

        const string article37 = "personal-tax/spf-finances_cir92-art-37-revenus-2026_2026-02-18.html";
        const string article269 = "personal-tax/spf-finances_cir92-art-269-revenus-2026_2026-06-02.html";

        new[] { rules.RelativeLimit, rules.Cap, rules.TaxedApartRate, rules.WithholdingRate, rules.BeyondLimitsWithholdingRate }
            .Select(amount => amount.Source.Document)
            .ShouldBe([article37, article37, "personal-tax/spf-finances_cir92-art-171-revenus-2026_2026-07-31.html", article269, article269]);
    }

    // Each slot holds the amount named for it, both years: the 15% taxed apart and the 15% withheld, or
    // the 30% limit and the 30% withheld beyond it, swapped would pass every test of values. The 2025
    // rules hold no envelope rate.
    [Theory]
    [InlineData(2025, null)]
    [InlineData(2026, "copyright_envelope_rate")]
    public void For_year_eachAmountSlotHoldsItsKey(int incomeYear, string? envelopeRate)
    {
        var rules = CopyrightRules.For(incomeYear).ShouldNotBeNull();

        RuleSets.Slots<RuleAmount>(rules).Concat(RuleSets.Slots<RuleAmount>(rules.FlatRateCosts)).Select(slot => (slot.Slot, slot.Value?.Key))
            .ShouldBe(
            [
                ("RelativeLimit", "copyright_relative_limit_rate"),
                ("Cap", "copyright_cap"),
                ("TaxedApartRate", "copyright_taxed_apart_rate"),
                ("WithholdingRate", "copyright_withholding_rate"),
                ("BeyondLimitsWithholdingRate", "copyright_beyond_limits_withholding_rate"),
                ("EnvelopeRate", envelopeRate),
                ("FirstBandRate", "copyright_flat_rate_costs_first_band_rate"),
                ("FirstBandCeiling", "copyright_flat_rate_costs_first_band"),
                ("SecondBandRate", "copyright_flat_rate_costs_second_band_rate"),
                ("SecondBandCeiling", "copyright_flat_rate_costs_second_band"),
            ]);
    }

    // Each text slot holds its year's document: the steps cite the slots themselves, so a swap of two
    // of the texts Of takes for a year would pass every test that compares a step to them. The flat-rate
    // costs need an attestation from 2026 only.
    public static TheoryData<int, string, string, string, string, string, string, string?> TextsByYear => new()
    {
        {
            2025, "personal-tax/spf-finances_cir92-art-37-revenus-2025_2025-08-01.html", "personal-tax/spf-finances_cir92-art-171-revenus-2025_2026-01-19.html",
            "personal-tax/spf-finances_cir92-art-32-revenus-2025_2024-12-02.html", "personal-tax/spf-finances_cir92-art-157-revenus-2025_2025-01-16.html",
            "personal-tax/spf-finances_arcir92-art-3-revenus-2025_2025-01-15.html", "personal-tax/spf-finances_arcir92-art-4-revenus-2025_2026-01-12.html", null
        },
        {
            2026, "personal-tax/spf-finances_cir92-art-37-revenus-2026_2026-02-18.html", "personal-tax/spf-finances_cir92-art-171-revenus-2026_2026-07-31.html",
            "personal-tax/spf-finances_cir92-art-32-revenus-2026_2026-01-29.html", "personal-tax/spf-finances_cir92-art-157-revenus-2026_2026-07-30.html",
            "personal-tax/spf-finances_arcir92-art-3-revenus-2026_2026-09-17.html", "personal-tax/spf-finances_arcir92-art-4-revenus-2026_2026-09-17.html",
            "personal-tax/ejustice_loi-programme-2026-05-30_2026-06-01.html"
        },
    };

    [Theory]
    [MemberData(nameof(TextsByYear))]
    public void For_year_eachTextSlotHoldsItsDocument(
        int incomeYear, string article37, string article171, string article32, string article157, string arCir3, string arCir4, string? attestation)
    {
        var rules = CopyrightRules.For(incomeYear).ShouldNotBeNull();

        RuleSets.Slots<Source>(rules).Concat(RuleSets.Slots<Source>(rules.FlatRateCosts)).Select(slot => (slot.Slot, slot.Value?.Document)).ShouldBe(
        [
            ("Limits", article37),
            ("PhaseIn", "personal-tax/spf-finances_cir92-art-551-revenus-2026_2026-01-21.html"),
            ("TaxedApart", article171),
            ("Deduction", "corporate-tax/spf-finances_cir92-art-49-revenus-2026_2026-02-04.html"),
            ("TaxedAsPay", article32),
            ("OutsideContributions", "social-contributions/ejustice_arrete-royal-38-statut-social-independants_2026-06-01.html"),
            ("OutsideAdvancePaymentSurcharge", article157),
            ("NetIncome", "personal-tax/spf-finances_cir92-art-22-revenus-2026_2026-02-02.html"),
            ("Who", arCir3),
            ("Bands", arCir4),
            ("Attestation", attestation),
        ]);
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

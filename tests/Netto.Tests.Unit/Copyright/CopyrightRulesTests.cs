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

    // From 2026 the flat-rate costs are an attestation holder's alone (AR/CIR 92, art. 3 al. 2): the
    // 2026 rules say why the royalty is taxed on its gross. 2025's flat-rate costs are Tax-Calc's input.
    [Fact]
    public void IncomeYear2026_saysTheFlatRateCostsNeedAnAttestation()
    {
        CopyrightRules.IncomeYear2026.NoFlatRateCosts.ShouldNotBeNull().Document.ShouldBe("personal-tax/spf-finances_arcir92-art-3-revenus-2026_2026-09-17.html");
    }

    // The conditions the card lists, each with the texts it comes from: works the director created and
    // transferred, for communication to the public, public performance or reproduction, effectively
    // exploited (art. 17 §1, 5°), at a price within professional needs (art. 53, 10°), the 281.45 fiche
    // (art. 57) and the royalty declared (art. 313).
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
            ]);
    }
}

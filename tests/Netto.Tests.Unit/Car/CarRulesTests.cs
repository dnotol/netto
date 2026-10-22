using Netto.Core.Car;
using Netto.Core.CorporateTax;
using Shouldly;

namespace Netto.Tests.Car;

// Art. 36, 66 and 198 CIR 92 (revenus 2026), the administration's FAQ on car benefits, circular
// 2024/C/50 and the SPF course's Module 5.3, as they stand for income year 2026.
public class CarRulesTests
{
    // Art. 36 §2: 6/7 of the catalogue value, a CO2 percentage never below 4%, which a car that emits
    // no CO2 takes, and a benefit of at least 1,690 a year (the indexed amount of revenus 2026).
    [Fact]
    public void IncomeYear2026_holdsTheBenefitsAmounts()
    {
        var rules = CarRules.IncomeYear2026;

        (rules.IncomeYear, rules.CatalogueShare.Value, rules.MinimumCo2Percentage.Value, rules.MinimumBenefit.Value)
            .ShouldBe((2026, 6m / 7m, 0.04m, 1_690m));
    }

    // Art. 36 §2: the catalogue value at 100, 94, 88, 82, 76 and 70% by bands of twelve months from
    // the first registration.
    [Fact]
    public void IncomeYear2026_holdsTheAgeBands()
    {
        CarRules.IncomeYear2026.AgeBands.Select(band => (band.FromMonth, band.Coefficient.Value)).ShouldBe(new[]
        {
            (1, 1.00m),
            (13, 0.94m),
            (25, 0.88m),
            (37, 0.82m),
            (49, 0.76m),
            (61, 0.70m),
        });
    }

    // Art. 198 §1, 9° and 9°bis: 17% of the benefit added back, 40% when the company pays the fuel,
    // or, for an electric car, the charging (Module 5.3); art. 66 §1: an electric car ordered or
    // leased before 2027 stays deductible in full (circular 2024/C/50).
    [Fact]
    public void IncomeYear2026_holdsTheCompanysRates()
    {
        var rules = CarRules.IncomeYear2026;

        (rules.AddBackRate.Value, rules.AddBackRateWithPrivateEnergy.Value, rules.ZeroEmissionDeduction.Value).ShouldBe((0.17m, 0.40m, 1.00m));
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2027)]
    public void For_incomeYearWithoutPublishedAmounts_hasNoRules(int incomeYear)
    {
        CarRules.For(incomeYear).ShouldBeNull();
    }

    [Fact]
    public void For_2026_isTheIncomeYear2026Rules()
    {
        CarRules.For(2026).ShouldBeSameAs(CarRules.IncomeYear2026);
    }

    // Each amount cites the text it is read from: the benefit's art. 36, the add-back's art. 198, the
    // 40% for an electric car the administration's course, the deduction art. 66.
    [Fact]
    public void IncomeYear2026_eachAmountCitesItsText()
    {
        var rules = CarRules.IncomeYear2026;

        new[] { rules.CatalogueShare, rules.MinimumCo2Percentage, rules.MinimumBenefit, rules.AddBackRate, rules.AddBackRateWithPrivateEnergy, rules.ZeroEmissionDeduction }
            .Select(amount => amount.Source)
            .ShouldBe([rules.Benefit, rules.Benefit, rules.Benefit, rules.AddBack, rules.ChargingReading, rules.Deduction]);
    }

    [Fact]
    public void IncomeYear2026_eachAgeBandCitesArticle36()
    {
        var rules = CarRules.IncomeYear2026;

        rules.AgeBands.Select(band => band.Coefficient.Source).Distinct().ShouldBe([rules.Benefit]);
    }

    // The conditions the card lists, each with the texts it comes from: an electric car ordered or
    // leased before 2027 (art. 66 and circular 2024/C/50), and at the director's disposal for private
    // use all year, which the benefit is computed for (art. 36 and the FAQ).
    [Fact]
    public void Conditions_2026_eachCitesItsText()
    {
        var rules = CarRules.IncomeYear2026;

        rules.Conditions.SelectMany(condition => condition.Sources, (condition, source) => (condition.Key, source)).ShouldBe(new[]
        {
            ("electric_ordered_before_2027", rules.Deduction),
            ("electric_ordered_before_2027", rules.Acquisition),
            ("private_use_all_year", rules.Benefit),
            ("private_use_all_year", rules.BenefitMethod),
        });
    }

    [Fact]
    public void Conditions_2026_citeEachTextByAShortLabel()
    {
        CarRules.IncomeYear2026.Conditions.SelectMany(condition => condition.Sources, (condition, source) => source.Label).ShouldBe(
            ["CIR 92, art. 66", "Circular 2024/C/50", "CIR 92, art. 36", "SPF FAQ on car benefits"]);
    }

    // Art. 2 §1, 25° with art. 215 al. 3, 5°: flat-rate benefits worth more than 20% of the total pay
    // allotted to directors cost the reduced rate.
    [Fact]
    public void CorporateTaxRules2026_holdTheExcessBenefitsShare()
    {
        var share = CorporateTaxRules.IncomeYear2026.ExcessBenefitsShare;

        (share.Value, share.Source.Document).ShouldBe((0.20m, "corporate-tax/spf-finances_cir92-art-2-revenus-2026_2026-07-30.html"));
    }
}

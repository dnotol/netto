using Netto.Core.CompanyCar;
using Netto.Core.CorporateTax;
using Shouldly;

namespace Netto.Tests.CompanyCar;

// Art. 36, 66, 198, 198bis and 550 CIR 92 (revenus 2026), art. 18 AR/CIR 92 (revenus 2026), the
// administration's FAQ on car benefits, circulars 2024/C/50, 2026/C/10 and 2026/C/11, parliamentary
// questions 819 and 96 and the SPF course's Module 5.3, as they stand for income year 2026.
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
    [InlineData(Fixtures.UnsupportedIncomeYear)]
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

    // Art. 36 §2: the CO2 percentage is 5.5% at the year's reference emission, 0.1 point more or less
    // for each gram above or below it, between 4% and 18%; a car whose CO2 the DIV does not hold counts
    // as 205 g/km on petrol, LPG or natural gas and 195 g/km on diesel, and a fake hybrid with no
    // corresponding car as its own CO2 × 2.5. Art. 18 §3, 9 AR/CIR 92: the references for 2026, 70 and
    // 58 g/km (royal decree of 17.12.2025).
    [Fact]
    public void IncomeYear2026_holdsTheCo2PercentagesAmounts()
    {
        var rules = CarRules.IncomeYear2026;

        (rules.BaseCo2Percentage.Value, rules.Co2PercentagePerGram.Value, rules.MinimumCo2Percentage.Value, rules.MaximumCo2Percentage.Value)
            .ShouldBe((0.055m, 0.001m, 0.04m, 0.18m));
        (rules.PetrolReferenceEmission.Value, rules.DieselReferenceEmission.Value, rules.PetrolEmissionWithoutData.Value, rules.DieselEmissionWithoutData.Value,
                rules.FakeHybridWithoutMatch.Value)
            .ShouldBe((70m, 58m, 205m, 195m, 2.5m));
    }

    // Art. 550 al. 1: 120% − 0.5% × coefficient × g/km, the coefficient 1 for a car running on diesel
    // alone, 0.95 for the others, 0.90 for natural gas under 12 fiscal horsepower; a diesel hybrid
    // takes 0.95 (parliamentary question 96).
    [Fact]
    public void IncomeYear2026_holdsTheDeductionFormula()
    {
        var rules = CarRules.IncomeYear2026;

        (rules.FormulaBase.Value, rules.FormulaPerGram.Value, rules.DieselCoefficient.Value, rules.OtherFuelCoefficient.Value,
                rules.DieselHybridCoefficient.Value, rules.SmallNaturalGasCoefficient.Value)
            .ShouldBe((1.20m, 0.005m, 1m, 0.95m, 0.95m, 0.90m));
    }

    // A company's deduction of a car that emits CO2, by the date it was ordered or leased: before
    // 01.07.2023 the formula between 50% and 100%, 40% from 200 g/km or without CO2 data (art. 550
    // al. 2, its minimum for a car acquired before 2018 being personal tax only, art. 198bis); from
    // 01.07.2023 to 31.12.2025 the formula at most 50%, 0% without CO2 data (al. 4); from 01.01.2026
    // nothing (art. 66 §1).
    [Fact]
    public void IncomeYear2026_holdsTheDeductionPeriods()
    {
        CarRules.IncomeYear2026.DeductionPeriods
            .Select(period => (period.AcquiredFrom, period.Fixed?.Value, period.Floor?.Value, period.Ceiling?.Value, period.WithoutCo2Data?.Value, period.HighEmissionFrom?.Value))
            .ShouldBe(new (DateOnly, decimal?, decimal?, decimal?, decimal?, decimal?)[]
            {
                (DateOnly.MinValue, null, 0.50m, 1.00m, 0.40m, 200m),
                (new DateOnly(2023, 7, 1), null, null, 0.50m, 0m, null),
                (new DateOnly(2026, 1, 1), 0m, null, null, null, null),
            });
    }

    // A fake hybrid is one bought or leased from 2018 (art. 36 §2 al. 9, art. 550 al. 6); a plug-in
    // hybrid acquired in the first half of 2023 deducts its petrol or diesel at 50% at most, beside
    // its other costs at the formula (art. 550 al. 7), which Netto does not split yet.
    [Fact]
    public void IncomeYear2026_holdsItsDates()
    {
        var rules = CarRules.IncomeYear2026;

        (rules.FakeHybridFrom, rules.PlugInFuelCappedFrom, rules.PlugInFuelCappedUntil)
            .ShouldBe((new DateOnly(2018, 1, 1), new DateOnly(2023, 1, 1), new DateOnly(2023, 6, 30)));
    }

    // The CO2 percentage's amounts cite art. 36, the references art. 18 AR/CIR 92, the formula and the
    // periods art. 550, a diesel hybrid's coefficient the Minister's answer, and the 0% from 2026 art. 66.
    [Fact]
    public void IncomeYear2026_eachFuelAmountCitesItsText()
    {
        var rules = CarRules.IncomeYear2026;
        var periods = rules.DeductionPeriods;

        new[]
            {
                rules.BaseCo2Percentage, rules.Co2PercentagePerGram, rules.MaximumCo2Percentage, rules.PetrolEmissionWithoutData, rules.DieselEmissionWithoutData,
                rules.FakeHybridWithoutMatch, rules.PetrolReferenceEmission, rules.DieselReferenceEmission, rules.FormulaBase, rules.FormulaPerGram,
                rules.DieselCoefficient, rules.OtherFuelCoefficient, rules.SmallNaturalGasCoefficient, rules.DieselHybridCoefficient,
                periods[0].Floor!, periods[0].Ceiling!, periods[0].WithoutCo2Data!, periods[0].HighEmissionFrom!, periods[1].Ceiling!, periods[1].WithoutCo2Data!,
                periods[2].Fixed!,
            }
            .Select(amount => amount.Source)
            .ShouldBe([
                rules.Benefit, rules.Benefit, rules.Benefit, rules.Benefit, rules.Benefit,
                rules.Benefit, rules.ReferenceEmission, rules.ReferenceEmission, rules.FormulaDeduction, rules.FormulaDeduction,
                rules.FormulaDeduction, rules.FormulaDeduction, rules.FormulaDeduction, rules.DieselHybrid,
                rules.FormulaDeduction, rules.FormulaDeduction, rules.FormulaDeduction, rules.FormulaDeduction, rules.FormulaDeduction, rules.FormulaDeduction,
                rules.Deduction,
            ]);
    }

    // The texts the steps also cite: the decree that set the references, art. 198bis that keeps the
    // relief for plug-in hybrids and the pre-2018 minimum from companies, the circulars on plug-in
    // hybrids and on fake hybrids, and the Minister's answer on a plug-in's costs in corporate tax.
    [Fact]
    public void IncomeYear2026_citesTheFuelCarsTextsByTheirFiles()
    {
        var rules = CarRules.IncomeYear2026;

        new[] { rules.ReferenceEmission, rules.ReferenceDecree, rules.FormulaDeduction, rules.CompanyExclusions, rules.PlugInDeduction, rules.FakeHybrid, rules.PlugInTransition, rules.DieselHybrid }
            .Select(source => (source.Label, source.Document))
            .ShouldBe([
                ("AR/CIR 92, art. 18", "personal-tax/spf-finances_arcir92-art-18-revenus-2026_2026-08-10.html"),
                ("Royal decree of 17.12.2025", "personal-tax/ejustice_arrete-royal-emission-reference-co2-2026_2025-12-24.html"),
                ("CIR 92, art. 550", "corporate-tax/spf-finances_cir92-art-550-revenus-2026_2026-03-02.html"),
                ("CIR 92, art. 198bis", "corporate-tax/spf-finances_cir92-art-198bis-revenus-2026_2026-03-02.html"),
                ("Circular 2026/C/11", "corporate-tax/spf-finances_circulaire-2026-c-11-hybrides-rechargeables_2026-01-09.html"),
                ("Circular 2026/C/10", "personal-tax/spf-finances_circulaire-2026-c-10-faux-hybride_2026-01-07.html"),
                ("Question 819", "corporate-tax/spf-finances_question-parlementaire-819_2026-08-05.html"),
                ("Question 96", "corporate-tax/spf-finances_question-parlementaire-96_2022-02-10.html"),
            ]);
    }

    // The conditions the card lists, each with the texts it comes from: ordered or leased by the date
    // the user gives (art. 66 and 550, circular 2024/C/50), at the director's disposal for private
    // use all year, which the benefit is computed for (art. 36 and the FAQ), and what makes a plug-in
    // hybrid a fake one (art. 36 and 550, circular 2026/C/10).
    [Fact]
    public void Conditions_2026_eachCitesItsText()
    {
        var rules = CarRules.IncomeYear2026;

        rules.Conditions.SelectMany(condition => condition.Sources, (condition, source) => (condition.Key, source)).ShouldBe(new[]
        {
            ("ordered_or_leased", rules.Deduction),
            ("ordered_or_leased", rules.FormulaDeduction),
            ("ordered_or_leased", rules.Acquisition),
            ("private_use_all_year", rules.Benefit),
            ("private_use_all_year", rules.BenefitMethod),
            ("fake_hybrid", rules.Benefit),
            ("fake_hybrid", rules.FormulaDeduction),
            ("fake_hybrid", rules.FakeHybrid),
        });
    }

    [Fact]
    public void Conditions_2026_citeEachTextByAShortLabel()
    {
        CarRules.IncomeYear2026.Conditions.SelectMany(condition => condition.Sources, (condition, source) => source.Label).ShouldBe(
            ["CIR 92, art. 66", "CIR 92, art. 550", "Circular 2024/C/50", "CIR 92, art. 36", "SPF FAQ on car benefits", "CIR 92, art. 36", "CIR 92, art. 550", "Circular 2026/C/10"]);
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

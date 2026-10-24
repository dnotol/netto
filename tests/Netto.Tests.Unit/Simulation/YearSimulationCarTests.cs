using Netto.Core.CompanyCar;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// YearSimulationTests' made-up company, 100,000 of profit before the director, with an electric car
// the company puts at the director's disposal all year: a catalogue value of 50,000, first registered
// in January 2026, so a benefit of 1,714.29 (CarCalculatorTests), and 8,000 of yearly costs. The
// company adds back 17% of the benefit, 291.43, and pays no private charging unless a test says it
// does. It prepays enough to leave no surcharge, and withholds more than any tax, unless a test says
// otherwise.
public class YearSimulationCarTests
{
    // 45,000 at 120 g/km of petrol, leased in January 2026: a benefit of 4,050.00, 9,000 of costs.
    private static readonly CarFacts PetrolCarLeasedIn2026 = new(45_000m, new DateOnly(2026, 1, 10), 9_000m, false, 0m)
    {
        Engine = CarEngine.Petrol, Co2 = 120, Acquired = new DateOnly(2026, 1, 5),
    };

    public static TheoryData<decimal, decimal, decimal, decimal> Setups => new()
    {
        { 50_000m, 100_000m, 0m, 0m },
        { 21_000m, 100_000m, 20_000m, 600m },
        { 160_000m, 50_000m, 0m, 0m },
        { 0m, 300_000m, 50_000m, 2_000m },
        { 5_000m, 9_000m, 0m, 0m },
    };

    // The pay's figures are GrossToNet's on the pay with the benefit.
    [Fact]
    public void Run_2026Car_taxesItsBenefitAsPay()
    {
        var (pay, _) = GrossToNet.Compute(
            2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, ContributionsPaidBy.Director, new TaxedWithPay { BenefitInKind = 1_714.29m });

        var result = Run(Scenario(car: ElectricCar));

        (result.DirectorPay.BenefitInKind, result.DirectorPay.NetInPocket, result.DirectorPay.Steps.Sum(step => step.Amount))
            .ShouldBe((1_714.29m, pay!.NetInPocket, pay.Steps.Sum(step => step.Amount)));
    }

    // The car's 8,000 come off the 100,000 the split shares; 17% of its benefit, 291.43, is added back
    // to the taxable income, 92,000 less the 50,000 of pay. The pay with its benefit, 51,714.29,
    // reaches the minimum: 20% of 42,291.43 is 8,458.29, and the profit after tax, 33,541.71, is what
    // is left once the add-back, never spent, is taken out again.
    [Fact]
    public void Run_2026Car_deductsItsCostsAndAddsBackAShareOfItsBenefit()
    {
        var result = Run(Scenario(car: ElectricCar));

        (result.ProfitBeforePay, result.DisallowedExpenses, result.Company.TaxableIncome, result.Company.Condition, result.CorporateTax, result.ProfitAfterTax)
            .ShouldBe((92_000m, 291.43m, 42_291.43m, ReducedRateCondition.MinimumPayMet, 8_458.29m, 33_541.71m));
    }

    // When the company pays the charging for private use, 40%: 685.72.
    [Fact]
    public void Run_2026CarCompanyPaysPrivateCharging_addsBackFortyPercent()
    {
        Run(Scenario(car: ElectricCar with { CompanyPaysPrivateEnergy = true })).DisallowedExpenses.ShouldBe(685.72m);
    }

    // The director's 600 for private use leave their pocket for the company, and lower the benefit
    // taxed to 1,114.29; the add-back stays on the benefit before it.
    [Fact]
    public void Run_2026CarWithAContribution_paysItToTheCompanyOutOfThePocket()
    {
        var without = Run(Scenario(car: ElectricCar));

        var result = Run(Scenario(car: ElectricCar with { PersonalContribution = 600m }));

        (result.ProfitBeforePay, result.DirectorPay.BenefitInKind, result.DisallowedExpenses, result.Car.Contribution)
            .ShouldBe((92_600m, 1_114.29m, 291.43m, 600m));
        without.Car.Contribution.ShouldBe(0m);
    }

    // The car's 8,000 of costs are a cost of the company, like its running costs (operator, 2026-10-23):
    // the layers, the pocket and the company add up to the profit before the director less them, the
    // director's contribution leaving the pocket for the company inside that whole.
    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026Car_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirectorLessTheCarsCosts(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, decimal contribution)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, revenue: profitBeforeDirector + 20_000m,
            car: ElectricCar with { PersonalContribution = contribution }));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector - 8_000m);
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026CarCompanyPaysAndNothingPrepaid_layersPocketAndCompanyAddUpToTheProfitLessTheCarsCosts(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, decimal contribution)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, revenue: profitBeforeDirector + 20_000m,
            advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.AsWithheld, paidBy: ContributionsPaidBy.Company,
            car: ElectricCar with { PersonalContribution = contribution, CompanyPaysPrivateEnergy = true }));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector - 8_000m);
    }

    // What the car brings the ledger, in order: what it costs the company, which only sets its profit
    // and so its tax base, like the royalty it pays, the benefit with its minimum, the contribution
    // leaving the pocket, the benefit taxed as pay, and the add-back.
    [Fact]
    public void Run_2026Car_listsItsSteps()
    {
        var result = Run(Scenario(car: ElectricCar with { PersonalContribution = 600m }));

        result.Steps.Where(step => step.Event == TaxableEvent.CompanyCar).Select(step => (step.RuleKey, step.Layer, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("car_costs", Layer.Corporate, Track.TaxBaseOnly, 8_000m),
            ("car_benefit", Layer.Personal, Track.TaxBaseOnly, 1_714.29m),
            ("car_contribution", Layer.Personal, Track.InPocket, -600m),
            ("car_benefit_taxed", Layer.Personal, Track.TaxBaseOnly, 1_114.29m),
            ("car_add_back", Layer.Corporate, Track.TaxBaseOnly, 291.43m),
        });
    }

    // A petrol car leased in 2026 (CarCalculatorTests): its 9,000 come off the 100,000 the split
    // shares, but the company cannot deduct the 4,950 beyond its benefit, and adds back 688.50 of that
    // benefit: 5,638.50 join the taxable income, 91,000 less the 50,000 of pay.
    [Fact]
    public void Run_2026FuelCar_addsWhatItCannotDeductToTheTaxableIncome()
    {
        var result = Run(Scenario(car: PetrolCarLeasedIn2026));

        (result.ProfitBeforePay, result.DisallowedExpenses, result.Company.TaxableIncome).ShouldBe((91_000m, 5_638.50m, 46_638.50m));
    }

    // Its steps say what it cannot deduct beside what it adds back; an electric car, deducted in
    // full, keeps the steps it had.
    [Fact]
    public void Run_2026FuelCar_listsWhatItCannotDeduct()
    {
        var result = Run(Scenario(car: PetrolCarLeasedIn2026));

        result.Steps.Where(step => step.Event == TaxableEvent.CompanyCar).Select(step => (step.RuleKey, step.Layer, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("car_costs", Layer.Corporate, Track.TaxBaseOnly, 9_000m),
            ("car_benefit", Layer.Personal, Track.TaxBaseOnly, 4_050m),
            ("car_contribution", Layer.Personal, Track.InPocket, 0m),
            ("car_benefit_taxed", Layer.Personal, Track.TaxBaseOnly, 4_050m),
            ("car_costs_not_deductible", Layer.Corporate, Track.TaxBaseOnly, 4_950m),
            ("car_add_back", Layer.Corporate, Track.TaxBaseOnly, 688.50m),
        });
    }

    // The costs it cannot deduct cite art. 66, whose §3, 3° keeps the part equal to the benefit and
    // the contribution deductible; its costs cite the rate of art. 66 for a car leased from 2026.
    [Fact]
    public void Run_2026FuelCar_citesItsDeductionsTexts()
    {
        var rules = CarRules.IncomeYear2026;

        var steps = Run(Scenario(car: PetrolCarLeasedIn2026)).Steps.Where(step => step.Event == TaxableEvent.CompanyCar).ToDictionary(step => step.RuleKey);

        steps.ShouldSatisfyAllConditions(
            () => (steps["car_costs"].Source, steps["car_costs"].AmountsUsed.Single()).ShouldBe((rules.Deduction, rules.DeductionPeriods[2].Fixed!)),
            () => (steps["car_costs_not_deductible"].Source, steps["car_costs_not_deductible"].AmountsUsed.Count).ShouldBe((rules.Deduction, 0)));
    }

    // A contribution of nothing moves nothing: a plain zero, never one a drain would read as money
    // coming back.
    [Fact]
    public void Run_2026CarWithoutAContribution_listsItsContributionAsAPlainZero()
    {
        var step = Run(Scenario(car: ElectricCar)).Steps.Single(step => step.RuleKey == "car_contribution");

        (step.Amount, decimal.IsNegative(step.Amount)).ShouldBe((0m, false));
    }

    // Its costs cite art. 66 and the full deduction; the benefit art. 36, the FAQ's method and every
    // amount it used; the benefit taxed art. 31, and the royal decree that puts it in INASTI's base;
    // the add-back art. 198 and its rate.
    [Fact]
    public void Run_2026Car_citesItsTexts()
    {
        var rules = CarRules.IncomeYear2026;

        var steps = Run(Scenario(car: ElectricCar)).Steps.Where(step => step.Event == TaxableEvent.CompanyCar).ToDictionary(step => step.RuleKey);

        steps.ShouldSatisfyAllConditions(
            () => (steps["car_costs"].Source, steps["car_costs"].AmountsUsed.Single()).ShouldBe((rules.Deduction, rules.ZeroEmissionDeduction)),
            () => steps["car_costs"].AlsoCites.ShouldBe([rules.Acquisition]),
            () => steps["car_benefit"].Source.ShouldBe(rules.Benefit),
            () => steps["car_benefit"].AmountsUsed.ShouldBe([rules.CatalogueShare, rules.MinimumCo2Percentage, rules.AgeBands[0].Coefficient, rules.MinimumBenefit]),
            () => steps["car_benefit"].AlsoCites.ShouldBe([rules.BenefitMethod]),
            () => (steps["car_benefit_taxed"].Source, steps["car_benefit_taxed"].AlsoCites.Single()).ShouldBe((rules.BenefitAsPay, rules.BenefitInContributions)),
            () => (steps["car_add_back"].Source, steps["car_add_back"].AmountsUsed.Single()).ShouldBe((rules.AddBack, rules.AddBackRate)));
    }

    // Art. 215 al. 3, 4°: 49,300 of pay is under the minimum, but with its benefit, 51,014.29, it
    // reaches it; without the car the same pay loses the rate on 120,000 before the director.
    [Fact]
    public void Run_2026CarBesideAPayJustUnderTheMinimum_keepsTheRateWithTheBenefitCounted()
    {
        var withCar = Run(Scenario(directorPay: 49_300m, revenue: 140_000m, car: ElectricCar));
        var without = Run(Scenario(directorPay: 49_300m, revenue: 140_000m));

        (withCar.Company.Condition, withCar.Company.DirectorPay, without.Company.Condition)
            .ShouldBe((ReducedRateCondition.MinimumPayMet, 51_014.29m, ReducedRateCondition.MinimumPayNotMet));
    }

    // Art. 215 al. 3, 5°: 1,714.29 is more than 20% of 6,714.29, the 5,000 of pay with it.
    [Fact]
    public void Run_2026CarBesideASmallPay_losesTheRateOnItsBenefit()
    {
        var result = Run(Scenario(directorPay: 5_000m, car: ElectricCar));

        (result.Company.Condition, result.Company.FlatRateBenefits).ShouldBe((ReducedRateCondition.ExcessBenefits, 1_714.29m));
    }

    // The cap weighs the benefit taxed, after the contribution: 1,114.29 is within 20% of 6,114.29,
    // so the pay test decides, and 87,891.43 of taxable income is above that pay.
    [Fact]
    public void Run_2026CarWithAContributionBesideASmallPay_weighsTheBenefitTaxed()
    {
        var result = Run(Scenario(directorPay: 5_000m, car: ElectricCar with { PersonalContribution = 600m }));

        (result.Company.Condition, result.Company.FlatRateBenefits).ShouldBe((ReducedRateCondition.MinimumPayNotMet, 1_114.29m));
    }

    [Fact]
    public void Run_2026CarInFirstFourTaxPeriods_losesTheRateOnExcessBenefits()
    {
        var result = Run(Scenario(directorPay: 5_000m, company: new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: true), car: ElectricCar));

        result.Company.Condition.ShouldBe(ReducedRateCondition.ExcessBenefits);
    }

    // Until its catalogue value is given there is no car: its costs and contribution count for
    // nothing, and the year runs as without it.
    [Fact]
    public void Run_2026CarWithoutCatalogueValue_runsTheYearWithoutIt()
    {
        var without = Run(Scenario());

        var result = Run(Scenario(car: ElectricCar with { CatalogueValue = 0m, PersonalContribution = 600m }));

        (result.NetInPocket, result.ProfitBeforePay, result.Car.Costs, result.Steps.Count(step => step.Event == TaxableEvent.CompanyCar))
            .ShouldBe((without.NetInPocket, without.ProfitBeforePay, 0m, 0));
    }

    [Fact]
    public void Run_2026NoCar_listsNoStepOfIt()
    {
        Run(Scenario()).Steps.ShouldNotContain(step => step.Event == TaxableEvent.CompanyCar);
    }

    [Fact]
    public void Run_2026Car_everyStepHasASourceWithAReferenceAndAnAddress()
    {
        Run(Scenario(car: ElectricCar)).Steps.ShouldAllBe(step => step.Source.Reference.Length > 0 && step.Source.Url.IsAbsoluteUri);
    }

    [Theory]
    [InlineData(-0.01, 8_000, 0, CarCalculator.CatalogueValueNegative)]
    [InlineData(50_000, -0.01, 0, CarCalculator.YearlyCostsNegative)]
    [InlineData(50_000, 8_000, -0.01, CarCalculator.ContributionNegative)]
    public void Run_carOutsideTheRules_isRefusedWithTheCalculatorsKey(double catalogueValue, double costs, double contribution, string expected)
    {
        var car = ElectricCar with { CatalogueValue = (decimal)catalogueValue, YearlyCosts = (decimal)costs, PersonalContribution = (decimal)contribution };

        YearSimulation.Run(Scenario(car: car)).ShouldBe((null, expected));
    }

    // 100,000 less the car's 8,000, plus the director's 600 for it; taken as given while refused.
    [Theory]
    [InlineData(8_000, 600, 92_600)]
    [InlineData(-1_000, 0, 101_000)]
    public void ProfitBeforePay_car_isWhatTheCompanyHasLeftOnceItPaidForIt(double costs, double contribution, double expected)
    {
        YearSimulation.ProfitBeforePay(Scenario(car: ElectricCar with { YearlyCosts = (decimal)costs, PersonalContribution = (decimal)contribution }))
            .ShouldBe((decimal)expected);
    }

    [Fact]
    public void ProfitBeforePay_carWithoutCatalogueValue_countsNothingOfIt()
    {
        YearSimulation.ProfitBeforePay(Scenario(car: ElectricCar with { CatalogueValue = 0m })).ShouldBe(100_000m);
    }
}

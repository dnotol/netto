using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Equipment;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// YearSimulationTests' made-up company, 100,000 of profit before the director, with the IT and phone the
// company provides all year (Fixtures.LaptopPhoneAndInternet): a PC, a mobile phone, its subscription and an
// internet connection, 216 of benefit (EquipmentCalculatorTests), and 1,500 of yearly costs. Every expected
// value is derived by hand from the official amounts (art. 18 §3, 10 AR/CIR 92, art. 215 al. 3 CIR 92), no
// Tax-Calc run: the pay's own figures are GrossToNet's on the pay with the benefit, which GrossToNetTests hold
// to Tax-Calc.
public class YearSimulationEquipmentTests
{
    public static TheoryData<decimal, decimal, decimal, decimal> Setups => new()
    {
        { 50_000m, 100_000m, 0m, 0m },
        { 21_000m, 100_000m, 20_000m, 16m },
        { 160_000m, 50_000m, 0m, 0m },
        { 0m, 300_000m, 50_000m, 216m },
        { 5_000m, 9_000m, 0m, 0m },
    };

    // The benefit is pay: in the taxable pay, the social contributions' base, the income tax and the withholding,
    // exactly as GrossToNet takes a benefit in kind of 216.
    [Fact]
    public void Run_equipment_taxesItsBenefitAsPay()
    {
        var (pay, _) = GrossToNet.Compute(
            2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, ContributionsPaidBy.Director, new TaxedWithPay { BenefitInKind = 216m });

        var result = Run(Scenario(equipment: LaptopPhoneAndInternet));

        (result.DirectorPay.BenefitInKind, result.DirectorPay.TaxablePay, result.DirectorPay.SocialContributions, result.DirectorPay.NetInPocket)
            .ShouldBe((216m, 50_216m, pay!.SocialContributions, pay.NetInPocket));
    }

    // The set's 1,500 come off the 100,000 the split shares, deductible in full with nothing added back: the
    // company's taxable income is the 98,500 less the 50,000 of pay. Art. 215 al. 3, 4° tests the pay with the
    // benefit, 50,216, which the taxable income does not exceed, so the reduced rate stays: 20% of 48,500.
    [Fact]
    public void Run_equipment_deductsItsCostsInFullAndCountsItsBenefitInThePayArticle215Tests()
    {
        var result = Run(Scenario(equipment: LaptopPhoneAndInternet));

        (result.ProfitBeforePay, result.DisallowedExpenses, result.Company.TaxableIncome, result.Company.DirectorPay, result.Company.FlatRateBenefits,
                result.Company.Condition, result.CorporateTax)
            .ShouldBe((98_500m, 0m, 48_500m, 50_216m, 216m, ReducedRateCondition.TaxableIncomeNotAbovePay, 9_700m));
    }

    // Art. 215 al. 3, 5°: the flat-rate benefits are worth no more than 20% of the pay with them. Beside 800 of
    // pay, 216 is more than 20% of 1,016, 203.20, and the rate is lost on it; beside 900, 20% of 1,116 is 223.20
    // and the pay test decides.
    [Theory]
    [InlineData(800, ReducedRateCondition.ExcessBenefits)]
    [InlineData(900, ReducedRateCondition.MinimumPayNotMet)]
    public void Run_equipmentBesideASmallPay_weighsItsBenefitAgainstItsShareOfThePay(int pay, ReducedRateCondition condition)
    {
        var result = Run(Scenario(directorPay: pay, equipment: LaptopPhoneAndInternet));

        (result.Company.Condition, result.Company.FlatRateBenefits).ShouldBe((condition, 216m));
    }

    // Beside the car, both benefits are taxed with the pay and weighed together: 1,714.29 + 216.
    [Fact]
    public void Run_equipmentAndCar_addTheirBenefits()
    {
        var result = Run(Scenario(equipment: LaptopPhoneAndInternet, car: ElectricCar));

        (result.DirectorPay.BenefitInKind, result.Company.FlatRateBenefits, result.ProfitBeforePay, result.Equipment.Benefit, result.Car.Benefit)
            .ShouldBe((1_930.29m, 1_930.29m, 90_500m, 216m, 1_714.29m));
    }

    // The director's 16 leave their pocket for the company and come off the benefit taxed, 200.
    [Fact]
    public void Run_equipmentWithAContribution_paysItToTheCompanyOutOfThePocket()
    {
        var result = Run(Scenario(equipment: LaptopPhoneAndInternet with { Contribution = 16m }));

        (result.ProfitBeforePay, result.DirectorPay.BenefitInKind, result.DisallowedExpenses, result.Equipment.Contribution).ShouldBe((98_516m, 200m, 0m, 16m));
    }

    // The set's costs are a cost of the company, like the car's: the layers, the pocket and the company add up to
    // the profit before the director less them, the contribution moving inside that whole.
    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_equipment_layersPocketAndCompanyAddUpToTheProfitBeforeTheDirectorLessItsCosts(
        decimal directorPay, decimal profitBeforeDirector, decimal reserve, decimal contribution)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, revenue: profitBeforeDirector + 20_000m,
            equipment: LaptopPhoneAndInternet with { Contribution = contribution }));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforeDirector - 1_500m);
    }

    // What the set brings the ledger, in order: its costs, which only set the company's profit, the benefit of
    // its pieces, the contribution leaving the pocket, and the benefit taxed as pay.
    [Fact]
    public void Run_equipment_listsItsSteps()
    {
        var result = Run(Scenario(equipment: LaptopPhoneAndInternet with { Contribution = 16m }));

        result.Steps.Where(step => step.Event == TaxableEvent.Equipment).Select(step => (step.RuleKey, step.Layer, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("equipment_costs", Layer.Corporate, Track.TaxBaseOnly, 1_500m),
            ("equipment_benefit", Layer.Personal, Track.TaxBaseOnly, 216m),
            ("equipment_contribution", Layer.Personal, Track.InPocket, -16m),
            ("equipment_benefit_taxed", Layer.Personal, Track.TaxBaseOnly, 200m),
        });
    }

    // The benefit's step cites each flat value it used, in the article's order, and the circulars it reads them
    // by: per appliance, a subscription per number, and the days of provision when it is part of the year.
    [Fact]
    public void Run_equipmentPartOfTheYear_citesItsValuesAndTheDaysOfProvision()
    {
        var rules = EquipmentRules.IncomeYear2026;

        var benefit = Run(Scenario(equipment: LaptopPhoneAndInternet with { ProvidedFrom = new DateOnly(2026, 9, 17) }))
            .Steps.Single(step => step.RuleKey == "equipment_benefit");

        benefit.AmountsUsed.ShouldBe([rules.PcBenefit, rules.MobilePhoneBenefit, rules.InternetBenefit, rules.PhoneSubscriptionBenefit]);
        benefit.AlsoCites.ShouldBe([rules.PerAppliance, rules.SubscriptionPerNumber, rules.PartOfYear]);
    }

    [Fact]
    public void Run_equipmentAllYearWithoutASubscription_citesNeitherTheDaysNorTheNumbers()
    {
        var rules = EquipmentRules.IncomeYear2026;

        var benefit = Run(Scenario(equipment: LaptopPhoneAndInternet with { PhoneSubscriptions = 0 })).Steps.Single(step => step.RuleKey == "equipment_benefit");

        benefit.AlsoCites.ShouldBe([rules.PerAppliance]);
    }

    // Without a set, its steps stay out of the ledger and the year is the one without it.
    [Fact]
    public void Run_withoutEquipment_listsNoneOfItsSteps()
    {
        var result = Run(Scenario());

        (result.Steps.Any(step => step.Event == TaxableEvent.Equipment), result.Equipment).ShouldBe((false, EquipmentResult.None));
    }

    // The flat computer and internet allowances beside the PC and the internet connection the company provides:
    // the engine refuses the plan, on the PC first.
    [Fact]
    public void Run_equipmentBesideTheFlatAllowances_isRefused()
    {
        YearSimulation.Run(Scenario(allowances: FlatRateAllowances, equipment: LaptopPhoneAndInternet)).Error.ShouldBe(EquipmentCalculator.PcWithComputerAllowance);
    }

    // What the split shares is the profit less the set's costs, as the year runs, while the rules refuse it too.
    [Fact]
    public void ProfitBeforePay_equipment_isTheProfitLessItsCostsLessTheContribution()
    {
        YearSimulation.ProfitBeforePay(Scenario(allowances: FlatRateAllowances, equipment: LaptopPhoneAndInternet with { Contribution = 16m }))
            .ShouldBe(100_000m - 2_418.44m - 1_484m);
    }
}

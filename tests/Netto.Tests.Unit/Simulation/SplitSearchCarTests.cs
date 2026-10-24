using Netto.Core.CompanyCar;
using Netto.Core.CorporateTax;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// SplitSearchTests' made-up company, 100,000 of profit before the director, with YearSimulationCarTests'
// electric car: 8,000 of yearly costs and a benefit of 1,714.29, of which the company adds back 291.43.
// The split shares the 92,000 the car leaves, and every pay it draws is the year at that pay, the
// car held, the benefit counted in the pay art. 215 tests and held within 20% of it.
public class SplitSearchCarTests
{
    private static readonly CompanyStatus FirstFourTaxPeriods = new(SmallCompany: true, FirstFourTaxPeriods: true);

    [Fact]
    public void Run_2026Car_drawsEveryPayFromNoneToTheProfitItLeaves()
    {
        var pays = Search(Scenario(car: ElectricCar)).Curve.Select(level => level.Pay).ToList();

        (pays[0], pays[^1]).ShouldBe((0m, 92_000m));
    }

    public static TheoryData<ContributionsPaidBy, bool> Payers => new()
    {
        { ContributionsPaidBy.Director, false },
        { ContributionsPaidBy.Company, false },
        { ContributionsPaidBy.Director, true },
        { ContributionsPaidBy.Company, true },
    };

    [Theory]
    [MemberData(nameof(Payers))]
    public void Run_2026Car_eachLevelIsTheYearAtThatPay(ContributionsPaidBy paidBy, bool asWithheld)
    {
        var scenario = Scenario(car: ElectricCar, paidBy: paidBy, taxPrepayments: asWithheld ? TaxPrepayments.AsWithheld : Withheld);

        Search(scenario).Curve.ShouldAllBe(level => level == LevelOf(scenario, level.Pay));
    }

    // The pay test with the benefit counted: half of what the 92,291.43 of taxable income at no pay
    // exceeds the benefit by, 45,288.57, under the minimum less the benefit, 49,285.71.
    [Fact]
    public void Run_2026Car_keepsTheRateFromThePayTestWithTheBenefitCounted()
    {
        Search(Scenario(car: ElectricCar)).SmallestPayKeepingReducedRate.ShouldBe(45_288.57m);
    }

    // In its first four tax periods the company needs no minimum pay, but the benefit must stay within
    // 20% of the pay with it: from 4 × 1,714.29.
    [Fact]
    public void Run_2026CarInFirstFourTaxPeriods_keepsTheRateFromFourTimesTheBenefit()
    {
        Search(Scenario(company: FirstFourTaxPeriods, car: ElectricCar)).SmallestPayKeepingReducedRate.ShouldBe(6_857.16m);
    }

    public static TheoryData<CompanyStatus, ContributionsPaidBy> StatusesAndPayers => new()
    {
        { SmallPastStartUp, ContributionsPaidBy.Director },
        { SmallPastStartUp, ContributionsPaidBy.Company },
        { FirstFourTaxPeriods, ContributionsPaidBy.Director },
        { FirstFourTaxPeriods, ContributionsPaidBy.Company },
    };

    // Wherever the search says the rate starts, the year keeps it there and loses it a cent lower.
    [Theory]
    [MemberData(nameof(StatusesAndPayers))]
    public void Run_2026Car_theYearKeepsTheRateFromTheSmallestPayAndLosesItACentLower(CompanyStatus company, ContributionsPaidBy paidBy)
    {
        var scenario = Scenario(company: company, paidBy: paidBy, car: ElectricCar);
        var smallest = Search(scenario).SmallestPayKeepingReducedRate.ShouldNotBeNull();

        (Run(scenario with { DirectorPay = smallest }).Company.Condition.AppliesReducedRate(),
                Run(scenario with { DirectorPay = smallest - 0.01m }).Company.Condition.AppliesReducedRate())
            .ShouldBe((true, false));
    }

    // A benefit no pay up to the profit can hold within 20%: 9,000 of profit before the director,
    // 1,000 left once the car is paid, and 4 × 1,714.29 needed. The rate does not depend on any pay
    // the split can give.
    [Fact]
    public void Run_2026CarBeyondItsShareOfAnyPay_doesNotWeighTheRate()
    {
        var result = Search(Scenario(revenue: 29_000m, car: ElectricCar));

        (result.SmallestPayKeepingReducedRate, result.BestKeepingReducedRate, result.BestLosingReducedRate)
            .ShouldBe(((decimal?)null, (PayLevel?)null, (PayLevel?)null));
    }

    [Fact]
    public void Run_2026CarBeyondItsShareOfAnyPayCompanyPays_doesNotWeighTheRate()
    {
        Search(Scenario(revenue: 29_000m, paidBy: ContributionsPaidBy.Company, car: ElectricCar)).SmallestPayKeepingReducedRate.ShouldBeNull();
    }

    [Fact]
    public void Run_carWithNegativeCosts_isRefusedWithTheCalculatorsKey()
    {
        SplitSearch.Run(Scenario(car: ElectricCar with { YearlyCosts = -0.01m })).ShouldBe((null, CarCalculator.YearlyCostsNegative));
    }

    private static PayLevel LevelOf(Scenario scenario, decimal pay)
    {
        var year = Run(scenario with { DirectorPay = pay });

        return new PayLevel(pay, year.NetInPocket, year.Dividend.Dividend, year.KeptInCompany, year.Company.Condition, year.MaximumDividend >= scenario.Reserve);
    }
}

using Netto.Core.CompanyCar;
using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.Simulation;

// SplitSearchTests' made-up company, 100,000 of profit before the director, with YearSimulationCarTests'
// electric car: 8,000 of yearly costs and a benefit of 1,714.29, of which the company adds back 291.43.
// The split shares the 92,000 the car leaves, and every pay it draws is the year at that pay, the
// car held, the benefit counted in the pay art. 215 tests and held within 20% of it.
public class SplitSearchCarTests
{
    private static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

    private static readonly CompanyStatus FirstFourTaxPeriods = new(SmallCompany: true, FirstFourTaxPeriods: true);

    private static readonly AdvancePayments Prepaid = new(100_000m, 0m, 0m, 0m);

    private static readonly TaxPrepayments Withheld = new(1_000_000m, AdvancePayments.None);

    private static readonly CarFacts ElectricCar = new(50_000m, new DateOnly(2026, 1, 10), 8_000m, false, 0m);

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

        (Year(scenario with { DirectorPay = smallest }).Company.Condition.AppliesReducedRate(),
                Year(scenario with { DirectorPay = smallest - 0.01m }).Company.Condition.AppliesReducedRate())
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

    private static Scenario Scenario(
        decimal revenue = 120_000m,
        CompanyStatus? company = null,
        TaxPrepayments? taxPrepayments = null,
        ContributionsPaidBy paidBy = ContributionsPaidBy.Director,
        CarFacts? car = null) =>
        new(2026, revenue, 20_000m, company ?? SmallPastStartUp, 50_000m, MunicipalRates.Entered(0.07m), 0m, WithholdingCategory.VvprbisThirdYearOnwards)
        {
            AdvancePayments = Prepaid,
            TaxPrepayments = taxPrepayments ?? Withheld,
            ContributionsPaidBy = paidBy,
            Car = car ?? CarFacts.None,
        };

    private static SplitSearchResult Search(Scenario scenario)
    {
        var (value, error) = SplitSearch.Run(scenario);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    private static YearResult Year(Scenario scenario)
    {
        var (value, error) = YearSimulation.Run(scenario);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }

    private static PayLevel LevelOf(Scenario scenario, decimal pay)
    {
        var year = Year(scenario with { DirectorPay = pay });

        return new PayLevel(pay, year.NetInPocket, year.Dividend.Dividend, year.KeptInCompany, year.Company.Condition, year.MaximumDividend >= scenario.Reserve);
    }
}

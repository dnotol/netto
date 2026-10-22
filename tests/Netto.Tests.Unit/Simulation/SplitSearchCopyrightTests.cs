using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.Simulation;

// SplitSearchTests' made-up company, 100,000 of profit before the director, paying a copyright royalty
// of 20,000: the split shares the 80,000 it leaves, and every pay it draws is the year at that pay, the
// royalty held, its limits moving with the pay, since 30% is measured on the royalty and the pay.
public class SplitSearchCopyrightTests
{
    private static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

    private static readonly AdvancePayments Prepaid = new(100_000m, 0m, 0m, 0m);

    private static readonly TaxPrepayments Withheld = new(1_000_000m, AdvancePayments.None);

    private static readonly CopyrightRoyalty Royalty = new(20_000m);

    [Fact]
    public void Run_2026Royalty_drawsEveryPayFromNoneToTheProfitItLeaves()
    {
        var pays = Search(Scenario()).Curve.Select(level => level.Pay).ToList();

        (pays[0], pays[^1]).ShouldBe((0m, 80_000m));
    }

    public static TheoryData<ContributionsPaidBy, bool, bool> Payers => new()
    {
        { ContributionsPaidBy.Director, false, true },
        { ContributionsPaidBy.Company, false, true },
        { ContributionsPaidBy.Director, true, true },
        { ContributionsPaidBy.Company, true, false },
    };

    [Theory]
    [MemberData(nameof(Payers))]
    public void Run_2026Royalty_eachLevelIsTheYearAtThatPay(ContributionsPaidBy paidBy, bool asWithheld, bool averageWithinCap)
    {
        var scenario = Scenario(paidBy: paidBy, taxPrepayments: asWithheld ? TaxPrepayments.AsWithheld : Withheld) with
        {
            Copyright = Royalty with { AverageWithinCap = averageWithinCap },
        };

        Search(scenario).Curve.ShouldAllBe(level => level == LevelOf(scenario, level.Pay));
    }

    // The royalty never counts in the minimum pay: the rate starts where it would for a company whose
    // profit before pay is the 80,000 the royalty leaves, half of it.
    [Fact]
    public void Run_2026Royalty_keepsTheRateFromThePayTestWithoutTheRoyalty()
    {
        var withRoyalty = Search(Scenario()).SmallestPayKeepingReducedRate;
        var without = Search(Scenario(revenue: 100_000m) with { Copyright = CopyrightRoyalty.None }).SmallestPayKeepingReducedRate;

        (withRoyalty, without).ShouldBe((40_000m, 40_000m));
    }

    [Fact]
    public void Run_2026Royalty_theYearKeepsTheRateFromTheSmallestPayAndLosesItACentLower()
    {
        var scenario = Scenario();
        var smallest = Search(scenario).SmallestPayKeepingReducedRate.ShouldNotBeNull();

        (Year(scenario with { DirectorPay = smallest }).Company.Condition.AppliesReducedRate(),
                Year(scenario with { DirectorPay = smallest - 0.01m }).Company.Condition.AppliesReducedRate())
            .ShouldBe((true, false));
    }

    // When the company pays the contributions, it pays those on the part taxed as pay too, which lower
    // its taxable income but not the pay art. 215 tests: the year keeps the rate from the smallest pay,
    // and the search, measuring that pay without them, may start it a few hundred euros late.
    [Fact]
    public void Run_2026RoyaltyCompanyPays_theYearKeepsTheRateFromTheSmallestPay()
    {
        var scenario = Scenario(paidBy: ContributionsPaidBy.Company);
        var smallest = Search(scenario).SmallestPayKeepingReducedRate.ShouldNotBeNull();

        Year(scenario with { DirectorPay = smallest }).Company.Condition.AppliesReducedRate().ShouldBeTrue();
    }

    [Fact]
    public void Run_negativeRoyalty_isRefusedWithTheCalculatorsKey()
    {
        SplitSearch.Run(Scenario() with { Copyright = new CopyrightRoyalty(-0.01m) }).ShouldBe((null, CopyrightCalculator.RoyaltyNegative));
    }

    private static Scenario Scenario(
        decimal revenue = 120_000m,
        TaxPrepayments? taxPrepayments = null,
        ContributionsPaidBy paidBy = ContributionsPaidBy.Director) =>
        new(2026, revenue, 20_000m, SmallPastStartUp, 50_000m, MunicipalRates.Entered(0.07m), 0m, WithholdingCategory.VvprbisThirdYearOnwards)
        {
            AdvancePayments = Prepaid,
            TaxPrepayments = taxPrepayments ?? Withheld,
            ContributionsPaidBy = paidBy,
            Copyright = Royalty,
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

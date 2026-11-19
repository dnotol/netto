using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// SplitSearchTests' made-up company, 100,000 of profit before the director, paying a copyright royalty
// of 20,000: the split shares the 80,000 it leaves, and every pay it draws is the year at that pay, the
// royalty held, its limits moving with the pay, since 30% is measured on the royalty and the pay.
public class SplitSearchCopyrightTests
{
    private static readonly CopyrightRoyalty Royalty = new(20_000m);

    // A royalty that follows the pay, for 75% of the working time spent creating.
    private static readonly CopyrightRoyalty Following = new(0m) { CreativeShare = 0.75m, FollowsPay = true };

    [Fact]
    public void Run_2026Royalty_drawsEveryPayFromNoneToTheProfitItLeaves()
    {
        var pays = Search(Scenario(royalty: Royalty)).Curve.Select(level => level.Pay).ToList();

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
        var withRoyalty = Search(Scenario(royalty: Royalty)).SmallestPayKeepingReducedRate;
        var without = Search(Scenario(revenue: 100_000m)).SmallestPayKeepingReducedRate;

        (withRoyalty, without).ShouldBe((40_000m, 40_000m));
    }

    [Fact]
    public void Run_2026Royalty_theYearKeepsTheRateFromTheSmallestPayAndLosesItACentLower()
    {
        var scenario = Scenario(royalty: Royalty);
        var smallest = Search(scenario).SmallestPayKeepingReducedRate.ShouldNotBeNull();

        (Run(scenario with { DirectorPay = smallest }).Company.Condition.AppliesReducedRate(),
                Run(scenario with { DirectorPay = smallest - 0.01m }).Company.Condition.AppliesReducedRate())
            .ShouldBe((true, false));
    }

    // When the company pays the contributions, it pays those on the part taxed as pay too, which lower
    // its taxable income but not the pay art. 215 tests: the year keeps the rate from the smallest pay,
    // and the search, measuring that pay without them, may start it a few hundred euros late.
    [Fact]
    public void Run_2026RoyaltyCompanyPays_theYearKeepsTheRateFromTheSmallestPay()
    {
        var scenario = Scenario(royalty: Royalty, paidBy: ContributionsPaidBy.Company);
        var smallest = Search(scenario).SmallestPayKeepingReducedRate.ShouldNotBeNull();

        Run(scenario with { DirectorPay = smallest }).Company.Condition.AppliesReducedRate().ShouldBeTrue();
    }

    [Fact]
    public void Run_negativeRoyalty_isRefusedWithTheCalculatorsKey()
    {
        SplitSearch.Run(Scenario() with { Copyright = new CopyrightRoyalty(-0.01m) }).ShouldBe((null, CopyrightCalculator.RoyaltyNegative));
    }

    [Fact]
    public void Run_royaltyAboveWhatTheCompanyHasBeforePay_isRefusedWithTheCalculatorsKey()
    {
        SplitSearch.Run(Scenario() with { Copyright = new CopyrightRoyalty(100_000.01m) }).ShouldBe((null, CopyrightCalculator.RoyaltyAboveProfit));
    }

    [Fact]
    public void Run_2026Royalty_eachLevelCarriesIt()
    {
        Search(Scenario(royalty: Royalty)).Curve.ShouldAllBe(level => level.Royalty == 20_000m);
    }

    // A royalty that follows the pay: each pay the search draws is the year at that pay with what a ruling
    // accepts there, 75% of the time creating.
    [Theory]
    [MemberData(nameof(Payers))]
    public void Run_2026RoyaltyFollowingThePay_eachLevelIsTheYearAtThatPay(ContributionsPaidBy paidBy, bool asWithheld, bool averageWithinCap)
    {
        var scenario = Scenario(paidBy: paidBy, taxPrepayments: asWithheld ? TaxPrepayments.AsWithheld : Withheld) with
        {
            Copyright = Following with { AverageWithinCap = averageWithinCap },
        };

        Search(scenario).Curve.ShouldAllBe(level => level == LevelOf(scenario, level.Pay));
    }

    [Fact]
    public void Run_2026RoyaltyFollowingThePay_eachLevelCarriesWhatARulingAcceptsAtItsPay()
    {
        Search(FollowingScenario()).Curve.ShouldAllBe(level => level.Royalty == CopyrightCalculator.Ceiling(CopyrightRules.IncomeYear2026, Following, level.Pay));
    }

    // Up to the largest pay the 100,000 pays together with what a ruling accepts beside it.
    [Fact]
    public void Run_2026RoyaltyFollowingThePay_drawsEveryPayFromNoneToThePayCeiling()
    {
        var pays = Search(FollowingScenario()).Curve.Select(level => level.Pay).ToList();

        (pays[0], pays[^1]).ShouldBe((0m, 81_250m));
    }

    [Theory]
    [MemberData(nameof(FollowingPayers))]
    public void Run_2026RoyaltyFollowingThePay_noPayWithinAThousandEurosLeavesMore(ContributionsPaidBy paidBy)
    {
        var scenario = FollowingScenario(paidBy);
        var best = Search(scenario).Best.ShouldNotBeNull();

        var around = Enumerable.Range(-1_000, 2_001).Select(euros => best.Pay + euros).Where(pay => pay is >= 0m and <= 81_250m)
            .Select(pay => Run(scenario with { DirectorPay = pay }).NetInPocket);

        around.ShouldAllBe(net => net <= best.NetInPocket);
    }

    public static TheoryData<ContributionsPaidBy> FollowingPayers => [ContributionsPaidBy.Director, ContributionsPaidBy.Company];

    [Theory]
    [MemberData(nameof(FollowingPayers))]
    public void Run_2026RoyaltyFollowingThePay_theYearKeepsTheRateFromTheSmallestPayAndLosesItACentLower(ContributionsPaidBy paidBy)
    {
        var scenario = FollowingScenario(paidBy);
        var smallest = Search(scenario).SmallestPayKeepingReducedRate.ShouldNotBeNull();

        (Run(scenario with { DirectorPay = smallest }).Company.Condition.AppliesReducedRate(),
                Run(scenario with { DirectorPay = smallest - 0.01m }).Company.Condition.AppliesReducedRate())
            .ShouldBe((true, false));
    }

    // The royalty lowers the taxable income art. 215 compares the pay with, and rises with the pay: the
    // rate is kept from P ≥ (100,000 − R(P)) / 2, 44,827.59 with its 10,344.82, where a company paying no
    // royalty keeps it from half its 100,000.
    [Fact]
    public void Run_2026RoyaltyFollowingThePay_keepsTheRateFromLessPayThanWithoutARoyalty()
    {
        var following = Search(FollowingScenario()).SmallestPayKeepingReducedRate;
        var without = Search(Scenario()).SmallestPayKeepingReducedRate;

        (following, without).ShouldBe((44_827.59m, 50_000m));
    }

    // 400,000 shared and all of the time creating: what a ruling accepts reaches the cap at a pay of
    // 231,660, and the pay runs on to 400,000 less the cap.
    [Fact]
    public void Run_2026RoyaltyFollowingPastTheCap_eachLevelIsTheYearAtThatPayUpToThePayCeiling()
    {
        var scenario = FollowingScenario(share: 1m, revenue: 420_000m);

        var curve = Search(scenario).Curve;

        curve.ShouldSatisfyAllConditions(
            () => curve[^1].Pay.ShouldBe(322_780m),
            () => curve.ShouldAllBe(level => level == LevelOf(scenario, level.Pay)));
    }

    // While a ruling accepts nothing, the search is the search without a royalty.
    [Fact]
    public void Run_2026RoyaltyFollowingWithoutAShare_isTheSearchWithoutARoyalty()
    {
        var withoutShare = Search(Scenario() with { Copyright = Following with { CreativeShare = null } });

        withoutShare.Curve.ShouldBe(Search(Scenario()).Curve);
    }

    private static Scenario FollowingScenario(ContributionsPaidBy paidBy = ContributionsPaidBy.Director, decimal share = 0.75m, decimal revenue = 120_000m) =>
        Scenario(revenue: revenue, paidBy: paidBy) with { Copyright = Following with { CreativeShare = share } };

    private static PayLevel LevelOf(Scenario scenario, decimal pay)
    {
        var year = Run(scenario with { DirectorPay = pay });

        return new PayLevel(pay, year.NetInPocket, year.Dividend.Dividend, year.KeptInCompany, year.Company.Condition, year.KeepsReserve)
        {
            Royalty = year.Copyright.Royalty,
        };
    }
}

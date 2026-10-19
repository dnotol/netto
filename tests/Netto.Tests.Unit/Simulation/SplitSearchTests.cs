using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;

namespace Netto.Tests.Simulation;

// Income year 2026 at a 7% municipal rate, for YearSimulationTests' made-up company: 120,000 of
// revenue and 20,000 of running costs, a small company past its first four tax periods, a VVPRbis
// dividend from the third distribution, nothing kept unless a test says otherwise. Every level is
// YearSimulation's figure at that pay; the best pays were found by running the year at every euro
// from 0 to the profit before pay (2026-10-19). On this company the net in pocket climbs to 20,799,
// where it peaks, and jumps at 50,000, half the profit, where the reduced corporate rate is kept.
public class SplitSearchTests
{
    private static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

    [Fact]
    public void Run_2026Example_findsThePayThatLeavesTheMost()
    {
        var best = Search(Scenario()).Best.ShouldNotBeNull();

        (best.Pay, best.NetInPocket, best.Dividend).ShouldBe((20_799m, 64_712.81m, 59_400.75m));
    }

    [Fact]
    public void Run_2026Example_bestPayGivesUpTheReducedRate()
    {
        Search(Scenario()).Best.ShouldNotBeNull().Condition.ShouldBe(ReducedRateCondition.MinimumPayNotMet);
    }

    [Fact]
    public void Run_2026Example_saysTheSmallestPayKeepingTheReducedRate()
    {
        Search(Scenario()).SmallestPayKeepingReducedRate.ShouldBe(50_000m);
    }

    [Fact]
    public void Run_2026Example_weighsTheBestPayKeepingTheReducedRateAgainstTheBestLosingIt()
    {
        var result = Search(Scenario());

        (result.BestKeepingReducedRate?.Pay, result.BestKeepingReducedRate?.NetInPocket, result.BestLosingReducedRate?.Pay)
            .ShouldBe(((decimal?)50_000m, (decimal?)62_529.24m, (decimal?)20_799m));
    }

    // Keeping the rate takes the 51,000 minimum here, and costs 345.02 against the best pay.
    [Fact]
    public void Run_2026Profit150000_keepsTheRateAtTheMinimumAndTheBestStillGivesItUp()
    {
        var result = Search(Scenario(revenue: 170_000m));

        (result.BestKeepingReducedRate?.Pay, result.BestKeepingReducedRate?.NetInPocket, result.Best?.Pay, result.Best?.NetInPocket)
            .ShouldBe(((decimal?)51_000m, (decimal?)95_117.79m, (decimal?)20_799m, (decimal?)95_462.81m));
    }

    [Fact]
    public void Run_2026Example_noPayWithinAThousandEurosLeavesMore()
    {
        var best = Search(Scenario()).Best.ShouldNotBeNull();

        var around = Enumerable.Range(-1_000, 2_001).Select(euros => Year(Scenario(directorPay: best.Pay + euros)).NetInPocket);

        around.ShouldAllBe(net => net <= best.NetInPocket);
    }

    [Fact]
    public void Run_2026Example_drawsEveryPayFromNoneToTheProfitBeforePay()
    {
        var pays = Search(Scenario()).Curve.Select(level => level.Pay).ToList();

        (pays[0], pays[^1], pays.Count(pay => pay % 1_000m == 0m)).ShouldBe((0m, 100_000m, 101));
    }

    [Fact]
    public void Run_2026Example_drawsThePaysInOrder()
    {
        var pays = Search(Scenario()).Curve.Select(level => level.Pay).ToList();

        pays.ShouldBe(pays.Order().Distinct());
    }

    // A cent under the smallest pay that keeps the reduced rate, and that pay, so the jump is drawn.
    [Fact]
    public void Run_2026Example_drawsTheJumpWhereTheReducedRateIsKept()
    {
        var curve = Search(Scenario()).Curve;

        curve.Where(level => level.Pay is 49_999.99m or 50_000m).Select(level => level.Condition.AppliesReducedRate())
            .ShouldBe(new[] { false, true });
    }

    [Fact]
    public void Run_2026Example_drawsTheBestPay()
    {
        var result = Search(Scenario());

        result.Curve.ShouldContain(result.Best.ShouldNotBeNull());
    }

    [Fact]
    public void Run_2026Example_eachLevelIsTheYearAtThatPay()
    {
        var curve = Search(Scenario()).Curve;

        curve.ShouldAllBe(level => level == LevelOf(level.Pay, 0m));
    }

    [Fact]
    public void Run_2026Reserve40000_bestPayKeepsIt()
    {
        var best = Search(Scenario(reserve: 40_000m)).Best.ShouldNotBeNull();

        (best.Pay, best.NetInPocket, best.KeptInCompany).ShouldBe((20_799m, 31_912.81m, 40_000m));
    }

    // The best pay overall, 20,799, would leave 59,400.75 of profit after tax, under the reserve. Up
    // to 20,000 the profit after tax, 75% of the rest, covers 60,000; the pocket peaks where the
    // dividend reaches the exempt 833 (18,890 pays 832.50), and no pay that keeps the rate covers it.
    [Fact]
    public void Run_2026Reserve60000_neverChoosesAPayThatCannotKeepIt()
    {
        var result = Search(Scenario(reserve: 60_000m));

        (result.Best?.Pay, result.Best?.NetInPocket, result.Best?.Dividend, result.BestKeepingReducedRate)
            .ShouldBe(((decimal?)18_890m, (decimal?)15_273.13m, (decimal?)832.50m, (PayLevel?)null));
    }

    [Fact]
    public void Run_2026Reserve60000_marksWhichPaysKeepIt()
    {
        var curve = Search(Scenario(reserve: 60_000m)).Curve;

        curve.ShouldAllBe(level => level.KeepsReserve == level.Pay <= 20_000m);
    }

    // 60,000 kept from earlier years above a 30,000 reserve are paid out at every pay, at 18% once the
    // dividend is past the exempt 833: they add the same to every pay, so the best pay stays 20,799.
    [Fact]
    public void Run_2026EarlierProfits_addTheSameAtEveryPay()
    {
        var best = Search(Scenario(reserve: 30_000m, earlierProfits: 60_000m)).Best.ShouldNotBeNull();

        (best.Pay, best.NetInPocket).ShouldBe((20_799m, 89_312.81m));
    }

    // With 60,000 kept from earlier years every pay keeps a 60,000 reserve, which the year's own
    // profit after tax covers only up to 20,000 (Run_2026Reserve60000_marksWhichPaysKeepIt).
    [Fact]
    public void Run_2026EarlierProfitsCoveringTheReserve_everyPayKeepsIt()
    {
        Search(Scenario(reserve: 60_000m, earlierProfits: 60_000m)).Curve.ShouldAllBe(level => level.KeepsReserve);
    }

    // The most the year can keep is 75,000, at no pay.
    [Fact]
    public void Run_2026ReserveNoPayCanKeep_namesNoBestPay()
    {
        var result = Search(Scenario(reserve: 80_000m));

        (result.Best, result.BestKeepingReducedRate, result.BestLosingReducedRate).ShouldBe(((PayLevel?)null, (PayLevel?)null, (PayLevel?)null));
    }

    [Fact]
    public void Run_2026FirstFourTaxPeriods_doesNotWeighTheRate()
    {
        var result = Search(Scenario(company: new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: true)));

        (result.SmallestPayKeepingReducedRate, result.BestKeepingReducedRate, result.BestLosingReducedRate)
            .ShouldBe(((decimal?)null, (PayLevel?)null, (PayLevel?)null));
    }

    [Fact]
    public void Run_2026FirstFourTaxPeriods_findsTheBestPayAtTheReducedRate()
    {
        var best = Search(Scenario(company: new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: true))).Best.ShouldNotBeNull();

        (best.Pay, best.NetInPocket).ShouldBe((20_799m, 67_960.06m));
    }

    [Fact]
    public void Run_2026NotSmallCompany_doesNotWeighTheRate()
    {
        var result = Search(Scenario(company: new CompanyStatus(SmallCompany: false, FirstFourTaxPeriods: false)));

        (result.SmallestPayKeepingReducedRate, result.BestKeepingReducedRate, result.BestLosingReducedRate, result.Best?.Pay)
            .ShouldBe(((decimal?)null, (PayLevel?)null, (PayLevel?)null, (decimal?)20_799m));
    }

    // INASTI's minimum contribution is owed whatever the pay, so on 10,000 of profit taking it all as
    // pay leaves the most; the rate is kept from 5,000.
    [Fact]
    public void Run_2026Profit10000_paysItAllWhenThatLeavesTheMost()
    {
        var result = Search(Scenario(revenue: 30_000m));

        (result.Best?.Pay, result.Best?.NetInPocket, result.BestLosingReducedRate?.Pay, result.BestLosingReducedRate?.NetInPocket)
            .ShouldBe(((decimal?)10_000m, (decimal?)6_438.32m, (decimal?)4_999m, (decimal?)4_662.87m));
    }

    [Theory]
    [InlineData(20_000)]
    [InlineData(15_000)]
    public void Run_2026NoProfitBeforePay_hasOnlyNoPay(int revenue)
    {
        var result = Search(Scenario(revenue: revenue));

        (result.Curve.Select(level => level.Pay).Single(), result.Best?.Pay, result.Best?.NetInPocket)
            .ShouldBe((0m, (decimal?)0m, (decimal?)-3_561.68m));
    }

    // 1,000,000 of profit is drawn every 10,000.
    [Fact]
    public void Run_2026LargeProfit_drawsAboutAHundredPays()
    {
        var pays = Search(Scenario(revenue: 1_020_000m)).Curve.Select(level => level.Pay).ToList();

        (pays.Count(pay => pay % 10_000m == 0m), pays.Count <= 110).ShouldBe((101, true));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(77_777)]
    public void Run_2026_ignoresTheScenariosOwnPay(int directorPay)
    {
        var result = Search(Scenario(directorPay: directorPay));
        var withoutPay = Search(Scenario(directorPay: 0m));

        (result.Best, result.Curve.SequenceEqual(withoutPay.Curve)).ShouldBe((withoutPay.Best, true));
    }

    [Fact]
    public void Run_negativeReserve_isRefusedWithYearSimulationsKey()
    {
        var (value, error) = SplitSearch.Run(Scenario(reserve: -0.01m));

        (value, error).ShouldBe((null, YearSimulation.ReserveNegative));
    }

    [Fact]
    public void Run_negativeRevenue_isRefusedWithYearSimulationsKey()
    {
        var (value, error) = SplitSearch.Run(Scenario(revenue: -0.01m));

        (value, error).ShouldBe((null, YearSimulation.RevenueNegative));
    }

    [Fact]
    public void Run_incomeYear2027_isRefused()
    {
        var (value, error) = SplitSearch.Run(Scenario() with { IncomeYear = 2027 });

        (value, error).ShouldBe((null, YearSimulation.IncomeYearNotSupported));
    }

    private static Scenario Scenario(
        decimal directorPay = 50_000m,
        decimal reserve = 0m,
        decimal revenue = 120_000m,
        CompanyStatus? company = null,
        decimal earlierProfits = 0m) =>
        new(2026, revenue, 20_000m, company ?? SmallPastStartUp, directorPay, MunicipalRates.Entered(0.07m), reserve,
            WithholdingCategory.VvprbisThirdYearOnwards, earlierProfits);

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

    private static PayLevel LevelOf(decimal pay, decimal reserve)
    {
        var year = Year(Scenario(directorPay: pay, reserve: reserve));

        return new PayLevel(pay, year.NetInPocket, year.Dividend.Dividend, year.KeptInCompany, year.Company.Condition,
            year.MaximumDividend >= reserve);
    }
}

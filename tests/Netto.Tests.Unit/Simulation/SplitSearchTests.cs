using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;

namespace Netto.Tests.Simulation;

// Income year 2026 at a 7% municipal rate, for YearSimulationTests' made-up company: 120,000 of
// revenue and 20,000 of running costs, a small company past its first four tax periods, a VVPRbis
// dividend from the third distribution, nothing kept unless a test says otherwise. Every level is
// YearSimulation's figure at that pay; the best pays were found by running the year at every euro
// from 0 to the profit before pay (2026-10-19). On this company the net in pocket climbs to 20,799,
// where it peaks, and jumps at 50,000, half the profit, where the reduced corporate rate is kept.
// The tax credit for low earned income added 910 to each best pay and moved none: each sits where
// the credit is whole (taxable income from 8,960 to 22,400), and the credit is never more. The
// company prepays enough by 10 April to leave no advance-payment surcharge at any pay, unless a test
// says otherwise, and withholds more than any director pay's tax; the payments it makes and what it
// withholds are held at every pay, like the rest of the scenario, and so is who pays the
// contributions: the director unless a test says the company does.
public class SplitSearchTests
{
    private static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

    // Its credit, 9,000, covers the surcharge on any tax these tests reach.
    private static readonly AdvancePayments Prepaid = new(100_000m, 0m, 0m, 0m);

    private static readonly TaxPrepayments Withheld = new(1_000_000m, AdvancePayments.None);

    [Fact]
    public void Run_2026Example_findsThePayThatLeavesTheMost()
    {
        var best = Search(Scenario()).Best.ShouldNotBeNull();

        (best.Pay, best.NetInPocket, best.Dividend).ShouldBe((20_799m, 65_622.81m, 59_400.75m));
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

    // Keeping the rate takes the 51,000 minimum here, and costs 1,255.02 against the best pay.
    [Fact]
    public void Run_2026Profit150000_keepsTheRateAtTheMinimumAndTheBestStillGivesItUp()
    {
        var result = Search(Scenario(revenue: 170_000m));

        (result.BestKeepingReducedRate?.Pay, result.BestKeepingReducedRate?.NetInPocket, result.Best?.Pay, result.Best?.NetInPocket)
            .ShouldBe(((decimal?)51_000m, (decimal?)95_117.79m, (decimal?)20_799m, (decimal?)96_372.81m));
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

    // Found by running the year at every euro from 0 to 100,000 with nothing prepaid (2026-10-20):
    // the surcharge, 1,336.52 at the best pay and 675.00 at 50,000, moves neither best pay.
    [Fact]
    public void Run_2026NothingPrepaid_findsTheBestPayWithItsSurcharge()
    {
        var result = Search(Scenario(advancePayments: AdvancePayments.None));

        (result.Best?.Pay, result.Best?.NetInPocket, result.Best?.Dividend, result.BestKeepingReducedRate?.Pay, result.BestKeepingReducedRate?.NetInPocket)
            .ShouldBe(((decimal?)20_799m, (decimal?)64_526.87m, (decimal?)58_064.23m, (decimal?)50_000m, (decimal?)61_975.74m));
    }

    // The payments entered are held at every pay: each level carries the surcharge they leave there.
    [Fact]
    public void Run_2026PaidByAprilAndJulyOnly_eachLevelIsTheYearAtThatPayWithThesePayments()
    {
        var payments = new AdvancePayments(2_000m, 2_000m, 0m, 0m);

        var curve = Search(Scenario(advancePayments: payments)).Curve;

        curve.ShouldAllBe(level => level == LevelOf(level.Pay, 0m, payments));
    }

    // Found by running the year at every euro from 0 to 100,000 with nothing withheld on the pay
    // (2026-10-20): the best pay's tax is small enough that its surcharge, 18.64, is under the 100
    // minimum, while 50,000 owes 486.57; the best pay does not move and gains more.
    [Fact]
    public void Run_2026NothingWithheldOnThePay_findsTheBestPayWithTheSurchargeOnItsTax()
    {
        var result = Search(Scenario(taxPrepayments: TaxPrepayments.None));

        (result.Best?.Pay, result.Best?.NetInPocket, result.BestKeepingReducedRate?.Pay, result.BestKeepingReducedRate?.NetInPocket)
            .ShouldBe(((decimal?)20_799m, (decimal?)65_622.81m, (decimal?)50_000m, (decimal?)62_042.67m));
    }

    // What is withheld and paid in advance on the pay is held at every pay, as the year would run there.
    [Fact]
    public void Run_2026PartWithheld_eachLevelIsTheYearAtThatPayWithTheSameWithholding()
    {
        var taxPrepayments = new TaxPrepayments(5_000m, new AdvancePayments(1_000m, 0m, 0m, 0m));

        var curve = Search(Scenario(taxPrepayments: taxPrepayments)).Curve;

        curve.ShouldAllBe(level => level == LevelOf(level.Pay, 0m, null, taxPrepayments));
    }

    // Nothing given as withheld: at every pay the company withholds what the key formula says on that
    // pay, so each level is the year run there with its own withholding.
    [Fact]
    public void Run_2026WithholdingAsTheKeyFormula_eachLevelIsTheYearAtThatPayWithTheWithholdingOnThatPay()
    {
        var curve = Search(Scenario(taxPrepayments: TaxPrepayments.AsWithheld)).Curve;

        curve.ShouldAllBe(level => level == LevelOf(level.Pay, 0m, null, TaxPrepayments.AsWithheld));
    }

    // The formula's withholding leaves a surcharge under the 100 minimum at 20,799 and at 50,000, as
    // withholding everything does, and never more in the pocket than withholding everything: both best
    // pays stay where they are, with the same net.
    [Fact]
    public void Run_2026WithholdingAsTheKeyFormula_findsTheBestPaysOfAPayWhoseTaxWasWithheld()
    {
        var result = Search(Scenario(taxPrepayments: TaxPrepayments.AsWithheld));

        (result.Best?.Pay, result.Best?.NetInPocket, result.BestKeepingReducedRate?.Pay, result.BestKeepingReducedRate?.NetInPocket)
            .ShouldBe(((decimal?)20_799m, (decimal?)65_622.81m, (decimal?)50_000m, (decimal?)62_529.24m));
    }

    [Fact]
    public void Run_2026DirectorPays_findsWhatNotSayingWhoPaysFinds()
    {
        var director = Search(Scenario(paidBy: ContributionsPaidBy.Director));
        var unsaid = Search(Scenario());

        (director.Best, director.Curve.SequenceEqual(unsaid.Curve)).ShouldBe((unsaid.Best, true));
    }

    [Fact]
    public void Run_2026CompanyPaysContributions_eachLevelIsTheYearAtThatPayWithTheCompanyPaying()
    {
        var curve = Search(Scenario(paidBy: ContributionsPaidBy.Company)).Curve;

        curve.ShouldAllBe(level => level == LevelOf(level.Pay, 0m, null, null, ContributionsPaidBy.Company));
    }

    // The reduced rate is kept once the taxable pay, the payslip gross plus the contributions the company
    // pays, reaches half the profit or the 51,000 minimum: from a payslip gross of 41,706.64 on 100,000
    // of profit, whose contributions are 8,293.36, the director-pays case's at 50,000, and from 42,540.76
    // on 150,000, where they are 8,459.24, the director-pays case's at 51,000.
    public static TheoryData<decimal, decimal> SmallestPayslipGrossKeepingTheRate => new()
    {
        { 120_000m, 41_706.64m },
        { 170_000m, 42_540.76m },
    };

    [Theory]
    [MemberData(nameof(SmallestPayslipGrossKeepingTheRate))]
    public void Run_2026CompanyPaysContributions_saysTheSmallestPayslipGrossKeepingTheReducedRate(decimal revenue, decimal smallest)
    {
        Search(Scenario(revenue: revenue, paidBy: ContributionsPaidBy.Company)).SmallestPayKeepingReducedRate.ShouldBe(smallest);
    }

    [Fact]
    public void Run_2026CompanyPaysContributions_drawsTheJumpWhereTheReducedRateIsKept()
    {
        var curve = Search(Scenario(paidBy: ContributionsPaidBy.Company)).Curve;

        curve.Where(level => level.Pay is 41_706.63m or 41_706.64m).Select(level => level.Condition.AppliesReducedRate())
            .ShouldBe(new[] { false, true });
    }

    // Found by running the year at every euro from 0 to 100,000 with the company paying (2026-10-20): the
    // best pay, 17,237 on the payslip, has 20,798.68 of taxable pay, next to the director-pays best of
    // 20,799, and leaves the same 65,622.81. The smallest pay keeping the rate, 41,706.64, is the
    // director-pays 50,000 to the cent and leaves its 62,529.24, where 41,707 leaves 62,529.14.
    [Fact]
    public void Run_2026CompanyPaysContributions_findsThePayThatLeavesTheMostAndTheBestKeepingTheRate()
    {
        var result = Search(Scenario(paidBy: ContributionsPaidBy.Company));

        (result.Best?.Pay, result.Best?.NetInPocket, result.BestKeepingReducedRate?.Pay, result.BestKeepingReducedRate?.NetInPocket,
                result.BestLosingReducedRate?.Pay)
            .ShouldBe(((decimal?)17_237m, (decimal?)65_622.81m, (decimal?)41_706.64m, (decimal?)62_529.24m, (decimal?)17_237m));
    }

    [Fact]
    public void Run_2026CompanyPaysContributions_noPayWithinAThousandEurosLeavesMore()
    {
        var best = Search(Scenario(paidBy: ContributionsPaidBy.Company)).Best.ShouldNotBeNull();

        var around = Enumerable.Range(-1_000, 2_001).Select(euros => Year(Scenario(directorPay: best.Pay + euros, paidBy: ContributionsPaidBy.Company)).NetInPocket);

        around.ShouldAllBe(net => net <= best.NetInPocket);
    }

    // Keeping the rate takes the 51,000 of taxable pay here, 42,540.76 on the payslip, and leaves the
    // 95,117.79 the director-pays case leaves at 51,000.
    [Fact]
    public void Run_2026Profit150000CompanyPaysContributions_keepsTheRateFromTheMinimumTaxablePayAndTheBestStillGivesItUp()
    {
        var result = Search(Scenario(revenue: 170_000m, paidBy: ContributionsPaidBy.Company));

        (result.BestKeepingReducedRate?.Pay, result.BestKeepingReducedRate?.NetInPocket, result.Best?.Pay, result.Best?.NetInPocket)
            .ShouldBe(((decimal?)42_540.76m, (decimal?)95_117.79m, (decimal?)17_237m, (decimal?)96_372.81m));
    }

    [Fact]
    public void Run_negativeAdvancePayment_isRefusedWithYearSimulationsKey()
    {
        var (value, error) = SplitSearch.Run(Scenario(advancePayments: new AdvancePayments(-0.01m, 0m, 0m, 0m)));

        (value, error).ShouldBe((null, YearSimulation.AdvancePaymentNegative));
    }

    [Fact]
    public void Run_2026Reserve40000_bestPayKeepsIt()
    {
        var best = Search(Scenario(reserve: 40_000m)).Best.ShouldNotBeNull();

        (best.Pay, best.NetInPocket, best.KeptInCompany).ShouldBe((20_799m, 32_822.81m, 40_000m));
    }

    // The best pay overall, 20,799, would leave 59,400.75 of profit after tax, under the reserve. Up
    // to 20,000 the profit after tax, 75% of the rest, covers 60,000; the pocket peaks where the
    // dividend reaches the exempt 833 (18,890 pays 832.50), and no pay that keeps the rate covers it.
    [Fact]
    public void Run_2026Reserve60000_neverChoosesAPayThatCannotKeepIt()
    {
        var result = Search(Scenario(reserve: 60_000m));

        (result.Best?.Pay, result.Best?.NetInPocket, result.Best?.Dividend, result.BestKeepingReducedRate)
            .ShouldBe(((decimal?)18_890m, (decimal?)16_183.13m, (decimal?)832.50m, (PayLevel?)null));
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

        (best.Pay, best.NetInPocket).ShouldBe((20_799m, 90_222.81m));
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

        (best.Pay, best.NetInPocket).ShouldBe((20_799m, 68_870.06m));
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
        decimal earlierProfits = 0m,
        AdvancePayments? advancePayments = null,
        TaxPrepayments? taxPrepayments = null,
        ContributionsPaidBy paidBy = ContributionsPaidBy.Director) =>
        new(2026, revenue, 20_000m, company ?? SmallPastStartUp, directorPay, MunicipalRates.Entered(0.07m), reserve,
            WithholdingCategory.VvprbisThirdYearOnwards, earlierProfits)
        {
            AdvancePayments = advancePayments ?? Prepaid,
            TaxPrepayments = taxPrepayments ?? Withheld,
            ContributionsPaidBy = paidBy,
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

    private static PayLevel LevelOf(
        decimal pay, decimal reserve, AdvancePayments? advancePayments = null, TaxPrepayments? taxPrepayments = null,
        ContributionsPaidBy paidBy = ContributionsPaidBy.Director)
    {
        var year = Year(Scenario(directorPay: pay, reserve: reserve, advancePayments: advancePayments, taxPrepayments: taxPrepayments, paidBy: paidBy));

        return new PayLevel(pay, year.NetInPocket, year.Dividend.Dividend, year.KeptInCompany, year.Company.Condition,
            year.MaximumDividend >= reserve);
    }
}

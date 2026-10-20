using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The river opens on the example year of YearSimulationTests: 120,000 revenue, 20,000 running costs,
// 50,000 pay at 7%, nothing kept, VVPRbis from the third distribution, and nothing prepaid by the
// company nor withheld or prepaid on the pay, which leaves 61,489.17 in the pocket. Of the 62,529.24
// a company that prepaid and a pay whose tax was withheld leave, the 675.00 surcharge on the
// 10,000 of corporate tax takes 553.50, its dividend withholding the rest, and the 486.57
// surcharge on the pay's tax takes all of itself. The pay alone leaves 29,092.73.
public class RiverInputsTests
{
    [Fact]
    public void Example_runsTheWholeYear()
    {
        var run = RiverInputs.Example.Run();

        (run.Error, run.Year?.NetInPocket).ShouldBe((null, 61_489.17m));
    }

    [Fact]
    public void Example_withholdsAndPrepaysNothingOnThePay()
    {
        var prepayments = RiverInputs.Example.TaxPrepayments;

        (prepayments.Withheld, prepayments.Paid.ByDeadline.All(payment => payment == new Input<decimal>(0m, InputOrigin.Example)), prepayments.Starter)
            .ShouldBe((new Input<decimal>(0m, InputOrigin.Example), true, new Input<bool>(false, InputOrigin.Default)));
    }

    // IncomeTaxAdvancePaymentCalculatorTests: 486.57 on the 11,333.96 of tax, and 14,317.50 by
    // 21 December brings it under the 100 minimum.
    [Fact]
    public void Run_example_surchargesThePaysTaxAndSaysWhatBy21DecemberLeavesNoneDue()
    {
        var payments = RiverInputs.Example.Run().Pay.ShouldNotBeNull().AdvancePayments;

        (payments.Surcharge, payments.LastDeadlineTopUp).ShouldBe((486.57m, 14_317.50m));
    }

    [Fact]
    public void WithTaxWithheld_makesItTheUsers()
    {
        RiverInputs.Example.WithTaxWithheld(11_334m).TaxPrepayments.Withheld.ShouldBe(new Input<decimal>(11_334m, InputOrigin.Yours));
    }

    // 11,334 withheld leaves a surcharge under the minimum: the pocket is the 61,975.74 of a pay
    // whose tax was withheld.
    [Fact]
    public void Run_taxWithheldReachesTheEngine()
    {
        RiverInputs.Example.WithTaxWithheld(11_334m).Run().Year.ShouldNotBeNull().NetInPocket.ShouldBe(61_975.74m);
    }

    [Fact]
    public void WithTaxAdvancePayment_makesThatPaymentTheUsers()
    {
        var paid = RiverInputs.Example.WithTaxAdvancePayment(3, 1_000m).TaxPrepayments.Paid;

        (paid.First, paid.Fourth).ShouldBe((new Input<decimal>(0m, InputOrigin.Example), new Input<decimal>(1_000m, InputOrigin.Yours)));
    }

    // 12,014 by 10 April avoids the surcharge and earns a bonus of 90.11.
    [Fact]
    public void Run_taxAdvancePaymentsReachTheEngine()
    {
        var pay = RiverInputs.Example.WithTaxAdvancePayment(0, 12_014m).Run().Pay.ShouldNotBeNull();

        (pay.AdvancePayments.Surcharge, pay.AdvancePayments.Bonus).ShouldBe((0m, 90.11m));
    }

    [Fact]
    public void WithStarter_makesTheStatusTheUsersAndExemptsThePaysTax()
    {
        var inputs = RiverInputs.Example.WithStarter(true);

        (inputs.TaxPrepayments.Starter, inputs.Run().Pay.ShouldNotBeNull().AdvancePayments.Exempt)
            .ShouldBe((new Input<bool>(true, InputOrigin.Yours), true));
    }

    [Fact]
    public void Run_negativeTaxWithheld_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.WithTaxWithheld(-0.01m).Run();

        (run.Year, run.Error).ShouldBe((null, GrossToNet.TaxWithheldNegative));
    }

    [Fact]
    public void Run_companySkippedNegativeTaxAdvancePayment_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.SkipCompany().WithTaxAdvancePayment(1, -0.01m).Run();

        (run.Pay, run.Error).ShouldBe((null, GrossToNet.TaxAdvancePaymentNegative));
    }

    [Fact]
    public void SameBesidesPay_taxWithheldChanged_isNot()
    {
        RiverInputs.Example.SameBesidesPay(RiverInputs.Example.WithTaxWithheld(1_000m)).ShouldBeFalse();
    }

    [Fact]
    public void Example_prepaysNothing()
    {
        RiverInputs.Example.AdvancePayments.ByDeadline.ShouldAllBe(payment => payment == new Input<decimal>(0m, InputOrigin.Example));
    }

    // 675 ÷ 4.5% = 15,000.
    [Fact]
    public void Run_example_surchargesTheCorporateTaxAndSaysWhatBy21DecemberCancelsIt()
    {
        var payments = RiverInputs.Example.Run().Year.ShouldNotBeNull().AdvancePayments;

        (payments.Surcharge, payments.LastDeadlineTopUp).ShouldBe((675.00m, 15_000.00m));
    }

    [Fact]
    public void WithAdvancePayment_makesThatPaymentTheUsers()
    {
        var payments = RiverInputs.Example.WithAdvancePayment(3, 15_000m).AdvancePayments;

        (payments.First, payments.Fourth).ShouldBe((new Input<decimal>(0m, InputOrigin.Example), new Input<decimal>(15_000m, InputOrigin.Yours)));
    }

    // 7,500 by 10 April earns 9%, the whole 675.
    [Fact]
    public void Run_advancePaymentsReachTheEngine()
    {
        var year = RiverInputs.Example.WithAdvancePayment(0, 7_500m).Run().Year.ShouldNotBeNull();

        (year.AdvancePayments.Surcharge, year.NetInPocket).ShouldBe((0m, 62_042.67m));
    }

    [Fact]
    public void Run_negativeAdvancePayment_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.WithAdvancePayment(1, -0.01m).Run();

        (run.Year, run.Error).ShouldBe((null, YearSimulation.AdvancePaymentNegative));
    }

    // One financial year sets both statuses the engine takes: the first three are exempt from the
    // surcharge (art. 218 §2), the first four from the minimum pay (art. 215 al. 3, 4°).
    [Theory]
    [InlineData(FinancialYear.First, true, ReducedRateCondition.FirstFourTaxPeriods)]
    [InlineData(FinancialYear.Third, true, ReducedRateCondition.FirstFourTaxPeriods)]
    [InlineData(FinancialYear.Fourth, false, ReducedRateCondition.FirstFourTaxPeriods)]
    [InlineData(FinancialYear.FifthOrLater, false, ReducedRateCondition.TaxableIncomeNotAbovePay)]
    public void Run_financialYear_setsBothStatuses(FinancialYear financialYear, bool exempt, ReducedRateCondition condition)
    {
        var year = RiverInputs.Example.WithFinancialYear(financialYear).Run().Year.ShouldNotBeNull();

        (year.AdvancePayments.Exempt, year.Company.Condition).ShouldBe((exempt, condition));
    }

    [Fact]
    public void Example_financialYearIsTheFifthOrLater()
    {
        RiverInputs.Example.FinancialYear.ShouldBe(new Input<FinancialYear>(FinancialYear.FifthOrLater, InputOrigin.Default));
    }

    // The split's search holds the payments, so a new payment needs a new search.
    [Fact]
    public void SameBesidesPay_advancePaymentChanged_isNot()
    {
        RiverInputs.Example.SameBesidesPay(RiverInputs.Example.WithAdvancePayment(2, 1_000m)).ShouldBeFalse();
    }

    [Fact]
    public void Example_amountsOfMoneyAreTheExamples()
    {
        var inputs = RiverInputs.Example;

        new[] { inputs.Company!.Revenue.Origin, inputs.Company.RunningCosts.Origin, inputs.DirectorPay.Origin, inputs.Reserve.Origin, inputs.EarlierProfits.Origin }
            .ShouldAllBe(origin => origin == InputOrigin.Example);
    }

    [Fact]
    public void Example_hasNoEarlierProfits()
    {
        RiverInputs.Example.EarlierProfits.Value.ShouldBe(0m);
    }

    [Fact]
    public void WithEarlierProfits_makesThemTheUsers()
    {
        RiverInputs.Example.WithEarlierProfits(60_000m).EarlierProfits.ShouldBe(new Input<decimal>(60_000m, InputOrigin.Yours));
    }

    // YearSimulationTests: 60,000 kept from earlier years, 30,000 kept at year end.
    [Fact]
    public void Run_earlierProfitsReachTheEngine()
    {
        RiverInputs.Example.WithEarlierProfits(60_000m).WithReserve(30_000m).Run().Year.ShouldNotBeNull().NetInPocket.ShouldBe(86_089.17m);
    }

    [Fact]
    public void Run_negativeEarlierProfits_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.WithEarlierProfits(-0.01m).Run();

        (run.Year, run.Error).ShouldBe((null, YearSimulation.EarlierProfitsNegative));
    }

    [Fact]
    public void Example_ratesAndStatusesAreDefaults()
    {
        var inputs = RiverInputs.Example;

        new[] { inputs.SmallCompany.Origin, inputs.FinancialYear.Origin, inputs.DividendCategory.Origin, inputs.Municipality.Origin }
            .ShouldAllBe(origin => origin == InputOrigin.Default);
    }

    [Fact]
    public void WithRevenue_makesTheRevenueTheUsers()
    {
        var inputs = RiverInputs.Example.WithRevenue(150_000m);

        inputs.Company!.Revenue.ShouldBe(new Input<decimal>(150_000m, InputOrigin.Yours));
    }

    [Fact]
    public void WithRevenue_leavesTheRunningCostsAnExample()
    {
        var inputs = RiverInputs.Example.WithRevenue(150_000m);

        inputs.Company!.RunningCosts.Origin.ShouldBe(InputOrigin.Example);
    }

    [Fact]
    public void WithEveryOtherInput_makesItTheUsers()
    {
        var inputs = RiverInputs.Example
            .WithRunningCosts(25_000m)
            .WithSmallCompany(false)
            .WithFinancialYear(FinancialYear.Fourth)
            .WithDirectorPay(40_000m)
            .WithReserve(5_000m)
            .WithDividendCategory(WithholdingCategory.Standard);

        (inputs.Company!.RunningCosts, inputs.SmallCompany, inputs.FinancialYear, inputs.DirectorPay, inputs.Reserve, inputs.DividendCategory)
            .ShouldBe((new Input<decimal>(25_000m, InputOrigin.Yours), new Input<bool>(false, InputOrigin.Yours),
                new Input<FinancialYear>(FinancialYear.Fourth, InputOrigin.Yours), new Input<decimal>(40_000m, InputOrigin.Yours),
                new Input<decimal>(5_000m, InputOrigin.Yours), new Input<WithholdingCategory>(WithholdingCategory.Standard, InputOrigin.Yours)));
    }

    [Fact]
    public void WithMunicipality_takesTheChoiceWithItsOwnOrigin()
    {
        var namur = MunicipalRates.IncomeYear2026.Municipalities.Single(municipality => municipality.Name == "Namur");

        var inputs = RiverInputs.Example.WithMunicipality(RiverInputs.Example.Municipality.Pick(namur));

        (inputs.Municipality.Municipality, inputs.Municipality.Origin).ShouldBe((namur, InputOrigin.Derived));
    }

    [Fact]
    public void Run_everyInputReachesTheEngine()
    {
        var run = RiverInputs.Example.WithReserve(15_000m).WithDividendCategory(WithholdingCategory.Standard).Run();

        (run.Year!.Dividend.Dividend, run.Year.Dividend.Rate).ShouldBe((24_325.00m, 0.30m));
    }

    [Fact]
    public void Run_yearsDirectorPayIsThePayTheCardsRead()
    {
        var run = RiverInputs.Example.Run();

        run.Pay.ShouldBeSameAs(run.Year!.DirectorPay);
    }

    [Fact]
    public void SkipCompany_runsThePayAlone()
    {
        var run = RiverInputs.Example.SkipCompany().Run();

        (run.Year, run.Pay?.GrossPay, run.Pay?.NetInPocket).ShouldBe((null, 50_000m, 29_092.73m));
    }

    [Fact]
    public void SkipCompany_keepsThePayAndItsOrigin()
    {
        var inputs = RiverInputs.Example.WithDirectorPay(42_000m).SkipCompany();

        (inputs.Company, inputs.DirectorPay).ShouldBe((null, new Input<decimal>(42_000m, InputOrigin.Yours)));
    }

    [Fact]
    public void EnterCompany_afterSkipping_startsTheUsersYearFromNothing()
    {
        var inputs = RiverInputs.Example.SkipCompany().EnterCompany();

        inputs.Company.ShouldBe(new CompanyFigures(new Input<decimal>(0m, InputOrigin.Yours), new Input<decimal>(0m, InputOrigin.Yours)));
    }

    [Fact]
    public void EnterCompany_whenTheCompanyIsThere_changesNothing()
    {
        var inputs = RiverInputs.Example.EnterCompany();

        inputs.ShouldBe(RiverInputs.Example);
    }

    [Fact]
    public void PayCeiling_isTheProfitBeforePay()
    {
        RiverInputs.Example.PayCeiling.ShouldBe(100_000m);
    }

    [Fact]
    public void PayCeiling_lossBeforePay_isZero()
    {
        RiverInputs.Example.WithRevenue(10_000m).PayCeiling.ShouldBe(0m);
    }

    [Fact]
    public void PayCeiling_companySkipped_isNone()
    {
        RiverInputs.Example.SkipCompany().PayCeiling.ShouldBeNull();
    }

    // The pay set stays the user's: it comes back once the profit rises again.
    [Fact]
    public void Run_payAboveTheProfitBeforePay_paysTheProfit()
    {
        var inputs = RiverInputs.Example.WithRevenue(60_000m);

        (inputs.Run().Pay?.GrossPay, inputs.DirectorPay.Value).ShouldBe((40_000m, 50_000m));
    }

    [Fact]
    public void Run_lossBeforePay_paysNothing()
    {
        var run = RiverInputs.Example.WithRevenue(10_000m).Run();

        (run.Pay?.GrossPay, run.Year?.KeptInCompany).ShouldBe((0m, -10_000m));
    }

    [Fact]
    public void Run_negativePay_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.WithDirectorPay(-0.01m).Run();

        (run.Pay, run.Year, run.Error).ShouldBe((null, null, Netto.Core.DirectorPay.GrossToNet.GrossPayNegative));
    }

    [Fact]
    public void Run_payAloneWithANegativePay_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.SkipCompany().WithDirectorPay(-0.01m).Run();

        (run.Pay, run.Error).ShouldBe((null, Netto.Core.DirectorPay.GrossToNet.GrossPayNegative));
    }

    [Fact]
    public void Run_negativeReserve_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.WithReserve(-0.01m).Run();

        (run.Year, run.Error).ShouldBe((null, YearSimulation.ReserveNegative));
    }

    // SplitSearchTests: on the example, 20,799 leaves the most.
    [Fact]
    public void SearchSplit_example_findsTheBestPay()
    {
        RiverInputs.Example.SearchSplit().ShouldNotBeNull().Best.ShouldNotBeNull().Pay.ShouldBe(20_799m);
    }

    [Fact]
    public void SearchSplit_holdsTheReserveSet()
    {
        RiverInputs.Example.WithReserve(30_000m).SearchSplit().ShouldNotBeNull().Best.ShouldNotBeNull().KeptInCompany.ShouldBe(30_000m);
    }

    [Fact]
    public void SearchSplit_companySkipped_isNone()
    {
        RiverInputs.Example.SkipCompany().SearchSplit().ShouldBeNull();
    }

    [Fact]
    public void SearchSplit_inputRefused_isNone()
    {
        RiverInputs.Example.WithReserve(-0.01m).SearchSplit().ShouldBeNull();
    }

    // The search does not depend on the pay, so moving it alone needs no new search.
    [Fact]
    public void SameBesidesPay_onlyThePayMoved_isTrue()
    {
        RiverInputs.Example.WithDirectorPay(20_799m).SameBesidesPay(RiverInputs.Example).ShouldBeTrue();
    }

    [Fact]
    public void SameBesidesPay_anotherInputChanged_isFalse()
    {
        var changed = new[]
        {
            RiverInputs.Example.WithReserve(10_000m),
            RiverInputs.Example.WithRevenue(130_000m),
            RiverInputs.Example.WithSmallCompany(false),
            RiverInputs.Example.WithMunicipality(RiverInputs.Example.Municipality.Enter(0.08m)),
            RiverInputs.Example.WithDividendCategory(WithholdingCategory.Standard),
            RiverInputs.Example.WithEarlierProfits(60_000m),
            RiverInputs.Example.SkipCompany(),
        };

        changed.ShouldAllBe(inputs => !inputs.SameBesidesPay(RiverInputs.Example));
    }
}

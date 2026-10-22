using Netto.Core.Allowances;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The river opens on the example year of YearSimulationTests: 120,000 revenue, 20,000 running costs,
// 50,000 pay at 7%, nothing kept, VVPRbis from the third distribution, the contributions paid by the
// director, nothing prepaid by the company nor on the pay, and the pay withheld on as the key formula
// says (decided 2026-10-20): 10,691.28, which leaves a surcharge under the minimum, so the pocket is
// the 61,975.74 of a pay whose tax was withheld, and the 675.00 surcharge on the 10,000 of corporate
// tax takes 553.50 of the 62,529.24 a company that prepaid would leave, its dividend withholding
// the rest. With nothing withheld the 486.57 surcharge on the pay's tax takes all of itself:
// 61,489.17. The pay alone leaves 29,579.30. The example grants no vouchers; with 220 days of meal
// vouchers (YearSimulationTests) the company has 98,039.80 left for the split and 49,120.00 of
// taxable income, which pays 9,824.00 and, nothing prepaid, a surcharge of 663.12, leaving 37,552.68
// to pay out: the pocket gets 29,579.30, the net dividend 30,943.14 and the vouchers' 1,960.20. It
// pays no expense allowances; with the most the circular allows for the office, internet and a
// computer, 2,418.44 (YearSimulationTests), the company has 97,581.56 left for the split and as much
// taxable income less the pay, 47,581.56, which pays 9,516.31 and a surcharge of 642.35, leaving
// 37,422.90 to pay out: the pocket gets 29,579.30, the net dividend 30,836.72 and the allowances'
// 2,418.44. The example holds none of these optional modules in its plan (decided 2026-10-21): a test
// adds the one it is about.
public class RiverInputsTests
{
    private static readonly RiverInputs VouchersInPlan = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);

    private static readonly RiverInputs AllowancesInPlan = RiverInputs.Example.AddToPlan(OptionalModule.Allowances);

    private static readonly RiverInputs RentInPlan = RiverInputs.Example.AddToPlan(OptionalModule.Rent);

    [Fact]
    public void Example_runsTheWholeYear()
    {
        var run = RiverInputs.Example.Run();

        (run.Error, run.Year?.NetInPocket).ShouldBe((null, 61_975.74m));
    }

    // Null is what the key formula withholds, derived from the pay, not an amount of the example.
    [Fact]
    public void Example_withholdsAsTheKeyFormulaAndPrepaysNothingOnThePay()
    {
        var prepayments = RiverInputs.Example.TaxPrepayments;

        (prepayments.Withheld, prepayments.Paid.ByDeadline.All(payment => payment == new Input<decimal>(0m, InputOrigin.Example)), prepayments.Starter)
            .ShouldBe((new Input<decimal?>(null, InputOrigin.Derived), true, new Input<bool>(false, InputOrigin.Default)));
    }

    // GrossToNetTests: twelve times the 890.94 on 4,166.67; 11,333.96 and 793.38 of tax less that
    // leaves 1,436.06 to pay, and the 59.52 of surcharge before the 90% is under the minimum.
    [Fact]
    public void Run_example_withholdsWhatTheKeyFormulaSaysAndLeavesNoSurcharge()
    {
        var pay = RiverInputs.Example.Run().Pay.ShouldNotBeNull();

        (pay.Assessment.Withheld, pay.Assessment.WithheldGiven, pay.AdvancePayments.Surcharge, pay.Assessment.Balance)
            .ShouldBe((10_691.28m, false, 0m, 1_436.06m));
    }

    // IncomeTaxAdvancePaymentCalculatorTests: 486.57 on the 11,333.96 of tax, and 14,317.50 by
    // 21 December brings it under the 100 minimum.
    [Fact]
    public void Run_nothingWithheld_surchargesThePaysTaxAndSaysWhatBy21DecemberLeavesNoneDue()
    {
        var payments = RiverInputs.Example.WithTaxWithheld(0m).Run().Pay.ShouldNotBeNull().AdvancePayments;

        (payments.Surcharge, payments.LastDeadlineTopUp).ShouldBe((486.57m, 14_317.50m));
    }

    [Fact]
    public void WithTaxWithheld_makesItTheUsers()
    {
        RiverInputs.Example.WithTaxWithheld(11_334m).TaxPrepayments.Withheld.ShouldBe(new Input<decimal?>(11_334m, InputOrigin.Yours));
    }

    [Fact]
    public void WithTaxWithheldAsThePaySlip_leavesItToTheKeyFormulaAgain()
    {
        RiverInputs.Example.WithTaxWithheld(11_334m).WithTaxWithheldAsThePaySlip().TaxPrepayments.Withheld
            .ShouldBe(new Input<decimal?>(null, InputOrigin.Derived));
    }

    // Nothing withheld leaves the whole surcharge on the pay's tax: 486.57 less in the pocket.
    [Fact]
    public void Run_taxWithheldReachesTheEngine()
    {
        RiverInputs.Example.WithTaxWithheld(0m).Run().Year.ShouldNotBeNull().NetInPocket.ShouldBe(61_489.17m);
    }

    // The amount given replaces the formula's, and the result says so.
    [Fact]
    public void Run_taxWithheld_isTheAmountTheAssessmentSetsAgainstTheTax()
    {
        var assessment = RiverInputs.Example.WithTaxWithheld(10_000m).Run().Pay.ShouldNotBeNull().Assessment;

        (assessment.Withheld, assessment.WithheldGiven, assessment.Balance).ShouldBe((10_000m, true, 2_127.34m));
    }

    [Fact]
    public void Example_contributionsArePaidByTheDirectorByDefault()
    {
        RiverInputs.Example.ContributionsPaidBy.ShouldBe(new Input<ContributionsPaidBy>(ContributionsPaidBy.Director, InputOrigin.Default));
    }

    [Fact]
    public void WithContributionsPaidBy_makesItTheUsers()
    {
        RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company).ContributionsPaidBy
            .ShouldBe(new Input<ContributionsPaidBy>(ContributionsPaidBy.Company, InputOrigin.Yours));
    }

    // YearSimulationTests: the company's 9,942.52 of contributions are pay, so its cost of the 50,000
    // is 59,942.52 and its taxable income 40,057.48.
    [Fact]
    public void Run_companyPaysTheContributions_reachesTheEngine()
    {
        var year = RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company).Run().Year.ShouldNotBeNull();

        (year.DirectorPay.ContributionsPaidByCompany, year.DirectorPay.CompanyCost, year.Company.TaxableIncome).ShouldBe((9_942.52m, 59_942.52m, 40_057.48m));
    }

    [Fact]
    public void Run_payAloneWithTheCompanyPayingTheContributions_reachesTheEngine()
    {
        var pay = RiverInputs.Example.SkipCompany().WithContributionsPaidBy(ContributionsPaidBy.Company).Run().Pay.ShouldNotBeNull();

        (pay.GrossPay, pay.ContributionsPaidByCompany, pay.CompanyCost).ShouldBe((50_000m, 9_942.52m, 59_942.52m));
    }

    // The search holds who pays, since the taxable pay the reduced rate is tested on depends on it.
    [Fact]
    public void SameBesidesPay_whoPaysTheContributionsChanged_isNot()
    {
        RiverInputs.Example.SameBesidesPay(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company)).ShouldBeFalse();
    }

    [Fact]
    public void WithTaxAdvancePayment_makesThatPaymentTheUsers()
    {
        var paid = RiverInputs.Example.WithTaxAdvancePayment(3, 1_000m).TaxPrepayments.Paid;

        (paid.First, paid.Fourth).ShouldBe((new Input<decimal>(0m, InputOrigin.Example), new Input<decimal>(1_000m, InputOrigin.Yours)));
    }

    // With nothing withheld, 12,014 by 10 April avoids the surcharge and earns a bonus of 90.11; on
    // the example most of it is beyond the base, for 9.92.
    [Fact]
    public void Run_taxAdvancePaymentsReachTheEngine()
    {
        var pay = RiverInputs.Example.WithTaxWithheld(0m).WithTaxAdvancePayment(0, 12_014m).Run().Pay.ShouldNotBeNull();

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

    // 7,500 by 10 April earns 9%, the whole 675: the 62,529.24 of a company that prepaid and a pay
    // whose tax was withheld.
    [Fact]
    public void Run_advancePaymentsReachTheEngine()
    {
        var year = RiverInputs.Example.WithAdvancePayment(0, 7_500m).Run().Year.ShouldNotBeNull();

        (year.AdvancePayments.Surcharge, year.NetInPocket).ShouldBe((0m, 62_529.24m));
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

    // YearSimulationTests: 60,000 kept from earlier years, 30,000 kept at year end, with the
    // example's own surcharge on the corporate tax and none on the pay's tax.
    [Fact]
    public void Run_earlierProfitsReachTheEngine()
    {
        RiverInputs.Example.WithEarlierProfits(60_000m).WithReserve(30_000m).Run().Year.ShouldNotBeNull().NetInPocket.ShouldBe(86_575.74m);
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

        new[] { inputs.SmallCompany.Origin, inputs.FinancialYear.Origin, inputs.DividendCategory.Origin, inputs.Municipality.Origin, inputs.ContributionsPaidBy.Origin }
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

        (run.Year, run.Pay?.GrossPay, run.Pay?.NetInPocket).ShouldBe((null, 50_000m, 29_579.30m));
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

    // No optional module shows on the example, so its figures, every one above, are a year without
    // them.
    [Fact]
    public void Example_holdsNoOptionalModuleInItsPlan()
    {
        (RiverInputs.Example.Composition, PlanComposition.Modules.Any(RiverInputs.Example.InPlan)).ShouldBe((PlanComposition.None, false));
    }

    // Its amounts for them are nothing, so adding every one changes no figure.
    [Fact]
    public void Run_exampleWithEveryModuleAdded_keepsTheExamplesFigures()
    {
        var everything = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module));

        (Figures(everything), everything.PayCeiling).ShouldBe((Figures(RiverInputs.Example), 100_000m));
    }

    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    public void AddToPlan_putsTheModuleInThePlan(OptionalModule module)
    {
        var inputs = RiverInputs.Example.AddToPlan(module);

        (inputs.InPlan(module), inputs.Composition.Count).ShouldBe((true, 1));
    }

    // Taken out, a module neither shows nor counts: the year runs as if the company paid nothing of it.
    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    public void Run_moduleTakenOutOfThePlan_runsTheYearWithoutIt(OptionalModule module)
    {
        var inputs = WithValues(module).TakeOutOfPlan(module);

        (inputs.InPlan(module), Figures(inputs), inputs.PayCeiling).ShouldBe((false, Figures(RiverInputs.Example), 100_000m));
    }

    // And its values wait in the river's inputs, as the user left them.
    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    public void TakeOutOfPlan_keepsTheModulesValues(OptionalModule module)
    {
        var inputs = WithValues(module);

        inputs.TakeOutOfPlan(module).ShouldBe(inputs with { Composition = PlanComposition.None });
    }

    // Added back, it counts with the values it left with: the pocket of each lever's own test above.
    [Theory]
    [InlineData(OptionalModule.Vouchers, 62_482.64)]
    [InlineData(OptionalModule.Allowances, 62_834.46)]
    [InlineData(OptionalModule.Rent, 62_067.06)]
    public void Run_moduleAddedBack_countsItsValuesAgain(OptionalModule module, decimal pocket)
    {
        var inputs = WithValues(module).TakeOutOfPlan(module).AddToPlan(module);

        inputs.Run().Year.ShouldNotBeNull().NetInPocket.ShouldBe(pocket);
    }

    // The split's search holds exactly what the year runs: none of a module out of the plan.
    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    public void SearchSplit_moduleOutOfThePlan_drawsEveryPayUpToTheProfitWithoutIt(OptionalModule module)
    {
        var search = WithValues(module).TakeOutOfPlan(module).SearchSplit().ShouldNotBeNull();

        (search.Curve[^1].Pay, search.Best?.Pay).ShouldBe((100_000m, (decimal?)20_799m));
    }

    // A module joining or leaving the plan moves the year, so the split's search runs again.
    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    public void SameBesidesPay_moduleAddedOrTakenOut_isNot(OptionalModule module)
    {
        var inputs = WithValues(module);

        inputs.SameBesidesPay(inputs.TakeOutOfPlan(module)).ShouldBeFalse();
    }

    // A refusal of a lever only matters while it is in the plan: out of it, the engine never sees the
    // amount, and the refusal comes back with the lever.
    [Fact]
    public void Run_refusedVouchersOutOfThePlan_runTheYear()
    {
        var refused = VouchersInPlan.WithEcoVouchers(250.01m);

        (refused.TakeOutOfPlan(OptionalModule.Vouchers).Run().Error, refused.Run().Error).ShouldBe((null, VoucherCalculator.EcoVouchersAboveMaximum));
    }

    // The office allowance excludes a rent only while both are in the plan.
    [Fact]
    public void Run_officeAllowanceOutOfThePlan_letsTheRentBePaid()
    {
        var run = RentAtTheCap(AllowancesInPlan).WithOfficeAllowance(100m).TakeOutOfPlan(OptionalModule.Allowances).Run();

        (run.Error, run.Year?.NetInPocket).ShouldBe((null, (decimal?)62_067.06m));
    }

    [Fact]
    public void Example_grantsNoVouchers()
    {
        RiverInputs.Example.Vouchers.ShouldBe(new VoucherInputs(
            new Input<int>(0, InputOrigin.Example), new Input<decimal>(0m, InputOrigin.Example), new Input<decimal>(0m, InputOrigin.Example)));
    }

    [Fact]
    public void WithVouchers_makesEachTheUsers()
    {
        var vouchers = RiverInputs.Example.WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m).Vouchers;

        vouchers.ShouldBe(new VoucherInputs(
            new Input<int>(220, InputOrigin.Yours), new Input<decimal>(250m, InputOrigin.Yours), new Input<decimal>(100m, InputOrigin.Yours)));
    }

    [Fact]
    public void Run_vouchersReachTheEngine()
    {
        var year = VouchersInPlan.WithMealVoucherDays(220).Run().Year.ShouldNotBeNull();

        (year.Vouchers.Total, year.ProfitBeforePay, year.ProfitAfterTax, year.NetInPocket).ShouldBe((1_960.20m, 98_039.80m, 37_552.68m, 62_482.64m));
    }

    [Fact]
    public void PayCeiling_vouchers_isTheProfitTheyLeave()
    {
        VouchersInPlan.WithMealVoucherDays(220).PayCeiling.ShouldBe(98_039.80m);
    }

    // The pay set stays the user's, beyond what the vouchers leave.
    [Fact]
    public void Run_payAboveTheProfitTheVouchersLeave_paysThatProfit()
    {
        var inputs = VouchersInPlan.WithDirectorPay(100_000m).WithMealVoucherDays(220);

        (inputs.Run().Pay?.GrossPay, inputs.DirectorPay.Value).ShouldBe((98_039.80m, 100_000m));
    }

    [Fact]
    public void Run_ecoVouchersAboveTheMaximum_isRefusedWithTheEnginesKey()
    {
        var run = VouchersInPlan.WithEcoVouchers(250.01m).Run();

        (run.Year, run.Error).ShouldBe((null, VoucherCalculator.EcoVouchersAboveMaximum));
    }

    // The split's search holds the vouchers, so new vouchers need a new search.
    [Fact]
    public void SameBesidesPay_vouchersChanged_isNot()
    {
        VouchersInPlan.SameBesidesPay(VouchersInPlan.WithMealVoucherDays(220)).ShouldBeFalse();
    }

    [Fact]
    public void SearchSplit_vouchers_drawsEveryPayUpToTheProfitTheyLeave()
    {
        VouchersInPlan.WithMealVoucherDays(220).SearchSplit().ShouldNotBeNull().Curve[^1].Pay.ShouldBe(98_039.80m);
    }

    // Skipped with the company: the pay alone runs without them, and they come back with it.
    [Fact]
    public void SkipCompany_runsThePayWithoutTheVouchers()
    {
        var inputs = VouchersInPlan.WithMealVoucherDays(220).SkipCompany();

        (inputs.Run().Year, inputs.Run().Pay?.NetInPocket, inputs.Vouchers.MealVoucherDays.Value).ShouldBe((null, 29_579.30m, 220));
    }

    [Fact]
    public void Example_paysNoAllowances()
    {
        var nothing = new Input<decimal>(0m, InputOrigin.Example);

        RiverInputs.Example.Allowances.ShouldBe(new AllowanceInputs(nothing, nothing, nothing, nothing));
    }

    [Fact]
    public void WithAllowances_makesEachTheUsers()
    {
        var allowances = RiverInputs.Example.WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(600m)
            .Allowances;

        allowances.ShouldBe(new AllowanceInputs(
            new Input<decimal>(1_938.44m, InputOrigin.Yours), new Input<decimal>(240m, InputOrigin.Yours), new Input<decimal>(240m, InputOrigin.Yours),
            new Input<decimal>(600m, InputOrigin.Yours)));
    }

    [Fact]
    public void Run_allowancesReachTheEngine()
    {
        var year = FlatRateAllowances(RiverInputs.Example).Run().Year.ShouldNotBeNull();

        (year.ProfitBeforePay, year.ProfitAfterTax, year.NetInPocket).ShouldBe((97_581.56m, 37_422.90m, 62_834.46m));
    }

    [Fact]
    public void PayCeiling_allowancesAndVouchers_isTheProfitTheyLeave()
    {
        (FlatRateAllowances(RiverInputs.Example).PayCeiling, FlatRateAllowances(VouchersInPlan.WithMealVoucherDays(220)).PayCeiling)
            .ShouldBe(((decimal?)97_581.56m, (decimal?)95_621.36m));
    }

    [Fact]
    public void Run_officeAllowanceAboveTheMaximum_isRefusedWithTheEnginesKey()
    {
        var run = AllowancesInPlan.WithOfficeAllowance(1_938.45m).Run();

        (run.Year, run.Error).ShouldBe((null, AllowanceCalculator.OfficeAllowanceAboveMaximum));
    }

    // The split's search holds the allowances, so new allowances need a new search.
    [Fact]
    public void SameBesidesPay_allowancesChanged_isNot()
    {
        AllowancesInPlan.SameBesidesPay(AllowancesInPlan.WithPhoneCosts(600m)).ShouldBeFalse();
    }

    [Fact]
    public void SearchSplit_allowances_drawsEveryPayUpToTheProfitTheyLeave()
    {
        FlatRateAllowances(RiverInputs.Example).SearchSplit().ShouldNotBeNull().Curve[^1].Pay.ShouldBe(97_581.56m);
    }

    // Skipped with the company: the pay alone runs without them, and they come back with it.
    [Fact]
    public void SkipCompany_runsThePayWithoutTheAllowances()
    {
        var inputs = FlatRateAllowances(RiverInputs.Example).SkipCompany();

        (inputs.Run().Year, inputs.Run().Pay?.NetInPocket, inputs.Allowances.Office.Value).ShouldBe((null, 29_579.30m, 1_938.44m));
    }

    [Fact]
    public void Example_paysNoRent()
    {
        var nothing = new Input<decimal>(0m, InputOrigin.Example);

        RiverInputs.Example.Rent.ShouldBe(new RentInputs(nothing, nothing, nothing));
    }

    [Fact]
    public void WithRent_makesEachTheUsers()
    {
        RentAtTheCap(RiverInputs.Example).Rent.ShouldBe(new RentInputs(
            new Input<decimal>(1_200m, InputOrigin.Yours), new Input<decimal>(0.12m, InputOrigin.Yours), new Input<decimal>(1_380m, InputOrigin.Yours)));
    }

    // 1,380.00 off the profit before pay, deducted in full: 48,620 of taxable income at 20%, 9,724.00,
    // and, nothing prepaid, a surcharge of 656.37, which leaves 38,239.63 to pay out, 31,506.44 net. The
    // pocket gets the pay's 29,579.30 less the rent's 398.68 of tax (GrossToNetTests), the rent's
    // 1,380.00 and that dividend: 91.32 more than the example's 61,975.74. The balance on the
    // assessment adds the rent's tax to the example's 1,436.06.
    [Fact]
    public void Run_rentReachesTheEngine()
    {
        var run = RentAtTheCap(RiverInputs.Example).Run();
        var year = run.Year.ShouldNotBeNull();

        (year.ProfitBeforePay, year.ProfitAfterTax, year.NetInPocket, run.Pay?.Assessment.Balance).ShouldBe((98_620m, 38_239.63m, 62_067.06m, (decimal?)1_834.74m));
    }

    [Fact]
    public void PayCeiling_rent_isTheProfitItLeaves()
    {
        RentAtTheCap(RiverInputs.Example).PayCeiling.ShouldBe(98_620m);
    }

    [Fact]
    public void Run_rentAboveTheCap_isRefusedWithTheEnginesKey()
    {
        var run = RentAtTheCap(RiverInputs.Example).WithRent(1_380.01m).Run();

        (run.Year, run.Error).ShouldBe((null, RentCalculator.RentAboveCap));
    }

    [Fact]
    public void Run_rentWithAnOfficeAllowance_isRefusedWithTheEnginesKey()
    {
        var run = RentAtTheCap(AllowancesInPlan).WithOfficeAllowance(100m).Run();

        (run.Year, run.Error).ShouldBe((null, RentCalculator.RentWithOfficeAllowance));
    }

    // The split's search holds the rent, so a new rent needs a new search.
    [Theory]
    [InlineData("cadastral income")]
    [InlineData("share")]
    [InlineData("rent")]
    public void SameBesidesPay_rentChanged_isNot(string changed)
    {
        var inputs = changed switch
        {
            "cadastral income" => RentInPlan.WithCadastralIncome(1_200m),
            "share" => RentInPlan.WithShareLet(0.12m),
            _ => RentInPlan.WithRent(100m),
        };

        RentInPlan.SameBesidesPay(inputs).ShouldBeFalse();
    }

    [Fact]
    public void SearchSplit_rent_drawsEveryPayUpToTheProfitItLeaves()
    {
        RentAtTheCap(RiverInputs.Example).SearchSplit().ShouldNotBeNull().Curve[^1].Pay.ShouldBe(98_620m);
    }

    // Skipped with the company: the pay alone runs without it, taxed alone, and it comes back with it.
    [Fact]
    public void SkipCompany_runsThePayWithoutTheRent()
    {
        var inputs = RentAtTheCap(RiverInputs.Example).SkipCompany();
        var pay = inputs.Run().Pay.ShouldNotBeNull();

        (inputs.Run().Year, pay.NetInPocket, pay.OtherIncomeTax, inputs.Rent.Rent.Value).ShouldBe((null, 29_579.30m, OtherIncomeTax.None, 1_380m));
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
            RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company),
            RiverInputs.Example.WithTaxWithheld(0m),
            RiverInputs.Example.SkipCompany(),
        };

        changed.ShouldAllBe(inputs => !inputs.SameBesidesPay(RiverInputs.Example));
    }

    // What a year shows in the bar and on the cards that hold no lever.
    private static (decimal?, decimal?, decimal?, decimal?, decimal?) Figures(RiverInputs inputs)
    {
        var year = inputs.Run().Year;
        return (year?.NetInPocket, year?.ProfitBeforePay, year?.ProfitAfterTax, year?.KeptInCompany, year?.DirectorPay.NetInPocket);
    }

    // The values each lever's own tests count, with the lever in the plan.
    private static RiverInputs WithValues(OptionalModule module) => module switch
    {
        OptionalModule.Vouchers => VouchersInPlan.WithMealVoucherDays(220),
        OptionalModule.Allowances => FlatRateAllowances(RiverInputs.Example),
        _ => RentAtTheCap(RiverInputs.Example),
    };

    private static RiverInputs FlatRateAllowances(RiverInputs inputs) =>
        inputs.AddToPlan(OptionalModule.Allowances).WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m);

    // 12% of a cadastral income of 1,200 let for the 2026 cap, 1,380.00 (RentCalculatorTests).
    private static RiverInputs RentAtTheCap(RiverInputs inputs) =>
        inputs.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

    // The move search keeps the home's facts while Rent is out of the plan, with no rent: the year it
    // starts from is Run's.
    [Fact]
    public void MoveScenario_rentFactsOutOfThePlan_runsTheSameYear()
    {
        var inputs = RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

        var scenario = inputs.MoveScenario();

        (scenario.Rent, YearSimulation.Run(scenario).Value?.NetInPocket).ShouldBe((new HomeOfficeRent(1_200m, 0.12m, 0m), inputs.Run().Year?.NetInPocket));
    }
}

using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What each module card draws, on the river's example (RiverInputsTests): 100,000 of profit before
// pay, 50,000 of pay leaving 29,579.30 net, its tax withheld as the key formula says, which leaves a
// surcharge under the minimum (486.57 is due with nothing withheld), corporate tax 10,000 at 20%
// because the taxable income, 50,000, does not exceed the pay, and, nothing being prepaid, a
// surcharge of 675.00 on it. The dividend's cards prepay 7,500 by 10 April, which cancels it: 40,000
// of profit after tax paid out as a VVPRbis dividend (withholding 7,200, refund 149.94, net 32,949.94).
// The example grants no vouchers; the vouchers' cards grant 220 days of meal vouchers, 250 of eco and
// 100 of sport/culture vouchers (YearSimulationTests): 2,310.20 to the pocket, 1,430.20 of it the
// company cannot deduct, which leaves 97,689.80 for the split and 49,120.00 of taxable income, taxed
// 9,824.00 and, nothing prepaid, surcharged 663.12.
public class ModuleViewsTests
{
    private static readonly RiverInputs Prepaid = RiverInputs.Example.WithAdvancePayment(0, 7_500m).WithTaxWithheld(11_334m);

    private static readonly RiverInputs AllVouchers = RiverInputs.Example.WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m);

    [Fact]
    public void Split_example_givesThePayAndTheCompanysProfit()
    {
        var view = Split(RiverInputs.Example);

        (view.Ceiling, view.Pay, view.CompanyProfit).ShouldBe((100_000m, 50_000m, (decimal?)50_000m));
    }

    [Fact]
    public void Split_profitBelowThePaySet_givesTheProfitAsPay()
    {
        var view = Split(RiverInputs.Example.WithRevenue(60_000m));

        (view.Ceiling, view.Pay, view.CompanyProfit).ShouldBe((40_000m, 40_000m, (decimal?)0m));
    }

    // The slider still has a place to sit while another card's amount is refused.
    [Fact]
    public void Split_refusedInputElsewhere_keepsThePayWithoutTheCompanysProfit()
    {
        var view = Split(RiverInputs.Example.WithReserve(-1m));

        (view.Pay, view.CompanyProfit).ShouldBe((50_000m, (decimal?)null));
    }

    // The company's profit before tax is what the split leaves it, without what it cannot deduct of the
    // vouchers, which Corporate tax adds back.
    [Fact]
    public void Split_vouchers_givesThePayAndTheCompanysProfitOutOfWhatTheyLeave()
    {
        var view = Split(AllVouchers);

        (view.Ceiling, view.Pay, view.CompanyProfit).ShouldBe((97_689.80m, 50_000m, (decimal?)47_689.80m));
    }

    [Fact]
    public void Split_negativePayRefused_putsTheSliderAtZero()
    {
        Split(RiverInputs.Example.WithDirectorPay(-1m)).Pay.ShouldBe(0m);
    }

    // The exact amount field shows the pay the split gives, or what was typed while it is refused.
    [Fact]
    public void Split_example_fieldShowsThePayTheSplitGives()
    {
        Split(RiverInputs.Example.WithRevenue(60_000m)).FieldPay.ShouldBe(40_000m);
    }

    [Fact]
    public void Split_negativePayRefused_fieldKeepsWhatWasTyped()
    {
        Split(RiverInputs.Example.WithDirectorPay(-1m)).FieldPay.ShouldBe(-1m);
    }

    [Fact]
    public void DirectorPay_example_drainsToTaxesOnPayAndThePocket()
    {
        var view = DirectorPay(RiverInputs.Example);

        view.Drains.ShouldBe(new[]
        {
            new Drain("social_contributions", DrainKind.PaidOut, 8_293.36m, "taxes_on_pay"),
            new Drain("income_tax", DrainKind.PaidOut, 11_333.96m, "taxes_on_pay"),
            new Drain("municipal_surcharge", DrainKind.PaidOut, 793.38m, "taxes_on_pay"),
            new Drain("low_earned_income_credit", DrainKind.Back, 0m, "taxes_on_pay"),
            new Drain("income_tax_advance_payment_surcharge", DrainKind.PaidOut, 0m, "taxes_on_pay"),
            new Drain("income_tax_advance_payment_bonus", DrainKind.Back, 0m, "taxes_on_pay"),
            new Drain("net_pay", DrainKind.PassedOn, 29_579.30m, "pocket"),
        });
    }

    // At the best pay the taxable income is 16,720.20, where the credit is whole.
    [Fact]
    public void DirectorPay_bestPay_creditComesBackFromTaxesOnPay()
    {
        var view = DirectorPay(RiverInputs.Example.WithDirectorPay(20_799m));

        view.Drains.Single(drain => drain.Key == "low_earned_income_credit")
            .ShouldBe(new Drain("low_earned_income_credit", DrainKind.Back, 910m, "taxes_on_pay"));
    }

    // The company's 9,942.52 of contributions are pay: its cost of the 50,000 is 59,942.52, and the
    // contributions drain says the company pays them. The formula withholds 14,336.52 on the taxable
    // pay, which leaves a surcharge under the minimum, so the net is 33,999.21.
    [Fact]
    public void DirectorPay_companyPays_showsTheCompanysCostAndDrainsTheContributionsAsPaidByIt()
    {
        var view = DirectorPay(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company));

        (view.CompanyPaysContributions, view.CompanyCost, view.Drains[0], view.Drains[^1]).ShouldBe((true, 59_942.52m,
            new Drain("social_contributions_paid_by_company", DrainKind.PaidOut, 9_942.52m, "taxes_on_pay"),
            new Drain("net_pay", DrainKind.PassedOn, 33_999.21m, "pocket")));
    }

    [Fact]
    public void DirectorPay_example_companysCostIsTheGross()
    {
        var view = DirectorPay(RiverInputs.Example);

        (view.CompanyPaysContributions, view.CompanyCost).ShouldBe((false, 50_000m));
    }

    [Fact]
    public void DirectorPay_example_payIsDerivedFromTheSplit()
    {
        DirectorPay(RiverInputs.Example).PayOrigin.ShouldBe(InputOrigin.Derived);
    }

    [Fact]
    public void DirectorPay_companySkipped_payKeepsItsOwnOrigin()
    {
        var inputs = RiverInputs.Example.SkipCompany();

        (DirectorPay(inputs).PayOrigin, DirectorPay(inputs.WithDirectorPay(40_000m)).PayOrigin).ShouldBe((InputOrigin.Example, InputOrigin.Yours));
    }

    [Fact]
    public void DirectorPay_example_listsThePaysStepsWithTheirSources()
    {
        DirectorPay(RiverInputs.Example).Details.Select(detail => detail.RuleKey)
            .ShouldBe(["social_contributions", "flat_rate_costs", "taxable_income", "income_tax", "municipal_surcharge", "low_earned_income_credit",
                "income_tax_advance_payment_surcharge", "income_tax_advance_payment_bonus"]);
    }

    // Nothing withheld: 486.57 on the pay's tax, and 14,317.50 by 21 December brings it under the
    // 100 minimum.
    [Fact]
    public void DirectorPay_nothingWithheld_saysTheSurchargeIsDueAndWhatByTheLastDeadlineLeavesNoneDue()
    {
        var view = DirectorPay(RiverInputs.Example.WithTaxWithheld(0m));

        (view.Surcharge, view.SurchargeAmount, view.TopUp, view.LastDeadline)
            .ShouldBe((TaxSurchargeState.Due, 486.57m, 14_317.50m, new DateOnly(2026, 12, 21)));
    }

    // The 10,691.28 the key formula withholds leaves 59.52 of surcharge before the 90%, under the minimum.
    [Fact]
    public void DirectorPay_example_saysTheSurchargeIsUnderTheMinimum()
    {
        DirectorPay(RiverInputs.Example).Surcharge.ShouldBe(TaxSurchargeState.BelowMinimum);
    }

    [Fact]
    public void DirectorPay_almostAllWithheld_saysTheSurchargeIsUnderTheMinimum()
    {
        DirectorPay(RiverInputs.Example.WithTaxWithheld(11_334m)).Surcharge.ShouldBe(TaxSurchargeState.BelowMinimum);
    }

    [Fact]
    public void DirectorPay_allWithheld_saysNoneIsLeft()
    {
        DirectorPay(RiverInputs.Example.WithTaxWithheld(20_000m)).Surcharge.ShouldBe(TaxSurchargeState.Covered);
    }

    [Fact]
    public void DirectorPay_starter_saysItIsExempt()
    {
        DirectorPay(RiverInputs.Example.WithStarter(true)).Surcharge.ShouldBe(TaxSurchargeState.Exempt);
    }

    // At no pay there is no tax to surcharge.
    [Fact]
    public void DirectorPay_noPay_hasNoTaxToSurcharge()
    {
        DirectorPay(RiverInputs.Example.WithDirectorPay(0m)).Surcharge.ShouldBe(TaxSurchargeState.NoTax);
    }

    // With nothing withheld, 12,014 by 10 April is the whole base and earns 90.11
    // (IncomeTaxAdvancePaymentCalculatorTests); on the example most of it is beyond the base.
    [Fact]
    public void DirectorPay_theWholeBaseBy10April_namesTheBonus()
    {
        var view = DirectorPay(RiverInputs.Example.WithTaxWithheld(0m).WithTaxAdvancePayment(0, 12_014m));

        (view.Surcharge, view.Bonus).ShouldBe((TaxSurchargeState.Covered, 90.11m));
    }

    // The field shows the year's withholding the pay slip gives, twelve times the month's, as derived.
    [Fact]
    public void WithheldField_example_showsThePaySlipsAmountAsDerived()
    {
        var field = WithheldField(RiverInputs.Example);

        (field.Amount, field.Origin, field.IsYours).ShouldBe((10_691.28m, InputOrigin.Derived, false));
    }

    [Fact]
    public void WithheldField_amountEntered_showsItAsYours()
    {
        var field = WithheldField(RiverInputs.Example.WithTaxWithheld(10_000m));

        (field.Amount, field.Origin, field.IsYours).ShouldBe((10_000m, InputOrigin.Yours, true));
    }

    [Fact]
    public void WithheldField_backToThePaySlips_showsItAsDerivedAgain()
    {
        var field = WithheldField(RiverInputs.Example.WithTaxWithheld(10_000m).WithTaxWithheldAsThePaySlip());

        (field.Amount, field.Origin).ShouldBe((10_691.28m, InputOrigin.Derived));
    }

    // While another card's amount is refused there is no pay slip to derive from; the field keeps
    // its place.
    [Fact]
    public void WithheldField_derivedWhileAnotherInputIsRefused_showsNothing()
    {
        var field = WithheldField(RiverInputs.Example.WithReserve(-1m));

        (field.Amount, field.Origin).ShouldBe((0m, InputOrigin.Derived));
    }

    [Fact]
    public void WithheldField_negativeAmountRefused_keepsWhatWasTyped()
    {
        WithheldField(RiverInputs.Example.WithTaxWithheld(-1m)).Amount.ShouldBe(-1m);
    }

    [Fact]
    public void TaxAdvancePaymentFields_example_oneFieldPerDeadlineWithItsDate()
    {
        var fields = TaxAdvancePaymentFields.From(RiverInputs.Example);

        fields.Select(field => (field.Deadline, field.Date, field.Paid)).ShouldBe(new[]
        {
            (0, new DateOnly(2026, 4, 10), new Input<decimal>(0m, InputOrigin.Example)),
            (1, new DateOnly(2026, 7, 10), new Input<decimal>(0m, InputOrigin.Example)),
            (2, new DateOnly(2026, 10, 12), new Input<decimal>(0m, InputOrigin.Example)),
            (3, new DateOnly(2026, 12, 21), new Input<decimal>(0m, InputOrigin.Example)),
        });
    }

    [Fact]
    public void CorporateTax_example_saysWhichConditionKeptTheReducedRate()
    {
        var view = CorporateTax(RiverInputs.Example);

        (view.TaxableIncome, view.Condition, view.MinimumDirectorPay, view.NoTaxableProfit)
            .ShouldBe((50_000m, ReducedRateCondition.TaxableIncomeNotAbovePay, 51_000m, false));
    }

    [Fact]
    public void CorporateTax_example_drainsTheTaxAndPassesTheRestToTheDividend()
    {
        CorporateTax(RiverInputs.Example).Drains.ShouldBe(new[]
        {
            new Drain("corporate_tax", DrainKind.PaidOut, 10_000.00m, "taxes_on_profit"),
            new Drain("advance_payment_surcharge", DrainKind.PaidOut, 675.00m, "taxes_on_profit"),
            new Drain("profit_after_tax", DrainKind.PassedOn, 39_325.00m, "dividend"),
        });
    }

    // What comes in is the profit the split leaves the company; the taxable income adds back the
    // 1,430.20 it cannot deduct of the vouchers.
    [Fact]
    public void CorporateTax_vouchers_saysWhatComesInAndWhatTheTaxableIncomeAddsBack()
    {
        var view = CorporateTax(AllVouchers);

        (view.ProfitAfterPay, view.DisallowedExpenses, view.TaxableIncome).ShouldBe((47_689.80m, 1_430.20m, 49_120.00m));
    }

    // The tax, its surcharge and the profit after tax add up to what came in, 47,689.80.
    [Fact]
    public void CorporateTax_vouchers_drainsWhatCameIn()
    {
        CorporateTax(AllVouchers).Drains.ShouldBe(new[]
        {
            new Drain("corporate_tax", DrainKind.PaidOut, 9_824.00m, "taxes_on_profit"),
            new Drain("advance_payment_surcharge", DrainKind.PaidOut, 663.12m, "taxes_on_profit"),
            new Drain("profit_after_tax", DrainKind.PassedOn, 37_202.68m, "dividend"),
        });
    }

    [Fact]
    public void CorporateTax_vouchers_listsWhatTheCompanyCannotDeductAmongItsSteps()
    {
        CorporateTax(AllVouchers).Details.Select(detail => detail.RuleKey)
            .ShouldBe(["disallowed_vouchers", "corporate_tax", "advance_payment_surcharge"]);
    }

    [Fact]
    public void CorporateTax_example_addsNothingBack()
    {
        var view = CorporateTax(RiverInputs.Example);

        (view.ProfitAfterPay, view.DisallowedExpenses).ShouldBe((50_000m, 0m));
    }

    [Fact]
    public void CorporateTax_payUnderTheMinimum_saysTheStandardRateHeld()
    {
        var view = CorporateTax(RiverInputs.Example.WithDirectorPay(30_000m));

        (view.Condition, view.Drains[0].Amount).ShouldBe((ReducedRateCondition.MinimumPayNotMet, 17_500.00m));
    }

    [Fact]
    public void CorporateTax_lossBeforePay_saysThereIsNoTaxableProfit()
    {
        CorporateTax(RiverInputs.Example.WithRevenue(10_000m)).NoTaxableProfit.ShouldBeTrue();
    }

    [Fact]
    public void CorporateTax_example_listsOnlyTheCompanysSteps()
    {
        CorporateTax(RiverInputs.Example).Details.Select(detail => detail.RuleKey).ShouldBe(["corporate_tax", "advance_payment_surcharge"]);
    }

    // 675 ÷ 4.5% = 15,000 by 21 December.
    [Fact]
    public void CorporateTax_example_saysTheSurchargeIsDueAndWhatByTheLastDeadlineCancelsIt()
    {
        var view = CorporateTax(RiverInputs.Example);

        (view.Surcharge, view.SurchargeAmount, view.TopUp, view.LastDeadline)
            .ShouldBe((SurchargeState.Due, 675.00m, 15_000.00m, new DateOnly(2026, 12, 21)));
    }

    [Fact]
    public void CorporateTax_prepaidEnough_saysThePaymentsCoverIt()
    {
        var view = CorporateTax(Prepaid);

        (view.Surcharge, view.SurchargeAmount, view.TopUp).ShouldBe((SurchargeState.Covered, 0m, 0m));
    }

    [Fact]
    public void CorporateTax_smallCompanyInItsSecondFinancialYear_saysItIsExempt()
    {
        CorporateTax(RiverInputs.Example.WithFinancialYear(FinancialYear.Second)).Surcharge.ShouldBe(SurchargeState.Exempt);
    }

    [Fact]
    public void CorporateTax_lossBeforePay_hasNoTaxToSurcharge()
    {
        CorporateTax(RiverInputs.Example.WithRevenue(10_000m)).Surcharge.ShouldBe(SurchargeState.NoTax);
    }

    [Fact]
    public void AdvancePaymentFields_example_oneFieldPerDeadlineWithItsDate()
    {
        var fields = AdvancePaymentFields.From(RiverInputs.Example);

        fields.Select(field => (field.Deadline, field.Date, field.Paid)).ShouldBe(new[]
        {
            (0, new DateOnly(2026, 4, 10), new Input<decimal>(0m, InputOrigin.Example)),
            (1, new DateOnly(2026, 7, 10), new Input<decimal>(0m, InputOrigin.Example)),
            (2, new DateOnly(2026, 10, 12), new Input<decimal>(0m, InputOrigin.Example)),
            (3, new DateOnly(2026, 12, 21), new Input<decimal>(0m, InputOrigin.Example)),
        });
    }

    [Fact]
    public void AdvancePaymentFields_paymentEntered_isTheUsers()
    {
        AdvancePaymentFields.From(RiverInputs.Example.WithAdvancePayment(2, 1_000m))[2].Paid.ShouldBe(new Input<decimal>(1_000m, InputOrigin.Yours));
    }

    [Fact]
    public void Dividend_nothingKept_paysAllOfTheProfitAfterTaxOut()
    {
        var view = Dividend(Prepaid);

        (view.Maximum, view.Kept, view.Dividend, view.Rate).ShouldBe((40_000.00m, 0m, 40_000.00m, 0.18m));
    }

    [Fact]
    public void Dividend_reserve_isKeptAndTheRestPaidOut()
    {
        var view = Dividend(Prepaid.WithReserve(15_000m));

        (view.Kept, view.Dividend).ShouldBe((15_000.00m, 25_000.00m));
    }

    [Fact]
    public void Dividend_reserveAboveTheProfitAfterTax_keepsItAll()
    {
        var view = Dividend(Prepaid.WithReserve(50_000m));

        (view.Kept, view.Dividend).ShouldBe((40_000.00m, 0m));
    }

    [Fact]
    public void Dividend_example_drainsTheWithholdingGetsTheRefundBackAndPassesTheRestOn()
    {
        Dividend(Prepaid.WithReserve(15_000m)).Drains.ShouldBe(new[]
        {
            new Drain("dividend_withholding", DrainKind.PaidOut, 4_500.00m, "taxes_on_profit"),
            new Drain("dividend_exemption_refund", DrainKind.Back, 149.94m, "taxes_on_profit"),
            new Drain("net_dividend", DrainKind.PassedOn, 20_649.94m, "pocket"),
            new Drain("kept_in_company", DrainKind.PassedOn, 15_000.00m, "kept_in_company"),
        });
    }

    // With no dividend the refund is zero, and still money that comes back rather than a payment.
    [Fact]
    public void Dividend_noDividend_listsTheRefundAsComingBack()
    {
        Dividend(Prepaid.WithReserve(50_000m)).Drains[1].ShouldBe(new Drain("dividend_exemption_refund", DrainKind.Back, 0m, "taxes_on_profit"));
    }

    [Fact]
    public void Dividend_lossYear_keepsTheLossInTheCompany()
    {
        var view = Dividend(Prepaid.WithRevenue(10_000m));

        (view.Maximum, view.Dividend, view.Drains[^1].Amount).ShouldBe((0m, 0m, -10_000m));
    }

    // 40,000 of this year's profit after tax and 60,000 kept from earlier years: the slider runs over
    // both, 30,000 kept and 70,000 paid out.
    [Fact]
    public void Dividend_earlierProfits_slidesOverThemAndTheProfitAfterTax()
    {
        var view = Dividend(Prepaid.WithEarlierProfits(60_000m).WithReserve(30_000m));

        (view.Maximum, view.ProfitAfterTax, view.EarlierProfits, view.Kept, view.Dividend).ShouldBe((100_000m, 40_000m, 60_000m, 30_000m, 70_000m));
    }

    [Fact]
    public void Dividend_example_listsOnlyTheDividendsSteps()
    {
        Dividend(Prepaid).Details.Select(detail => detail.RuleKey).ShouldBe(["dividend_withholding", "dividend_exemption_refund"]);
    }

    // No vouchers: the profit before pay goes on to the split whole, and there are no steps to list.
    [Fact]
    public void Vouchers_example_passTheProfitBeforePayOnToTheSplit()
    {
        var view = Vouchers(RiverInputs.Example);

        (view.Total, view.Deductible, view.Disallowed, view.Details.Count, view.Drains.Single())
            .ShouldBe((0m, 0m, 0m, 0, new Drain("profit_before_pay", DrainKind.PassedOn, 100_000m, "split")));
    }

    [Fact]
    public void Vouchers_allThree_drainEachToThePocketAndTheRestToTheSplit()
    {
        Vouchers(AllVouchers).Drains.ShouldBe(new[]
        {
            new Drain("meal_vouchers", DrainKind.PassedOn, 1_960.20m, "pocket"),
            new Drain("eco_vouchers", DrainKind.PassedOn, 250m, "pocket"),
            new Drain("sport_culture_vouchers", DrainKind.PassedOn, 100m, "pocket"),
            new Drain("profit_before_pay", DrainKind.PassedOn, 97_689.80m, "split"),
        });
    }

    [Fact]
    public void Vouchers_allThree_sayWhatTheCompanyDeductsAndWhatItCannot()
    {
        var view = Vouchers(AllVouchers);

        (view.Total, view.Deductible, view.Disallowed).ShouldBe((2_310.20m, 880.00m, 1_430.20m));
    }

    [Fact]
    public void Vouchers_allThree_listTheirStepsWithWhatTheCompanyCannotDeduct()
    {
        Vouchers(AllVouchers).Details.Select(detail => detail.RuleKey)
            .ShouldBe(["meal_vouchers", "eco_vouchers", "sport_culture_vouchers", "disallowed_vouchers"]);
    }

    // The exemption's texts first, then the amounts each step used under the text they come from.
    [Fact]
    public void Vouchers_mealVouchers_citeTheExemptionAndTheShares()
    {
        var meal = Vouchers(AllVouchers).Details[0];

        meal.Sources.Select(group => (group.Source, group.Amounts.Count)).ShouldBe(new[]
        {
            (VoucherRules.IncomeYear2026.Exemption, 0),
            (VoucherRules.IncomeYear2026.MealVoucherCompanyShare.Source, 2),
        });
    }

    // The module is named rather than typed: RiverModule is internal, a theory's parameters public.
    [Theory]
    [InlineData(YearSimulation.RevenueNegative, false, "CompanyYear")]
    [InlineData(YearSimulation.RunningCostsNegative, false, "CompanyYear")]
    [InlineData(GrossToNet.GrossPayNegative, false, "Split")]
    [InlineData(GrossToNet.GrossPayNegative, true, "DirectorPay")]
    [InlineData(GrossToNet.MunicipalRateOutOfRange, false, "DirectorPay")]
    [InlineData(YearSimulation.ReserveNegative, false, "Dividend")]
    [InlineData(YearSimulation.EarlierProfitsNegative, false, "Dividend")]
    [InlineData(YearSimulation.AdvancePaymentNegative, false, "CorporateTax")]
    [InlineData(GrossToNet.TaxWithheldNegative, false, "DirectorPay")]
    [InlineData(GrossToNet.TaxAdvancePaymentNegative, true, "DirectorPay")]
    [InlineData(VoucherCalculator.MealVoucherDaysNegative, false, "Vouchers")]
    [InlineData(VoucherCalculator.MealVoucherDaysAboveYear, false, "Vouchers")]
    [InlineData(VoucherCalculator.EcoVouchersNegative, false, "Vouchers")]
    [InlineData(VoucherCalculator.EcoVouchersAboveMaximum, false, "Vouchers")]
    [InlineData(VoucherCalculator.SportCultureVouchersNegative, false, "Vouchers")]
    [InlineData(VoucherCalculator.SportCultureVouchersAboveMaximum, false, "Vouchers")]
    [InlineData(YearSimulation.IncomeYearNotSupported, false, "River")]
    public void RiverErrors_engineRefusal_isShownOnTheModuleHoldingTheField(string error, bool companySkipped, string module)
    {
        RiverErrors.Owner(error, companySkipped).ToString().ShouldBe(module);
    }

    private static SplitView Split(RiverInputs inputs) => SplitView.From(inputs, inputs.Run());

    private static DirectorPayView DirectorPay(RiverInputs inputs) =>
        DirectorPayView.From(inputs, inputs.Run().Pay.ShouldNotBeNull());

    private static WithheldFieldView WithheldField(RiverInputs inputs) => WithheldFieldView.From(inputs, inputs.Run());

    private static CorporateTaxView CorporateTax(RiverInputs inputs) =>
        CorporateTaxView.From(inputs.Run().Year.ShouldNotBeNull());

    private static DividendView Dividend(RiverInputs inputs) =>
        DividendView.From(inputs.Run().Year.ShouldNotBeNull());

    private static VoucherView Vouchers(RiverInputs inputs) =>
        VoucherView.From(inputs.Run().Year.ShouldNotBeNull());
}

using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What each module card draws, on the river's example (RiverInputsTests): 100,000 of profit before
// pay, 50,000 of pay leaving 29,579.30 net, corporate tax 10,000 at 20% because the taxable income,
// 50,000, does not exceed the pay, and, nothing being prepaid, a surcharge of 675.00 on it. The
// dividend's cards prepay 7,500 by 10 April, which cancels it: 40,000 of profit after tax paid out as
// a VVPRbis dividend (withholding 7,200, refund 149.94, net 32,949.94).
public class ModuleViewsTests
{
    private static readonly RiverInputs Prepaid = RiverInputs.Example.WithAdvancePayment(0, 7_500m);

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
            .ShouldBe(["social_contributions", "flat_rate_costs", "taxable_income", "income_tax", "municipal_surcharge", "low_earned_income_credit"]);
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
    [InlineData(YearSimulation.IncomeYearNotSupported, false, "River")]
    public void RiverErrors_engineRefusal_isShownOnTheModuleHoldingTheField(string error, bool companySkipped, string module)
    {
        RiverErrors.Owner(error, companySkipped).ToString().ShouldBe(module);
    }

    private static SplitView Split(RiverInputs inputs) => SplitView.From(inputs, inputs.Run());

    private static DirectorPayView DirectorPay(RiverInputs inputs) =>
        DirectorPayView.From(inputs, inputs.Run().Pay.ShouldNotBeNull());

    private static CorporateTaxView CorporateTax(RiverInputs inputs) =>
        CorporateTaxView.From(inputs.Run().Year.ShouldNotBeNull());

    private static DividendView Dividend(RiverInputs inputs) =>
        DividendView.From(inputs.Run().Year.ShouldNotBeNull());
}

using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What each module card draws, on the river's example (RiverInputsTests): 100,000 of profit before
// pay, 50,000 of pay leaving 29,579.30 net, corporate tax 10,000 at 20% because the taxable income,
// 50,000, does not exceed the pay, then 40,000 of profit after tax paid out as a VVPRbis dividend
// (withholding 7,200, refund 149.94, net 32,949.94).
public class ModuleViewsTests
{
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

    [Fact]
    public void DirectorPay_example_drainsToTaxesOnPayAndThePocket()
    {
        var view = DirectorPay(RiverInputs.Example);

        view.Drains.ShouldBe(new[]
        {
            new Drain("social_contributions", DrainKind.PaidOut, 8_293.36m, "taxes_on_pay"),
            new Drain("income_tax", DrainKind.PaidOut, 11_333.96m, "taxes_on_pay"),
            new Drain("municipal_surcharge", DrainKind.PaidOut, 793.38m, "taxes_on_pay"),
            new Drain("net_pay", DrainKind.PassedOn, 29_579.30m, "pocket"),
        });
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

    // The pay slip is the gross-to-net view the pay page drew, on the same result.
    [Fact]
    public void DirectorPay_example_paySlipIsTheGrossToNetViewOfThePay()
    {
        var view = DirectorPay(RiverInputs.Example);

        view.PaySlip.Details.Select(detail => detail.RuleKey)
            .ShouldBe(["social_contributions", "flat_rate_costs", "taxable_income", "income_tax", "municipal_surcharge"]);
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
            new Drain("profit_after_tax", DrainKind.PassedOn, 40_000.00m, "dividend"),
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
        CorporateTax(RiverInputs.Example).Details.Select(detail => detail.RuleKey).ShouldBe(["corporate_tax"]);
    }

    [Fact]
    public void Dividend_nothingKept_paysAllOfTheProfitAfterTaxOut()
    {
        var view = Dividend(RiverInputs.Example);

        (view.Maximum, view.Kept, view.Dividend, view.Rate).ShouldBe((40_000.00m, 0m, 40_000.00m, 0.18m));
    }

    [Fact]
    public void Dividend_reserve_isKeptAndTheRestPaidOut()
    {
        var view = Dividend(RiverInputs.Example.WithReserve(15_000m));

        (view.Kept, view.Dividend).ShouldBe((15_000.00m, 25_000.00m));
    }

    [Fact]
    public void Dividend_reserveAboveTheProfitAfterTax_keepsItAll()
    {
        var view = Dividend(RiverInputs.Example.WithReserve(50_000m));

        (view.Kept, view.Dividend).ShouldBe((40_000.00m, 0m));
    }

    [Fact]
    public void Dividend_example_drainsTheWithholdingGetsTheRefundBackAndPassesTheRestOn()
    {
        Dividend(RiverInputs.Example.WithReserve(15_000m)).Drains.ShouldBe(new[]
        {
            new Drain("dividend_withholding", DrainKind.PaidOut, 4_500.00m, "taxes_on_profit"),
            new Drain("dividend_exemption_refund", DrainKind.Back, 149.94m, "taxes_on_profit"),
            new Drain("net_dividend", DrainKind.PassedOn, 20_649.94m, "pocket"),
            new Drain("kept_in_company", DrainKind.PassedOn, 15_000.00m, "kept_in_company"),
        });
    }

    [Fact]
    public void Dividend_lossYear_keepsTheLossInTheCompany()
    {
        var view = Dividend(RiverInputs.Example.WithRevenue(10_000m));

        (view.Maximum, view.Dividend, view.Drains[^1].Amount).ShouldBe((0m, 0m, -10_000m));
    }

    [Fact]
    public void Dividend_example_listsOnlyTheDividendsSteps()
    {
        Dividend(RiverInputs.Example).Details.Select(detail => detail.RuleKey).ShouldBe(["dividend_withholding", "dividend_exemption_refund"]);
    }

    // The module is named rather than typed: RiverModule is internal, a theory's parameters public.
    [Theory]
    [InlineData(YearSimulation.RevenueNegative, false, "CompanyYear")]
    [InlineData(YearSimulation.RunningCostsNegative, false, "CompanyYear")]
    [InlineData(GrossToNet.GrossPayNegative, false, "Split")]
    [InlineData(GrossToNet.GrossPayNegative, true, "DirectorPay")]
    [InlineData(GrossToNet.MunicipalRateOutOfRange, false, "DirectorPay")]
    [InlineData(YearSimulation.ReserveNegative, false, "Dividend")]
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

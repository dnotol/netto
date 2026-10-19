using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Shouldly;

namespace Netto.Tests.Simulation;

// Income year 2026 at a 7% municipal rate, for a made-up company with 100,000 of profit before pay
// unless a test says otherwise. Director pay figures are GrossToNetTests' (50,000: contributions
// 8,293.36, income tax 11,333.96, surcharge 793.38, net 29,579.30); the company and dividend
// figures follow CorporateTaxCalculatorTests and DividendCalculatorTests. The dividend is what the
// profit after tax leaves once the reserve is kept in the company.
public class YearSimulationTests
{
    private static readonly CompanyStatus SmallPastStartUp = new(SmallCompany: true, FirstFourTaxPeriods: false);

    public static TheoryData<decimal, decimal, decimal, WithholdingCategory> Setups => new()
    {
        { 50_000m, 100_000m, 0m, WithholdingCategory.VvprbisThirdYearOnwards },
        { 21_000m, 100_000m, 20_000m, WithholdingCategory.Standard },
        { 160_000m, 50_000m, 0m, WithholdingCategory.Standard },
        { 0m, 300_000m, 500_000m, WithholdingCategory.VvprbisSecondYear },
    };

    // The Lever catalogue's scenario (ii) without the fund fee: taxable income 50,000 does not
    // exceed the pay, so the company keeps 20% although the pay is under 51,000.
    [Fact]
    public void Run_2026Pay50000AndNothingKept_leavesItAllInThePocketOrPaidOut()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        (result.CorporateTax, result.SocialContributions, result.PersonalTax, result.NetInPocket, result.KeptInCompany)
            .ShouldBe((10_000.00m, 8_293.36m, 19_177.40m, 62_529.24m, 0m));
    }

    [Fact]
    public void Run_2026Pay50000AndNothingKept_saysWhichConditionKeptTheReducedRate()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        result.Company.Condition.ShouldBe(ReducedRateCondition.TaxableIncomeNotAbovePay);
    }

    [Fact]
    public void Run_2026PayUnderTheMinimumAndTaxableAbovePay_pays25Percent()
    {
        var result = Run(Scenario(directorPay: 30_000m, reserve: 0m));

        (result.Company.Condition, result.CorporateTax).ShouldBe((ReducedRateCondition.MinimumPayNotMet, 17_500.00m));
    }

    [Theory]
    [MemberData(nameof(Setups))]
    public void Run_2026_layersPocketAndCompanyAddUpToTheProfitBeforePay(
        decimal directorPay, decimal profitBeforePay, decimal reserve, WithholdingCategory category)
    {
        var result = Run(Scenario(directorPay: directorPay, reserve: reserve, category: category, revenue: profitBeforePay + 20_000m));

        (result.CorporateTax + result.SocialContributions + result.PersonalTax + result.NetInPocket + result.KeptInCompany)
            .ShouldBe(profitBeforePay);
    }

    [Fact]
    public void Run_2026_directorPayIsWhatGrossToNetGives()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));
        var (pay, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.Entered(0.07m));

        (result.DirectorPay.SocialContributions, result.DirectorPay.IncomeTax, result.DirectorPay.MunicipalSurcharge, result.DirectorPay.NetInPocket)
            .ShouldBe((pay!.SocialContributions, pay.IncomeTax, pay.MunicipalSurcharge, pay.NetInPocket));
    }

    [Fact]
    public void Run_2026_returnsTheStepsInTheLawsOrderWithTheirTags()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        result.Steps.Select(step => (step.RuleKey, step.Layer, step.Event, step.Track, step.Amount)).ShouldBe(new[]
        {
            ("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, 10_000.00m),
            ("social_contributions", Layer.Social, TaxableEvent.DirectorPay, Track.PaidOut, 8_293.36m),
            ("flat_rate_costs", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, 1_251.20m),
            ("taxable_income", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, 40_455.44m),
            ("income_tax", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 11_333.96m),
            ("municipal_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, 793.38m),
            ("dividend_withholding", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, 7_200.00m),
            ("dividend_exemption_refund", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, -149.94m),
        });
    }

    [Fact]
    public void Run_2026_everyStepHasASourceWithAReferenceAndAnAddress()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        result.Steps.ShouldAllBe(step => step.Source.Reference.Length > 0 && step.Source.Url.IsAbsoluteUri);
    }

    [Fact]
    public void Run_2026_theMaximumDividendIsTheProfitAfterTax()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 15_000m));

        (result.ProfitAfterTax, result.MaximumDividend).ShouldBe((40_000.00m, 40_000.00m));
    }

    [Fact]
    public void Run_2026NothingKept_paysAllOfTheProfitAfterTaxOut()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 0m));

        (result.Dividend.Dividend, result.KeptInCompany).ShouldBe((40_000.00m, 0m));
    }

    [Fact]
    public void Run_2026Reserve_keepsItAndPaysOutTheRest()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 15_000m));

        (result.Dividend.Dividend, result.KeptInCompany).ShouldBe((25_000.00m, 15_000.00m));
    }

    [Fact]
    public void Run_2026Reserve_taxesTheDividendLeftAtTheDeclaredRate()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 15_000m));

        (result.Dividend.Withholding, result.Dividend.ExemptionRefund, result.Dividend.NetDividend)
            .ShouldBe((4_500.00m, 149.94m, 20_649.94m));
    }

    [Fact]
    public void Run_2026ReserveAboveTheProfitAfterTax_paysNoDividendAndKeepsAllOfIt()
    {
        var result = Run(Scenario(directorPay: 50_000m, reserve: 40_000.01m));

        (result.Dividend.Dividend, result.KeptInCompany).ShouldBe((0m, 40_000.00m));
    }

    [Fact]
    public void Run_2026Loss_allowsNoDividend()
    {
        var result = Run(Scenario(directorPay: 30_000m, reserve: 0m, revenue: 40_000m));

        (result.ProfitAfterTax, result.MaximumDividend, result.Dividend.Dividend).ShouldBe((-10_000m, 0m, 0m));
    }

    [Fact]
    public void Run_2026Loss_paysNoCorporateTaxAndKeepsTheLossInTheCompany()
    {
        var result = Run(Scenario(directorPay: 30_000m, reserve: 0m, revenue: 40_000m));

        (result.CorporateTax, result.KeptInCompany).ShouldBe((0m, -10_000m));
    }

    [Fact]
    public void Run_negativeRevenue_isRefused()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 0m, reserve: 0m, revenue: -0.01m));

        (value, error).ShouldBe((null, YearSimulation.RevenueNegative));
    }

    [Fact]
    public void Run_negativeRunningCosts_isRefused()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 0m, reserve: 0m) with { RunningCosts = -0.01m });

        (value, error).ShouldBe((null, YearSimulation.RunningCostsNegative));
    }

    [Fact]
    public void Run_negativeReserve_isRefused()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: -0.01m));

        (value, error).ShouldBe((null, YearSimulation.ReserveNegative));
    }

    [Fact]
    public void ProfitBeforePay_isTheRevenueLessTheRunningCosts()
    {
        var scenario = Scenario(directorPay: 50_000m, reserve: 0m, revenue: 15_000m);

        scenario.ProfitBeforePay.ShouldBe(-5_000m);
    }

    [Fact]
    public void Run_negativeDirectorPay_isRefusedWithGrossToNetsKey()
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: -0.01m, reserve: 0m));

        (value, error).ShouldBe((null, GrossToNet.GrossPayNegative));
    }

    // 2025 has dividend amounts for the Tax-Calc tests but no corporate or INASTI amounts, and 2027's
    // amounts are not published.
    [Theory]
    [InlineData(2025)]
    [InlineData(2027)]
    public void Run_incomeYearOtherThan2026_isRefused(int incomeYear)
    {
        var (value, error) = YearSimulation.Run(Scenario(directorPay: 50_000m, reserve: 0m) with { IncomeYear = incomeYear });

        (value, error).ShouldBe((null, YearSimulation.IncomeYearNotSupported));
    }

    private static Scenario Scenario(
        decimal directorPay,
        decimal reserve,
        WithholdingCategory category = WithholdingCategory.VvprbisThirdYearOnwards,
        decimal revenue = 120_000m) =>
        new(2026, revenue, 20_000m, SmallPastStartUp, directorPay, MunicipalRates.Entered(0.07m), reserve, category);

    private static YearResult Run(Scenario scenario)
    {
        var (value, error) = YearSimulation.Run(scenario);

        error.ShouldBeNull();
        return value.ShouldNotBeNull();
    }
}

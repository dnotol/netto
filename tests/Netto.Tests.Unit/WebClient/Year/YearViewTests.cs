using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Web.Client.Year;
using Shouldly;

namespace Netto.Tests.WebClient.Year;

// Income year 2026 at a 7% municipal rate, on the engine's own figures (YearSimulationTests): 100,000
// of profit before pay, 50,000 of director pay and a 40,000 VVPRbis dividend give corporate tax
// 10,000, social contributions 8,293.36, personal tax 19,177.40, net in pocket 62,529.24 and nothing
// kept in the company.
public class YearViewTests
{
    [Fact]
    public void From_2026Example_splitsTheProfitBeforePayIntoFiveParts()
    {
        var view = View(directorPay: 50_000m, dividend: 40_000m);

        view.Split.Select(part => (part.Key, part.Amount)).ShouldBe(new[]
        {
            ("corporate_tax", 10_000.00m),
            ("social_contributions", 8_293.36m),
            ("personal_tax", 19_177.40m),
            ("net_in_pocket", 62_529.24m),
            ("kept_in_company", 0.00m),
        });
    }

    [Fact]
    public void From_2026Example_givesEachPartItsShareOfTheProfitBeforePay()
    {
        var view = View(directorPay: 50_000m, dividend: 40_000m);

        view.Split.Select(part => part.Share).ShouldBe(new decimal?[] { 0.1m, 0.0829336m, 0.191774m, 0.6252924m, 0m });
    }

    [Fact]
    public void From_2026Loss_drawsNoShares()
    {
        var view = View(directorPay: 30_000m, dividend: 0m, revenue: 40_000m);

        view.Split.ShouldAllBe(part => part.Share == null);
    }

    [Fact]
    public void From_2026Example_drawsTheCompanyThenTheDirectorPayThenTheDividend()
    {
        var view = View(directorPay: 50_000m, dividend: 40_000m);

        view.Ledgers.Select(ledger => ledger.Key).ShouldBe(["company", "director_pay", "dividend"]);
    }

    [Fact]
    public void From_2026Example_companyLedgerRunsFromTheProfitBeforePayToWhatItKeeps()
    {
        var company = View(directorPay: 50_000m, dividend: 25_000m).Ledgers[0];

        company.Rows.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
        {
            ("profit_before_pay", LedgerRowKind.Start, 100_000m),
            ("director_pay", LedgerRowKind.Out, 50_000m),
            ("company_taxable_income", LedgerRowKind.Subtotal, 50_000m),
            ("corporate_tax", LedgerRowKind.PaidOut, 10_000.00m),
            ("profit_after_tax", LedgerRowKind.Subtotal, 40_000.00m),
            ("dividend", LedgerRowKind.Out, 25_000m),
            ("kept_in_company", LedgerRowKind.End, 15_000.00m),
        });
    }

    [Fact]
    public void From_2026Example_directorPayLedgerRunsFromGrossThroughWhatIsPaidOutToNet()
    {
        var pay = View(directorPay: 50_000m, dividend: 40_000m).Ledgers[1];

        pay.Rows.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
        {
            ("gross_pay", LedgerRowKind.Start, 50_000m),
            ("social_contributions", LedgerRowKind.PaidOut, 8_293.36m),
            ("income_tax", LedgerRowKind.PaidOut, 11_333.96m),
            ("municipal_surcharge", LedgerRowKind.PaidOut, 793.38m),
            ("net_pay", LedgerRowKind.End, 29_579.30m),
        });
    }

    // The refund is a negative step paid out; the ledger shows it as money coming back.
    [Fact]
    public void From_2026Example_dividendLedgerShowsTheRefundComingBack()
    {
        var dividend = View(directorPay: 50_000m, dividend: 40_000m).Ledgers[2];

        dividend.Rows.Select(row => (row.Key, row.Kind, row.Amount)).ShouldBe(new[]
        {
            ("gross_dividend", LedgerRowKind.Start, 40_000m),
            ("dividend_withholding", LedgerRowKind.PaidOut, 7_200.00m),
            ("dividend_exemption_refund", LedgerRowKind.Back, 149.94m),
            ("net_dividend", LedgerRowKind.End, 32_949.94m),
        });
    }

    [Fact]
    public void From_2026Example_listsEveryStepWithItsAmount()
    {
        var view = View(directorPay: 50_000m, dividend: 40_000m);

        view.Details.Select(detail => (detail.RuleKey, detail.Amount)).ShouldBe(new[]
        {
            ("corporate_tax", 10_000.00m),
            ("social_contributions", 8_293.36m),
            ("flat_rate_costs", 1_251.20m),
            ("taxable_income", 40_455.44m),
            ("income_tax", 11_333.96m),
            ("municipal_surcharge", 793.38m),
            ("dividend_withholding", 7_200.00m),
            ("dividend_exemption_refund", -149.94m),
        });
    }

    [Fact]
    public void From_2026Example_saysWhichConditionDecidedTheCorporateRate()
    {
        var view = View(directorPay: 50_000m, dividend: 40_000m);

        view.Condition.ShouldBe(ReducedRateCondition.TaxableIncomeNotAbovePay);
    }

    private static YearView View(decimal directorPay, decimal dividend, decimal revenue = 120_000m)
    {
        var scenario = new Scenario(2026, revenue, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false),
            directorPay, MunicipalRates.Entered(0.07m), dividend, WithholdingCategory.VvprbisThirdYearOnwards);
        var (value, error) = YearSimulation.Run(scenario);

        error.ShouldBeNull();
        return YearView.From(value.ShouldNotBeNull());
    }
}

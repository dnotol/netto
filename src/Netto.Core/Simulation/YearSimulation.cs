using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;

namespace Netto.Core.Simulation;

// One income year in the order the law computes it: the director pay lowers the company's taxable
// income, corporate tax takes its share of what is left, and the dividend comes out of the profit
// after tax. In steady state the year pays out its own profit (decided 2026-10-19), so the dividend
// cannot exceed it; earlier reserves are not modelled yet. No disallowed expenses, no losses carried
// forward, no advance-payment surcharges, and INASTI's company contribution sits in the running costs.
public static class YearSimulation
{
    public const string IncomeYearNotSupported = GrossToNet.IncomeYearNotSupported;
    public const string RevenueNegative = "revenue_negative";
    public const string RunningCostsNegative = "running_costs_negative";
    public const string DividendNegative = "dividend_negative";
    public const string DividendAboveProfitAfterTax = "dividend_above_profit_after_tax";

    public static (YearResult? Value, string? Error) Run(Scenario scenario)
    {
        var corporateRules = CorporateTaxRules.For(scenario.IncomeYear);
        var dividendRules = DividendRules.For(scenario.IncomeYear);
        if (corporateRules is null || dividendRules is null)
            return (null, IncomeYearNotSupported);
        if (scenario.Revenue < 0m)
            return (null, RevenueNegative);
        if (scenario.RunningCosts < 0m)
            return (null, RunningCostsNegative);
        if (scenario.Dividend < 0m)
            return (null, DividendNegative);

        var (pay, payError) = GrossToNet.Compute(scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate);
        if (pay is null)
            return (null, payError);

        var profitBeforePay = scenario.Revenue - scenario.RunningCosts;
        var company = CorporateTaxCalculator.Compute(corporateRules, profitBeforePay - pay.GrossPay, pay.GrossPay, scenario.Company);
        var profitAfterTax = company.TaxableIncome - company.CorporateTax;
        if (scenario.Dividend > Math.Max(0m, profitAfterTax))
            return (null, DividendAboveProfitAfterTax);

        var dividend = DividendCalculator.Compute(dividendRules, scenario.Dividend, scenario.DividendCategory);
        var rate = dividendRules.Rate(scenario.DividendCategory);

        IReadOnlyList<Step> steps =
        [
            new Step("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, company.CorporateTax,
                CorporateTaxRules.Source, corporateRules.Amounts),
            .. pay.Steps,
            new Step("dividend_withholding", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, dividend.Withholding,
                rate.Source, [rate]),
            new Step("dividend_exemption_refund", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, -dividend.ExemptionRefund,
                DividendRules.ExemptionRefund, [dividendRules.ExemptAmount, rate]),
        ];

        return (new YearResult(
            scenario,
            profitBeforePay,
            company,
            pay,
            dividend,
            PaidOut(steps, Layer.Corporate),
            PaidOut(steps, Layer.Social),
            PaidOut(steps, Layer.Personal),
            pay.NetInPocket + dividend.NetDividend,
            profitAfterTax - dividend.Dividend,
            steps), null);
    }

    private static decimal PaidOut(IReadOnlyList<Step> steps, Layer layer) =>
        steps.Where(step => step.Layer == layer && step.Track == Track.PaidOut).Sum(step => step.Amount);
}

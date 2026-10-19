using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;

namespace Netto.Core.Simulation;

// One income year in the order the law computes it: the director pay lowers the company's taxable
// income, corporate tax takes its share of what is left, and the dividend is the profit after tax
// less the reserve the user keeps in the company, never below zero. In steady state the year pays
// out its own profit (decided 2026-10-19); earlier reserves are not modelled yet. No disallowed expenses, no losses carried
// forward, no advance-payment surcharges, and INASTI's company contribution sits in the running costs.
public static class YearSimulation
{
    public const string IncomeYearNotSupported = GrossToNet.IncomeYearNotSupported;
    public const string RevenueNegative = "revenue_negative";
    public const string RunningCostsNegative = "running_costs_negative";
    public const string ReserveNegative = "reserve_negative";

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
        if (scenario.Reserve < 0m)
            return (null, ReserveNegative);

        var (pay, payError) = GrossToNet.Compute(scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate);
        if (pay is null)
            return (null, payError);

        var company = Company(scenario, corporateRules, dividendRules, pay.NetInPocket);
        var dividend = company.Dividend;
        var rate = dividendRules.Rate(scenario.DividendCategory);

        IReadOnlyList<Step> steps =
        [
            new Step("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, company.Tax.CorporateTax,
                CorporateTaxRules.Source, corporateRules.Amounts),
            .. pay.Steps,
            new Step("dividend_withholding", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, dividend.Withholding,
                rate.Source, [rate]),
            new Step("dividend_exemption_refund", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, -dividend.ExemptionRefund,
                DividendRules.ExemptionRefund, [dividendRules.ExemptAmount, rate]),
        ];

        return (new YearResult(
            scenario,
            scenario.ProfitBeforePay,
            company.Tax,
            pay,
            dividend,
            PaidOut(steps, Layer.Corporate),
            PaidOut(steps, Layer.Social),
            PaidOut(steps, Layer.Personal),
            company.NetInPocket,
            company.KeptInCompany,
            company.ProfitAfterTax,
            company.MaximumDividend,
            steps), null);
    }

    // The year at the scenario's pay as the split's search reads it: Run's figures for the pocket, the
    // dividend and the company, without its steps or the pay's next 1,000, since the search runs the
    // year at a hundred pays and more. The scenario's other inputs have passed Run already; null for a
    // year without rules.
    internal static PayLevel? Level(Scenario scenario)
    {
        if (CorporateTaxRules.For(scenario.IncomeYear) is not { } corporateRules || DividendRules.For(scenario.IncomeYear) is not { } dividendRules)
            return null;
        if (GrossToNet.NetInPocket(scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate.Value) is not { } netPay)
            return null;

        var company = Company(scenario, corporateRules, dividendRules, netPay);
        return new PayLevel(scenario.DirectorPay, company.NetInPocket, company.Dividend.Dividend, company.KeptInCompany, company.Tax.Condition,
            company.MaximumDividend >= scenario.Reserve);
    }

    // What follows the director pay: corporate tax on the profit it leaves, then the dividend out of the
    // profit after tax once the reserve is kept. What is not kept is paid out: a reserve above the
    // profit after tax leaves no dividend.
    private static CompanyFigures Company(Scenario scenario, CorporateTaxRules corporateRules, DividendRules dividendRules, decimal netPay)
    {
        var tax = CorporateTaxCalculator.Compute(corporateRules, scenario.ProfitBeforePay - scenario.DirectorPay, scenario.DirectorPay, scenario.Company);
        var profitAfterTax = tax.TaxableIncome - tax.CorporateTax;
        var maximumDividend = Math.Max(0m, profitAfterTax);
        var dividend = DividendCalculator.Compute(dividendRules, Math.Max(0m, maximumDividend - scenario.Reserve), scenario.DividendCategory);

        return new CompanyFigures(tax, profitAfterTax, maximumDividend, dividend, netPay + dividend.NetDividend, profitAfterTax - dividend.Dividend);
    }

    private static decimal PaidOut(IReadOnlyList<Step> steps, Layer layer) =>
        steps.Where(step => step.Layer == layer && step.Track == Track.PaidOut).Sum(step => step.Amount);

    private sealed record CompanyFigures(
        CorporateTaxResult Tax,
        decimal ProfitAfterTax,
        decimal MaximumDividend,
        DividendResult Dividend,
        decimal NetInPocket,
        decimal KeptInCompany);
}

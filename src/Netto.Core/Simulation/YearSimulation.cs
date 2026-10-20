using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;

namespace Netto.Core.Simulation;

// One income year in the order the law computes it: what the company pays the director besides pay,
// the vouchers, comes off what it earns and reaches the pocket untaxed, which leaves the profit before
// pay the split shares; the company's cost of the director pay, the gross and the social contributions
// it pays on the director's behalf, lowers the company's taxable income, what it cannot deduct of the
// vouchers is added back to it, corporate tax takes its share, with the surcharge the advance payments
// did not cover, and the dividend is the profit after tax and the profits kept from earlier years,
// less the reserve the user keeps in the company, never below zero. In steady state the year pays out
// its own profit (decided 2026-10-19). No other disallowed expenses, no losses carried forward, and
// INASTI's company contribution sits in the running costs.
public static class YearSimulation
{
    public const string IncomeYearNotSupported = GrossToNet.IncomeYearNotSupported;
    public const string RevenueNegative = "revenue_negative";
    public const string RunningCostsNegative = "running_costs_negative";
    public const string ReserveNegative = "reserve_negative";
    public const string EarlierProfitsNegative = "earlier_profits_negative";
    public const string AdvancePaymentNegative = "advance_payment_negative";

    public static (YearResult? Value, string? Error) Run(Scenario scenario)
    {
        var corporateRules = CorporateTaxRules.For(scenario.IncomeYear);
        var dividendRules = DividendRules.For(scenario.IncomeYear);
        var voucherRules = VoucherRules.For(scenario.IncomeYear);
        if (corporateRules is null || dividendRules is null || voucherRules is null)
            return (null, IncomeYearNotSupported);
        if (scenario.Revenue < 0m)
            return (null, RevenueNegative);
        if (scenario.RunningCosts < 0m)
            return (null, RunningCostsNegative);
        if (scenario.Reserve < 0m)
            return (null, ReserveNegative);
        if (scenario.EarlierProfits < 0m)
            return (null, EarlierProfitsNegative);
        if (scenario.AdvancePayments.ByDeadline.Any(amount => amount < 0m))
            return (null, AdvancePaymentNegative);

        var (vouchers, voucherError) = VoucherCalculator.Compute(voucherRules, scenario.Vouchers);
        if (vouchers is null)
            return (null, voucherError);

        var (pay, payError) = GrossToNet.Compute(scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate, scenario.TaxPrepayments, scenario.ContributionsPaidBy);
        if (pay is null)
            return (null, payError);

        var company = Company(scenario, corporateRules, dividendRules, vouchers, pay.NetInPocket, pay.TaxablePay, pay.CompanyCost);
        var dividend = company.Dividend;
        var rate = dividendRules.Rate(scenario.DividendCategory);

        IReadOnlyList<Step> steps =
        [
            .. scenario.Vouchers.Any ? VoucherSteps(voucherRules, vouchers) : [],
            new Step("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, company.Tax.CorporateTax,
                CorporateTaxRules.Source, corporateRules.Amounts),
            new Step("advance_payment_surcharge", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, company.AdvancePayments.Surcharge,
                corporateRules.AdvancePayments.Source, corporateRules.AdvancePayments.Amounts),
            .. pay.Steps,
            new Step("dividend_withholding", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, dividend.Withholding,
                rate.Source, [rate]),
            new Step("dividend_exemption_refund", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, -dividend.ExemptionRefund,
                DividendRules.ExemptionRefund, [dividendRules.ExemptAmount, rate]),
        ];

        return (new YearResult(
            scenario,
            company.ProfitBeforePay,
            vouchers,
            company.Tax,
            company.AdvancePayments,
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

    // What the director pay and the company's profit share between them: what the company earns less
    // what it pays the director in vouchers, taken as given even while the rules refuse them, so the
    // split keeps its place. Null for a year without voucher rules.
    public static decimal? ProfitBeforePay(Scenario scenario) =>
        VoucherRules.For(scenario.IncomeYear) is { } rules ? scenario.ProfitBeforeDirector - VoucherCalculator.Cost(rules, scenario.Vouchers) : null;

    // The year at the scenario's pay as the split's search reads it: Run's figures for the pocket, the
    // dividend and the company, without its steps or the pay's next 1,000, since the search runs the
    // year at a hundred pays and more. The scenario's other inputs have passed Run already; null for a
    // year without rules.
    internal static PayLevel? Level(Scenario scenario)
    {
        if (CorporateTaxRules.For(scenario.IncomeYear) is not { } corporateRules || DividendRules.For(scenario.IncomeYear) is not { } dividendRules
            || VoucherRules.For(scenario.IncomeYear) is not { } voucherRules)
            return null;
        if (VoucherCalculator.Compute(voucherRules, scenario.Vouchers).Value is not { } vouchers)
            return null;
        if (GrossToNet.Level(scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate.Value, scenario.TaxPrepayments, scenario.ContributionsPaidBy)
            is not { } pay)
            return null;

        var company = Company(scenario, corporateRules, dividendRules, vouchers, pay.NetInPocket, pay.TaxablePay, pay.CompanyCost);
        return new PayLevel(scenario.DirectorPay, company.NetInPocket, company.Dividend.Dividend, company.KeptInCompany, company.Tax.Condition,
            company.MaximumDividend >= scenario.Reserve);
    }

    // The smallest director pay, as the split moves it, that keeps the reduced corporate rate out of the
    // profit before pay: the smallest pay art. 215 accepts out of the taxable income the company would
    // have at no pay, which adds back what it cannot deduct of the vouchers, and, when the company pays
    // the contributions, the smallest payslip gross whose taxable pay reaches it. Null when the rate does
    // not depend on the pay, when no pay up to the profit before pay keeps it (more of the vouchers the
    // company cannot deduct than that profit), or for a year without rules. When the company pays the
    // contributions some gross always keeps it: their minimum alone is more than half of what it can
    // add back.
    internal static decimal? SmallestPayKeepingReducedRate(Scenario scenario)
    {
        if (CorporateTaxRules.For(scenario.IncomeYear) is not { } corporateRules || VoucherRules.For(scenario.IncomeYear) is not { } voucherRules
            || VoucherCalculator.Compute(voucherRules, scenario.Vouchers).Value is not { } vouchers)
            return null;

        var profitBeforePay = scenario.ProfitBeforeDirector - vouchers.Total;
        var ceiling = Math.Max(0m, profitBeforePay);
        if (CorporateTaxCalculator.SmallestPayKeepingReducedRate(corporateRules, profitBeforePay + vouchers.Disallowed, scenario.Company) is not { } taxablePay)
            return null;

        return scenario.ContributionsPaidBy == ContributionsPaidBy.Company
            ? GrossToNet.SmallestGrossPayWithTaxablePay(scenario.IncomeYear, taxablePay, ceiling)
            : taxablePay <= ceiling ? taxablePay : null;
    }

    // What follows the director pay: its cost to the company comes off the profit before pay, and art. 215
    // tests its minimum pay on the taxable pay, which counts what the company bore for the director too
    // (SPF return explanations, part 2, cadre XVI, 1); both are the gross plus the contributions the
    // company pays on the director's behalf, or the gross alone. The vouchers are never pay: what the
    // company cannot deduct of them is added back to its taxable income, which art. 215's condition
    // reads, and, spent, comes out of the profit after tax. Corporate tax on that taxable income and the
    // surcharge the advance payments leave on it, which is not deductible and so moves neither the
    // taxable income nor the rate, then the dividend out of the profit after tax and the profits kept
    // from earlier years, once the reserve is kept. The reserve is what stays in the whole company at
    // year end (decided 2026-10-19): what is not kept is paid out, and a reserve above what the company
    // holds leaves no dividend. A loss eats into the earlier profits first. The law withholds on the
    // distribution, whatever year its profits come from (CIR 92 art. 269 §2), so the whole dividend
    // takes the declared rate and the exempt 833 once.
    private static CompanyFigures Company(
        Scenario scenario, CorporateTaxRules corporateRules, DividendRules dividendRules, VoucherResult vouchers, decimal netPay, decimal taxablePay,
        decimal companyCost)
    {
        var profitBeforePay = scenario.ProfitBeforeDirector - vouchers.Total;
        var tax = CorporateTaxCalculator.Compute(corporateRules, profitBeforePay - companyCost + vouchers.Disallowed, taxablePay, scenario.Company);
        var advancePayments = AdvancePaymentCalculator.Compute(corporateRules.AdvancePayments, tax.CorporateTax, scenario.AdvancePayments, scenario.Company);
        var profitAfterTax = tax.TaxableIncome - tax.CorporateTax - advancePayments.Surcharge - vouchers.Disallowed;
        var held = profitAfterTax + scenario.EarlierProfits;
        var maximumDividend = Math.Max(0m, held);
        var dividend = DividendCalculator.Compute(dividendRules, Math.Max(0m, maximumDividend - scenario.Reserve), scenario.DividendCategory);

        return new CompanyFigures(profitBeforePay, tax, advancePayments, profitAfterTax, maximumDividend, dividend,
            netPay + dividend.NetDividend + vouchers.Total, held - dividend.Dividend);
    }

    // Each kind of voucher reaches the pocket exempt, at what the company pays into it, and what the
    // company cannot deduct of them joins its profit before tax. Only once it grants any, so a year
    // without them keeps its steps.
    private static IEnumerable<Step> VoucherSteps(VoucherRules rules, VoucherResult vouchers) =>
    [
        new Step("meal_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, vouchers.MealVouchers,
            rules.Exemption, [rules.MealVoucherCompanyShare, rules.MealVoucherHolderShare]),
        new Step("eco_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, vouchers.EcoVouchers,
            rules.Exemption, [rules.EcoVouchersMaximum, rules.EcoVoucherFaceValueMaximum]),
        new Step("sport_culture_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, vouchers.SportCultureVouchers,
            rules.Exemption, [rules.SportCultureVouchersMaximum]),
        new Step("disallowed_vouchers", Layer.Corporate, TaxableEvent.CompanyProfit, Track.TaxBaseOnly, vouchers.Disallowed,
            rules.NonDeductible, [rules.MealVoucherCompanyShare, rules.MealVoucherDeduction, rules.MealVoucherDeductionAtMaximum]),
    ];

    private static decimal PaidOut(IReadOnlyList<Step> steps, Layer layer) =>
        steps.Where(step => step.Layer == layer && step.Track == Track.PaidOut).Sum(step => step.Amount);

    private sealed record CompanyFigures(
        decimal ProfitBeforePay,
        CorporateTaxResult Tax,
        AdvancePaymentResult AdvancePayments,
        decimal ProfitAfterTax,
        decimal MaximumDividend,
        DividendResult Dividend,
        decimal NetInPocket,
        decimal KeptInCompany);
}

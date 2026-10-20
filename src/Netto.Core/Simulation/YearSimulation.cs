using Netto.Core.Allowances;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Rules;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;

namespace Netto.Core.Simulation;

// One income year in the order the law computes it: what the company pays the director besides pay,
// the vouchers, the expense allowances and the rent of a home office, comes off what it earns and
// reaches the pocket, which leaves the profit before pay the split shares; the vouchers and the
// allowances untaxed, the rent taxed with the pay, its tax paid out of the pocket (GrossToNet's
// OtherIncomeTax); the company's cost of the director pay, the
// gross and the social contributions it pays on the director's behalf, lowers the company's taxable
// income, what it cannot deduct of the vouchers is added back to it, corporate tax takes its share,
// with the surcharge the advance payments did not cover, and the dividend is the profit after tax and
// the profits kept from earlier years, less the reserve the user keeps in the company, never below
// zero. In steady state the year pays out its own profit (decided 2026-10-19). No other disallowed
// expenses, no losses carried forward, and INASTI's company contribution sits in the running costs.
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
        var levers = LeverRules.For(scenario.IncomeYear);
        if (corporateRules is null || dividendRules is null || levers is null)
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

        var (besidesPay, besidesPayError) = BesidesPayOf(scenario, levers);
        if (besidesPay is null)
            return (null, besidesPayError);

        var (pay, payError) = GrossToNet.Compute(
            scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate, scenario.TaxPrepayments, scenario.ContributionsPaidBy, besidesPay.Rent.NetRent);
        if (pay is null)
            return (null, payError);

        var company = Company(scenario, corporateRules, dividendRules, besidesPay, pay.NetInPocket - pay.OtherIncomeTax.Total, pay.TaxablePay, pay.CompanyCost);
        var dividend = company.Dividend;
        var rate = dividendRules.Rate(scenario.DividendCategory);

        IReadOnlyList<Step> steps =
        [
            .. scenario.Vouchers.Any ? VoucherSteps(levers.Vouchers, besidesPay.Vouchers) : [],
            .. scenario.Allowances.Any ? AllowanceSteps(levers.Allowances, besidesPay.Allowances) : [],
            .. scenario.Rent.Any ? RentSteps(levers.Rent, besidesPay.Rent) : [],
            new Step("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, company.Tax.CorporateTax,
                CorporateTaxRules.Source, corporateRules.Amounts),
            new Step("advance_payment_surcharge", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, company.AdvancePayments.Surcharge,
                corporateRules.AdvancePayments.Source, corporateRules.AdvancePayments.Amounts),
            .. pay.Steps,
            .. scenario.Rent.Any ? RentTaxSteps(PersonalTaxRules.For(scenario.IncomeYear)!, pay.OtherIncomeTax, scenario.MunicipalRate) : [],
            new Step("dividend_withholding", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, dividend.Withholding,
                rate.Source, [rate]),
            new Step("dividend_exemption_refund", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, -dividend.ExemptionRefund,
                DividendRules.ExemptionRefund, [dividendRules.ExemptAmount, rate]),
        ];

        return (new YearResult(
            scenario,
            company.ProfitBeforePay,
            besidesPay.Vouchers,
            besidesPay.Rent,
            besidesPay.Disallowed,
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
    // what it pays the director in vouchers, expense allowances and rent, taken as given even while the
    // rules refuse them, so the split keeps its place. Null for a year without voucher rules.
    public static decimal? ProfitBeforePay(Scenario scenario) =>
        VoucherRules.For(scenario.IncomeYear) is { } rules
            ? scenario.ProfitBeforeDirector - VoucherCalculator.Cost(rules, scenario.Vouchers) - scenario.Allowances.Total - scenario.Rent.Rent
            : null;

    // The year at the scenario's pay as the split's search reads it: Run's figures for the pocket, the
    // dividend and the company, without its steps or the pay's next 1,000, since the search runs the
    // year at a hundred pays and more. The scenario's other inputs have passed Run already; null for a
    // year without rules.
    internal static PayLevel? Level(Scenario scenario)
    {
        if (CorporateTaxRules.For(scenario.IncomeYear) is not { } corporateRules || DividendRules.For(scenario.IncomeYear) is not { } dividendRules
            || LeverRules.For(scenario.IncomeYear) is not { } levers)
            return null;
        if (BesidesPayOf(scenario, levers).Value is not { } besidesPay)
            return null;
        if (GrossToNet.Level(
                scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate.Value, scenario.TaxPrepayments, scenario.ContributionsPaidBy, besidesPay.Rent.NetRent)
            is not { } pay)
            return null;

        var company = Company(scenario, corporateRules, dividendRules, besidesPay, pay.YearNetInPocket, pay.TaxablePay, pay.CompanyCost);
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
        if (CorporateTaxRules.For(scenario.IncomeYear) is not { } corporateRules || LeverRules.For(scenario.IncomeYear) is not { } levers
            || BesidesPayOf(scenario, levers).Value is not { } besidesPay)
            return null;

        var profitBeforePay = scenario.ProfitBeforeDirector - besidesPay.Total;
        var ceiling = Math.Max(0m, profitBeforePay);
        if (CorporateTaxCalculator.SmallestPayKeepingReducedRate(corporateRules, profitBeforePay + besidesPay.Disallowed, scenario.Company) is not { } taxablePay)
            return null;

        return scenario.ContributionsPaidBy == ContributionsPaidBy.Company
            ? GrossToNet.SmallestGrossPayWithTaxablePay(scenario.IncomeYear, taxablePay, ceiling)
            : taxablePay <= ceiling ? taxablePay : null;
    }

    // What the company pays the director besides pay as the rules accept it, the vouchers, the expense
    // allowances then the rent, which the office allowance excludes, or the key of the first the rules
    // refuse.
    private static (BesidesPay? Value, string? Error) BesidesPayOf(Scenario scenario, LeverRules levers)
    {
        var (vouchers, voucherError) = VoucherCalculator.Compute(levers.Vouchers, scenario.Vouchers);
        if (vouchers is null)
            return (null, voucherError);

        var (allowances, allowanceError) = AllowanceCalculator.Accept(levers.Allowances, scenario.Allowances);
        if (allowances is null)
            return (null, allowanceError);

        var (rent, rentError) = RentCalculator.Compute(levers.Rent, scenario.Rent, allowances.Office);
        if (rent is null)
            return (null, rentError);

        return (new BesidesPay(vouchers, allowances, rent), null);
    }

    // What follows the director pay: its cost to the company comes off the profit before pay, and art. 215
    // tests its minimum pay on the taxable pay, which counts what the company bore for the director too
    // (SPF return explanations, part 2, cadre XVI, 1); both are the gross plus the contributions the
    // company pays on the director's behalf, or the gross alone. What the company pays the director
    // besides pay is never pay: what it cannot deduct of it is added back to its taxable income, which
    // art. 215's condition reads, and, spent, comes out of the profit after tax. Corporate tax on that
    // taxable income and the surcharge the advance payments leave on it, which is not deductible and so
    // moves neither the taxable income nor the rate, then the dividend out of the profit after tax and
    // the profits kept from earlier years, once the reserve is kept. The reserve is what stays in the
    // whole company at year end (decided 2026-10-19): what is not kept is paid out, and a reserve above
    // what the company holds leaves no dividend. A loss eats into the earlier profits first. The law
    // withholds on the distribution, whatever year its profits come from (CIR 92 art. 269 §2), so the
    // whole dividend takes the declared rate and the exempt 833 once.
    private static CompanyFigures Company(
        Scenario scenario, CorporateTaxRules corporateRules, DividendRules dividendRules, BesidesPay besidesPay, decimal netPay, decimal taxablePay,
        decimal companyCost)
    {
        var profitBeforePay = scenario.ProfitBeforeDirector - besidesPay.Total;
        var tax = CorporateTaxCalculator.Compute(corporateRules, profitBeforePay - companyCost + besidesPay.Disallowed, taxablePay, scenario.Company);
        var advancePayments = AdvancePaymentCalculator.Compute(corporateRules.AdvancePayments, tax.CorporateTax, scenario.AdvancePayments, scenario.Company);
        var profitAfterTax = tax.TaxableIncome - tax.CorporateTax - advancePayments.Surcharge - besidesPay.Disallowed;
        var held = profitAfterTax + scenario.EarlierProfits;
        var maximumDividend = Math.Max(0m, held);
        var dividend = DividendCalculator.Compute(dividendRules, Math.Max(0m, maximumDividend - scenario.Reserve), scenario.DividendCategory);

        return new CompanyFigures(profitBeforePay, tax, advancePayments, profitAfterTax, maximumDividend, dividend,
            netPay + dividend.NetDividend + besidesPay.Total, held - dividend.Dividend);
    }

    // Each kind of voucher reaches the pocket exempt, at what the company pays into it, and what the
    // company cannot deduct of them, the vouchers as the corporate layer taxes them, joins its taxable
    // income. Only once it grants any, so a year without them keeps its steps.
    private static IEnumerable<Step> VoucherSteps(VoucherRules rules, VoucherResult vouchers) =>
    [
        new Step("meal_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, vouchers.MealVouchers,
            rules.Exemption, [rules.MealVoucherCompanyShare, rules.MealVoucherHolderShare]),
        new Step("eco_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, vouchers.EcoVouchers,
            rules.Exemption, [rules.EcoVouchersMaximum, rules.EcoVoucherFaceValueMaximum]),
        new Step("sport_culture_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, vouchers.SportCultureVouchers,
            rules.Exemption, [rules.SportCultureVouchersMaximum]),
        new Step("disallowed_vouchers", Layer.Corporate, TaxableEvent.Vouchers, Track.TaxBaseOnly, vouchers.Disallowed,
            rules.NonDeductible, [rules.MealVoucherCompanyShare, rules.MealVoucherDeduction, rules.MealVoucherDeductionAtMaximum]),
    ];

    // Each allowance reaches the pocket untaxed, at what the company pays: a reimbursement of its own
    // costs, not pay, within the maxima the administration accepts. The company deducts them in full, so
    // none of them joins its taxable income. Only once it pays any, so a year without them keeps its
    // steps.
    private static IEnumerable<Step> AllowanceSteps(AllowanceRules rules, ExpenseAllowances allowances) =>
    [
        new Step("office_allowance", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, allowances.Office,
            rules.Reimbursement, [.. rules.OfficeAllowanceMaxima.Select(maximum => maximum.Amount)]),
        new Step("internet_allowance", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, allowances.Internet,
            rules.Reimbursement, [rules.InternetAllowanceMonthlyMaximum]),
        new Step("computer_allowance", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, allowances.Computer,
            rules.Reimbursement, [rules.ComputerAllowanceMonthlyMaximum]),
        new Step("phone_costs", Layer.Personal, TaxableEvent.ExpenseAllowances, Track.InPocket, allowances.Phone,
            rules.Reimbursement, []),
    ];

    // The rent reaches the pocket at what the company pays, kept at or below the cap so that none of it
    // is pay; the director is taxed on it, never less than the floor, less its flat charges. The
    // company deducts it in full, so none of it joins its taxable income. Only once it pays one, so a
    // year without it keeps its steps.
    private static IEnumerable<Step> RentSteps(RentRules rules, RentResult rent) =>
    [
        new Step("rent", Layer.Personal, TaxableEvent.Rent, Track.InPocket, rent.Rent,
            rules.Cap, [rules.CapMultiplier, rules.RevaluationCoefficient]),
        new Step("rent_charges", Layer.Personal, TaxableEvent.Rent, Track.TaxBaseOnly, rent.Charges,
            rules.NetRent, [rules.ChargesRate, rules.ChargesLimitMultiplier, rules.RevaluationCoefficient]),
        new Step("rent_taxable_income", Layer.Personal, TaxableEvent.Rent, Track.TaxBaseOnly, rent.NetRent,
            rules.RentAsIncome, [rules.FloorUplift, rules.IndexationCoefficient]),
    ];

    // What the rent adds to the director's personal tax, taxed with the pay: the income tax and the
    // municipal surcharge on the total less the pay's own, the credit for low earned income it takes
    // away, and what that lost credit adds to the surcharge on the pay's tax, less the bonus. Each is
    // paid out of the pocket, the credit's as a positive amount since it is lost, not refunded.
    private static IEnumerable<Step> RentTaxSteps(PersonalTaxRules rules, OtherIncomeTax tax, RuleAmount municipalRate) =>
    [
        new Step("rent_income_tax", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, tax.IncomeTax,
            PersonalTaxRules.IncomeTaxScale, rules.IncomeTaxAmounts),
        new Step("rent_municipal_surcharge", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, tax.MunicipalSurcharge,
            PersonalTaxRules.MunicipalSurcharge, [municipalRate]),
        new Step("rent_low_earned_income_credit_lost", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, tax.LowEarnedIncomeCreditLost,
            rules.LowEarnedIncomeCredit.Source, rules.LowEarnedIncomeCredit.Amounts),
        new Step("rent_income_tax_advance_payments", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, tax.AdvancePayments,
            rules.AdvancePayments.Source, rules.AdvancePayments.SurchargeAmounts),
    ];

    private static decimal PaidOut(IReadOnlyList<Step> steps, Layer layer) =>
        steps.Where(step => step.Layer == layer && step.Track == Track.PaidOut).Sum(step => step.Amount);

    // What the company pays the director besides pay, lever by lever: what it costs the company and
    // brings the pocket before the personal tax on the rent, the same amount, and what the company
    // cannot deduct of it, the vouchers' part alone, since it deducts the expense allowances and the
    // rent in full.
    private sealed record BesidesPay(VoucherResult Vouchers, ExpenseAllowances Allowances, RentResult Rent)
    {
        public decimal Total => Vouchers.Total + Allowances.Total + Rent.Rent;

        public decimal Disallowed => Vouchers.Disallowed;
    }

    // The rules of the levers paid besides pay, for one income year, or null when one has none.
    private sealed record LeverRules(VoucherRules Vouchers, AllowanceRules Allowances, RentRules Rent)
    {
        public static LeverRules? For(int incomeYear) =>
            VoucherRules.For(incomeYear) is { } vouchers && AllowanceRules.For(incomeYear) is { } allowances && RentRules.For(incomeYear) is { } rent
                ? new LeverRules(vouchers, allowances, rent)
                : null;
    }

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

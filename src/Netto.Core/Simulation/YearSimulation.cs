using Netto.Core.Allowances;
using Netto.Core.BenefitsInKind;
using Netto.Core.CompanyCar;
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
// reaches the pocket, and the costs of a car it puts at the director's disposal come off it too, less
// what the director pays it for the car's private use, which leaves the profit before pay the split
// shares; the vouchers and the allowances untaxed, the rent taxed with the pay, its tax paid out of
// the pocket (GrossToNet's OtherIncomeTax), the car's benefit in kind taxed as pay but never paid;
// the company's cost of the director pay, the gross and the social contributions it pays on the
// director's behalf, lowers the company's taxable income, what it cannot deduct of the vouchers and
// what it adds back of the car's benefit join it, corporate tax takes its share, with the surcharge
// the advance payments did not cover, and the dividend is the profit after tax and the profits kept
// from earlier years, less the reserve the user keeps in the company, never below zero. The car's
// costs land in the muddy pocket. In steady state the year pays out its own profit (decided
// 2026-10-19). No other disallowed expenses, no losses carried forward, and INASTI's company
// contribution sits in the running costs.
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
            scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate, scenario.TaxPrepayments, scenario.ContributionsPaidBy, besidesPay.Rent.NetRent,
            besidesPay.Benefits.TaxedAsPay);
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
            .. scenario.Car.Any ? CarSteps(levers.Car, besidesPay.Car) : [],
            new Step("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, company.Tax.CorporateTax,
                CorporateTaxRules.Source, corporateRules.Amounts) { AlsoCites = [corporateRules.PayTested, corporateRules.ExcessBenefitsMemorandum] },
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
            besidesPay.Car,
            besidesPay.Disallowed,
            company.Tax,
            company.AdvancePayments,
            pay,
            dividend,
            PaidOut(steps, Layer.Corporate),
            PaidOut(steps, Layer.Social),
            PaidOut(steps, Layer.Personal),
            besidesPay.Benefits.MuddyPocket,
            company.NetInPocket,
            company.KeptInCompany,
            company.ProfitAfterTax,
            company.MaximumDividend,
            steps), null);
    }

    // What the director pay and the company's profit share between them: what the company earns less
    // what it pays the director in vouchers, expense allowances and rent and what its car costs it, plus
    // what the director pays it for the car, taken as given even while the rules refuse them, so the
    // split keeps its place. A car counts once its catalogue value is given. Null for a year without
    // voucher rules.
    public static decimal? ProfitBeforePay(Scenario scenario) =>
        VoucherRules.For(scenario.IncomeYear) is { } rules
            ? scenario.ProfitBeforeDirector - VoucherCalculator.Cost(rules, scenario.Vouchers) - scenario.Allowances.Total - scenario.Rent.Rent
              - (scenario.Car.Any ? scenario.Car.YearlyCosts - scenario.Car.PersonalContribution : 0m)
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
                scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate.Value, scenario.TaxPrepayments, scenario.ContributionsPaidBy, besidesPay.Rent.NetRent,
                besidesPay.Benefits.TaxedAsPay)
            is not { } pay)
            return null;

        var company = Company(scenario, corporateRules, dividendRules, besidesPay, pay.YearNetInPocket, pay.TaxablePay, pay.CompanyCost);
        return new PayLevel(scenario.DirectorPay, company.NetInPocket, company.Dividend.Dividend, company.KeptInCompany, company.Tax.Condition,
            company.MaximumDividend >= scenario.Reserve);
    }

    // The smallest director pay, as the split moves it, that keeps the reduced corporate rate out of the
    // profit before pay: the smallest pay art. 215 accepts out of the taxable income the company would
    // have at no pay, which adds back what it cannot deduct of the vouchers and the car's benefit, with
    // the car's benefit counted in the pay and held within its share of it (CorporateTaxCalculator),
    // and, when the company pays the contributions, the smallest payslip gross whose taxable pay, the
    // benefit aside, reaches it. Null when the rate does not depend on the pay, when no pay up to the
    // profit before pay keeps it (more of the vouchers the company cannot deduct than that profit, or a
    // benefit beyond its share of any pay), or for a year without rules. When the company pays the
    // contributions and no benefit is capped, some gross always keeps it: their minimum alone is more
    // than half of what it can add back.
    internal static decimal? SmallestPayKeepingReducedRate(Scenario scenario)
    {
        if (CorporateTaxRules.For(scenario.IncomeYear) is not { } corporateRules || LeverRules.For(scenario.IncomeYear) is not { } levers
            || BesidesPayOf(scenario, levers).Value is not { } besidesPay)
            return null;

        var profitBeforePay = scenario.ProfitBeforeDirector - besidesPay.FromProfit;
        var ceiling = Math.Max(0m, profitBeforePay);
        var benefit = besidesPay.Benefits.TaxedAsPay;
        if (CorporateTaxCalculator.SmallestPayKeepingReducedRate(corporateRules, profitBeforePay + besidesPay.Disallowed, scenario.Company, benefit) is not { } taxablePay)
            return null;

        return scenario.ContributionsPaidBy == ContributionsPaidBy.Company
            ? GrossToNet.SmallestGrossPayWithTaxablePay(scenario.IncomeYear, taxablePay, ceiling, benefit)
            : taxablePay <= ceiling ? taxablePay : null;
    }

    // What the company pays the director besides pay as the rules accept it, the vouchers, the expense
    // allowances then the rent, which the office allowance excludes, and the car, or the key of the
    // first the rules refuse.
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

        var (car, carError) = CarCalculator.Compute(levers.Car, scenario.Car);
        if (car is null)
            return (null, carError);

        return (new BesidesPay(vouchers, allowances, rent, car), null);
    }

    // What follows the director pay: its cost to the company comes off the profit before pay, and art. 215
    // tests its minimum pay on the taxable pay, which counts what the company bore for the director and
    // the car's benefit too (SPF return explanations, part 2, cadre XVI, 1; the commentary on art. 215,
    // n° 215/44), and holds that benefit within its share of it (al. 3, 5°); the cost is the gross plus
    // the contributions the company pays on the director's behalf, or the gross alone. What the company
    // pays the director besides pay is never pay: what it cannot deduct of it, and what it adds back of
    // the car's benefit, join its taxable income, which art. 215's condition reads; the vouchers', spent,
    // and the add-back, never spent, come out of the profit after tax again. Corporate tax on that
    // taxable income and the surcharge the advance payments leave on it, which is not deductible and so
    // moves neither the taxable income nor the rate, then the dividend out of the profit after tax and
    // the profits kept from earlier years, once the reserve is kept. The reserve is what stays in the
    // whole company at year end (decided 2026-10-19): what is not kept is paid out, and a reserve above
    // what the company holds leaves no dividend. A loss eats into the earlier profits first. The law
    // withholds on the distribution, whatever year its profits come from (CIR 92 art. 269 §2), so the
    // whole dividend takes the declared rate and the exempt 833 once. The director's contribution for the
    // car leaves the pocket for the company.
    private static CompanyFigures Company(
        Scenario scenario, CorporateTaxRules corporateRules, DividendRules dividendRules, BesidesPay besidesPay, decimal netPay, decimal taxablePay,
        decimal companyCost)
    {
        var profitBeforePay = scenario.ProfitBeforeDirector - besidesPay.FromProfit;
        var tax = CorporateTaxCalculator.Compute(
            corporateRules, profitBeforePay - companyCost + besidesPay.Disallowed, taxablePay, scenario.Company, besidesPay.Benefits.TaxedAsPay);
        var advancePayments = AdvancePaymentCalculator.Compute(corporateRules.AdvancePayments, tax.CorporateTax, scenario.AdvancePayments, scenario.Company);
        var profitAfterTax = tax.TaxableIncome - tax.CorporateTax - advancePayments.Surcharge - besidesPay.Disallowed;
        var held = profitAfterTax + scenario.EarlierProfits;
        var maximumDividend = Math.Max(0m, held);
        var dividend = DividendCalculator.Compute(dividendRules, Math.Max(0m, maximumDividend - scenario.Reserve), scenario.DividendCategory);

        return new CompanyFigures(profitBeforePay, tax, advancePayments, profitAfterTax, maximumDividend, dividend,
            netPay + dividend.NetDividend + besidesPay.Total - besidesPay.Benefits.Contribution, held - dividend.Dividend);
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

    // The car's costs land in the muddy pocket, at what the company spends on them, deducted in full;
    // its benefit on the scale, at least the minimum, with each age coefficient it used; the director's
    // contribution leaving the pocket, a plain zero without one; the benefit it leaves taxed as pay, in
    // Director pay's figures; and the share of the benefit before the contribution the company adds back
    // to its taxable income. Only once there is a car, so a year without one keeps its steps.
    private static IEnumerable<Step> CarSteps(CarRules rules, CarResult car) =>
    [
        new Step("car_costs", Layer.Corporate, TaxableEvent.CompanyCar, Track.Muddy, car.Costs,
            rules.Deduction, [rules.ZeroEmissionDeduction]) { AlsoCites = [rules.Acquisition] },
        new Step("car_benefit", Layer.Personal, TaxableEvent.CompanyCar, Track.TaxBaseOnly, car.BenefitBeforeContribution,
            rules.Benefit, [rules.CatalogueShare, rules.MinimumCo2Percentage, .. car.Periods.Select(period => period.Period.AgeCoefficient).Distinct(), rules.MinimumBenefit])
        {
            AlsoCites = [rules.BenefitMethod],
        },
        new Step("car_contribution", Layer.Personal, TaxableEvent.CompanyCar, Track.InPocket, car.Contribution == 0m ? 0m : -car.Contribution,
            rules.Benefit, []),
        new Step("car_benefit_taxed", Layer.Personal, TaxableEvent.CompanyCar, Track.TaxBaseOnly, car.Benefit,
            rules.BenefitAsPay, []) { AlsoCites = [rules.BenefitInContributions] },
        new Step("car_add_back", Layer.Corporate, TaxableEvent.CompanyCar, Track.TaxBaseOnly, car.AddBack,
            rules.AddBack, [car.AddBackRate ?? rules.AddBackRate]),
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
    // brings the pocket before the personal tax on the rent, the same amount (Total), what comes off the
    // profit before pay, that and the costs of what it puts at the director's disposal less what the
    // director pays it for that use (FromProfit), and what the company cannot deduct of it or adds back:
    // the vouchers' part and the benefits' (Disallowed), since it deducts the expense allowances and the
    // rent in full. The car is read for its steps and the card; its money moves only as the plain figures
    // of its benefit in kind (Benefits), which a laptop or a phone will give the same way.
    private sealed record BesidesPay(VoucherResult Vouchers, ExpenseAllowances Allowances, RentResult Rent, CarResult Car)
    {
        public BenefitInKind Benefits => Car.BenefitInKind;

        public decimal Total => Vouchers.Total + Allowances.Total + Rent.Rent;

        public decimal FromProfit => Total + Benefits.Costs - Benefits.Contribution;

        public decimal Disallowed => Vouchers.Disallowed + Benefits.NotDeductible;
    }

    // The rules of the levers paid besides pay, for one income year, or null when one has none.
    private sealed record LeverRules(VoucherRules Vouchers, AllowanceRules Allowances, RentRules Rent, CarRules Car)
    {
        public static LeverRules? For(int incomeYear) =>
            VoucherRules.For(incomeYear) is { } vouchers && AllowanceRules.For(incomeYear) is { } allowances && RentRules.For(incomeYear) is { } rent
            && CarRules.For(incomeYear) is { } car
                ? new LeverRules(vouchers, allowances, rent, car)
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

using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation.Levers;
using Netto.Core.SocialContributions;

namespace Netto.Core.Simulation;

// One income year in the order the law computes it, folded over the levers in their order (LeverOrder):
// what the company earns, once it has paid INASTI the yearly contribution every company owes whatever it
// pays its director (CompanyContributionRules, a cost it deducts like its running costs), is what the
// levers start from; what the company pays the director besides pay, or puts at their disposal, comes
// off it, each lever taking what the scenario gives it, which leaves the profit before pay the split
// shares; each lever brings the pocket what its rules say, the company cannot deduct some of it, and it
// hands the pay what is taxed with it, a rent's net, a car's and the IT and phone's benefits in kind, a royalty's movable
// income, whose tax the year counts on the whole income (GrossToNet). The company's cost of the director
// pay, the gross and the social contributions it pays on the director's behalf, lowers the company's
// taxable income, what it cannot deduct of the levers joins it, corporate tax takes its share, with the
// surcharge the advance payments did not cover, and the dividend is the profit after tax and the profits
// kept from earlier years, less the reserve the user keeps in the company, never below zero. In steady
// state the year pays out its own profit (decided 2026-10-19). The only disallowed expenses are what the
// levers leave undeductible (Lever.Disallowed), the share of the car's benefit in kind the company may not
// deduct among them, added back to the taxable income; no losses carried forward. Each lever says what it does through one contract (Lever, LeverYear), and the
// year reads a lever's result by its type only where YearResult or PayLevel hands a card a typed result:
// the vouchers', the rent's, the IT and phone's, the car's and the royalty's.
public static class YearSimulation
{
    public static readonly Refusal IncomeYearNotSupported = GrossToNet.IncomeYearNotSupported;
    public static readonly Refusal RevenueNegative = new("revenue_negative", RefusedInput.Revenue);
    public static readonly Refusal RunningCostsNegative = new("running_costs_negative", RefusedInput.RunningCosts);
    public static readonly Refusal ReserveNegative = new("reserve_negative", RefusedInput.Reserve);
    public static readonly Refusal EarlierProfitsNegative = new("earlier_profits_negative", RefusedInput.EarlierProfits);
    public static readonly Refusal AdvancePaymentNegative = new("advance_payment_negative", RefusedInput.CompanyAdvancePayments);

    private const decimal Cent = 0.01m;

    public static (YearResult? Value, Refusal? Error) Run(Scenario scenario) =>
        LeversFor(scenario.IncomeYear) is { } levers ? Run(scenario, levers) : (null, IncomeYearNotSupported);

    // The year over the levers given, in their order: the year's own (LeverOrder), or a test's.
    internal static (YearResult? Value, Refusal? Error) Run(Scenario scenario, IReadOnlyList<Lever> levers)
    {
        var corporateRules = CorporateTaxRules.For(scenario.IncomeYear);
        var dividendRules = DividendRules.For(scenario.IncomeYear);
        var taxRules = PersonalTaxRules.For(scenario.IncomeYear, scenario.Region);
        var municipalRates = MunicipalRates.For(scenario.IncomeYear, scenario.Region);
        var contributionRules = CompanyContributionRules.For(scenario.IncomeYear);
        if (corporateRules is null || dividendRules is null || taxRules is null || municipalRates is null || contributionRules is null)
            return (null, IncomeYearNotSupported);

        scenario = AtPay(scenario, levers);
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
            scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate, scenario.TaxPrepayments, scenario.ContributionsPaidBy, besidesPay.TaxedWithPay,
            scenario.Region, scenario.FundFeeRate, scenario.Household);
        if (pay is null)
            return (null, payError);

        var company = Company(scenario, corporateRules, dividendRules, besidesPay, pay.YearNetInPocket, pay.TaxablePayWithoutRequalified, pay.CompanyCost);
        var dividend = company.Dividend;
        var rate = dividendRules.Rate(scenario.DividendCategory);
        var contribution = contributionRules.Contribution(scenario.CompanyContributionBand);

        IReadOnlyList<Step> steps =
        [
            new Step("company_contribution", Layer.Corporate, TaxableEvent.CompanyContribution, Track.TaxBaseOnly, contribution.Value,
                contributionRules.Source, contributionRules.Used(scenario.CompanyContributionBand)) { AlsoCites = [contributionRules.Law, contributionRules.Deduction] },
            .. besidesPay.Years.SelectMany(year => year.Steps()),
            new Step("corporate_tax", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, company.Tax.CorporateTax,
                corporateRules.Source, corporateRules.Amounts) { AlsoCites = [corporateRules.PayTested, corporateRules.ExcessBenefitsMemorandum] },
            new Step("advance_payment_surcharge", Layer.Corporate, TaxableEvent.CompanyProfit, Track.PaidOut, company.AdvancePayments.Surcharge,
                corporateRules.AdvancePayments.Source, corporateRules.AdvancePayments.Amounts),
            .. pay.Steps,
            .. besidesPay.Years.SelectMany(year => year.StepsAfterPay(pay, taxRules, municipalRates.Surcharge, scenario.MunicipalRate)),
            new Step("dividend_withholding", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, dividend.Withholding,
                rate.Source, [rate]),
            new Step("dividend_exemption_refund", Layer.Personal, TaxableEvent.Dividend, Track.PaidOut, -dividend.ExemptionRefund,
                dividendRules.ExemptionRefund, [dividendRules.ExemptAmount, rate]),
        ];

        return (new YearResult(
            scenario,
            contribution.Value,
            besidesPay.ProfitBeforePay,
            besidesPay.Of<VoucherYear>().Vouchers,
            besidesPay.Of<RentYear>().Rent,
            besidesPay.Of<EquipmentYear>().Set,
            besidesPay.Of<CarYear>().Car,
            besidesPay.Of<CopyrightYear>().Copyright,
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
    // INASTI's company contribution and what each lever takes off it, taken as given even while the
    // rules refuse it, so the split keeps its place: a car once its catalogue value is given, and a
    // royalty that follows the pay at what it comes to at the scenario's pay. The year runs with this
    // very figure. Null for a year without the levers' or the contribution's rules.
    public static decimal? ProfitBeforePay(Scenario scenario) =>
        LeversFor(scenario.IncomeYear) is { } levers ? ProfitBeforePay(scenario, levers) : null;

    internal static decimal ProfitBeforePay(Scenario scenario, IReadOnlyList<Lever> levers) => Left(scenario, levers)[^1];

    // The most the split gives as pay: the profit before pay, never below zero, while no lever's amount
    // follows the pay; beside one that does, a royalty that follows it, the largest pay the company pays
    // together with what those levers take beside it, out of what the levers that do not follow it
    // leave, to the cent. The pay and what follows it rise together, so that pay is found by halving
    // between no pay and all of it, which holds where the cap stops the royalty too. Taken as given even
    // while the rules refuse an amount, so the split keeps its place. Null for a year without rules.
    public static decimal? PayCeiling(Scenario scenario) =>
        LeversFor(scenario.IncomeYear) is { } levers ? PayCeiling(scenario, levers) : null;

    internal static decimal PayCeiling(Scenario scenario, IReadOnlyList<Lever> levers)
    {
        var following = levers.Where(lever => lever.FollowsPay(scenario)).ToList();
        var shared = Left(scenario, [.. levers.Where(lever => !lever.FollowsPay(scenario))])[^1];
        if (following.Count == 0)
            return Math.Max(0m, shared);
        if (shared <= 0m)
            return 0m;

        bool Fits(decimal pay)
        {
            var atPay = scenario with { DirectorPay = pay };
            return following.Aggregate(pay, (taken, lever) => taken + lever.FromProfit(atPay)) <= shared;
        }

        if (Fits(shared))
            return shared;

        var (low, high) = (0m, shared);
        while (high - low > Cent)
        {
            var middle = Money.DownToCent((low + high) / 2m);
            if (Fits(middle))
                low = middle;
            else
                high = middle;
        }

        return low;
    }

    // The year at the scenario's pay as the split's search reads it: Run's figures for the pocket, the
    // dividend and the company, without its steps or the pay's next 1,000, since the search runs the
    // year at a hundred pays and more. The scenario's other inputs have passed Run already; null for a
    // year without rules.
    internal static PayLevel? Level(Scenario scenario)
    {
        if (CorporateTaxRules.For(scenario.IncomeYear) is not { } corporateRules || DividendRules.For(scenario.IncomeYear) is not { } dividendRules
            || LeversFor(scenario.IncomeYear) is not { } levers)
            return null;

        scenario = AtPay(scenario, levers);
        if (BesidesPayOf(scenario, levers).Value is not { } besidesPay)
            return null;
        if (GrossToNet.Level(
                scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate.Value, scenario.TaxPrepayments, scenario.ContributionsPaidBy, besidesPay.TaxedWithPay,
                scenario.Region, scenario.FundFeeRate, scenario.Household)
            is not { } pay)
            return null;

        var company = Company(scenario, corporateRules, dividendRules, besidesPay, pay.YearNetInPocket, pay.TaxablePayWithoutRequalified, pay.CompanyCost);
        return new PayLevel(scenario.DirectorPay, company.NetInPocket, company.Dividend.Dividend, company.KeptInCompany, company.Tax.Condition,
            YearResult.ReserveKept(company.MaximumDividend, scenario.Reserve)) { Royalty = besidesPay.Of<CopyrightYear>().Copyright.Royalty };
    }

    // The smallest director pay, as the split moves it, that keeps the reduced corporate rate out of the
    // profit before pay: the smallest pay art. 215 accepts out of the taxable income the company would
    // have at no pay, which adds back what it cannot deduct of the vouchers and the car's benefit, with
    // the benefits in kind, the car's and the IT and phone's, counted in the pay and held within their share
    // of it (CorporateTaxCalculator),
    // and, when the company pays the contributions, the smallest payslip gross whose taxable pay, the
    // benefit aside, reaches it. Null when the rate does not depend on the pay, when no pay up to the
    // profit before pay keeps it (more of the vouchers the company cannot deduct than that profit, or a
    // benefit beyond its share of any pay), or for a year without rules. When the company pays the
    // contributions and no benefit is capped, some gross always keeps it: their minimum alone is more
    // than half of what it can add back. A royalty that follows the pay rises with it and lowers the
    // taxable income art. 215 compares the pay with, so the smallest pay is found by halving to the cent
    // between no pay and the pay ceiling: a pay keeps the rate when the smallest pay with the royalty
    // held at what it comes to there is at most that pay, which, once true, stays true at every higher
    // pay. Null when not even the pay ceiling keeps it.
    internal static decimal? SmallestPayKeepingReducedRate(Scenario scenario)
    {
        if (LeversFor(scenario.IncomeYear) is not { } levers)
            return null;
        if (!levers.Any(lever => lever.FollowsPay(scenario)))
            return SmallestPayWithLeversHeld(scenario, levers);

        var ceiling = PayCeiling(scenario, levers);
        bool Keeps(decimal pay) =>
            SmallestPayWithLeversHeld(AtPay(scenario with { DirectorPay = pay }, levers), levers) is { } smallest && smallest <= pay;

        if (!Keeps(ceiling))
            return null;
        if (Keeps(0m))
            return 0m;

        var (low, high) = (0m, ceiling);
        while (high - low > Cent)
        {
            var middle = Money.DownToCent((low + high) / 2m);
            if (Keeps(middle))
                high = middle;
            else
                low = middle;
        }

        return high;
    }

    // The smallest pay keeping the reduced rate with what each lever takes held as the scenario holds it.
    private static decimal? SmallestPayWithLeversHeld(Scenario scenario, IReadOnlyList<Lever> levers)
    {
        if (CorporateTaxRules.For(scenario.IncomeYear) is not { } corporateRules || BesidesPayOf(scenario, levers).Value is not { } besidesPay)
            return null;

        var profitBeforePay = besidesPay.ProfitBeforePay;
        var ceiling = Math.Max(0m, profitBeforePay);
        var benefit = besidesPay.TaxedWithPay.BenefitInKind;
        if (CorporateTaxCalculator.SmallestPayKeepingReducedRate(corporateRules, profitBeforePay + besidesPay.Disallowed, scenario.Company, benefit) is not { } taxablePay)
            return null;

        return scenario.ContributionsPaidBy == ContributionsPaidBy.Company
            ? GrossToNet.SmallestGrossPayWithTaxablePay(scenario.IncomeYear, taxablePay, ceiling, benefit, scenario.Region, scenario.FundFeeRate)
            : taxablePay <= ceiling ? taxablePay : null;
    }

    // The scenario with what follows the pay set at the scenario's own pay, lever by lever.
    private static Scenario AtPay(Scenario scenario, IReadOnlyList<Lever> levers) => levers.Aggregate(scenario, (atPay, lever) => lever.AtPay(atPay));

    // The year's levers in the law's order, or none for a year without their rules or without those of
    // INASTI's company contribution, which comes off what the company earns before them.
    private static IReadOnlyList<Lever>? LeversFor(int incomeYear) =>
        CompanyContributionRules.For(incomeYear) is null ? null : LeverOrder.For(incomeYear);

    // What the company owes INASTI for the year in the band the scenario declares, whatever it pays its
    // director. Every entry point answers a year without its rules before anything asks for it.
    private static RuleAmount ContributionOf(Scenario scenario) =>
        CompanyContributionRules.For(scenario.IncomeYear)?.Contribution(scenario.CompanyContributionBand)
        ?? throw new InvalidOperationException($"Income year {scenario.IncomeYear} has no company contribution rules: its entry point answers it first.");

    // What the company earns once it has paid INASTI its yearly contribution, less what each lever takes
    // off it as given, in their order: what is left before each lever, then, after the last, the profit
    // before pay. The one walk the year, the profit before pay and the pay ceiling subtract the
    // contribution and the levers by.
    private static decimal[] Left(Scenario scenario, IReadOnlyList<Lever> levers)
    {
        var left = new decimal[levers.Count + 1];
        left[0] = Money.Minus(scenario.ProfitBeforeDirector, ContributionOf(scenario).Value);
        for (var i = 0; i < levers.Count; i++)
            left[i + 1] = left[i] - levers[i].FromProfit(scenario);

        return left;
    }

    // What the company pays the director besides pay as the rules accept it, lever by lever in their
    // order, each beside what the levers before it leave of what the company earns, or the first
    // refusal of the rules; a lever after it is not asked.
    private static (BesidesPay? Value, Refusal? Error) BesidesPayOf(Scenario scenario, IReadOnlyList<Lever> levers)
    {
        var left = Left(scenario, levers);
        var years = new LeverYear[levers.Count];
        for (var i = 0; i < levers.Count; i++)
        {
            var (year, error) = levers[i].Compute(scenario, left[i]);
            if (year is null)
                return (null, error);

            years[i] = year;
        }

        return (new BesidesPay(years, left[^1]), null);
    }

    // What follows the director pay: its cost to the company comes off the profit before pay, and art. 215
    // tests its minimum pay on the taxable pay, which counts what the company bore for the director and
    // the benefits in kind too, the car's and the IT and phone's (SPF return explanations, part 2, cadre XVI, 1; the commentary on art. 215,
    // n° 215/44), but never a royalty's part taxed as pay (decided unattended: art. 215 asks for pay to
    // an art. 32 director, and leaving it out never overstates the gain), and holds that benefit within
    // its share of it (al. 3, 5°); the cost is the gross plus the contributions the company pays on the
    // director's behalf, or the gross alone. What the company pays the director besides pay is never
    // pay: what it cannot deduct of it, and what it adds back of the car's benefit, join its taxable
    // income, which art. 215's condition reads; the vouchers', spent, and the add-back, never spent,
    // come out of the profit after tax again. Corporate tax on that taxable income and the surcharge the
    // advance payments leave on it, which is not deductible and so moves neither the taxable income nor
    // the rate, then the dividend out of the profit after tax and the profits kept from earlier years,
    // once the reserve is kept. The reserve is what stays in the whole company at year end (decided
    // 2026-10-19): what is not kept is paid out, and a reserve above what the company holds leaves no
    // dividend. A loss eats into the earlier profits first. The law withholds on the distribution,
    // whatever year its profits come from (CIR 92 art. 269 §2), so the whole dividend takes the declared
    // rate and the exempt 833 once. The pocket takes what the levers bring it, the director's
    // contribution for the car leaving it for the company.
    private static CompanyFigures Company(
        Scenario scenario, CorporateTaxRules corporateRules, DividendRules dividendRules, BesidesPay besidesPay, decimal netPay, decimal taxablePay,
        decimal companyCost)
    {
        var tax = CorporateTaxCalculator.Compute(
            corporateRules, besidesPay.ProfitBeforePay - companyCost + besidesPay.Disallowed, taxablePay, scenario.Company, besidesPay.TaxedWithPay.BenefitInKind);
        var advancePayments = AdvancePaymentCalculator.Compute(corporateRules.AdvancePayments, tax.CorporateTax, scenario.AdvancePayments, scenario.Company);
        var profitAfterTax = tax.TaxableIncome - tax.CorporateTax - advancePayments.Surcharge - besidesPay.Disallowed;
        var held = profitAfterTax + scenario.EarlierProfits;
        var maximumDividend = Math.Max(0m, held);
        var dividend = DividendCalculator.Compute(dividendRules, Math.Max(0m, maximumDividend - scenario.Reserve), scenario.DividendCategory);

        return new CompanyFigures(tax, advancePayments, profitAfterTax, maximumDividend, dividend, netPay + dividend.NetDividend + besidesPay.ToPocket,
            held - dividend.Dividend);
    }

    private static decimal PaidOut(IReadOnlyList<Step> steps, Layer layer) =>
        steps.Where(step => step.Layer == layer && step.Track == Track.PaidOut).Sum(step => step.Amount);

    // What the company pays the director besides pay, lever by lever as the rules took it (Years), with
    // what the levers leave of what the company earns (ProfitBeforePay) and their sums: what reaches the
    // pocket besides the pay before the personal tax on it, less what the director pays the company for
    // it (ToPocket); what the company cannot deduct of it or adds back (Disallowed); and what they hand
    // the pay to be taxed with it (TaxedWithPay).
    private sealed record BesidesPay(IReadOnlyList<LeverYear> Years, decimal ProfitBeforePay)
    {
        public decimal ToPocket { get; } = Years.Sum(year => year.ToPocket);

        public decimal Disallowed { get; } = Years.Sum(year => year.Disallowed);

        public TaxedWithPay TaxedWithPay { get; } = Years.Aggregate(TaxedWithPay.None, (taxed, year) => taxed.Plus(year.TaxedWithPay));

        // The year of the lever of that kind, for the year's result and the split, which read its result.
        public TYear Of<TYear>()
            where TYear : LeverYear =>
            Years.OfType<TYear>().Single();
    }

    private sealed record CompanyFigures(
        CorporateTaxResult Tax,
        AdvancePaymentResult AdvancePayments,
        decimal ProfitAfterTax,
        decimal MaximumDividend,
        DividendResult Dividend,
        decimal NetInPocket,
        decimal KeptInCompany);
}

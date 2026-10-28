using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;

namespace Netto.Core.DirectorPay;

// A year's director pay, with the social contributions paid by the director personally or by the
// company on the director's behalf, in steady state: the contributions paid in the year equal the
// final contributions owed on its income. The income tax on it is surcharged where too little of it
// was withheld or paid in advance during the year, and the advance payments beyond what avoids that
// earn a bonus. The pay is paid in twelve equal months, each withheld on by the key formula with the
// rules in force when it is paid: the statement shows them, the year's withholding is the sum of its
// twelve months unless the user gave the amount, and the assessment is what the year's tax return
// leaves to pay or refunds.
//
// The social insurance fund charges a management fee on the contributions, at the rate the user gives
// (none unless given), which it bills with each quarter of them (AR n° 38, art. 20 §4). The director
// deducts it with the contributions as their own (CIR 92, art. 52, 7°, read with Com.IR 52/124), Netto's
// reading where no text names the fee as deductible: it lowers the income the contributions are owed on,
// and when the company pays the contributions on the director's behalf it pays the fee with them, pay
// too, then deducted.
//
// Another income can be taxed with the pay, a home office's net rent, globalised with it (Tax-Calc
// T13 and T14). The pay's own figures, its steps, its net and its statement, stay those of the pay
// taken alone, as art. 158 takes it for the surcharge; what the other income adds to the year's tax
// is its own record: the tax on the total less the pay's, the credit for low earned income it takes
// away by raising the total net income (art. 289ter), and the surcharge less the bonus that credit
// moves, the surcharge base subtracting only the credit granted. The year's figures, the advance
// payments with their suggested payment, the next 1,000 and the assessment, are on the whole income.
//
// A benefit in kind, a company car's, is pay never paid in cash (art. 31 al. 2, 2° and art. 32): it
// joins the taxable pay, so INASTI's base, the income tax and the key formula's withholding (annex
// III, n° 14.1) take it as they would the same amount paid in cash, while the pocket and the
// company's cost of the pay leave it out; the statement shows its twelfth beside the gross.
//
// Movable income the company pays besides the payslip, a copyright royalty, comes as plain figures.
// The part the law taxes as director pay (RequalifiedPay) is pay paid in cash: in INASTI's base, on
// the scale, in the surcharge's base and in the pocket, but not on the payslip, so the key formula
// does not withhold on it and the pay's cost to the company leaves it out, the company paying it as
// the income it is; what the company pays for the director's contributions on it is pay too. The
// part taxed apart is on the year's figures, the pay's own staying the pay's, and TaxedApartTax is
// what it adds on top of the pay and the rent. What the company withheld at source on all of it is
// credited on the assessment, never in the surcharge's base (Tax-Calc T16 to T18).
//
// A director taxed jointly with a spouse or legal cohabitant is taxed in the household's joint
// assessment (PersonalTaxCalculator). What the pay, the year's figures and the assessment's balance count
// is the river's share of the household's tax: the joint tax, less what the partner would owe filing
// alone on their own professional income (PersonalTaxCalculator.PartnerAlone), which is the partner's,
// as what was withheld on it is (decided by the operator, 2026-10-28; the baseline the partner filing
// alone rather than the household with the director earning nothing since 2026-10-29, which gave the
// partner a quotient towards the director the pay was then charged with). So the surcharge's base is the
// pay's share of the household's tax (art. 157 and 158, which take director pay apart), and the river
// never counts the partner's own tax. The household's two columns, the partner filing alone and what the
// marital quotient saves are beside them (Household).
public static class GrossToNet
{
    public static readonly Refusal GrossPayNegative = new("gross_pay_negative", RefusedInput.DirectorPay);
    public static readonly Refusal MunicipalRateOutOfRange = new("municipal_rate_out_of_range", RefusedInput.MunicipalRate);
    public static readonly Refusal IncomeYearNotSupported = new("income_year_not_supported", RefusedInput.IncomeYear);
    public static readonly Refusal TaxWithheldNegative = new("tax_withheld_negative", RefusedInput.TaxWithheld);
    public static readonly Refusal TaxAdvancePaymentNegative = new("tax_advance_payment_negative", RefusedInput.DirectorAdvancePayments);
    public static readonly Refusal FundFeeRateOutOfRange = new("fund_fee_rate_out_of_range", RefusedInput.FundFeeRate);
    public static readonly Refusal PartnerIncomeNegative = new("partner_income_negative", RefusedInput.PartnerIncome);
    public static readonly Refusal ChildrenNegative = new("children_negative", RefusedInput.Children);
    public static readonly Refusal ChildrenUnder3OutOfRange = new("children_under_3_out_of_range", RefusedInput.ChildrenUnder3);
    public static readonly Refusal DependantsInCareNegative = new("dependants_in_care_negative", RefusedInput.DependantsInCare);
    public static readonly Refusal OtherDependantsNegative = new("other_dependants_negative", RefusedInput.OtherDependants);

    private const decimal MaximumMunicipalRate = 0.10m;

    // No text caps a fund's fee, which the minister sets for each fund every year: a guard against a rate
    // typed wrong, as the municipal rate's, well above what the funds charge.
    private const decimal MaximumFundFeeRate = 0.10m;

    private const decimal NextSlice = 1_000m;
    private const decimal Convergence = 0.000001m;
    private const int MaximumPasses = 100;

    // The gross pay is the pay on the payslip, whoever pays the contributions; not saying who runs the
    // pay as the director's own. What is taxed with it is what the levers hand it besides the payslip,
    // nothing unless given; the engine's own rules give it, never below zero. The director lives in
    // Wallonia unless given another region, which has no rules yet and is refused as a year without
    // them is. The fund's fee is a fraction, 3.05% is 0.0305, none unless given. The director is taxed
    // alone unless given a household taxed jointly, whose partner's income is refused below zero.
    public static (GrossToNetResult? Value, Refusal? Error) Compute(
        int incomeYear, decimal grossPay, RuleAmount municipalRate, TaxPrepayments prepayments, ContributionsPaidBy paidBy = ContributionsPaidBy.Director,
        TaxedWithPay? withPay = null, Region region = Region.Wallonia, decimal? fundFeeRate = null, Household? household = null)
    {
        withPay ??= TaxedWithPay.None;
        household ??= Household.None;
        var movable = withPay.Movable;
        ArgumentOutOfRangeException.ThrowIfLessThan(withPay.OtherIncome, 0m);
        ArgumentOutOfRangeException.ThrowIfLessThan(withPay.BenefitInKind, 0m);
        ArgumentOutOfRangeException.ThrowIfLessThan(movable.TaxedAsPay, 0m);
        ArgumentOutOfRangeException.ThrowIfLessThan(movable.TaxedApartIncome, 0m);
        ArgumentOutOfRangeException.ThrowIfLessThan(movable.Withheld, 0m);

        var socialRules = SocialContributionRules.For(incomeYear);
        var taxRules = PersonalTaxRules.For(incomeYear, region);
        var withholdingRules = WithholdingRules.For(incomeYear);
        var municipalRates = MunicipalRates.For(incomeYear, region);
        if (socialRules is null || taxRules is null || withholdingRules is null || municipalRates is null)
            return (null, IncomeYearNotSupported);
        if (grossPay < 0m)
            return (null, GrossPayNegative);
        if (municipalRate.Value is < 0m or > MaximumMunicipalRate)
            return (null, MunicipalRateOutOfRange);
        if (fundFeeRate is < 0m or > MaximumFundFeeRate)
            return (null, FundFeeRateOutOfRange);
        if (prepayments.Withheld < 0m)
            return (null, TaxWithheldNegative);
        if (prepayments.Paid.ByDeadline.Any(amount => amount < 0m))
            return (null, TaxAdvancePaymentNegative);
        if (household.TaxedJointly && household.PartnerIncome < 0m)
            return (null, PartnerIncomeNegative);
        if (DependantsRefusal(household.Dependants) is { } dependantsRefusal)
            return (null, dependantsRefusal);

        var figures = Figures.For(socialRules, taxRules, withholdingRules, grossPay, municipalRate.Value, prepayments, paidBy, withPay, fundFeeRate, household);
        var next = Figures.For(socialRules, taxRules, withholdingRules, grossPay + NextSlice, municipalRate.Value, prepayments, paidBy, withPay, fundFeeRate, household);
        var advancePayments = IncomeTaxAdvancePaymentCalculator.WithTopUps(
            taxRules.AdvancePayments, figures.YearAdvancePayments, figures.Tax.IncomeTax, Credits(figures.YearTax),
            prepayments with { Withheld = figures.Withheld });

        return (new GrossToNetResult(
            incomeYear,
            region,
            grossPay,
            municipalRate.Value,
            figures.SocialContributions,
            figures.FundFee,
            figures.ContributionsPaidByCompany,
            figures.BenefitInKind,
            figures.TaxablePay,
            figures.CompanyCost,
            figures.Tax.FlatRateCosts,
            figures.Tax.TaxableIncome,
            figures.Tax.IncomeTax,
            figures.Tax.MunicipalSurcharge,
            figures.Tax.LowEarnedIncomeCredit,
            advancePayments,
            figures.NetInPocket,
            Steps(socialRules, taxRules, municipalRates.Surcharge, figures, municipalRate, fundFeeRate, household),
            new NextThousand(
                next.SocialContributions - figures.SocialContributions,
                next.YearTax.IncomeTax - figures.YearTax.IncomeTax,
                next.YearTax.MunicipalSurcharge - figures.YearTax.MunicipalSurcharge,
                figures.YearTax.LowEarnedIncomeCredit - next.YearTax.LowEarnedIncomeCredit,
                next.YearAdvancePayments.Surcharge - next.YearAdvancePayments.Bonus - figures.YearAdvancePayments.Surcharge + figures.YearAdvancePayments.Bonus,
                next.YearNetInPocket - figures.YearNetInPocket)
            {
                FundFee = next.FundFee - figures.FundFee,
                ChildTaxCredit = Money.Minus(figures.YearTax.ChildTaxCredit, next.YearTax.ChildTaxCredit),
            },
            Statement(figures),
            AssessmentCalculator.Compute(taxRules, figures.YearTax, advancePayments, figures.Withheld, figures.WithheldGiven, prepayments.Paid, movable.Withheld),
            figures.OtherIncomeTax,
            figures.RequalifiedPay,
            figures.TaxablePayWithoutRequalified,
            figures.TaxedApartTax)
        {
            Household = HouseholdOf(taxRules, figures, municipalRate.Value, withPay, household),
            ChildTaxCredit = figures.Tax.ChildTaxCredit,
            Supplements = figures.Tax.Supplements,
            SupplementsTo = figures.Tax.SupplementsTo,
            DependantsSave = household.Dependants == Dependants.None
                ? 0m
                : Money.Minus(YearShareOf(taxRules, figures, municipalRate.Value, withPay, household with { Dependants = Dependants.None }), NetTax(figures.YearTax)),
            HouseholdSaves = household == Household.None
                ? 0m
                : Money.Minus(YearShareOf(taxRules, figures, municipalRate.Value, withPay, Household.None), NetTax(figures.YearTax)),
        }, null);
    }

    // The river's share of the tax on the year's whole income were the household as given: the household's
    // tax, less what the partner would owe filing alone in a joint year, which no dependant changes.
    private static decimal YearShareOf(PersonalTaxRules taxRules, Figures figures, decimal municipalRate, TaxedWithPay withPay, Household household)
    {
        var tax = PersonalTaxCalculator.Compute(
            taxRules, figures.TaxablePay, figures.SocialContributions + figures.FundFee, municipalRate, withPay.OtherIncome, withPay.Movable.TaxedApart, household);

        return NetTax(tax) - (household.TaxedJointly && figures.PartnerAlone is { } partnerAlone ? NetTax(partnerAlone) : 0m);
    }

    // The tax the household's facts move: the income tax and the municipal surcharge, less the credit for
    // children. The credit for low earned income is the director's on their own income, whatever the
    // household.
    private static decimal NetTax(PersonalTaxResult tax) => tax.IncomeTax + tax.MunicipalSurcharge - tax.ChildTaxCredit;

    // The dependants as the user declared them, refused on the field that holds them: a count below zero,
    // or more children under 3 than children.
    private static Refusal? DependantsRefusal(Dependants dependants) => dependants switch
    {
        { Children: < 0 } => ChildrenNegative,
        { ChildrenUnder3: < 0 } => ChildrenUnder3OutOfRange,
        _ when dependants.ChildrenUnder3 > dependants.Children => ChildrenUnder3OutOfRange,
        { DependantsAged66InCare: < 0 } => DependantsInCareNegative,
        { OtherDependants: < 0 } => OtherDependantsNegative,
        _ => null,
    };

    // The tax credits the advance-payment surcharge's base subtracts (art. 175, 1°): the credit for low
    // earned income and the credit for children, both federal. The river's share of a joint year counts
    // the household's credit for children whole, the partner filing alone having none.
    private static decimal Credits(PersonalTaxResult tax) => tax.LowEarnedIncomeCredit + tax.ChildTaxCredit;

    // The household's year beside the pay's: its two columns on the whole income, what the partner would
    // owe filing alone, and what the marital quotient saves the pay's share of the household's tax: that
    // share were the quotient not applied to the year, less the share with it, which is the household's tax
    // without the quotient less with it, the partner filing alone being the baseline of both. Both are net
    // of the credit for children (NetTax), which the quotient can move as it moves the supplements; the
    // credit for low earned income is the same either way. None for a director taxed alone.
    private static HouseholdTax? HouseholdOf(PersonalTaxRules taxRules, Figures figures, decimal municipalRate, TaxedWithPay withPay, Household household)
    {
        if (figures.PartnerAlone is not { } partnerAlone || figures.YearTax.Joint is not { } year)
            return null;

        var without = PersonalTaxCalculator.WithoutQuotient(
            taxRules, figures.TaxablePay, figures.SocialContributions + figures.FundFee, municipalRate, withPay.OtherIncome, withPay.Movable.TaxedApart, household);
        var shareWithout = NetTax(without) - NetTax(partnerAlone);

        return new HouseholdTax(year, partnerAlone, Money.Minus(shareWithout, NetTax(figures.YearTax)));
    }

    // The pay's figures alone, for the split's search, which runs the year at a hundred pays and more
    // and reads only the net in pocket, the other income's tax, the taxable pay and the company's cost
    // of them: no steps, no statement, and not the next 1,000, which costs as much again. Null for a
    // year without rules; the pay and the rate are Compute's to refuse.
    internal static Figures? Level(
        int incomeYear, decimal grossPay, decimal municipalRate, TaxPrepayments prepayments, ContributionsPaidBy paidBy, TaxedWithPay withPay, Region region,
        decimal? fundFeeRate, Household household)
    {
        if (SocialContributionRules.For(incomeYear) is not { } socialRules || PersonalTaxRules.For(incomeYear, region) is not { } taxRules
            || WithholdingRules.For(incomeYear) is not { } withholdingRules)
            return null;

        return Figures.For(socialRules, taxRules, withholdingRules, grossPay, municipalRate, prepayments, paidBy, withPay, fundFeeRate, household);
    }

    // When the company pays the contributions, the smallest payslip gross whose taxable pay, a benefit
    // in kind beside it left out, reaches an amount, to the cent: the contributions and the fund's fee on
    // them are owed on the gross and the benefit, and the taxable pay rises with the gross, so it is found
    // by halving between no pay and the ceiling. Null when no gross up to the ceiling reaches it, or for a
    // year without rules.
    internal static decimal? SmallestGrossPayWithTaxablePay(
        int incomeYear, decimal taxablePay, decimal ceiling, decimal benefitInKind, Region region, decimal? fundFeeRate)
    {
        if (SocialContributionRules.For(incomeYear) is not { } socialRules || PersonalTaxRules.For(incomeYear, region) is not { } taxRules)
            return null;

        decimal TaxablePayOf(decimal grossPay) => grossPay + CompanyPaidToTheFund(socialRules, taxRules, grossPay + benefitInKind, fundFeeRate);

        if (TaxablePayOf(0m) >= taxablePay)
            return 0m;
        if (TaxablePayOf(ceiling) < taxablePay)
            return null;

        var (low, high) = (0m, ceiling);
        while (high - low > 0.01m)
        {
            var middle = Money.DownToCent((low + high) / 2m);
            if (TaxablePayOf(middle) >= taxablePay)
                high = middle;
            else
                low = middle;
        }

        return high;
    }

    // Paid by the director, the contributions are owed on the year's net taxable income, which is the
    // gross less the contributions themselves, the fund's fee on them, and the flat-rate costs on what is
    // left: a fixed point. Each pass shrinks the error at least fourfold (the steepest rate is 20.50%,
    // the fee at most 10% of it), so it is solved without rounding, then billed as four rounded
    // quarters, and the fee, the flat-rate costs and the base follow from that billed amount. Billing
    // moves in steps of 0.04, so at about one pay level in six no amount is its own fixed point to the
    // cent, and recomputing the contributions from the base returned differs by 0.04 (decided
    // 2026-10-19).
    private static decimal SolveContributions(SocialContributionRules socialRules, PersonalTaxRules taxRules, decimal grossPay, decimal? fundFeeRate)
    {
        var withFee = 1m + (fundFeeRate ?? 0m);
        var contributions = 0m;
        for (var pass = 0; pass < MaximumPasses; pass++)
        {
            var payAfterContributions = grossPay - contributions * withFee;
            var flatRateCosts = Math.Clamp(
                taxRules.FlatRateCostsRate.Value * payAfterContributions, 0m, taxRules.FlatRateCostsMaximum.Value);
            var next = SocialContributionCalculator.Unrounded(socialRules, payAfterContributions - flatRateCosts);
            if (Math.Abs(next - contributions) < Convergence)
                return SocialContributionCalculator.Billed(next);

            contributions = next;
        }

        throw new InvalidOperationException($"The social contributions did not converge for a gross pay of {grossPay}.");
    }

    // Paid by the company, the contributions are pay too and are deducted as the director's own, so the
    // pay less the own contributions is the payslip gross itself: they are owed on the gross less the
    // flat-rate costs on it, with no fixed point to solve, and recomputing them from the base returned
    // gives them to the cent.
    private static decimal CompanyPaidContributions(SocialContributionRules socialRules, PersonalTaxRules taxRules, decimal grossPay) =>
        SocialContributionCalculator.Annual(socialRules, grossPay - PersonalTaxCalculator.FlatRateCosts(taxRules, grossPay));

    // What the company pays the fund for the director on that pay: the contributions and the fee on them,
    // both deducted, so the fee changes nothing of their base.
    private static decimal CompanyPaidToTheFund(SocialContributionRules socialRules, PersonalTaxRules taxRules, decimal grossPay, decimal? fundFeeRate)
    {
        var contributions = CompanyPaidContributions(socialRules, taxRules, grossPay);
        return contributions + SocialContributionCalculator.Fee(contributions, fundFeeRate);
    }

    // The fund's fee is a step of its own once its rate is given, right after the contributions it rides
    // on, and deducted with them: the text it is deducted under, the rate the user gave, and the
    // commentary Netto reads that text with. A joint year's income tax cites what it used besides a single
    // person's amounts, the marital quotient and the tax-free amount the partners share.
    private static IReadOnlyList<Step> Steps(
        SocialContributionRules socialRules, PersonalTaxRules taxRules, Source municipalSurcharge, Figures figures, RuleAmount municipalRate, decimal? fundFeeRate,
        Household household) =>
    [
        new Step("social_contributions", Layer.Social, TaxableEvent.DirectorPay, Track.PaidOut, figures.SocialContributions, socialRules.Source, socialRules.Amounts),
        .. fundFeeRate is { } rate
            ? [new Step("fund_fee", Layer.Social, TaxableEvent.DirectorPay, Track.PaidOut, figures.FundFee, socialRules.FundFeeDeduction, [socialRules.FundFeeRate(rate)])
                { AlsoCites = [socialRules.FundFeeCommentary] }]
            : Array.Empty<Step>(),
        new Step("flat_rate_costs", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, figures.Tax.FlatRateCosts,
            taxRules.DirectorPayDeductions, taxRules.FlatRateCostsAmounts),
        new Step("taxable_income", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, figures.Tax.TaxableIncome, taxRules.DirectorPayDeductions, []),
        new Step("income_tax", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, figures.Tax.IncomeTax, taxRules.IncomeTaxScale,
            taxRules.IncomeTaxAmountsFor(household)) { AlsoCites = taxRules.IncomeTaxAlsoCites(household) },
        new Step("municipal_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, figures.Tax.MunicipalSurcharge, municipalSurcharge, [municipalRate]),
        new Step("low_earned_income_credit", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, -figures.Tax.LowEarnedIncomeCredit,
            taxRules.LowEarnedIncomeCredit.Source, taxRules.LowEarnedIncomeCredit.Amounts),
        .. household.Dependants.Children > 0
            ? [new Step("child_tax_credit", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, -figures.Tax.ChildTaxCredit,
                taxRules.Supplements.CreditSource, [taxRules.Supplements.ChildCreditCapPerChild])]
            : Array.Empty<Step>(),
        new Step("income_tax_advance_payment_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, figures.AdvancePayments.Surcharge,
            taxRules.AdvancePayments.Source, taxRules.AdvancePayments.SurchargeAmounts),
        new Step("income_tax_advance_payment_bonus", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, -figures.AdvancePayments.Bonus,
            taxRules.AdvancePayments.BonusSource, taxRules.AdvancePayments.BonusAmounts),
    ];

    // The twelve months of the income year, each line its own twelfth of the year's figure rounded to
    // the cent, so the twelve can differ from the year by a few cents: the cents are not spread over
    // the months. The withholding is the key formula's on the taxable pay over twelve by the rules of
    // the month's period.
    private static PayStatement Statement(Figures figures)
    {
        var grossPay = Money.RoundToCent(figures.GrossPay / YearParts.Months);
        var paidByCompany = Money.RoundToCent(figures.ContributionsPaidByCompany / YearParts.Months);
        var benefit = Money.RoundToCent(figures.BenefitInKind / YearParts.Months);
        var months = figures.Withholding
            .SelectMany(period => Enumerable.Range(period.FirstMonth, period.MonthCount).Select(month => new PayStatementMonth(
                month, grossPay, paidByCompany, benefit, period.Withholding.MonthlyTaxablePay, period.Withholding.MonthlyWithholding,
                grossPay - period.Withholding.MonthlyWithholding)))
            .ToList();

        return new PayStatement(months, figures.Withholding);
    }

    // Each of the year's withholding rules with the months it covers, from the month its rules date
    // falls in to the month before the next one's, the last to December, and the key formula's lines
    // on a month's taxable pay by them, by the method the household's withholding takes
    // (WithholdingHouseholds.Of), less the reductions for the dependants it takes (WithholdingDependants.Of):
    // the tax is computed first, as it says which column the dependants' supplements go to.
    private static IReadOnlyList<PayStatementPeriod> PeriodsOf(
        WithholdingRules rules, decimal monthlyTaxablePay, WithholdingHousehold household, WithholdingDependants dependants)
    {
        var periods = rules.Periods();

        return
        [
            .. periods.Select((period, i) => new PayStatementPeriod(
                period,
                period.RulesFrom.Value.Month,
                i + 1 < periods.Count ? periods[i + 1].RulesFrom.Value.Month - 1 : YearParts.Months,
                WithholdingCalculator.Figures(period, monthlyTaxablePay, household, dependants))),
        ];
    }

    // One pay's figures. GrossPay is the pay on the payslip; FundFee is the fund's fee on the
    // contributions, nothing without its rate; ContributionsPaidByCompany is the contributions and that
    // fee when the company pays them, nothing when the director does; BenefitInKind is the
    // benefit taxed as pay and never paid; RequalifiedPay is movable income taxed as pay, paid besides
    // the payslip; Withholding is, for each period of the year's withholding rules, the key formula's
    // lines on a month of the payslip's taxable pay, over twelve rounded to the cent; Withheld is the
    // year's withholding, the amount given (WithheldGiven) or the sum of the twelve months' by those
    // periods. Tax, AdvancePayments and NetInPocket are the pay's taken alone, the requalified pay in
    // it; YearTax and YearAdvancePayments are on the whole income, the same as the pay's when nothing
    // else is taxed with it; OtherIncomeTax is what the rent adds to the pay, and TaxedApartTax what an
    // income taxed apart adds on top of both. In a household taxed jointly, Tax and YearTax are what the
    // river's share of the household's tax, their Joint the household's whole columns, and PartnerAlone
    // what the partner would owe filing alone, which they leave out; none for a director taxed alone.
    internal sealed record Figures(
        decimal GrossPay,
        decimal SocialContributions,
        decimal FundFee,
        decimal ContributionsPaidByCompany,
        decimal BenefitInKind,
        decimal RequalifiedPay,
        decimal TaxablePayWithoutRequalified,
        IReadOnlyList<PayStatementPeriod> Withholding,
        decimal Withheld,
        bool WithheldGiven,
        PersonalTaxResult Tax,
        IncomeTaxAdvancePaymentResult AdvancePayments,
        decimal NetInPocket,
        PersonalTaxResult YearTax,
        IncomeTaxAdvancePaymentResult YearAdvancePayments,
        OtherIncomeTax OtherIncomeTax,
        OtherIncomeTax TaxedApartTax,
        PersonalTaxResult? PartnerAlone)
    {
        // What the director declares as pay: the gross, what the company bore for them, the benefits
        // in kind (cadre XVI, 1) and movable income taxed as pay (Tax-Calc T18).
        public decimal TaxablePay => GrossPay + ContributionsPaidByCompany + BenefitInKind + RequalifiedPay;

        // What the pay takes out of the company: the gross and what it bears for the director, which is
        // pay. What a benefit costs the company is its own, a car's costs, and so is movable income.
        public decimal CompanyCost => GrossPay + ContributionsPaidByCompany;

        // What the pay leaves once the other income's tax and the tax apart are paid too, those incomes
        // themselves aside.
        public decimal YearNetInPocket => NetInPocket - OtherIncomeTax.Total - TaxedApartTax.Total;

        public static Figures For(
            SocialContributionRules socialRules, PersonalTaxRules taxRules, WithholdingRules withholdingRules, decimal grossPay, decimal municipalRate,
            TaxPrepayments prepayments, ContributionsPaidBy paidBy, TaxedWithPay withPay, decimal? fundFeeRate, Household household)
        {
            // The contributions are owed on the benefit and the requalified pay as on the gross, on their
            // total, and the fund's fee rides on them; the director deducts both as their own.
            var benefitInKind = withPay.BenefitInKind;
            var movable = withPay.Movable;
            var asPay = movable.TaxedAsPay;
            var companyPays = paidBy == ContributionsPaidBy.Company;
            var contributions = companyPays
                ? CompanyPaidContributions(socialRules, taxRules, grossPay + benefitInKind + asPay)
                : SolveContributions(socialRules, taxRules, grossPay + benefitInKind + asPay, fundFeeRate);
            var fundFee = SocialContributionCalculator.Fee(contributions, fundFeeRate);
            var ownContributions = contributions + fundFee;
            var paidByCompany = companyPays ? ownContributions : 0m;
            var taxablePay = grossPay + paidByCompany + benefitInKind + asPay;
            var partnerAlone = household.TaxedJointly ? PersonalTaxCalculator.PartnerAlone(taxRules, household.PartnerIncome, municipalRate) : null;
            var tax = Share(PersonalTaxCalculator.Compute(taxRules, taxablePay, ownContributions, municipalRate, household: household), partnerAlone);
            var withholding = PeriodsOf(
                withholdingRules, Money.RoundToCent((taxablePay - asPay) / YearParts.Months), WithholdingHouseholds.Of(household),
                WithholdingDependants.Of(household, tax.SupplementsTo));
            var withheld = prepayments.Withheld ?? withholding.Sum(period => period.Withholding.MonthlyWithholding * period.MonthCount);
            var withWithheld = prepayments with { Withheld = withheld };
            var advancePayments = IncomeTaxAdvancePaymentCalculator.Figures(taxRules.AdvancePayments, tax.IncomeTax, Credits(tax), withWithheld);
            var (withOther, withOtherAdvancePayments) = withPay.OtherIncome == 0m
                ? (tax, advancePayments)
                : YearOf(taxRules, taxablePay, ownContributions, municipalRate, withPay with { Movable = movable with { TaxedApart = null } }, tax, withWithheld, household, partnerAlone);
            var (yearTax, yearAdvancePayments) = movable.TaxedApart is null
                ? (withOther, withOtherAdvancePayments)
                : YearOf(taxRules, taxablePay, ownContributions, municipalRate, withPay, tax, withWithheld, household, partnerAlone);

            // The contributions and the fee come off the pocket when the director pays them; when the
            // company does, they are pay that goes straight back out, so the pocket starts from the
            // payslip gross. The flat-rate costs only lower the tax base: they are never subtracted from
            // the pocket. The credit for low earned income is refunded where it exceeds the tax, so it all
            // comes back. What was withheld or paid in advance is the tax paid early, not a cost: only the
            // surcharge and the bonus reach the pocket. The benefit in kind is never paid: the pocket bears
            // its tax, not the benefit. The requalified pay is paid, as the income it is.
            return new Figures(
                grossPay, contributions, fundFee, paidByCompany, benefitInKind, asPay,
                asPay == 0m ? taxablePay : grossPay + benefitInKind + (companyPays ? CompanyPaidToTheFund(socialRules, taxRules, grossPay + benefitInKind, fundFeeRate) : 0m),
                withholding, withheld, prepayments.Withheld is not null, tax, advancePayments,
                taxablePay - benefitInKind - ownContributions - tax.IncomeTax - tax.MunicipalSurcharge + tax.LowEarnedIncomeCredit + tax.ChildTaxCredit
                - advancePayments.Surcharge + advancePayments.Bonus,
                yearTax,
                yearAdvancePayments,
                Added(withPay.OtherIncome, withOther, withOtherAdvancePayments, tax, advancePayments),
                Added(movable.TaxedApartIncome, yearTax, yearAdvancePayments, withOther, withOtherAdvancePayments),
                partnerAlone);
        }

        // The river's share of the household's tax: each line less what the partner would owe filing alone,
        // the household's columns kept whole; the tax itself for a director taxed alone. The partner has no
        // credit for low earned income, so it is the director's either way; filing alone they have no
        // dependants, so the supplements and the credit for children are the household's whole.
        private static PersonalTaxResult Share(PersonalTaxResult tax, PersonalTaxResult? partnerAlone) => partnerAlone is null ? tax : tax with
        {
            BasicTax = Money.Minus(tax.BasicTax, partnerAlone.BasicTax),
            TaxFreeAmountReduction = Money.Minus(tax.TaxFreeAmountReduction, partnerAlone.TaxFreeAmountReduction),
            StateTax = Money.Minus(tax.StateTax, partnerAlone.StateTax),
            ReducedStateTax = Money.Minus(tax.ReducedStateTax, partnerAlone.ReducedStateTax),
            RegionalAdditionalTax = Money.Minus(tax.RegionalAdditionalTax, partnerAlone.RegionalAdditionalTax),
            IncomeTax = Money.Minus(tax.IncomeTax, partnerAlone.IncomeTax),
            MunicipalSurcharge = Money.Minus(tax.MunicipalSurcharge, partnerAlone.MunicipalSurcharge),
            LowEarnedIncomeCredit = Money.Minus(tax.LowEarnedIncomeCredit, partnerAlone.LowEarnedIncomeCredit),
        };

        // What an income moves from the figures without it to those with it: the income tax and the
        // municipal surcharge it adds, the credits it takes away, and the surcharge less the bonus those
        // lost credits add; a plain zero for what it moves nothing of.
        private static OtherIncomeTax Added(
            decimal income, PersonalTaxResult with, IncomeTaxAdvancePaymentResult withAdvancePayments, PersonalTaxResult without,
            IncomeTaxAdvancePaymentResult withoutAdvancePayments) =>
            new(
                income,
                Money.Minus(with.IncomeTax, without.IncomeTax),
                Money.Minus(with.MunicipalSurcharge, without.MunicipalSurcharge),
                Money.Minus(without.LowEarnedIncomeCredit, with.LowEarnedIncomeCredit),
                Money.Minus(withAdvancePayments.Surcharge - withAdvancePayments.Bonus, withoutAdvancePayments.Surcharge - withoutAdvancePayments.Bonus))
            {
                ChildTaxCreditLost = Money.Minus(without.ChildTaxCredit, with.ChildTaxCredit),
            };

        // The tax on the pay with what is taxed with it (the other income and an income taxed apart), and
        // the surcharge on the pay's own tax, as art. 158 takes it separately, less the credit the total
        // leaves; taxablePay already holds the benefit and the requalified pay, and ownContributions the
        // fund's fee.
        private static (PersonalTaxResult Tax, IncomeTaxAdvancePaymentResult AdvancePayments) YearOf(
            PersonalTaxRules taxRules, decimal taxablePay, decimal ownContributions, decimal municipalRate, TaxedWithPay withPay, PersonalTaxResult payTax,
            TaxPrepayments prepayments, Household household, PersonalTaxResult? partnerAlone)
        {
            var yearTax = Share(
                PersonalTaxCalculator.Compute(taxRules, taxablePay, ownContributions, municipalRate, withPay.OtherIncome, withPay.Movable.TaxedApart, household), partnerAlone);
            return (yearTax, IncomeTaxAdvancePaymentCalculator.Figures(taxRules.AdvancePayments, payTax.IncomeTax, Credits(yearTax), prepayments));
        }
    }
}

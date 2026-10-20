using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;

namespace Netto.Core.DirectorPay;

// A year's director pay, with the social contributions paid by the director personally or by the
// company on the director's behalf, in steady state: the contributions paid in the year equal the
// final contributions owed on its income. The income tax on it is surcharged where too little of it
// was withheld or paid in advance during the year, and the advance payments beyond what avoids that
// earn a bonus. The pay is paid in twelve equal months, each withheld on by the key formula: the
// statement shows them, the year's withholding is twelve times the month's unless the user gave the
// amount, and the assessment is what the year's tax return leaves to pay or refunds.
//
// Another income can be taxed with the pay, a home office's net rent, globalised with it (Tax-Calc
// T13 and T14). The pay's own figures, its steps, its net and its statement, stay those of the pay
// taken alone, as art. 158 takes it for the surcharge; what the other income adds to the year's tax
// is its own record: the tax on the total less the pay's, the credit for low earned income it takes
// away by raising the total net income (art. 289ter), and the surcharge less the bonus that credit
// moves, the surcharge base subtracting only the credit granted. The year's figures, the advance
// payments with their suggested payment, the next 1,000 and the assessment, are on the whole income.
public static class GrossToNet
{
    public const string GrossPayNegative = "gross_pay_negative";
    public const string MunicipalRateOutOfRange = "municipal_rate_out_of_range";
    public const string IncomeYearNotSupported = "income_year_not_supported";
    public const string TaxWithheldNegative = "tax_withheld_negative";
    public const string TaxAdvancePaymentNegative = "tax_advance_payment_negative";

    private const decimal MaximumMunicipalRate = 0.10m;
    private const decimal NextSlice = 1_000m;
    private const decimal Convergence = 0.000001m;
    private const int MaximumPasses = 100;
    private const int MonthsInYear = 12;

    // The gross pay is the pay on the payslip, whoever pays the contributions; not saying who runs the
    // pay as the director's own. The other income is the net income taxed with the pay, none unless
    // given; the engine's own rules give it, never below zero.
    public static (GrossToNetResult? Value, string? Error) Compute(
        int incomeYear, decimal grossPay, RuleAmount municipalRate, TaxPrepayments prepayments, ContributionsPaidBy paidBy = ContributionsPaidBy.Director,
        decimal otherIncome = 0m)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(otherIncome, 0m);

        var socialRules = SocialContributionRules.For(incomeYear);
        var taxRules = PersonalTaxRules.For(incomeYear);
        var withholdingRules = WithholdingRules.For(incomeYear);
        if (socialRules is null || taxRules is null || withholdingRules is null)
            return (null, IncomeYearNotSupported);
        if (grossPay < 0m)
            return (null, GrossPayNegative);
        if (municipalRate.Value is < 0m or > MaximumMunicipalRate)
            return (null, MunicipalRateOutOfRange);
        if (prepayments.Withheld < 0m)
            return (null, TaxWithheldNegative);
        if (prepayments.Paid.ByDeadline.Any(amount => amount < 0m))
            return (null, TaxAdvancePaymentNegative);

        var figures = Figures.For(socialRules, taxRules, withholdingRules, grossPay, municipalRate.Value, prepayments, paidBy, otherIncome);
        var next = Figures.For(socialRules, taxRules, withholdingRules, grossPay + NextSlice, municipalRate.Value, prepayments, paidBy, otherIncome);
        var advancePayments = IncomeTaxAdvancePaymentCalculator.Compute(
            taxRules.AdvancePayments, figures.Tax.IncomeTax, figures.YearTax.LowEarnedIncomeCredit, prepayments with { Withheld = figures.Withheld });

        return (new GrossToNetResult(
            incomeYear,
            grossPay,
            municipalRate.Value,
            figures.SocialContributions,
            figures.ContributionsPaidByCompany,
            figures.TaxablePay,
            figures.CompanyCost,
            figures.Tax.FlatRateCosts,
            figures.Tax.TaxableIncome,
            figures.Tax.IncomeTax,
            figures.Tax.MunicipalSurcharge,
            figures.Tax.LowEarnedIncomeCredit,
            advancePayments,
            figures.NetInPocket,
            Steps(socialRules, taxRules, figures, municipalRate),
            new NextThousand(
                next.SocialContributions - figures.SocialContributions,
                next.YearTax.IncomeTax - figures.YearTax.IncomeTax,
                next.YearTax.MunicipalSurcharge - figures.YearTax.MunicipalSurcharge,
                figures.YearTax.LowEarnedIncomeCredit - next.YearTax.LowEarnedIncomeCredit,
                next.YearAdvancePayments.Surcharge - next.YearAdvancePayments.Bonus - figures.YearAdvancePayments.Surcharge + figures.YearAdvancePayments.Bonus,
                next.YearNetInPocket - figures.YearNetInPocket),
            Statement(withholdingRules, figures),
            AssessmentCalculator.Compute(figures.YearTax, advancePayments, figures.Withheld, figures.WithheldGiven, prepayments.Paid),
            figures.OtherIncomeTax), null);
    }

    // The pay's figures alone, for the split's search, which runs the year at a hundred pays and more
    // and reads only the net in pocket, the other income's tax, the taxable pay and the company's cost
    // of them: no steps, no statement, and not the next 1,000, which costs as much again. Null for a
    // year without rules; the pay and the rate are Compute's to refuse.
    internal static Figures? Level(
        int incomeYear, decimal grossPay, decimal municipalRate, TaxPrepayments prepayments, ContributionsPaidBy paidBy, decimal otherIncome)
    {
        if (SocialContributionRules.For(incomeYear) is not { } socialRules || PersonalTaxRules.For(incomeYear) is not { } taxRules
            || WithholdingRules.For(incomeYear) is not { } withholdingRules)
            return null;

        return Figures.For(socialRules, taxRules, withholdingRules, grossPay, municipalRate, prepayments, paidBy, otherIncome);
    }

    // When the company pays the contributions, the smallest payslip gross whose taxable pay reaches an
    // amount, to the cent: the taxable pay rises with the gross, so it is found by halving between no
    // pay and a ceiling whose taxable pay reaches it. Null for a year without rules.
    internal static decimal? SmallestGrossPayWithTaxablePay(int incomeYear, decimal taxablePay, decimal ceiling)
    {
        if (SocialContributionRules.For(incomeYear) is not { } socialRules || PersonalTaxRules.For(incomeYear) is not { } taxRules)
            return null;

        decimal TaxablePayOf(decimal grossPay) => grossPay + CompanyPaidContributions(socialRules, taxRules, grossPay);

        if (TaxablePayOf(0m) >= taxablePay)
            return 0m;
        if (TaxablePayOf(ceiling) < taxablePay)
            throw new InvalidOperationException($"No payslip gross up to {ceiling} has a taxable pay of {taxablePay}.");

        var (low, high) = (0m, ceiling);
        while (high - low > 0.01m)
        {
            var middle = Math.Floor((low + high) * 50m) / 100m;
            if (TaxablePayOf(middle) >= taxablePay)
                high = middle;
            else
                low = middle;
        }

        return high;
    }

    // Paid by the director, the contributions are owed on the year's net taxable income, which is the
    // gross less the contributions themselves and less the flat-rate costs on what is left: a fixed
    // point. Each pass shrinks the error at least fivefold (the steepest rate is 20.50%), so it is
    // solved without rounding, then billed as four rounded quarters, and the flat-rate costs and the
    // base follow from that billed amount. Billing moves in steps of 0.04, so at about one pay level
    // in six no amount is its own fixed point to the cent, and recomputing the contributions from the
    // base returned differs by 0.04 (decided 2026-10-19).
    private static decimal SolveContributions(SocialContributionRules socialRules, PersonalTaxRules taxRules, decimal grossPay)
    {
        var contributions = 0m;
        for (var pass = 0; pass < MaximumPasses; pass++)
        {
            var payAfterContributions = grossPay - contributions;
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

    private static IReadOnlyList<Step> Steps(
        SocialContributionRules socialRules, PersonalTaxRules taxRules, Figures figures, RuleAmount municipalRate) =>
    [
        new Step("social_contributions", Layer.Social, TaxableEvent.DirectorPay, Track.PaidOut, figures.SocialContributions, socialRules.Source, socialRules.Amounts),
        new Step("flat_rate_costs", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, figures.Tax.FlatRateCosts,
            PersonalTaxRules.DirectorPayDeductions, taxRules.FlatRateCostsAmounts),
        new Step("taxable_income", Layer.Personal, TaxableEvent.DirectorPay, Track.TaxBaseOnly, figures.Tax.TaxableIncome, PersonalTaxRules.DirectorPayDeductions, []),
        new Step("income_tax", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, figures.Tax.IncomeTax, PersonalTaxRules.IncomeTaxScale, taxRules.IncomeTaxAmounts),
        new Step("municipal_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, figures.Tax.MunicipalSurcharge, PersonalTaxRules.MunicipalSurcharge, [municipalRate]),
        new Step("low_earned_income_credit", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, -figures.Tax.LowEarnedIncomeCredit,
            taxRules.LowEarnedIncomeCredit.Source, taxRules.LowEarnedIncomeCredit.Amounts),
        new Step("income_tax_advance_payment_surcharge", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, figures.AdvancePayments.Surcharge,
            taxRules.AdvancePayments.Source, taxRules.AdvancePayments.SurchargeAmounts),
        new Step("income_tax_advance_payment_bonus", Layer.Personal, TaxableEvent.DirectorPay, Track.PaidOut, -figures.AdvancePayments.Bonus,
            taxRules.AdvancePayments.BonusSource, taxRules.AdvancePayments.BonusAmounts),
    ];

    // The twelve months of the income year, each line its own twelfth of the year's figure rounded to
    // the cent, so the twelve can differ from the year by a few cents: the cents are not spread over
    // the months. The withholding is the key formula's on the taxable pay over twelve, the same every
    // month; a month from UnmodelledRulesFrom is flagged.
    private static PayStatement Statement(WithholdingRules rules, Figures figures)
    {
        var grossPay = Money.RoundToCent(figures.GrossPay / MonthsInYear);
        var paidByCompany = Money.RoundToCent(figures.ContributionsPaidByCompany / MonthsInYear);
        var withholding = figures.Withholding.MonthlyWithholding;
        var months = Enumerable.Range(1, MonthsInYear)
            .Select(month => new PayStatementMonth(
                month, grossPay, paidByCompany, figures.Withholding.MonthlyTaxablePay, withholding, grossPay - withholding,
                new DateOnly(rules.IncomeYear, month, 1) >= rules.UnmodelledRulesFrom.Value))
            .ToList();

        return new PayStatement(months, figures.Withholding, rules.UnmodelledRulesFrom, rules.Source, rules.Amounts);
    }

    // One pay's figures. GrossPay is the pay on the payslip; ContributionsPaidByCompany is the
    // contributions when the company pays them, nothing when the director does; Withholding is the key
    // formula's lines on a month of the taxable pay, the taxable pay over twelve rounded to the cent;
    // Withheld is the year's withholding, the amount given (WithheldGiven) or twelve times the month's.
    // Tax, AdvancePayments and NetInPocket are the pay's taken alone; YearTax and YearAdvancePayments
    // are on the whole income, the same as the pay's when nothing else is taxed with it, and
    // OtherIncomeTax is what the rest adds.
    internal sealed record Figures(
        decimal GrossPay,
        decimal SocialContributions,
        decimal ContributionsPaidByCompany,
        WithholdingResult Withholding,
        decimal Withheld,
        bool WithheldGiven,
        PersonalTaxResult Tax,
        IncomeTaxAdvancePaymentResult AdvancePayments,
        decimal NetInPocket,
        PersonalTaxResult YearTax,
        IncomeTaxAdvancePaymentResult YearAdvancePayments,
        OtherIncomeTax OtherIncomeTax)
    {
        // What the director declares as pay: the gross, and what the company bore for them (cadre XVI, 1).
        public decimal TaxablePay => GrossPay + ContributionsPaidByCompany;

        // What the pay takes out of the company: the same amount, since what it bears for the director is pay.
        public decimal CompanyCost => GrossPay + ContributionsPaidByCompany;

        // What the pay leaves once the other income's tax is paid too, the other income itself aside.
        public decimal YearNetInPocket => NetInPocket - OtherIncomeTax.Total;

        public static Figures For(
            SocialContributionRules socialRules, PersonalTaxRules taxRules, WithholdingRules withholdingRules, decimal grossPay, decimal municipalRate,
            TaxPrepayments prepayments, ContributionsPaidBy paidBy, decimal otherIncome)
        {
            var companyPays = paidBy == ContributionsPaidBy.Company;
            var contributions = companyPays
                ? CompanyPaidContributions(socialRules, taxRules, grossPay)
                : SolveContributions(socialRules, taxRules, grossPay);
            var paidByCompany = companyPays ? contributions : 0m;
            var taxablePay = grossPay + paidByCompany;
            var withholding = WithholdingCalculator.Figures(withholdingRules, Money.RoundToCent(taxablePay / MonthsInYear));
            var withheld = prepayments.Withheld ?? withholding.MonthlyWithholding * MonthsInYear;
            var withWithheld = prepayments with { Withheld = withheld };
            var tax = PersonalTaxCalculator.Compute(taxRules, taxablePay, contributions, municipalRate);
            var advancePayments = IncomeTaxAdvancePaymentCalculator.Figures(taxRules.AdvancePayments, tax.IncomeTax, tax.LowEarnedIncomeCredit, withWithheld);
            var (yearTax, yearAdvancePayments) = otherIncome == 0m
                ? (tax, advancePayments)
                : YearOf(taxRules, taxablePay, contributions, municipalRate, otherIncome, tax, withWithheld);

            // The contributions come off the pocket when the director pays them; when the company does,
            // they are pay that goes straight back out, so the pocket starts from the payslip gross.
            // The flat-rate costs only lower the tax base: they are never subtracted from the pocket.
            // The credit for low earned income is refunded where it exceeds the tax, so it all comes back.
            // What was withheld or paid in advance is the tax paid early, not a cost: only the surcharge
            // and the bonus reach the pocket.
            return new Figures(
                grossPay, contributions, paidByCompany, withholding, withheld, prepayments.Withheld is not null, tax, advancePayments,
                taxablePay - contributions - tax.IncomeTax - tax.MunicipalSurcharge + tax.LowEarnedIncomeCredit
                - advancePayments.Surcharge + advancePayments.Bonus,
                yearTax,
                yearAdvancePayments,
                new OtherIncomeTax(
                    otherIncome,
                    Difference(yearTax.IncomeTax, tax.IncomeTax),
                    Difference(yearTax.MunicipalSurcharge, tax.MunicipalSurcharge),
                    Difference(tax.LowEarnedIncomeCredit, yearTax.LowEarnedIncomeCredit),
                    Difference(yearAdvancePayments.Surcharge - yearAdvancePayments.Bonus, advancePayments.Surcharge - advancePayments.Bonus)));
        }

        // What the other income moves, a plain zero when it moves nothing: decimal subtraction can
        // return a negative zero, which the steps would read as money coming back.
        private static decimal Difference(decimal withOther, decimal alone) => withOther == alone ? 0m : withOther - alone;

        // The tax on the pay and the other income together, and the surcharge on the pay's own tax, as
        // art. 158 takes it separately, less the credit the total leaves.
        private static (PersonalTaxResult Tax, IncomeTaxAdvancePaymentResult AdvancePayments) YearOf(
            PersonalTaxRules taxRules, decimal taxablePay, decimal contributions, decimal municipalRate, decimal otherIncome, PersonalTaxResult payTax,
            TaxPrepayments prepayments)
        {
            var yearTax = PersonalTaxCalculator.Compute(taxRules, taxablePay, contributions, municipalRate, otherIncome);
            return (yearTax, IncomeTaxAdvancePaymentCalculator.Figures(taxRules.AdvancePayments, payTax.IncomeTax, yearTax.LowEarnedIncomeCredit, prepayments));
        }
    }
}

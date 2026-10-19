using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;

namespace Netto.Core.DirectorPay;

// A year's director pay, with the social contributions paid by the director personally, in steady
// state: the contributions paid in the year equal the final contributions owed on its income.
public static class GrossToNet
{
    public const string GrossPayNegative = "gross_pay_negative";
    public const string MunicipalRateOutOfRange = "municipal_rate_out_of_range";
    public const string IncomeYearNotSupported = "income_year_not_supported";

    private const decimal MaximumMunicipalRate = 0.10m;
    private const decimal NextSlice = 1_000m;
    private const decimal Convergence = 0.000001m;
    private const int MaximumPasses = 100;

    public static (GrossToNetResult? Value, string? Error) Compute(int incomeYear, decimal grossPay, RuleAmount municipalRate)
    {
        var socialRules = SocialContributionRules.For(incomeYear);
        var taxRules = PersonalTaxRules.For(incomeYear);
        if (socialRules is null || taxRules is null)
            return (null, IncomeYearNotSupported);
        if (grossPay < 0m)
            return (null, GrossPayNegative);
        if (municipalRate.Value is < 0m or > MaximumMunicipalRate)
            return (null, MunicipalRateOutOfRange);

        var figures = Figures.For(socialRules, taxRules, grossPay, municipalRate.Value);
        var next = Figures.For(socialRules, taxRules, grossPay + NextSlice, municipalRate.Value);

        return (new GrossToNetResult(
            incomeYear,
            grossPay,
            municipalRate.Value,
            figures.SocialContributions,
            figures.Tax.FlatRateCosts,
            figures.Tax.TaxableIncome,
            figures.Tax.IncomeTax,
            figures.Tax.MunicipalSurcharge,
            figures.NetInPocket,
            Steps(socialRules, taxRules, figures, municipalRate),
            new NextThousand(
                next.SocialContributions - figures.SocialContributions,
                next.Tax.IncomeTax - figures.Tax.IncomeTax,
                next.Tax.MunicipalSurcharge - figures.Tax.MunicipalSurcharge,
                next.NetInPocket - figures.NetInPocket)), null);
    }

    // The contributions are owed on the year's net taxable income, which is the gross less the
    // contributions themselves and less the flat-rate costs on what is left: a fixed point. Each
    // pass shrinks the error at least fivefold (the steepest rate is 20.50%), so it is solved
    // without rounding, then billed as four rounded quarters, and the flat-rate costs and the base
    // follow from that billed amount. Billing moves in steps of 0.04, so at about one pay level in
    // six no amount is its own fixed point to the cent, and recomputing the contributions from the
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

    private static IReadOnlyList<Step> Steps(
        SocialContributionRules socialRules, PersonalTaxRules taxRules, Figures figures, RuleAmount municipalRate) =>
    [
        new Step("social_contributions", Track.PaidOut, figures.SocialContributions, socialRules.Source, socialRules.Amounts),
        new Step("flat_rate_costs", Track.TaxBaseOnly, figures.Tax.FlatRateCosts,
            PersonalTaxRules.DirectorPayDeductions, taxRules.FlatRateCostsAmounts),
        new Step("taxable_income", Track.TaxBaseOnly, figures.Tax.TaxableIncome, PersonalTaxRules.DirectorPayDeductions, []),
        new Step("income_tax", Track.PaidOut, figures.Tax.IncomeTax, PersonalTaxRules.IncomeTaxScale, taxRules.IncomeTaxAmounts),
        new Step("municipal_surcharge", Track.PaidOut, figures.Tax.MunicipalSurcharge, PersonalTaxRules.MunicipalSurcharge, [municipalRate]),
    ];

    private sealed record Figures(decimal SocialContributions, PersonalTaxResult Tax, decimal NetInPocket)
    {
        public static Figures For(
            SocialContributionRules socialRules, PersonalTaxRules taxRules, decimal grossPay, decimal municipalRate)
        {
            var contributions = SolveContributions(socialRules, taxRules, grossPay);
            var tax = PersonalTaxCalculator.Compute(taxRules, grossPay, contributions, municipalRate);

            // The flat-rate costs only lower the tax base: they are never subtracted from the pocket.
            return new Figures(contributions, tax, grossPay - contributions - tax.IncomeTax - tax.MunicipalSurcharge);
        }
    }
}

namespace Netto.Core.DirectorPay;

// The key formula's five steps on a month's pay, each line rounded to the cent half away from zero
// where the formula rounds (n° 3), in the chain the SPF simulator « Précompte professionnel 2026 »
// (sheet « Dirigeants d'entreprise ») follows: the reduction rounded; the pay less it rounded, then
// times 12; the flat-rate costs rounded, then capped; the basic tax rounded; the tax on the tax-free
// amount taken off it, never below zero; the twelfth rounded. The formula takes the reduction off
// any pay, so a pay under it gives a year below zero, a tax floored at zero and nothing withheld.
public static class WithholdingCalculator
{
    public const string MonthlyPayNegative = "monthly_pay_negative";

    private const int MonthsInYear = 12;

    public static (WithholdingResult? Value, string? Error) Compute(WithholdingRules rules, decimal monthlyTaxablePay)
    {
        if (monthlyTaxablePay < 0m)
            return (null, MonthlyPayNegative);

        var reduction = Money.RoundToCent(OnTable(rules.Reduction, monthlyTaxablePay));
        var annualGross = Money.RoundToCent(monthlyTaxablePay - reduction) * MonthsInYear;
        var flatRateCosts = Math.Min(Money.RoundToCent(annualGross * rules.FlatRateCostsRate.Value), rules.FlatRateCostsMaximum.Value);
        var annualNetTaxable = annualGross - flatRateCosts;
        var basicTax = Money.RoundToCent(OnTable(rules.Scale, annualNetTaxable));
        var annualTax = Math.Max(0m, basicTax - rules.TaxFreeAmountReduction.Value);

        return (new WithholdingResult(
            monthlyTaxablePay,
            reduction,
            annualGross,
            flatRateCosts,
            annualNetTaxable,
            basicTax,
            rules.TaxFreeAmountReduction.Value,
            annualTax,
            Money.RoundToCent(annualTax / MonthsInYear)), null);
    }

    // What the table gives for the amount: the first band whose upper limit the amount does not
    // exceed, its base plus its rate on the slice over the previous band's upper limit.
    private static decimal OnTable(IReadOnlyList<WithholdingBand> table, decimal amount)
    {
        var lowerLimit = 0m;
        foreach (var band in table)
        {
            if (band.UpperLimit is { } upperLimit && amount > upperLimit.Value)
            {
                lowerLimit = upperLimit.Value;
                continue;
            }

            return (band.Base?.Value ?? 0m) + (band.Rate?.Value ?? 0m) * (amount - lowerLimit);
        }

        throw new InvalidOperationException("Every band of the table has an upper limit.");
    }
}

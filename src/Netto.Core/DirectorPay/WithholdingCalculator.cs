using Netto.Core.Rules;

namespace Netto.Core.DirectorPay;

// The key formula's five steps on a month's pay, each line rounded to the cent half away from zero
// where the formula rounds (n° 3), in the chain the SPF simulator « Précompte professionnel 2026 »
// (sheet « Dirigeants d'entreprise ») follows: the reduction rounded; the pay less it rounded, then
// times 12; the flat-rate costs rounded, then capped; the basic tax rounded; the tax on the tax-free
// amount taken off it, never below zero; the twelfth rounded. The formula takes the reduction off
// any pay, so a pay under it gives a year below zero, a tax floored at zero and nothing withheld.
//
// For a director whose partner has no professional income the basic tax is the marital quotient's
// (key formula n° 11 b, annex III n° 34), as the simulator's rows 34 and 35 chain it: 30% of the annual
// net taxable imputed to the partner, rounded, then capped at the period's maximum; the tax on it and
// the tax on the rest, each on the same scale and rounded; their sum less twice the tax on the tax-free
// amount, never below zero.
//
// Off either method's tax, the reductions for the dependants (key formula n° 13 to 15; annex III n° 38
// to 40), each a whole euro amount of the annex, as the simulator's columns L and M take them: n° 38's
// row for the children counted, n° 39, 2 and 3 for each other dependant, n° 40, 1 for a single parent
// under the single method; all of them never beyond the tax (n° 13), so the year's tax never below zero.
public static class WithholdingCalculator
{
    public static readonly Refusal MonthlyPayNegative = new("monthly_pay_negative", RefusedInput.DirectorPay);

    public static (WithholdingResult? Value, Refusal? Error) Compute(
        WithholdingRules rules, decimal monthlyTaxablePay, WithholdingHousehold household = WithholdingHousehold.AsSingle, WithholdingDependants? dependants = null)
    {
        if (monthlyTaxablePay < 0m)
            return (null, MonthlyPayNegative);

        return (Figures(rules, monthlyTaxablePay, household, dependants), null);
    }

    // The lines of a pay Compute accepts, for GrossToNet, which refuses a negative pay itself.
    internal static WithholdingResult Figures(
        WithholdingRules rules, decimal monthlyTaxablePay, WithholdingHousehold household = WithholdingHousehold.AsSingle, WithholdingDependants? dependants = null)
    {
        dependants ??= WithholdingDependants.None;
        dependants.ThrowIfImpossible();

        var reduction = Money.RoundToCent(OnTable(rules.Reduction, monthlyTaxablePay));
        var annualGross = Money.RoundToCent(monthlyTaxablePay - reduction) * YearParts.Months;
        var flatRateCosts = Math.Min(Money.RoundToCent(annualGross * rules.FlatRateCostsRate.Value), rules.FlatRateCostsMaximum.Value);
        var annualNetTaxable = annualGross - flatRateCosts;
        var (imputedToPartner, taxFreeAmountReduction) = household switch
        {
            WithholdingHousehold.AsSingle => (0m, rules.TaxFreeAmountReduction.Value),
            WithholdingHousehold.PartnerWithoutIncome or WithholdingHousehold.PartnerWithoutIncomeBothAged66 =>
                (Math.Min(Money.RoundToCent(annualNetTaxable * rules.QuotientRate.Value), rules.QuotientMaximumFor(household).Value), rules.JointTaxFreeAmountReduction.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(household), household, "No withholding method for this household."),
        };
        var partnerTax = Money.RoundToCent(OnTable(rules.Scale, imputedToPartner));
        var basicTax = Money.RoundToCent(OnTable(rules.Scale, annualNetTaxable - imputedToPartner));
        var childrenReduction = rules.ChildReductionFor(dependants.CountedChildren);
        var dependantsReduction = dependants.DependantsAged66InCare * rules.DependantAged66InCareReduction.Value
            + dependants.CountedOtherDependants * rules.OtherDependantReduction.Value;
        var singleParentReduction = dependants.SingleParentUnder(household) ? rules.SingleParentReduction.Value : 0m;
        var annualTax = Math.Max(0m, partnerTax + basicTax - taxFreeAmountReduction - childrenReduction - dependantsReduction - singleParentReduction);

        return new WithholdingResult(
            monthlyTaxablePay,
            reduction,
            annualGross,
            flatRateCosts,
            annualNetTaxable,
            basicTax,
            taxFreeAmountReduction,
            annualTax,
            Money.RoundToCent(annualTax / YearParts.Months))
        {
            Household = household,
            ImputedToPartner = imputedToPartner,
            PartnerTax = partnerTax,
            Dependants = dependants,
            ChildrenReduction = childrenReduction,
            DependantsReduction = dependantsReduction,
            SingleParentReduction = singleParentReduction,
        };
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

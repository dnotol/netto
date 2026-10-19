namespace Netto.Core.PersonalTax;

// Every amount is rounded to the cent where the administration's calculation detail shows it
// rounded, which is what makes the results match Tax-Calc to the cent.
public static class PersonalTaxCalculator
{
    public static PersonalTaxResult Compute(
        PersonalTaxRules rules, decimal directorPay, decimal ownContributions, decimal municipalRate)
    {
        var flatRateCosts = FlatRateCosts(rules, directorPay - ownContributions);
        var taxableIncome = Math.Max(0m, directorPay - ownContributions - flatRateCosts);

        // A tax-free amount larger than the income only frees what there is: a single person cannot
        // pass the unused part on to anyone.
        var basicTax = Money.RoundToCent(TaxOnScale(rules.Scale, taxableIncome));
        var taxFreeAmountReduction = Money.RoundToCent(
            rules.TaxFreeAmountRate.Value * Math.Min(rules.TaxFreeAmount.Value, taxableIncome));
        var stateTax = Math.Max(0m, basicTax - taxFreeAmountReduction);

        // The federal government keeps the State tax less the autonomy factor, and Wallonia adds its
        // regional tax on that share. The two rates are set to cancel out, but each part is rounded
        // on its own, so their sum can be a few cents off the State tax.
        var reducedStateTax = Money.RoundToCent(stateTax * (1 - rules.AutonomyFactor.Value));
        var regionalAdditionalTax = Money.RoundToCent(reducedStateTax * rules.RegionalAdditionalRate.Value);
        var incomeTax = reducedStateTax + regionalAdditionalTax;

        return new PersonalTaxResult(
            flatRateCosts,
            taxableIncome,
            basicTax,
            taxFreeAmountReduction,
            stateTax,
            reducedStateTax,
            regionalAdditionalTax,
            incomeTax,
            Money.RoundToCent(incomeTax * municipalRate));
    }

    // A percentage of the pay left after the director's own social contributions, rounded to the
    // cent, up to a yearly maximum.
    internal static decimal FlatRateCosts(PersonalTaxRules rules, decimal payAfterContributions) =>
        Math.Clamp(
            Money.RoundToCent(rules.FlatRateCostsRate.Value * payAfterContributions),
            0m,
            rules.FlatRateCostsMaximum.Value);

    private static decimal TaxOnScale(IReadOnlyList<TaxBracket> scale, decimal taxableIncome)
    {
        var tax = 0m;
        var lowerLimit = 0m;
        foreach (var bracket in scale)
        {
            var upperLimit = bracket.UpperLimit?.Value ?? decimal.MaxValue;
            if (taxableIncome <= lowerLimit)
                break;

            tax += bracket.Rate.Value * (Math.Min(taxableIncome, upperLimit) - lowerLimit);
            lowerLimit = upperLimit;
        }

        return tax;
    }
}

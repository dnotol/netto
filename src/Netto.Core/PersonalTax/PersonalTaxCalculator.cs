namespace Netto.Core.PersonalTax;

// Every amount is rounded to the cent where the administration's calculation detail shows it
// rounded, which is what makes the results match Tax-Calc to the cent.
public static class PersonalTaxCalculator
{
    // The other income is the net income of another category taxed with the pay, the net rent of a
    // home office let to the company (art. 7 and 13), never negative. None unless given. It is
    // globalised with the pay's net professional income, as Tax-Calc T13 and T14 show: the scale, the
    // tax-free amount and so the municipal surcharge apply to their total.
    public static PersonalTaxResult Compute(
        PersonalTaxRules rules, decimal directorPay, decimal ownContributions, decimal municipalRate, decimal otherIncome = 0m)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(otherIncome, 0m);

        var flatRateCosts = FlatRateCosts(rules, directorPay - ownContributions);
        var taxableIncome = Math.Max(0m, directorPay - ownContributions - flatRateCosts);
        var totalNetIncome = taxableIncome + otherIncome;

        // A tax-free amount larger than the income only frees what there is: a single person cannot
        // pass the unused part on to anyone.
        var basicTax = Money.RoundToCent(TaxOnScale(rules.Scale, totalNetIncome));
        var taxFreeAmountReduction = Money.RoundToCent(
            rules.TaxFreeAmountRate.Value * Math.Min(rules.TaxFreeAmount.Value, totalNetIncome));
        var stateTax = Math.Max(0m, basicTax - taxFreeAmountReduction);

        // The federal government keeps the State tax less the autonomy factor, and Wallonia adds its
        // regional tax on that share. The two rates are set to cancel out, but each part is rounded
        // on its own, so their sum can be a few cents off the State tax.
        var reducedStateTax = Money.RoundToCent(stateTax * (1 - rules.AutonomyFactor.Value));
        var regionalAdditionalTax = Money.RoundToCent(reducedStateTax * rules.RegionalAdditionalRate.Value);
        var incomeTax = reducedStateTax + regionalAdditionalTax;

        // The surcharge is a share of the tax before any credit comes off it (circular AFER 39/2008,
        // n° 51), so the credit for low earned income comes last and leaves it as it is. The dividend,
        // which bore its withholding, is not declared (art. 313) and the exempt part is exempt, so the
        // pay is all the professional income and all of it is activity income (art. 289ter, §2, al. 3
        // changes nothing); the rent is not professional income, but it joins the total net income
        // that §1 and §2, al. 4 limit the credit by.
        return new PersonalTaxResult(
            flatRateCosts,
            taxableIncome,
            basicTax,
            taxFreeAmountReduction,
            stateTax,
            reducedStateTax,
            regionalAdditionalTax,
            incomeTax,
            Money.RoundToCent(incomeTax * municipalRate),
            LowEarnedIncomeCredit(rules, taxableIncome, totalNetIncome),
            otherIncome);
    }

    // Art. 289ter, §2 on the activity income alone, as when it is the whole income.
    public static decimal LowEarnedIncomeCredit(PersonalTaxRules rules, decimal activityIncome) =>
        LowEarnedIncomeCredit(rules, activityIncome, activityIncome);

    // Art. 289ter, §2 on the activity income: a straight line up from the floor to the full amount,
    // then down from the end of the full amount to nothing at the ceiling, rounded to the cent as the
    // administration's examples and Tax-Calc round it. The total net income, the activity income and
    // any other, limits it: past the ceiling it is too high for any credit (§1), and past the end of
    // the full amount the credit is at most what the same falling line gives on that total (§2, al. 4),
    // rounded alike (derived: no Tax-Calc run lands there yet).
    public static decimal LowEarnedIncomeCredit(PersonalTaxRules rules, decimal activityIncome, decimal totalNetIncome)
    {
        var credit = rules.LowEarnedIncomeCredit;
        var (full, floor, fullFrom, fullUpTo, ceiling) =
            (credit.FullAmount.Value, credit.Floor.Value, credit.FullFrom.Value, credit.FullUpTo.Value, credit.Ceiling.Value);

        if (totalNetIncome > ceiling)
            return 0m;

        var amount = activityIncome switch
        {
            _ when activityIncome <= floor || activityIncome >= ceiling => 0m,
            _ when activityIncome < fullFrom => full * (activityIncome - floor) / (fullFrom - floor),
            _ when activityIncome <= fullUpTo => full,
            _ => full * (ceiling - activityIncome) / (ceiling - fullUpTo),
        };

        return totalNetIncome > fullUpTo
            ? Math.Min(Money.RoundToCent(amount), Money.RoundToCent(full * (ceiling - totalNetIncome) / (ceiling - fullUpTo)))
            : Money.RoundToCent(amount);
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

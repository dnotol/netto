namespace Netto.Core.PersonalTax;

// Each line as the administration's calculation detail shows it: the basic tax on the scale, the
// reduction for the tax-free amount, the State tax left, its federal share after the autonomy
// factor, the regional additional tax on that share, their sum, the municipal surcharge on it, and
// the tax credit for low earned income, which comes off after them and is refunded beyond them.
// TaxableIncome is the pay's, its net professional income; OtherIncome the net income taxed with it,
// the rent of a home office, and the scale's lines are on their total. IncomeTaxedApart is an income
// taxed apart at its own rate, a copyright royalty within its limits, and TaxedApart that tax, which
// joins the State tax the autonomy factor and the regional tax split (Tax-Calc T16): from
// ReducedStateTax on, the lines are on everything.
public sealed record PersonalTaxResult(
    decimal FlatRateCosts,
    decimal TaxableIncome,
    decimal BasicTax,
    decimal TaxFreeAmountReduction,
    decimal StateTax,
    decimal ReducedStateTax,
    decimal RegionalAdditionalTax,
    decimal IncomeTax,
    decimal MunicipalSurcharge,
    decimal LowEarnedIncomeCredit,
    decimal OtherIncome = 0m,
    decimal IncomeTaxedApart = 0m,
    decimal TaxedApart = 0m)
{
    public decimal TotalNetIncome => TaxableIncome + OtherIncome + IncomeTaxedApart;
}

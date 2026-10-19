namespace Netto.Core.PersonalTax;

// Each line as the administration's calculation detail shows it: the basic tax on the scale, the
// reduction for the tax-free amount, the State tax left, its federal share after the autonomy
// factor, the regional additional tax on that share, their sum, the municipal surcharge on it, and
// the tax credit for low earned income, which comes off after them and is refunded beyond them.
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
    decimal LowEarnedIncomeCredit);

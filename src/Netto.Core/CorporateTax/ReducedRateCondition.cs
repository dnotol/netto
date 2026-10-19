namespace Netto.Core.CorporateTax;

// Which part of art. 215 decided the company's rate.
public enum ReducedRateCondition
{
    // Not declared a small company the reduced rate is open to: 25% on everything.
    NotSmallCompany,

    // Within its first four tax periods: the pay condition does not apply yet.
    FirstFourTaxPeriods,

    // A director's pay reaches the minimum.
    MinimumPayMet,

    // The pay is under the minimum, but the taxable income does not exceed it.
    TaxableIncomeNotAbovePay,

    // The pay is under the minimum and the taxable income exceeds it: 25% on everything.
    MinimumPayNotMet,
}

public static class ReducedRateConditionExtensions
{
    public static bool AppliesReducedRate(this ReducedRateCondition condition) => condition is
        ReducedRateCondition.FirstFourTaxPeriods or ReducedRateCondition.MinimumPayMet or ReducedRateCondition.TaxableIncomeNotAbovePay;
}

namespace Netto.Core.CorporateTax;

// The minimum director pay and the share of the total pay flat-rate benefits may reach are the ones
// the condition was tested against, from that year's rules; DirectorPay and FlatRateBenefits the pay
// and the benefits in it that it tested.
public sealed record CorporateTaxResult(
    decimal TaxableIncome,
    ReducedRateCondition Condition,
    decimal MinimumDirectorPay,
    decimal CorporateTax,
    decimal DirectorPay,
    decimal FlatRateBenefits,
    decimal ExcessBenefitsShare)
{
    // What art. 215 al. 3, 4° found of the pay on its own: MinimumPayMet, TaxableIncomeNotAbovePay or
    // MinimumPayNotMet, also where benefits beyond their share decided the rate (Condition). Null where
    // the pay is not tested: a company the reduced rate is not open to, or in its first four tax periods.
    public ReducedRateCondition? PayTest { get; init; }

    // Whether the flat-rate benefits stay within their share of the total pay, the benefits in it
    // (al. 3, 5°), strictly. Null without such benefits, or for a company the reduced rate is not open to.
    public bool? BenefitsWithinShare { get; init; }
}

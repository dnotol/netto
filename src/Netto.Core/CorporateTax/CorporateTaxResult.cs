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
    decimal ExcessBenefitsShare);

namespace Netto.Core.CorporateTax;

// The minimum director pay is the one the condition was tested against, from that year's rules.
public sealed record CorporateTaxResult(decimal TaxableIncome, ReducedRateCondition Condition, decimal MinimumDirectorPay, decimal CorporateTax);

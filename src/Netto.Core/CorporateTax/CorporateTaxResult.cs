namespace Netto.Core.CorporateTax;

public sealed record CorporateTaxResult(decimal TaxableIncome, ReducedRateCondition Condition, decimal CorporateTax);

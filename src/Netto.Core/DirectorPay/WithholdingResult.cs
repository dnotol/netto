namespace Netto.Core.DirectorPay;

// Each line of the key formula for one month's pay, rounded where the formula rounds it (n° 3):
// the reduction in place of social deductions (n° 7, 1°); the annual gross, the pay less the
// reduction times 12 (n° 7, 2°); the flat-rate costs on it (n° 8, 2°); the annual net taxable, the
// gross less the costs; the basic tax on the scale (n° 10); the tax on the tax-free amount taken
// off it (n° 11, a); the annual tax that leaves, never below zero (n° 21); and its twelfth, the
// month's withholding (n° 16).
public sealed record WithholdingResult(
    decimal MonthlyTaxablePay,
    decimal Reduction,
    decimal AnnualGross,
    decimal FlatRateCosts,
    decimal AnnualNetTaxable,
    decimal BasicTax,
    decimal TaxFreeAmountReduction,
    decimal AnnualTax,
    decimal MonthlyWithholding);

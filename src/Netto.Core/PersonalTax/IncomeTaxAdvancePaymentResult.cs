namespace Netto.Core.PersonalTax;

// What the withholding and the advance payments leave on the income tax on director pay. The base is
// the tax at 106% less what was withheld and the tax credits, never below zero. Each payment counts
// first towards what avoids the surcharge, earliest first, up to the smallest amount whose credit
// covers what is left of it (Needed); its credit is that part's. The surcharge before the 90% is
// what the credits leave; Surcharge is what is due, nothing when the 90% is under the minimum or
// the minimum share of the base (BelowMinimum). What each payment has beyond its needed part earns
// its deadline's bonus, earliest first, on a base of at most the tax at 106% less the withholding,
// the credits and the needed payments. LastDeadlineTopUp is the smallest payment more by the last
// deadline after which no surcharge is due. A starter owes none and needs no payment to avoid it.
public sealed record IncomeTaxAdvancePaymentResult(
    decimal TaxAt106Percent,
    decimal Base,
    decimal SurchargeBeforeCredits,
    IReadOnlyList<decimal> Needed,
    IReadOnlyList<decimal> Credits,
    decimal SurchargeBeforeKeptRate,
    decimal Surcharge,
    bool BelowMinimum,
    IReadOnlyList<decimal> Bonuses,
    decimal Bonus,
    decimal LastDeadlineTopUp,
    bool Exempt);

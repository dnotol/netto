using Netto.Core.Simulation;

namespace Netto.Core.PersonalTax;

// What the director's income tax on the year's pay was paid with before the assessment: what the
// company withheld on the pay, and what the director paid in advance by each of the income year's
// deadlines. Starter is what the user declares when the year falls in the first three years of a
// first self-employed activity (CIR 92 art. 164): no surcharge is then due, though the bonus still
// is. Nothing withheld or prepaid unless given.
public sealed record TaxPrepayments(decimal Withheld, AdvancePayments Paid, bool Starter = false)
{
    public static TaxPrepayments None { get; } = new(0m, AdvancePayments.None);
}

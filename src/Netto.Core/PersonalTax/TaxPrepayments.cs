using Netto.Core.Simulation;

namespace Netto.Core.PersonalTax;

// What the director's income tax on the year's pay was paid with before the assessment: what the
// company withheld on the pay, and what the director paid in advance by each of the income year's
// deadlines. Withheld is the amount the user says was withheld, or null for what the company
// withholds by the key formula, the sum of the twelve months' withholding on the pay, each by the rules in force when it is paid, which GrossToNet
// derives from the pay and sets before the surcharge is computed (decided 2026-10-20). Starter is
// what the user declares when the year falls in the first three years of a first self-employed
// activity (CIR 92 art. 164): no surcharge is then due, though the bonus still is. None is nothing
// withheld and nothing prepaid; AsWithheld is the formula's withholding and nothing prepaid.
public sealed record TaxPrepayments(decimal? Withheld, AdvancePayments Paid, bool Starter = false)
{
    public static TaxPrepayments None { get; } = new(0m, AdvancePayments.None);

    public static TaxPrepayments AsWithheld { get; } = new(null, AdvancePayments.None);
}

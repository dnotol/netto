using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// The balance on the director's assessment (avertissement-extrait de rôle) for the year's pay:
// FinalTax is what the assessment charges, the income tax with the municipal surcharge, less the tax
// credits for low earned income and for children, plus the advance-payment surcharge and less the
// bonus; Withheld is what the company withheld on the pay over the year, the amount the user gave
// (WithheldGiven) or
// what the key formula withholds, and AdvancePayments what the director paid in advance on its tax,
// both set against it (CIR 92 art. 304, §2); Balance is what is left, to pay when positive, refunded
// when negative. Source is the article the balance follows.
public sealed record Assessment(
    decimal FinalTax,
    decimal Withheld,
    bool WithheldGiven,
    decimal AdvancePayments,
    decimal Balance,
    Source Source)
{
    // What the company withheld at source on movable income it paid the director besides the pay, a
    // copyright royalty's withholding, credited too (art. 279) and refunded beyond the tax (art. 304,
    // §2); nothing without one.
    public decimal WithheldBesidesPay { get; init; }
}

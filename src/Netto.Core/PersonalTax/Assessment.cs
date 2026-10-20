using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// The balance on the director's assessment (avertissement-extrait de rôle) for the year's pay:
// FinalTax is what the assessment charges, the income tax with the municipal surcharge, less the tax
// credit for low earned income, plus the advance-payment surcharge and less the bonus; Withheld is
// what the company withheld on the pay over the year and AdvancePayments what the director paid in
// advance on its tax, both set against it (CIR 92 art. 304, §2); Balance is what is left, to pay when
// positive, refunded when negative. Source is the article the balance follows.
public sealed record Assessment(
    decimal FinalTax,
    decimal Withheld,
    decimal AdvancePayments,
    decimal Balance,
    Source Source);

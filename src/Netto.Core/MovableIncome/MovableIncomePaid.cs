using Netto.Core.PersonalTax;

namespace Netto.Core.MovableIncome;

// What the company pays the director as movable income, whatever it is paid for: the amount it pays
// and deducts in full (Amount), the part the personal tax takes apart at its own rate (TaxedApart), the
// part beyond the law's limits that is taxed as the director's pay (TaxedAsPay: in the contributions'
// base, on the scale and in the advance-payment surcharge's base, never on the payslip), and what the
// company withheld at source on all of it, which the assessment credits and refunds beyond the tax
// (Withheld). The amount is the two parts. Plain figures: the module that values the income produces
// them, and YearSimulation composes them beside the other levers, so the layers only ever see amounts.
public sealed record MovableIncomePaid(decimal Amount, IncomeTaxedApart? TaxedApart, decimal TaxedAsPay, decimal Withheld)
{
    public static MovableIncomePaid None { get; } = new(0m, null, 0m, 0m);

    // The part taxed apart, nothing without one.
    public decimal TaxedApartIncome => TaxedApart?.Income ?? 0m;
}

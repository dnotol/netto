namespace Netto.Core.CorporateTax;

// The surcharge the year's advance payments leave: the tax times the surcharge rate, the credit each
// payment earns by its deadline, in their order, and what is left, never below zero, since a company
// gets no bonus for paying more. TopUps is what more paid by each deadline, in their order, cancels
// what is left, nothing when nothing is; the last deadline's is LastDeadlineTopUp. A small company in
// its first three financial years is exempt, and owes nothing whatever it paid.
public sealed record AdvancePaymentResult(
    decimal SurchargeBeforeCredits,
    IReadOnlyList<decimal> Credits,
    decimal Surcharge,
    IReadOnlyList<decimal> TopUps,
    bool Exempt)
{
    public decimal LastDeadlineTopUp => TopUps[^1];
}

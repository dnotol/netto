namespace Netto.Core.CorporateTax;

// The surcharge the year's advance payments leave: the tax times the surcharge rate, the credit each
// payment earns by its deadline, in their order, and what is left, never below zero, since a company
// gets no bonus for paying more. LastDeadlineTopUp is what more paid by the last deadline cancels
// what is left, nothing when nothing is. A small company in its first three financial years is
// exempt, and owes nothing whatever it paid.
public sealed record AdvancePaymentResult(
    decimal SurchargeBeforeCredits,
    IReadOnlyList<decimal> Credits,
    decimal Surcharge,
    decimal LastDeadlineTopUp,
    bool Exempt);

using Netto.Core.Rules;

namespace Netto.Core.CorporateTax;

// The surcharge on the corporate tax that advance payments did not cover (CIR 92 art. 218, applying
// art. 157 to 168 of personal tax), for one income year: the tax times the surcharge multiplier of
// the reference rate, less each payment times its deadline's multiplier of it (art. 159). For
// companies the reference rate is at least 3%, and there is no 90% limit, no 106% base and no
// de-minimis. Source is art. 218 itself, which the step cites as its rule.
public sealed record AdvancePaymentRules(
    Source Source,
    RuleAmount ReferenceRate,
    RuleAmount SurchargeMultiplier,
    IReadOnlyList<AdvancePaymentDeadline> Deadlines)
{
    public decimal SurchargeRate => SurchargeMultiplier.Value * ReferenceRate.Value;

    public IReadOnlyList<RuleAmount> Amounts => [ReferenceRate, SurchargeMultiplier, .. Deadlines.Select(deadline => deadline.CreditMultiplier)];
}

// A payment made by the date earns a credit of its multiplier of the reference rate.
public sealed record AdvancePaymentDeadline(RuleDate Date, RuleAmount CreditMultiplier)
{
    public decimal CreditRate(AdvancePaymentRules rules) => CreditMultiplier.Value * rules.ReferenceRate.Value;
}

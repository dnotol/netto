using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// The marital quotient (CIR 92 art. 87 and 88) for one income year: in a joint assessment, a partner
// whose professional income is less than Rate of the two partners' together is attributed a share of
// the other's, up to Cap, or CapBothAged66 when both partners were 66 on 1 January of the tax year (the
// same amount before the law of 15.07.2026 told them apart). Source is the text the step cites as its
// rule.
public sealed record MaritalQuotientRules(Source Source, RuleAmount Rate, RuleAmount Cap, RuleAmount CapBothAged66)
{
    public IReadOnlyList<RuleAmount> Amounts => [Rate, Cap, CapBothAged66];

    public RuleAmount CapFor(bool bothAged66) => bothAged66 ? CapBothAged66 : Cap;
}

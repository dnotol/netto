using Netto.Core.Rules;

namespace Netto.Core.Copyright;

// The flat-rate costs a copyright income within the limits is taxed less, for one income year: its net
// is the gross less the costs of acquiring or keeping it (CIR 92, art. 22 §3, NetIncome), which, without
// proof of real costs, are a flat rate of the gross (AR/CIR 92, art. 3, Who) that art. 4 raises for
// copyright (Bands): FirstBandRate of the income up to FirstBandCeiling, SecondBandRate of the part from
// there up to SecondBandCeiling, nothing beyond. From 2026 they are only for income from activities an
// attestation du travail des arts covers when the income is paid (the programme law of 30.05.2026,
// Attestation, in art. 3 al. 2 and art. 4 al. 2); before, every author took them.
public sealed record CopyrightFlatRateCosts(
    Source NetIncome,
    Source Who,
    Source Bands,
    Source? Attestation,
    RuleAmount FirstBandRate,
    RuleAmount FirstBandCeiling,
    RuleAmount SecondBandRate,
    RuleAmount SecondBandCeiling)
{
    public bool NeedsAttestation => Attestation is not null;

    public IReadOnlyList<RuleAmount> Amounts => [FirstBandRate, FirstBandCeiling, SecondBandRate, SecondBandCeiling];
}

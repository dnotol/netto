using Netto.Core.Rules;

namespace Netto.Core.DirectorPay;

// One band of a table of annex III to the AR/CIR 92 (the director's monthly reduction, the basic
// scale): what the table gives at the band's lower edge, the previous band's upper limit, plus its
// rate on the slice over that edge, up to its own upper limit. A flat band has no rate, the scale's
// first band no base (nothing lies below it), and the last band of a table no upper limit. The base
// is the table's own figure, never the previous band taken to its limit: 1,426.68 where 385 plus
// 21.50% of 4,845 gives 1,426.675, a half cent that rounds the other way one pay in a hundred.
public sealed record WithholdingBand(RuleAmount? Base, RuleAmount? Rate, RuleAmount? UpperLimit)
{
    public IEnumerable<RuleAmount> Amounts => new RuleAmount?[] { Base, Rate, UpperLimit }.OfType<RuleAmount>();
}

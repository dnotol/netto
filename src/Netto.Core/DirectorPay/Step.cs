using Netto.Core.Rules;

namespace Netto.Core.DirectorPay;

// One step from gross pay to net in pocket: the rule it applies, the amount it gives, the official
// text behind it and the amounts of that income year it used.
public sealed record Step(string RuleKey, Track Track, decimal Amount, Source Source, IReadOnlyList<RuleAmount> AmountsUsed);

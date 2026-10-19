using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// The tax credit for low earned income (CIR 92 art. 289ter) for one income year. Nothing when the
// total net income exceeds the ceiling; otherwise, on the activity income, nothing up to the floor,
// rising to the full amount at FullFrom, the full amount up to FullUpTo, then falling to nothing at
// the ceiling. Source is the article itself, which the step cites as its rule.
public sealed record LowEarnedIncomeCreditRules(
    Source Source,
    RuleAmount FullAmount,
    RuleAmount Floor,
    RuleAmount FullFrom,
    RuleAmount FullUpTo,
    RuleAmount Ceiling)
{
    public IReadOnlyList<RuleAmount> Amounts => [FullAmount, Floor, FullFrom, FullUpTo, Ceiling];
}

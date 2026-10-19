using Netto.Core.Rules;

namespace Netto.Core.Simulation;

// One step of the year, from the company's profit to the director's pocket: the rule it applies, the
// layer that applies it, the money it applies to, what it means for the pocket, the amount it gives,
// the official text behind it and the amounts of that income year it used. A window selects and
// orders steps by these tags; the engine knows nothing about windows.
public sealed record Step(
    string RuleKey,
    Layer Layer,
    TaxableEvent Event,
    Track Track,
    decimal Amount,
    Source Source,
    IReadOnlyList<RuleAmount> AmountsUsed);

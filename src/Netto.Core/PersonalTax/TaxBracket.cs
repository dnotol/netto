using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// One slice of the progressive scale: its rate applies from the previous bracket's upper limit
// (0 for the first) up to this one's. The top bracket has no upper limit.
public sealed record TaxBracket(RuleAmount Rate, RuleAmount? UpperLimit);

namespace Netto.Core.Rules;

// One amount of an income year's rules (a rate, a ceiling, a bracket edge), with its source. Rates
// are fractions: 20.50% is 0.205.
public sealed record RuleAmount(string Key, decimal Value, Source Source);

namespace Netto.Core.Rules;

// One date of an income year's rules (a deadline), with its source. A calendar date, as the law and
// the administration publish it, with no time of day.
public sealed record RuleDate(string Key, DateOnly Value, Source Source);

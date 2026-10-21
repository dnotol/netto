namespace Netto.Core.Rules;

// One condition of a lever the user confirms, because Netto cannot see it (an agreement, the days
// worked, the share of a home let): a stable key the page gives its words, the texts it comes from,
// and the amounts those words name.
public sealed record LeverCondition(string Key, IReadOnlyList<Source> Sources)
{
    public IReadOnlyList<RuleAmount> Amounts { get; init; } = [];
}

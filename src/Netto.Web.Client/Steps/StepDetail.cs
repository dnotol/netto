using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Web.Client.Steps;

// One step as a module card lists it under « where each figure comes from »: its amount, the lines
// of its computation when the rule is a chain of them (the key formula's), then each official text
// behind it with the amounts of the year it used. Public because StepDetails, a component, takes it
// as a parameter.
public sealed record StepDetail(string RuleKey, decimal Amount, IReadOnlyList<SourceGroup> Sources)
{
    public IReadOnlyList<StepLine> Lines { get; init; } = [];

    // A step cited for its rule alone: another line of the card states its amount, or it is nothing, and its
    // heading draws no amount.
    public bool WithoutAmount { get; init; }

    // Money that comes back: the refund on exempt dividends, the tax credit for low earned income. The engine
    // negates what comes back, so a refund or a credit of nothing is a negative zero, which is not less than
    // zero: the sign bit says so, as it does on the drains (Drain.PaidOutTo, GrossToNetView).
    public bool ComesBack => decimal.IsNegative(Amount);

    // The amount as the card shows it: an addition when it comes back, else the amount.
    public string AmountShown => ComesBack ? Formats.Addition(-Amount) : Formats.Euros(Amount);

    public static StepDetail From(Step step) => From(step.RuleKey, step.Amount, step.Source, step.AmountsUsed, step.AlsoCites);

    // The step's own source first, even with no amount of its own, then each other source in the
    // order its amounts appear, then the other texts the rule rests on.
    public static StepDetail From(string ruleKey, decimal amount, Source source, IReadOnlyList<RuleAmount> amountsUsed, IReadOnlyList<Source>? alsoCites = null) =>
        new(ruleKey, amount, amountsUsed.Select(used => used.Source)
            .Prepend(source)
            .Concat(alsoCites ?? [])
            .Distinct()
            .Select(each => new SourceGroup(each, amountsUsed.Where(used => used.Source == each).ToList()))
            .ToList());
}

public sealed record SourceGroup(Source Source, IReadOnlyList<RuleAmount> Amounts);

// One line of a step's computation, in the rule's order: the engine's figure under its key.
public sealed record StepLine(string Key, decimal Amount);

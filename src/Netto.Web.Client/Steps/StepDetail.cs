using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Web.Client.Steps;

// One step as a window lists it under « where each figure comes from »: its amount, then each
// official text behind it with the amounts of the year it used. Public because StepDetails, a
// component, takes it as a parameter.
public sealed record StepDetail(string RuleKey, decimal Amount, IReadOnlyList<SourceGroup> Sources)
{
    // The step's own source first, even with no amount of its own, then each other source in the
    // order its amounts appear.
    public static StepDetail From(Step step) =>
        new(step.RuleKey, step.Amount, step.AmountsUsed.Select(amount => amount.Source)
            .Prepend(step.Source)
            .Distinct()
            .Select(source => new SourceGroup(source, step.AmountsUsed.Where(amount => amount.Source == source).ToList()))
            .ToList());
}

public sealed record SourceGroup(Source Source, IReadOnlyList<RuleAmount> Amounts);

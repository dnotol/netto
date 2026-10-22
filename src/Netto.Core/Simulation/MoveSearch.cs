namespace Netto.Core.Simulation;

// One change Netto recommends to the user's current setup: what it changes in the scenario. A lever
// adds its own kind of move, and the finder that proposes it.
public abstract record Move
{
    public abstract Scenario Apply(Scenario scenario);
}

// The director pay set to Pay, the split's best.
public sealed record PayMove(decimal Pay) : Move
{
    public override Scenario Apply(Scenario scenario) => scenario with { DirectorPay = Pay };
}

// A move in the plan with what it adds to the pocket on top of the moves before it.
public sealed record PlannedMove(Move Move, decimal Gain);

// The moves from the user's current setup, Today, in the order they were taken, each adding what it
// adds after the ones before it, so the gains sum to what the whole plan leaves in the pocket less
// what Today leaves.
public sealed record MovePlan(decimal TodayInPocket, IReadOnlyList<PlannedMove> Moves, decimal PlanInPocket)
{
    public decimal Gain => PlanInPocket - TodayInPocket;
}

// Proposes the best move of one kind from a scenario, or none.
internal interface IMoveFinder
{
    Move? Find(Scenario scenario);
}

// The moves from Today, found greedily: every finder proposes its move from where the plan stands,
// the one that adds the most to the pocket is taken, and the others are asked again on top of it,
// until no move adds a euro or more. A move that leaves the company less than the reserve is never
// taken, nor one the engine refuses. Every gain is the difference of two runs of the year
// (YearSimulation.Run), never arithmetic of its own.
public static class MoveSearch
{
    // Less than this is not worth a move.
    private const decimal SmallestGain = 1m;

    // Moves that keep undoing each other stop here.
    private const int MaximumRounds = 10;

    public static (MovePlan? Value, string? Error) Run(Scenario today) => Run(today, [new SplitMoveFinder()]);

    internal static (MovePlan? Value, string? Error) Run(Scenario today, IReadOnlyList<IMoveFinder> finders)
    {
        // Today's inputs are refused, if at all, before any move.
        var (year, error) = YearSimulation.Run(today);
        if (year is null)
            return (null, error);

        var current = today;
        var inPocket = year.NetInPocket;
        var moves = new List<PlannedMove>();
        for (var round = 0; round < MaximumRounds; round++)
        {
            var best = finders
                .Select(finder => finder.Find(current))
                .OfType<Move>()
                .Select(move => (Move: move, After: move.Apply(current)))
                .Select(candidate => (candidate.Move, candidate.After, Year: YearSimulation.Run(candidate.After).Value))
                .Where(candidate => candidate.Year is { } after && after.MaximumDividend >= candidate.After.Reserve)
                .Select(candidate => (candidate.Move, candidate.After, Gain: candidate.Year!.NetInPocket - inPocket))
                .Where(candidate => candidate.Gain >= SmallestGain)
                .Aggregate(((Move Move, Scenario After, decimal Gain)?)null, (taken, candidate) => taken is { } kept && kept.Gain >= candidate.Gain ? kept : candidate);
            if (best is not { } move)
                break;

            moves.Add(new PlannedMove(move.Move, move.Gain));
            current = move.After;
            inPocket += move.Gain;
        }

        return (new MovePlan(year.NetInPocket, moves, inPocket), null);
    }
}

// The split's best pay, when it is not the pay already set.
internal sealed class SplitMoveFinder : IMoveFinder
{
    public Move? Find(Scenario scenario) =>
        SplitSearch.Run(scenario).Value?.Best is { } best && best.Pay != scenario.DirectorPay ? new PayMove(best.Pay) : null;
}

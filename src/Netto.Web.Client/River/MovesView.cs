using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the moves card draws: the engine's moves from the plan starred « Today » (MoveSearch), or why
// there are none. Found once per Today, since the search runs the split at every move; which moves
// the river already holds is read from the river at each change.
internal sealed record MovesView(MovesState State, string? Error, decimal TodayPay, IReadOnlyList<PlannedMove> Moves, decimal Total)
{
    public static MovesView Of(RiverInputs? today)
    {
        if (today is null)
            return new MovesView(MovesState.NoToday, null, 0m, [], 0m);
        if (today.FindMoves() is not { } found)
            return new MovesView(MovesState.StartsAtPay, null, 0m, [], 0m);
        if (found.Value is not { } plan)
            return new MovesView(MovesState.Refused, found.Error, 0m, [], 0m);

        var pay = Math.Min(today.DirectorPay.Value, today.PayCeiling ?? 0m);
        return plan.Moves.Count == 0
            ? new MovesView(MovesState.AlreadyBest, null, pay, [], 0m)
            : new MovesView(MovesState.Moves, null, pay, plan.Moves, plan.Gain);
    }

    // Whether the river holds the move's values already.
    public static bool Applied(PlannedMove move, RiverInputs river) => move.Move switch
    {
        PayMove pay => river.DirectorPay.Value == pay.Pay,
        _ => false,
    };

    // The river with the move's values, everything else as it is.
    public static RiverInputs Apply(PlannedMove move, RiverInputs river) => move.Move switch
    {
        PayMove pay => river.WithDirectorPay(pay.Pay),
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the river cannot apply."),
    };
}

internal enum MovesState
{
    // No plan is starred « Today », the setup moves are measured from.
    NoToday,

    // Today skips the company's year: the split, and so every move, starts from it.
    StartsAtPay,

    // The engine refuses an amount of Today (Error).
    Refused,

    // No move adds a euro to Today.
    AlreadyBest,

    Moves,
}

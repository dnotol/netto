using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the moves card draws: the engine's moves from the plan starred « Today » (MoveSearch), or why
// there are none. Found once per Today, since the search runs the split at every move; which moves
// the river already holds is read from the river at each change.
internal sealed record MovesView(MovesState State, string? Error, RiverInputs? Today, decimal TodayPay, IReadOnlyList<PlannedMove> Moves, decimal Total)
{
    // On that date, which decides the deadline a payment is proposed by.
    public static MovesView Of(RiverInputs? today, DateOnly date)
    {
        if (today is null)
            return new MovesView(MovesState.NoToday, null, null, 0m, [], 0m);
        if (today.FindMoves(date) is not { } found)
            return new MovesView(MovesState.StartsAtPay, null, today, 0m, [], 0m);
        if (found.Value is not { } plan)
            return new MovesView(MovesState.Refused, found.Error, today, 0m, [], 0m);

        var pay = Math.Min(today.DirectorPay.Value, today.PayCeiling ?? 0m);
        return plan.Moves.Count == 0
            ? new MovesView(MovesState.AlreadyBest, null, today, pay, [], 0m)
            : new MovesView(MovesState.Moves, null, today, pay, plan.Moves, plan.Gain);
    }

    // Whether the river holds the move's values already: its pay, or Today's payment by the deadline
    // with the move's amount on top.
    public bool Applied(PlannedMove move, RiverInputs river) => move.Move switch
    {
        PayMove pay => river.DirectorPay.Value == pay.Pay,
        CompanyAdvancePaymentMove payment => river.AdvancePayments.ByDeadline[payment.Deadline].Value == Planned(payment),
        DirectorAdvancePaymentMove payment => river.TaxPrepayments.Paid.ByDeadline[payment.Deadline].Value == Planned(payment),
        _ => false,
    };

    // The river with the move's values, everything else as it is.
    public RiverInputs Apply(PlannedMove move, RiverInputs river) => move.Move switch
    {
        PayMove pay => river.WithDirectorPay(pay.Pay),
        CompanyAdvancePaymentMove payment => river.WithAdvancePayment(payment.Deadline, Planned(payment)),
        DirectorAdvancePaymentMove payment => river.WithTaxAdvancePayment(payment.Deadline, Planned(payment)),
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the river cannot apply."),
    };

    private decimal Planned(CompanyAdvancePaymentMove payment) => (Today?.AdvancePayments.ByDeadline[payment.Deadline].Value ?? 0m) + payment.Amount;

    private decimal Planned(DirectorAdvancePaymentMove payment) => (Today?.TaxPrepayments.Paid.ByDeadline[payment.Deadline].Value ?? 0m) + payment.Amount;
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

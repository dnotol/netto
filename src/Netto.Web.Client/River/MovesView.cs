using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the Optimise view draws: the engine's moves (MoveSearch) that retune the plan on screen, From,
// or why there are none, with what that plan leaves in the pocket and what it leaves with every move. The
// page searches once per plan and date (MovesSource), since the search runs the split at every move; while
// the plan on screen is not the one searched, the view is searching, so no move found for another plan is
// ever drawn or applied.
internal sealed record MovesView(
    MovesState State,
    Refusal? Error,
    RiverInputs From,
    decimal FromPay,
    IReadOnlyList<PlannedMove> Moves,
    decimal Total,
    decimal FromInPocket,
    decimal PlanInPocket)
{
    // The moves from that plan on that date, searched here: for the tests, the page searching once painted.
    public static MovesView Of(RiverInputs from, DateOnly date) => Of(from, from.FindMoves(date));

    // While the page has not searched the moves from that plan yet, which it does once it has painted
    // (MovesSource): no figure and no move.
    public static MovesView Searching(RiverInputs from) => new(MovesState.Searching, null, from, 0m, [], 0m, 0m, 0m);

    // From what the search found for that plan (RiverInputs.FindMoves), none for one that starts at the pay.
    public static MovesView Of(RiverInputs from, (MovePlan? Value, Refusal? Error)? found)
    {
        if (found is not { } result)
            return new MovesView(MovesState.StartsAtPay, null, from, 0m, [], 0m, 0m, 0m);
        if (result.Value is not { } plan)
            return new MovesView(MovesState.Refused, result.Error, from, 0m, [], 0m, 0m, 0m);

        var pay = from.PayUsed;
        return plan.Moves.Count == 0
            ? new MovesView(MovesState.AlreadyBest, null, from, pay, [], 0m, plan.TodayInPocket, plan.TodayInPocket)
            : new MovesView(MovesState.Moves, null, from, pay, plan.Moves, plan.Gain, plan.TodayInPocket, plan.PlanInPocket);
    }

    // What the pocket holds once the move at that place in the list is taken: what the plan leaves, with
    // the gains of the moves up to it and its own, so the last move's is what every move leaves. It is the
    // moves' running account, not a run of the moves above alone: a line a later round topped up
    // (Move.After) holds that round's gain too, measured on top of the moves between.
    public decimal InPocketAfter(int index) => FromInPocket + Moves.Take(index + 1).Sum(move => move.Gain);

    // The plan with the move's values, everything else as it is. Every move retunes a value the plan holds:
    // none adds a lever (MoveSearch.Run). A royalty that follows the pay holds a pay above what the company
    // pays beside it there, as the move does, in the river's own run.
    public RiverInputs Apply(PlannedMove move, RiverInputs river) => move.Move switch
    {
        PayMove pay => river.WithDirectorPay(pay.Pay),
        CompanyAdvancePaymentMove payment => river.WithAdvancePayment(payment.Deadline, Planned(payment)),
        DirectorAdvancePaymentMove payment => river.WithTaxAdvancePayment(payment.Deadline, Planned(payment)),
        EcoVouchersMove eco => river.WithEcoVouchers(eco.Amount),
        SportCultureVouchersMove sport => river.WithSportCultureVouchers(sport.Amount),
        RentMove rent => river.WithRent(rent.Rent),
        CopyrightFollowsPayMove => river.WithRoyaltyFollowingPay(),
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the river cannot apply."),
    };

    // The plan with every move's values, in the order they were taken: the plan the search ends on, whose
    // pocket is PlanInPocket.
    public RiverInputs ApplyAll(RiverInputs river) => Moves.Aggregate(river, (applied, move) => Apply(move, applied));

    // A payment's line holds what the moves add by its deadline: on top of what the plan pays there.
    private decimal Planned(CompanyAdvancePaymentMove payment) => From.AdvancePayments.ByDeadline[payment.Deadline].Value + payment.Amount;

    private decimal Planned(DirectorAdvancePaymentMove payment) => From.TaxPrepayments.Paid.ByDeadline[payment.Deadline].Value + payment.Amount;
}

internal enum MovesState
{
    // The page has not searched the moves from the plan on screen yet: it does once it has painted (MovesSource).
    Searching,

    // The plan skips the company's year: the split, and so every move, starts from it.
    StartsAtPay,

    // The engine refuses an amount of the plan (Error).
    Refused,

    // No move adds a euro to the plan.
    AlreadyBest,

    Moves,
}

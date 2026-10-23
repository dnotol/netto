using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the Optimisation tab draws: the engine's moves from the plan starred « Today » (MoveSearch), or why
// there are none, with what Today leaves in the pocket and what the plan leaves with every move. Found
// once per Today, since the search runs the split at every move; which moves the river already holds is
// read from the river at each change.
internal sealed record MovesView(
    MovesState State,
    string? Error,
    RiverInputs? Today,
    decimal TodayPay,
    IReadOnlyList<PlannedMove> Moves,
    decimal Total,
    decimal TodayInPocket,
    decimal PlanInPocket)
{
    // On that date, which decides the deadline a payment is proposed by.
    public static MovesView Of(RiverInputs? today, DateOnly date) => Of(today, today?.FindMoves(date));

    // From what the search found for Today (RiverInputs.FindMoves), none without a Today or for a Today
    // that starts at the pay: the page searches once per Today, and the tab draws the view of it.
    public static MovesView Of(RiverInputs? today, (MovePlan? Value, string? Error)? found)
    {
        if (today is null)
            return new MovesView(MovesState.NoToday, null, null, 0m, [], 0m, 0m, 0m);
        if (found is not { } result)
            return new MovesView(MovesState.StartsAtPay, null, today, 0m, [], 0m, 0m, 0m);
        if (result.Value is not { } plan)
            return new MovesView(MovesState.Refused, result.Error, today, 0m, [], 0m, 0m, 0m);

        var pay = Math.Min(today.DirectorPay.Value, today.PayCeiling ?? 0m);
        return plan.Moves.Count == 0
            ? new MovesView(MovesState.AlreadyBest, null, today, pay, [], 0m, plan.TodayInPocket, plan.TodayInPocket)
            : new MovesView(MovesState.Moves, null, today, pay, plan.Moves, plan.Gain, plan.TodayInPocket, plan.PlanInPocket);
    }

    // What the pocket holds once the move at that place in the list is taken: Today's, with the gains of
    // the moves up to it and its own, so the last move's is the plan's. It is the plan's running account,
    // not a run of the moves above alone: a line a later round topped up (Move.After) holds that round's
    // gain too, measured on top of the moves between.
    public decimal InPocketAfter(int index) => TodayInPocket + Moves.Take(index + 1).Sum(move => move.Gain);

    // Whether the river holds the move's values already: its pay, Today's payment by the deadline with
    // the move's amount on top, a lever's amount with its module in the plan, or a royalty in the plan
    // that follows the pay.
    public bool Applied(PlannedMove move, RiverInputs river) => move.Move switch
    {
        PayMove pay => river.DirectorPay.Value == pay.Pay,
        CompanyAdvancePaymentMove payment => river.AdvancePayments.ByDeadline[payment.Deadline].Value == Planned(payment),
        DirectorAdvancePaymentMove payment => river.TaxPrepayments.Paid.ByDeadline[payment.Deadline].Value == Planned(payment),
        EcoVouchersMove eco => river.InPlan(OptionalModule.Vouchers) && river.Vouchers.EcoVouchers.Value == eco.Amount,
        SportCultureVouchersMove sport => river.InPlan(OptionalModule.Vouchers) && river.Vouchers.SportCultureVouchers.Value == sport.Amount,
        RentMove rent => river.InPlan(OptionalModule.Rent) && river.Rent.Rent.Value == rent.Rent,
        CopyrightFollowsPayMove => river.InPlan(OptionalModule.Copyright) && river.Copyright.FollowsPay.Value,
        _ => false,
    };

    // The river with the move's values, everything else as it is. Applying a lever's move is the one
    // action that adds a module to the plan: the user asked for it. A royalty that follows the pay holds
    // a pay above what the company pays beside it there, as the move does, in the river's own run.
    public RiverInputs Apply(PlannedMove move, RiverInputs river) => move.Move switch
    {
        PayMove pay => river.WithDirectorPay(pay.Pay),
        CompanyAdvancePaymentMove payment => river.WithAdvancePayment(payment.Deadline, Planned(payment)),
        DirectorAdvancePaymentMove payment => river.WithTaxAdvancePayment(payment.Deadline, Planned(payment)),
        EcoVouchersMove eco => river.WithEcoVouchers(eco.Amount).AddToPlan(OptionalModule.Vouchers),
        SportCultureVouchersMove sport => river.WithSportCultureVouchers(sport.Amount).AddToPlan(OptionalModule.Vouchers),
        RentMove rent => WithTodaysHome(river).WithRent(rent.Rent).AddToPlan(OptionalModule.Rent),
        CopyrightFollowsPayMove => WithTodaysCreativeShare(river).WithRoyaltyFollowingPay().AddToPlan(OptionalModule.Copyright),
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the river cannot apply."),
    };

    // The rent is Today's home's: its cadastral income and share let come with it.
    private RiverInputs WithTodaysHome(RiverInputs river) => Today is { } today
        ? river.WithCadastralIncome(today.Rent.CadastralIncome.Value).WithShareLet(today.Rent.ShareLet.Value)
        : river;

    // The royalty follows the pay at what a ruling accepts for Today's creative share, under Today's
    // statuses: they come with it as Today holds them, origins and all.
    private RiverInputs WithTodaysCreativeShare(RiverInputs river) => Today is { } today
        ? river with
        {
            Copyright = river.Copyright with
            {
                CreativeShare = today.Copyright.CreativeShare, AverageWithinCap = today.Copyright.AverageWithinCap, HoldsAttestation = today.Copyright.HoldsAttestation,
            },
        }
        : river;

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

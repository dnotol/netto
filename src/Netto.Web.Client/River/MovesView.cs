using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the Optimisation tab draws: the engine's moves (MoveSearch) from the plan they are measured from
// (MovesBaseline), the plan starred « Today » or, while none is, the plan in the river as it was opened or
// saved, or why there are none, with what that plan leaves in the pocket and what it leaves with every
// move. Found once per plan and date, since the search runs the split at every move; which moves the
// river already holds is read from the river at each change.
internal sealed record MovesView(
    MovesState State,
    Refusal? Error,
    MovesBaseline From,
    decimal FromPay,
    IReadOnlyList<PlannedMove> Moves,
    decimal Total,
    decimal FromInPocket,
    decimal PlanInPocket)
{
    // From Today on that date, which decides the deadline a payment is proposed by.
    public static MovesView Of(RiverInputs today, DateOnly date) => Of(MovesBaseline.Today(today), date);

    // From what the search found for Today (RiverInputs.FindMoves), none for a Today that starts at the pay.
    public static MovesView Of(RiverInputs today, (MovePlan? Value, Refusal? Error)? found) => Of(MovesBaseline.Today(today), found);

    // From the plan they are measured from, on that date.
    public static MovesView Of(MovesBaseline from, DateOnly date) => Of(from, from.Plan.FindMoves(date));

    // While the page has not searched the moves from that plan yet, which it does once it has painted
    // (MovesSource): no figure and no move, only the plan they will be from.
    public static MovesView Searching(MovesBaseline from) => new(MovesState.Searching, null, from, 0m, [], 0m, 0m, 0m);

    // From what the search found for that plan, none for one that starts at the pay: the page searches
    // once per plan and date (MovesSource), and the tab draws the view of what it found.
    public static MovesView Of(MovesBaseline from, (MovePlan? Value, Refusal? Error)? found)
    {
        if (found is not { } result)
            return new MovesView(MovesState.StartsAtPay, null, from, 0m, [], 0m, 0m, 0m);
        if (result.Value is not { } plan)
            return new MovesView(MovesState.Refused, result.Error, from, 0m, [], 0m, 0m, 0m);

        var pay = from.Plan.PayUsed;
        return plan.Moves.Count == 0
            ? new MovesView(MovesState.AlreadyBest, null, from, pay, [], 0m, plan.TodayInPocket, plan.TodayInPocket)
            : new MovesView(MovesState.Moves, null, from, pay, plan.Moves, plan.Gain, plan.TodayInPocket, plan.PlanInPocket);
    }

    // What the pocket holds once the move at that place in the list is taken: what the plan the moves are
    // from leaves, with the gains of the moves up to it and its own, so the last move's is what every move
    // leaves. It is the moves' running account, not a run of the moves above alone: a line a later round
    // topped up (Move.After) holds that round's gain too, measured on top of the moves between.
    public decimal InPocketAfter(int index) => FromInPocket + Moves.Take(index + 1).Sum(move => move.Gain);

    // Whether the river has changed since the plan in it, which the moves are from while no plan is
    // starred Today, was opened or saved: said beside the moves, or beside its being the best already.
    // Today is the setup the river is measured against, and moves away from by design.
    public bool RiverChanged(RiverInputs river) =>
        !From.IsToday && State is MovesState.Moves or MovesState.AlreadyBest && river != From.Plan;

    // Whether the river holds the move's values already: its pay, the payment by the deadline of the plan
    // the moves are from with the move's amount on top, a lever's amount with its module in the plan, or a
    // royalty in the plan that follows the pay.
    public bool Applied(PlannedMove move, RiverInputs river) => move.Move switch
    {
        PayMove pay => river.DirectorPay.Value == pay.Pay,
        CompanyAdvancePaymentMove payment => river.AdvancePayments.ByDeadline[payment.Deadline].Value == Planned(payment),
        DirectorAdvancePaymentMove payment => river.TaxPrepayments.Paid.ByDeadline[payment.Deadline].Value == Planned(payment),
        EcoVouchersMove eco => river.InPlan(OptionalModule.Vouchers) && river.Vouchers.EcoVouchers.Value == eco.Amount,
        SportCultureVouchersMove sport => river.InPlan(OptionalModule.Vouchers) && river.Vouchers.SportCultureVouchers.Value == sport.Amount,
        RentMove rent => river.InPlan(OptionalModule.Rent) && river.Rent.Rent.Value == rent.Rent,
        CopyrightFollowsPayMove => river.InPlan(OptionalModule.Copyright) && river.Copyright.FollowsPay.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the river cannot tell is applied."),
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
        RentMove rent => WithPlansHome(river).WithRent(rent.Rent).AddToPlan(OptionalModule.Rent),
        CopyrightFollowsPayMove => WithPlansCreativeShare(river).WithRoyaltyFollowingPay().AddToPlan(OptionalModule.Copyright),
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the river cannot apply."),
    };

    // The rent is the home's of the plan the moves are from: its cadastral income and share let come with it.
    private RiverInputs WithPlansHome(RiverInputs river) =>
        river.WithCadastralIncome(From.Plan.Rent.CadastralIncome.Value).WithShareLet(From.Plan.Rent.ShareLet.Value);

    // The royalty follows the pay at what a ruling accepts for the creative share of the plan the moves
    // are from, under its statuses: they come with it as that plan holds them, origins and all.
    private RiverInputs WithPlansCreativeShare(RiverInputs river) => river with
    {
        Copyright = river.Copyright with
        {
            CreativeShare = From.Plan.Copyright.CreativeShare, AverageWithinCap = From.Plan.Copyright.AverageWithinCap, HoldsAttestation = From.Plan.Copyright.HoldsAttestation,
        },
    };

    private decimal Planned(CompanyAdvancePaymentMove payment) => From.Plan.AdvancePayments.ByDeadline[payment.Deadline].Value + payment.Amount;

    private decimal Planned(DirectorAdvancePaymentMove payment) => From.Plan.TaxPrepayments.Paid.ByDeadline[payment.Deadline].Value + payment.Amount;
}

internal enum MovesState
{
    // The page has not searched the moves from that plan yet: it does once it has painted (MovesSource).
    Searching,

    // The plan the moves are from skips the company's year: the split, and so every move, starts from it.
    StartsAtPay,

    // The engine refuses an amount of the plan the moves are from (Error).
    Refused,

    // No move adds a euro to the plan the moves are from.
    AlreadyBest,

    Moves,
}

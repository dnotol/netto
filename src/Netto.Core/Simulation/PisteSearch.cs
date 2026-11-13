using Netto.Core.Rules;

namespace Netto.Core.Simulation;

// An idea, a lever not in the plan, as the engine measured it: the plan the Optimiser leaves without it
// (Without), the plan it leaves with it (With), and the moves of that plan that set the lever's own values
// (Adds), what keeping the idea puts in the plan. Its gain is the pocket of the one plan less the other's,
// never the lever's move line: the pay retunes around a rent.
public sealed record Piste(LeverKind Lever, MovePlan Without, MovePlan With, IReadOnlyList<PlannedMove> Adds)
{
    public decimal Gain => With.PlanInPocket - Without.PlanInPocket;
}

// An idea's gain, optimised against optimised: the Optimiser's own search of the plan, retuning only the
// levers in it (MoveSearch.Run), is the « without » side, shared; the « with » side is the same search from
// the plan with the lever, its facts as the viewer gave them, run with the retuning finders and that lever's
// own. Both pockets are runs of the year (YearSimulation.Run), so the gain is the difference of two runs,
// never arithmetic of its own. Ideas are the levers whose gain in cash the engine measures from the viewer's
// facts: a car, or the IT and phone the company provides, always takes cash from the pocket, and no
// allowance is proposed without a ruling.
public static class PisteSearch
{
    // The levers that can be ideas, in the river's order.
    public static IReadOnlyList<LeverKind> Levers { get; } = [LeverKind.Vouchers, LeverKind.Rent, LeverKind.Copyright];

    // The idea of that lever on that date: without, what the Optimiser found for the plan on that date with
    // the levers in it (inPlan); withLever, the plan's scenario with the lever's facts, as the river runs it
    // once the lever is added. Refused as the engine refuses the plan with the lever. A lever in the plan, or
    // one that is no idea, is a caller's mistake.
    public static (Piste? Value, Refusal? Error) Run(MovePlan without, Scenario withLever, LeverKind lever, DateOnly date, IReadOnlySet<LeverKind> inPlan)
    {
        ArgumentNullException.ThrowIfNull(without);
        ArgumentNullException.ThrowIfNull(inPlan);
        if (!Levers.Contains(lever))
            throw new ArgumentOutOfRangeException(nameof(lever), lever, "Only the vouchers, the rent and the royalty are ideas.");
        if (inPlan.Contains(lever))
            throw new ArgumentException("A lever in the plan is no idea.", nameof(lever));

        var (with, error) = MoveSearch.Run(withLever, MoveSearch.FindersFor(date, new HashSet<LeverKind>(inPlan) { lever }));
        if (with is null)
            return (null, error);

        return (new Piste(lever, without, with, [.. with.Moves.Where(move => LeverOf(move.Move) == lever)]), null);
    }

    // The lever whose value a move sets, none for what every plan holds: the pay and both advance payments.
    internal static LeverKind? LeverOf(Move move) => move switch
    {
        PayMove or CompanyAdvancePaymentMove or DirectorAdvancePaymentMove => null,
        EcoVouchersMove or SportCultureVouchersMove => LeverKind.Vouchers,
        RentMove => LeverKind.Rent,
        CopyrightFollowsPayMove => LeverKind.Copyright,
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move whose lever an idea cannot tell."),
    };
}

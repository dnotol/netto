using Netto.Core.Rules;
using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What an idea asks before its gain: what its card holds and Netto cannot see. The vouchers a yes, that the
// company grants them on their card's conditions; the rent the home's cadastral income and the share of it
// let; the royalty the creative share and its two statuses, the four previous years' average within the cap
// and the arts attestation. Public because the Ideas tab, a component, takes them.
public enum PisteQuestion
{
    VouchersGranted,
    CadastralIncome,
    ShareLet,
    CreativeShare,
    AverageWithinCap,
    HoldsAttestation,
}

// A trial copy of the plan for one idea, a lever not in it: the plan on screen (Plan), the river with the
// viewer's answers on top of it (River), the lever still out, and for the vouchers whether the viewer said
// yes to their card's conditions (Granted). The answers are the lever's own inputs, which the plan keeps for
// a lever out of it, so its kept facts are the first answers. The idea is searched on the river with the
// lever added (WithLever), and kept as that river with the values of the idea's own moves: the vouchers at
// their maxima, the rent at its cap, the royalty following the pay; never the retunes, which are the
// Optimiser's. The plan on screen never changes before « Keep ». Public because the Ideas tab, a
// component, takes it.
public sealed record PisteTrial(LeverKind Lever, RiverInputs Plan, RiverInputs River, bool Granted)
{
    // The trial of that idea from the plan on screen, with no answer yet: a lever in the plan, or one that
    // is no idea (PisteSearch.Levers), is a caller's mistake.
    public static PisteTrial Of(RiverInputs plan, LeverKind lever)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!PisteSearch.Levers.Contains(lever))
            throw new ArgumentOutOfRangeException(nameof(lever), lever, "Only the vouchers, the rent and the royalty are ideas.");
        if (plan.LeversInPlan.Contains(lever))
            throw new ArgumentException("A lever in the plan is no idea.", nameof(lever));

        return new PisteTrial(lever, plan, plan, Granted: false);
    }

    // The module of the river an idea's lever is.
    public static OptionalModule ModuleOf(LeverKind lever) => lever switch
    {
        LeverKind.Vouchers => OptionalModule.Vouchers,
        LeverKind.Rent => OptionalModule.Rent,
        LeverKind.Copyright => OptionalModule.Copyright,
        _ => throw new ArgumentOutOfRangeException(nameof(lever), lever, "A lever that is no idea."),
    };

    // Everything the idea asks, in its card's order.
    public IReadOnlyList<PisteQuestion> Questions => Lever switch
    {
        LeverKind.Vouchers => [PisteQuestion.VouchersGranted],
        LeverKind.Rent => [PisteQuestion.CadastralIncome, PisteQuestion.ShareLet],
        LeverKind.Copyright => [PisteQuestion.CreativeShare, PisteQuestion.AverageWithinCap, PisteQuestion.HoldsAttestation],
        _ => throw new ArgumentOutOfRangeException(nameof(Lever), Lever, "A lever that is no idea."),
    };

    // What the gain still needs: an amount with no default not given yet, a share none is given for. A status
    // has its default, and the vouchers' yes changes no figure, so neither keeps the gain from being measured.
    public IReadOnlyList<PisteQuestion> Open => [.. Questions.Where(Unanswered)];

    // Whether « Try » can be pressed: nothing left that the gain needs, and for the vouchers the yes.
    public bool CanTry => Open.Count == 0 && (Lever != LeverKind.Vouchers || Granted);

    // Whether the viewer typed an answer the plan does not hold.
    public bool Answered => River != Plan;

    // The trial copy: the river with the answers and the lever added, what the idea is searched on.
    public RiverInputs WithLever => River.AddToPlan(ModuleOf(Lever));

    public PisteTrial WithGranted(bool granted) => this with { Granted = granted };

    public PisteTrial WithCadastralIncome(decimal amount) => this with { River = River.WithCadastralIncome(amount) };

    // A fraction: 15% is 0.15.
    public PisteTrial WithShareLet(decimal share) => this with { River = River.WithShareLet(share) };

    // Cleared, the share is none again.
    public PisteTrial WithCreativeShare(decimal? share) => this with { River = River.WithCreativeShare(share) };

    public PisteTrial WithAverageWithinCap(bool within) => this with { River = River.WithAverageWithinCap(within) };

    public PisteTrial WithAttestation(bool holds) => this with { River = River.WithAttestation(holds) };

    // The idea measured on that date against the Optimiser's own plan for the plan on screen (without):
    // none for a plan that starts at the pay, which has no moves either.
    internal (Piste? Value, Refusal? Error)? Search(MovePlan without, DateOnly date) =>
        WithLever.Company is null ? null : PisteSearch.Run(without, WithLever.MoveScenario(), Lever, date, Plan.LeversInPlan);

    // The river with the lever added, its answers, and the values of the idea's own moves (piste.Adds), as
    // the viewer keeps it: an edit of the plan like any other. The vouchers only after the yes.
    public RiverInputs Keep(Piste piste)
    {
        ArgumentNullException.ThrowIfNull(piste);
        if (piste.Lever != Lever)
            throw new ArgumentException("An idea is kept with its own measure.", nameof(piste));
        if (!CanTry)
            throw new InvalidOperationException("An idea is kept only once everything it asks is answered.");

        return piste.Adds.Aggregate(WithLever, (river, move) => move.Move switch
        {
            EcoVouchersMove eco => river.WithEcoVouchers(eco.Amount),
            SportCultureVouchersMove sport => river.WithSportCultureVouchers(sport.Amount),
            RentMove rent => river.WithRent(rent.Rent),
            CopyrightFollowsPayMove => river.WithRoyaltyFollowingPay(),
            _ => throw new ArgumentOutOfRangeException(nameof(piste), move.Move, "A move an idea cannot keep."),
        });
    }

    private bool Unanswered(PisteQuestion question) => question switch
    {
        PisteQuestion.VouchersGranted or PisteQuestion.AverageWithinCap or PisteQuestion.HoldsAttestation => false,
        PisteQuestion.CadastralIncome => River.Rent.CadastralIncome.Value == 0m,
        PisteQuestion.ShareLet => River.Rent.ShareLet.Value == 0m,
        PisteQuestion.CreativeShare => River.Copyright.CreativeShare.Value is null,
        _ => throw new ArgumentOutOfRangeException(nameof(question), question, "A question no idea asks."),
    };
}

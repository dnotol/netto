using Netto.Core.Calendar;
using Netto.Core.Copyright;
using Netto.Core.Rent;
using Netto.Core.Vouchers;

namespace Netto.Core.Simulation;

// One change Netto recommends to the user's current setup: what it changes in the scenario. A lever
// adds its own kind of move, and the finder that proposes it.
public abstract record Move
{
    public abstract Scenario Apply(Scenario scenario);

    // Moves of one kind change the same value: a later one replaces the earlier in the plan.
    public virtual object Kind => GetType();

    // This move taken after an earlier one of its kind, as the plan's line shows the two: the later
    // value, for a move that sets one.
    public virtual Move After(Move earlier) => this;
}

// The director pay set to Pay, the split's best.
public sealed record PayMove(decimal Pay) : Move
{
    public override Scenario Apply(Scenario scenario) => scenario with { DirectorPay = Pay };
}

// What the company pays more by one deadline of the income year, the first being 0, on its corporate
// tax: what cancels the surcharge, by the date of that deadline.
public sealed record CompanyAdvancePaymentMove(int Deadline, DateOnly Date, decimal Amount) : Move
{
    public override object Kind => (GetType(), Deadline);

    // A payment adds to what is paid by its deadline: two are their sum.
    public override Move After(Move earlier) => earlier is CompanyAdvancePaymentMove paid ? this with { Amount = paid.Amount + Amount } : this;

    public override Scenario Apply(Scenario scenario) => scenario with { AdvancePayments = scenario.AdvancePayments.Add(Deadline, Amount) };
}

// What the director pays more by one deadline on the income tax on their pay: what leaves no
// surcharge due.
public sealed record DirectorAdvancePaymentMove(int Deadline, DateOnly Date, decimal Amount) : Move
{
    public override object Kind => (GetType(), Deadline);

    public override Move After(Move earlier) => earlier is DirectorAdvancePaymentMove paid ? this with { Amount = paid.Amount + Amount } : this;

    public override Scenario Apply(Scenario scenario) =>
        scenario with { TaxPrepayments = scenario.TaxPrepayments with { Paid = scenario.TaxPrepayments.Paid.Add(Deadline, Amount) } };
}

// The eco vouchers the company grants raised to Amount, their yearly maximum.
public sealed record EcoVouchersMove(decimal Amount) : Move
{
    public override Scenario Apply(Scenario scenario) => scenario with { Vouchers = scenario.Vouchers with { EcoVouchers = Amount } };
}

// The sport/culture vouchers the company grants raised to Amount, their yearly maximum.
public sealed record SportCultureVouchersMove(decimal Amount) : Move
{
    public override Scenario Apply(Scenario scenario) => scenario with { Vouchers = scenario.Vouchers with { SportCultureVouchers = Amount } };
}

// The rent the company pays for the office in the director's home raised to Rent, the cap.
public sealed record RentMove(decimal Rent) : Move
{
    public override Scenario Apply(Scenario scenario) => scenario with { Rent = scenario.Rent with { Rent = Rent } };
}

// The copyright royalty the company pays the director raised to Royalty, what a ruling accepts for
// their creative share within the limits.
public sealed record CopyrightRoyaltyMove(decimal Royalty) : Move
{
    public override Scenario Apply(Scenario scenario) => scenario with { Copyright = scenario.Copyright with { Royalty = Royalty } };
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

// A finder whose moves hold only while a condition on the rest of the plan does, which a later move of
// another kind can break: whether the plan still keeps it, against Today.
internal interface IMoveGuard
{
    bool Holds(Scenario today, Scenario plan);
}

// The moves from Today, found greedily: every finder proposes its move from where the plan stands,
// the one that adds the most to the pocket is taken, and the others are asked again on top of it,
// until no move adds a euro or more. A move of a kind already taken (Move.Kind) replaces it in its
// line, which then adds both gains, so the plan has one line per value it changes. A move that leaves
// the company less than the reserve is never taken, nor one the engine refuses, nor one after which a
// finder's guard (IMoveGuard) no longer holds. A guarded move taken early can stop moves that would
// have left more had it come last, so when a guarding finder has a move from Today the search runs a
// second time, those finders joining only once the others have none left, and keeps the plan that
// leaves more. Every gain is the difference of two runs of the year (YearSimulation.Run), never
// arithmetic of its own.
public static class MoveSearch
{
    // Less than this is not worth a move.
    private const decimal SmallestGain = 1m;

    // Moves that keep undoing each other stop here.
    private const int MaximumRounds = 10;

    // On that date: a payment is proposed by the first deadline not yet past, none once all are.
    public static (MovePlan? Value, string? Error) Run(Scenario today, DateOnly date) =>
        Run(today, [
            new SplitMoveFinder(), new CompanyAdvancePaymentFinder(date), new DirectorAdvancePaymentFinder(date),
            new EcoVouchersFinder(), new SportCultureVouchersFinder(), new RentFinder(), new CopyrightRoyaltyFinder(),
        ]);

    internal static (MovePlan? Value, string? Error) Run(Scenario today, IReadOnlyList<IMoveFinder> finders)
    {
        // Today's inputs are refused, if at all, before any move.
        var (year, error) = YearSimulation.Run(today);
        if (year is null)
            return (null, error);

        var guards = finders.OfType<IMoveGuard>().ToList();
        var guarding = finders.Where(finder => finder is IMoveGuard).ToList();
        var plan = Search(today, year.NetInPocket, finders, [], guards);
        if (guarding.All(finder => finder.Find(today) is null))
            return (plan, null);

        var late = Search(today, year.NetInPocket, finders.Except(guarding).ToList(), guarding, guards);
        return (late.PlanInPocket > plan.PlanInPocket ? late : plan, null);
    }

    // Round after round from Today with the early finders, the late ones joining once the early ones
    // have no move left.
    private static MovePlan Search(
        Scenario today, decimal todayInPocket, IReadOnlyList<IMoveFinder> early, IReadOnlyList<IMoveFinder> late, IReadOnlyList<IMoveGuard> guards)
    {
        var asked = early;
        var current = today;
        var inPocket = todayInPocket;
        var moves = new List<PlannedMove>();
        for (var round = 0; round < MaximumRounds; round++)
        {
            var best = Best(asked, today, current, inPocket, guards);
            if (best is null && late.Count > 0 && asked == early)
            {
                asked = [.. early, .. late];
                best = Best(asked, today, current, inPocket, guards);
            }
            if (best is not { } move)
                break;

            var taken = moves.FindIndex(planned => Equals(planned.Move.Kind, move.Move.Kind));
            if (taken < 0)
                moves.Add(new PlannedMove(move.Move, move.Gain));
            else
                moves[taken] = new PlannedMove(move.Move.After(moves[taken].Move), moves[taken].Gain + move.Gain);
            current = move.After;
            inPocket += move.Gain;
        }

        return new MovePlan(todayInPocket, moves, inPocket);
    }

    // The move that adds the most from where the plan stands, of those the engine takes, that keep the
    // reserve and every guard, and add a euro or more; none otherwise.
    private static (Move Move, Scenario After, decimal Gain)? Best(
        IReadOnlyList<IMoveFinder> finders, Scenario today, Scenario current, decimal inPocket, IReadOnlyList<IMoveGuard> guards) =>
        finders
            .Select(finder => finder.Find(current))
            .OfType<Move>()
            .Select(move => (Move: move, After: move.Apply(current)))
            .Where(candidate => guards.All(guard => guard.Holds(today, candidate.After)))
            .Select(candidate => (candidate.Move, candidate.After, Year: YearSimulation.Run(candidate.After).Value))
            .Where(candidate => candidate.Year is { } after && after.MaximumDividend >= candidate.After.Reserve)
            .Select(candidate => (candidate.Move, candidate.After, Gain: candidate.Year!.NetInPocket - inPocket))
            .Where(candidate => candidate.Gain >= SmallestGain)
            .Aggregate(((Move Move, Scenario After, decimal Gain)?)null, (taken, candidate) => taken is { } kept && kept.Gain >= candidate.Gain ? kept : candidate);
}

// The split's best pay, when it is not the pay already set.
internal sealed class SplitMoveFinder : IMoveFinder
{
    public Move? Find(Scenario scenario) =>
        SplitSearch.Run(scenario).Value?.Best is { } best && best.Pay != scenario.DirectorPay ? new PayMove(best.Pay) : null;
}

// What the company pays more by the first deadline still open on the date, so that the surcharge on
// its corporate tax is cancelled: the calendar's figure (PaymentCalendar); nothing when there is none,
// or every deadline is past.
internal sealed class CompanyAdvancePaymentFinder(DateOnly date) : IMoveFinder
{
    public Move? Find(Scenario scenario) =>
        YearSimulation.Run(scenario).Value is { } year && PaymentCalendar.Of(year, date).Company is { Open: { } open, More: > 0m } company
            ? new CompanyAdvancePaymentMove(open, company.Payments[open].Deadline.Value, company.More)
            : null;
}

// What the director pays more by the first deadline still open on the date, so that no surcharge is
// due on the income tax on their pay: the calendar's figure; nothing when none is, or every deadline is
// past.
internal sealed class DirectorAdvancePaymentFinder(DateOnly date) : IMoveFinder
{
    public Move? Find(Scenario scenario) =>
        YearSimulation.Run(scenario).Value is { } year && PaymentCalendar.Of(year, date).Director is { Open: { } open, More: > 0m } director
            ? new DirectorAdvancePaymentMove(open, director.Payments[open].Deadline.Value, director.More)
            : null;
}

// The eco vouchers at their yearly maximum, when the company grants less: they need no fact Netto
// cannot see, only the conditions the Vouchers card lists.
internal sealed class EcoVouchersFinder : IMoveFinder
{
    public Move? Find(Scenario scenario) =>
        VoucherRules.For(scenario.IncomeYear)?.EcoVouchersMaximum.Value is { } maximum && scenario.Vouchers.EcoVouchers < maximum
            ? new EcoVouchersMove(maximum)
            : null;
}

// The sport/culture vouchers at their yearly maximum, when the company grants less.
internal sealed class SportCultureVouchersFinder : IMoveFinder
{
    public Move? Find(Scenario scenario) =>
        VoucherRules.For(scenario.IncomeYear)?.SportCultureVouchersMaximum.Value is { } maximum && scenario.Vouchers.SportCultureVouchers < maximum
            ? new SportCultureVouchersMove(maximum)
            : null;
}

// The rent at the cap, when the cadastral income and the share let are given and the rent is below
// it.
internal sealed class RentFinder : IMoveFinder
{
    public Move? Find(Scenario scenario)
    {
        if (RentRules.For(scenario.IncomeYear) is not { } rules || scenario.Rent is not { CadastralIncome: > 0m, ShareLet: > 0m } rent)
            return null;

        var cap = RentCalculator.Cap(rules, rent);
        return rent.Rent < cap ? new RentMove(cap) : null;
    }
}

// The royalty a ruling accepts for the director's creative share, within the limits, once the share is
// given and when it is above the royalty paid (CopyrightCalculator.Ceiling): never a lower royalty. It
// guards what it raised: a royalty the moves raised above Today's stays at most its ceiling at the
// plan's pay, so a later move that lowers the pay is not taken; a royalty Today already holds is the
// user's, never a reason to refuse a move.
internal sealed class CopyrightRoyaltyFinder : IMoveFinder, IMoveGuard
{
    public Move? Find(Scenario scenario) =>
        Ceiling(scenario) is { } ceiling && ceiling > scenario.Copyright.Royalty ? new CopyrightRoyaltyMove(ceiling) : null;

    public bool Holds(Scenario today, Scenario plan) =>
        plan.Copyright.Royalty <= today.Copyright.Royalty || plan.Copyright.Royalty <= Ceiling(plan);

    private static decimal? Ceiling(Scenario scenario) =>
        CopyrightRules.For(scenario.IncomeYear) is { } rules ? CopyrightCalculator.Ceiling(rules, scenario.Copyright, scenario.DirectorPay) : null;
}

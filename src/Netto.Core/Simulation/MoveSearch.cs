using Netto.Core.Calendar;
using Netto.Core.Copyright;
using Netto.Core.Rent;
using Netto.Core.Rules;
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

// The copyright royalty set to follow the pay: at each pay, what an advance ruling accepts for the
// director's creative share there, so that a later move of the pay carries it along. A pay above the
// largest the company pays together with that royalty (YearSimulation.PayCeiling) is held there, as the
// river holds it, so the year the move leaves is one the company pays.
public sealed record CopyrightFollowsPayMove : Move
{
    public override Scenario Apply(Scenario scenario)
    {
        var following = scenario with { Copyright = scenario.Copyright with { FollowsPay = true } };
        return YearSimulation.PayCeiling(following) is { } ceiling && following.DirectorPay > ceiling ? following with { DirectorPay = ceiling } : following;
    }
}

// A move in the plan with what it adds to the pocket on top of the moves before it, and the two years it
// stands between: Before, the year before its line's first round, and After, the year after its last, so
// that what the move changes can be told from the engine's own figures. A line taken in several rounds
// spans the moves taken between them too.
public sealed record PlannedMove(Move Move, decimal Gain, YearResult Before, YearResult After);

// The moves from the user's current setup, Today, in the order they were taken, each adding what it
// adds after the ones before it, so the gains sum to what the whole plan leaves in the pocket less
// what Today leaves.
public sealed record MovePlan(decimal TodayInPocket, IReadOnlyList<PlannedMove> Moves, decimal PlanInPocket)
{
    public decimal Gain => PlanInPocket - TodayInPocket;
}

// The optional levers of the year, as LeverOrder computes them: what a plan may hold or leave out. The
// client says which are in the plan (its composition); a lever that is out is its None in the scenario,
// which the engine cannot tell from one in the plan at nothing.
public enum LeverKind
{
    Vouchers,
    Allowances,
    Rent,
    Equipment,
    Car,
    Copyright,
}

// Whether a finder's move retunes what the plan holds, or would add a lever the plan leaves out.
public enum MoveKind
{
    Retune,
    Add,
}

// Proposes the best move of one kind from a scenario, or none.
internal interface IMoveFinder
{
    // The lever whose value the move sets, none for what every plan holds: the pay and the advance payments.
    LeverKind? Lever { get; }

    Move? Find(Scenario scenario);
}

// The moves from Today, found greedily: every finder proposes its move from where the plan stands,
// the one that adds the most to the pocket is taken, and the others are asked again on top of it,
// until no move adds a euro or more. A move of a kind already taken (Move.Kind) replaces it in its
// line, which then adds both gains, so the plan has one line per value it changes. A move that leaves
// the company less than the reserve is never taken, nor one the engine refuses. Every gain is the
// difference of two runs of the year (YearSimulation.Run), never arithmetic of its own, so the gains
// sum to what the plan leaves in the pocket less what Today leaves.
public static class MoveSearch
{
    // Less than this is not worth a move.
    private const decimal SmallestGain = 1m;

    // Moves that keep undoing each other stop here.
    private const int MaximumRounds = 10;

    // The moves that retune the levers in the plan (inPlan), never one adding a lever, on that date: a
    // payment is proposed by the first deadline not yet past, none once all are.
    public static (MovePlan? Value, Refusal? Error) Run(Scenario today, DateOnly date, IReadOnlySet<LeverKind> inPlan) =>
        Run(today, FindersFor(date, inPlan));

    // Every finder on that date, in the order a tie between their moves is settled.
    internal static IReadOnlyList<IMoveFinder> Finders(DateOnly date) =>
    [
        new SplitMoveFinder(), new CompanyAdvancePaymentFinder(date), new DirectorAdvancePaymentFinder(date),
        new EcoVouchersFinder(), new SportCultureVouchersFinder(), new RentFinder(), new CopyrightFollowsPayFinder(),
    ];

    // The finders that retune a plan holding those levers, in that order: the pay, the payments and the finders
    // of each lever in the plan.
    internal static IReadOnlyList<IMoveFinder> FindersFor(DateOnly date, IReadOnlySet<LeverKind> inPlan) =>
        [.. Finders(date).Where(finder => KindOf(finder, inPlan) == MoveKind.Retune)];

    // A finder retunes what every plan holds, or a lever in the plan; it would add a lever that is out.
    internal static MoveKind KindOf(IMoveFinder finder, IReadOnlySet<LeverKind> inPlan) =>
        finder.Lever is { } lever && !inPlan.Contains(lever) ? MoveKind.Add : MoveKind.Retune;

    // Round after round from Today.
    internal static (MovePlan? Value, Refusal? Error) Run(Scenario today, IReadOnlyList<IMoveFinder> finders)
    {
        // Today's inputs are refused, if at all, before any move.
        var (year, error) = YearSimulation.Run(today);
        if (year is null)
            return (null, error);

        var current = today;
        var currentYear = year;
        var moves = new List<PlannedMove>();
        for (var round = 0; round < MaximumRounds; round++)
        {
            if (Best(finders, current, currentYear.NetInPocket) is not { } move)
                break;

            var taken = moves.FindIndex(planned => Equals(planned.Move.Kind, move.Move.Kind));
            if (taken < 0)
                moves.Add(new PlannedMove(move.Move, move.Gain, currentYear, move.Year));
            else
                moves[taken] = new PlannedMove(move.Move.After(moves[taken].Move), moves[taken].Gain + move.Gain, moves[taken].Before, move.Year);
            current = move.After;
            currentYear = move.Year;
        }

        return (new MovePlan(year.NetInPocket, moves, currentYear.NetInPocket), null);
    }

    // The move that adds the most from where the plan stands, of those the engine takes, that keep the
    // reserve and add a euro or more, with the year it leaves; none otherwise.
    private static (Move Move, Scenario After, YearResult Year, decimal Gain)? Best(IReadOnlyList<IMoveFinder> finders, Scenario current, decimal inPocket) =>
        finders
            .Select(finder => finder.Find(current))
            .OfType<Move>()
            .Select(move => (Move: move, After: move.Apply(current)))
            .Select(candidate => (candidate.Move, candidate.After, Year: YearSimulation.Run(candidate.After).Value))
            .Where(candidate => candidate.Year is { KeepsReserve: true })
            .Select(candidate => (candidate.Move, candidate.After, Year: candidate.Year!))
            .Select(candidate => (candidate.Move, candidate.After, candidate.Year, Gain: candidate.Year.NetInPocket - inPocket))
            .Where(candidate => candidate.Gain >= SmallestGain)
            .Aggregate(((Move Move, Scenario After, YearResult Year, decimal Gain)?)null,
                (taken, candidate) => taken is { } kept && kept.Gain >= candidate.Gain ? kept : candidate);
}

// The split's best pay, when it is not the pay already set.
internal sealed class SplitMoveFinder : IMoveFinder
{
    public LeverKind? Lever => null;

    public Move? Find(Scenario scenario) =>
        SplitSearch.Run(scenario).Value?.Best is { } best && best.Pay != scenario.DirectorPay ? new PayMove(best.Pay) : null;
}

// What the company pays more by the first deadline still open on the date, so that the surcharge on
// its corporate tax is cancelled: the calendar's figure (PaymentCalendar); nothing when there is none,
// or every deadline is past.
internal sealed class CompanyAdvancePaymentFinder(DateOnly date) : IMoveFinder
{
    public LeverKind? Lever => null;

    public Move? Find(Scenario scenario) =>
        YearSimulation.Run(scenario).Value is { } year && PaymentCalendar.Of(year, date)?.Company is { Open: { } open, More: > 0m } company
            ? new CompanyAdvancePaymentMove(open, company.Payments[open].Deadline.Value, company.More)
            : null;
}

// What the director pays more by the first deadline still open on the date, so that no surcharge is
// due on the income tax on their pay: the calendar's figure; nothing when none is, or every deadline is
// past.
internal sealed class DirectorAdvancePaymentFinder(DateOnly date) : IMoveFinder
{
    public LeverKind? Lever => null;

    public Move? Find(Scenario scenario) =>
        YearSimulation.Run(scenario).Value is { } year && PaymentCalendar.Of(year, date)?.Director is { Open: { } open, More: > 0m } director
            ? new DirectorAdvancePaymentMove(open, director.Payments[open].Deadline.Value, director.More)
            : null;
}

// The eco vouchers at their yearly maximum, when the company grants less: they need no fact Netto
// cannot see, only the conditions the Vouchers card lists.
internal sealed class EcoVouchersFinder : IMoveFinder
{
    public LeverKind? Lever => LeverKind.Vouchers;

    public Move? Find(Scenario scenario) =>
        VoucherRules.For(scenario.IncomeYear)?.EcoVouchersMaximum.Value is { } maximum && scenario.Vouchers.EcoVouchers < maximum
            ? new EcoVouchersMove(maximum)
            : null;
}

// The sport/culture vouchers at their yearly maximum, when the company grants less.
internal sealed class SportCultureVouchersFinder : IMoveFinder
{
    public LeverKind? Lever => LeverKind.Vouchers;

    public Move? Find(Scenario scenario) =>
        VoucherRules.For(scenario.IncomeYear)?.SportCultureVouchersMaximum.Value is { } maximum && scenario.Vouchers.SportCultureVouchers < maximum
            ? new SportCultureVouchersMove(maximum)
            : null;
}

// The rent at the cap, when the cadastral income and the share let are given and the rent is below
// it.
internal sealed class RentFinder : IMoveFinder
{
    public LeverKind? Lever => LeverKind.Rent;

    public Move? Find(Scenario scenario)
    {
        if (RentRules.For(scenario.IncomeYear) is not { } rules || scenario.Rent is not { CadastralIncome: > 0m, ShareLet: > 0m } rent)
            return null;

        var cap = RentCalculator.Cap(rules, rent);
        return rent.Rent < cap ? new RentMove(cap) : null;
    }
}

// Letting the royalty follow the pay, once a ruling accepts some royalty at the pay (the share given, the
// four previous years within the cap, a year with an envelope rate), when the royalty is fixed below what
// it accepts there (CopyrightCalculator.Ceiling): never from a royalty at or above it, which is the
// user's. The move holds the pay within what the company pays with its royalty, and once the royalty
// follows, every pay a later move sets is one of the split's, within that same ceiling, and carries the
// royalty with it, so no later move can leave it above what a ruling accepts.
internal sealed class CopyrightFollowsPayFinder : IMoveFinder
{
    public LeverKind? Lever => LeverKind.Copyright;

    public Move? Find(Scenario scenario) =>
        !scenario.Copyright.FollowsPay && CopyrightRules.For(scenario.IncomeYear) is { } rules
        && CopyrightCalculator.Ceiling(rules, scenario.Copyright, scenario.DirectorPay) is { } ceiling && ceiling > scenario.Copyright.Royalty
            ? new CopyrightFollowsPayMove()
            : null;
}

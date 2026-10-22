using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
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

    public override Scenario Apply(Scenario scenario) => scenario with { AdvancePayments = scenario.AdvancePayments.Add(Deadline, Amount) };
}

// What the director pays more by one deadline on the income tax on their pay: what leaves no
// surcharge due.
public sealed record DirectorAdvancePaymentMove(int Deadline, DateOnly Date, decimal Amount) : Move
{
    public override object Kind => (GetType(), Deadline);

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
// until no move adds a euro or more. A move of a kind already taken (Move.Kind) replaces it in its
// line, which then adds both gains, so the plan has one line per value it changes. A move that leaves the company less than the reserve is never
// taken, nor one the engine refuses. Every gain is the difference of two runs of the year
// (YearSimulation.Run), never arithmetic of its own.
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
            new EcoVouchersFinder(), new SportCultureVouchersFinder(), new RentFinder(),
        ]);

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

            var taken = moves.FindIndex(planned => Equals(planned.Move.Kind, move.Move.Kind));
            if (taken < 0)
                moves.Add(new PlannedMove(move.Move, move.Gain));
            else
                moves[taken] = new PlannedMove(move.Move, moves[taken].Gain + move.Gain);
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

// What the company pays more by the first deadline still open on the date, so that the surcharge on
// its corporate tax is cancelled; nothing when there is none, or every deadline is past.
internal sealed class CompanyAdvancePaymentFinder(DateOnly date) : IMoveFinder
{
    public Move? Find(Scenario scenario)
    {
        if (CorporateTaxRules.For(scenario.IncomeYear)?.AdvancePayments is not { } rules || Open(rules.Deadlines.Select(deadline => deadline.Date.Value), date) is not { } open)
            return null;
        if (YearSimulation.Run(scenario).Value is not { } year)
            return null;

        var amount = AdvancePaymentCalculator.TopUpBy(rules, year.Company.CorporateTax, scenario.AdvancePayments, scenario.Company, open);
        return amount > 0m ? new CompanyAdvancePaymentMove(open, rules.Deadlines[open].Date.Value, amount) : null;
    }

    // The first deadline on or after the date: a deadline is open until the end of its day.
    internal static int? Open(IEnumerable<DateOnly> deadlines, DateOnly date) =>
        deadlines.Select((deadline, i) => (deadline, i)).Where(entry => entry.deadline >= date).Select(entry => (int?)entry.i).FirstOrDefault();
}

// What the director pays more by the first deadline still open on the date, so that no surcharge is
// due on the income tax on their pay; nothing when none is, or every deadline is past.
internal sealed class DirectorAdvancePaymentFinder(DateOnly date) : IMoveFinder
{
    public Move? Find(Scenario scenario)
    {
        if (PersonalTaxRules.For(scenario.IncomeYear)?.AdvancePayments is not { } rules
            || CompanyAdvancePaymentFinder.Open(rules.Deadlines.Select(deadline => deadline.Date.Value), date) is not { } open)
            return null;
        if (YearSimulation.Run(scenario).Value is not { } year)
            return null;

        var amount = GrossToNet.AdvancePaymentTopUpBy(
            scenario.IncomeYear, scenario.DirectorPay, scenario.MunicipalRate.Value, scenario.TaxPrepayments, scenario.ContributionsPaidBy, year.Rent.NetRent, open);
        return amount > 0m ? new DirectorAdvancePaymentMove(open, rules.Deadlines[open].Date.Value, amount.Value) : null;
    }
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

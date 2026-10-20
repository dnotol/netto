using Netto.Core.CorporateTax;

namespace Netto.Core.Simulation;

// The year run at every director pay out of the profit before pay, what the vouchers, the expense
// allowances and the rent leave of what the company earns, with everything else the scenario holds kept
// as it is: the vouchers, the allowances, the rent, whose tax each pay's year counts on the total it is
// taxed with, the reserve, the statuses, the municipal rate, the dividend's withholding rate and who
// pays the social contributions.
// The pay is the gross on the payslip, whoever pays them; the scenario's own pay is not used. It finds
// the pay that leaves the most in the pocket while the profit after tax still covers the reserve and,
// when a pay out of that profit can keep the reduced corporate rate, the best pay on each side of the
// smallest pay that keeps it, so the split can weigh the two.
//
// The net in pocket is piecewise linear in the pay, with one jump, where the reduced rate is kept. A
// grid of round amounts draws it, the jump is added a cent either side, and on each side the best pay
// of the grid is refined to the euro between its neighbours. Rounding to the cent (INASTI's quarters,
// each line of the tax) makes the curve uneven by a few cents from one euro to the next, so the
// refinement narrows down by thirds only while its probes are far enough apart for the slope to show
// through, then tries every euro left. A tie goes to the lower pay.
public static class SplitSearch
{
    // At most this many steps of the grid, each a round amount: €1,000 for €100,000 of profit. The
    // curve is a straight line between most of them, and the browser runs the search again at every
    // change of an input other than the pay.
    private const int MaximumSteps = 100;
    private static readonly decimal[] RoundSteps = [100m, 200m, 250m, 500m];

    // Below this width every euro is tried.
    private const decimal LastEuros = 20m;

    public static (SplitSearchResult? Value, string? Error) Run(Scenario scenario)
    {
        if (CorporateTaxRules.For(scenario.IncomeYear) is null)
            return (null, YearSimulation.IncomeYearNotSupported);

        // The scenario's other inputs are refused, if at all, whatever the pay.
        var (noPay, error) = YearSimulation.Run(scenario with { DirectorPay = 0m });
        if (noPay is null)
            return (null, error);

        var levels = new Levels(scenario);
        var ceiling = Math.Max(0m, noPay.ProfitBeforePay);
        var step = GridStep(ceiling);
        var grid = Grid(ceiling, step).Select(levels.At).ToList();
        var threshold = YearSimulation.SmallestPayKeepingReducedRate(scenario);

        if (threshold is not { } smallest)
        {
            var best = levels.BestBetween(grid, 0m, ceiling, step);
            return (new SplitSearchResult(Curve(grid, best), best, null, null, null), null);
        }

        var jump = smallest > 0m ? new[] { levels.At(smallest - 0.01m), levels.At(smallest) } : [levels.At(smallest)];
        var losing = smallest > 0m ? levels.BestBetween(grid, 0m, smallest - 0.01m, step) : null;
        var keeping = levels.BestBetween([.. grid, levels.At(smallest)], smallest, ceiling, step);

        return (new SplitSearchResult(
            Curve([.. grid, .. jump], losing, keeping),
            Levels.Better(losing, keeping),
            smallest,
            keeping,
            losing), null);
    }

    private static decimal GridStep(decimal ceiling)
    {
        for (var scale = 1m; ; scale *= 10m)
        {
            foreach (var round in RoundSteps)
            {
                if (round * scale * MaximumSteps >= ceiling)
                    return round * scale;
            }
        }
    }

    // From no pay to the whole profit, the last step shorter when the profit is not a round amount.
    private static IEnumerable<decimal> Grid(decimal ceiling, decimal step)
    {
        for (var pay = 0m; pay < ceiling; pay += step)
            yield return pay;

        yield return ceiling;
    }

    private static List<PayLevel> Curve(IEnumerable<PayLevel> levels, params PayLevel?[] bests) =>
        [.. levels.Concat(bests.OfType<PayLevel>()).DistinctBy(level => level.Pay).OrderBy(level => level.Pay)];

    // Each pay is run once, however often the search asks for it.
    private sealed class Levels(Scenario scenario)
    {
        private readonly Dictionary<decimal, PayLevel> _byPay = [];

        public PayLevel At(decimal pay)
        {
            if (_byPay.TryGetValue(pay, out var known))
                return known;

            var level = YearSimulation.Level(scenario with { DirectorPay = pay })
                ?? throw new InvalidOperationException($"The year has no rules at a pay of {pay} although it ran at no pay.");
            _byPay[pay] = level;
            return level;
        }

        // The best of the candidates between from and to that cover the reserve, refined to the euro
        // between its neighbours on the grid. Within one side of the jump the curve rises then falls,
        // and the pays that cover the reserve come first, since the profit after tax falls as the pay
        // rises: a pay that does not cover it counts as the lowest of all.
        public PayLevel? BestBetween(IEnumerable<PayLevel> candidates, decimal from, decimal to, decimal step)
        {
            var best = candidates.Where(level => level.Pay >= from && level.Pay <= to && level.KeepsReserve)
                .Aggregate((PayLevel?)null, Better);
            if (best is null)
                return null;

            var low = Math.Ceiling(Math.Max(from, best.Pay - step));
            var high = Math.Floor(Math.Min(to, best.Pay + step));
            while (high - low > LastEuros)
            {
                var third = Math.Floor((high - low) / 3m);
                if (Value(At(low + third)) < Value(At(high - third)))
                    low += third;
                else
                    high -= third;
            }

            for (var pay = low; pay <= high; pay++)
                best = Better(best, At(pay));

            return best;
        }

        public static PayLevel? Better(PayLevel? current, PayLevel? other)
        {
            if (other is not { KeepsReserve: true })
                return current;
            if (current is null || other.NetInPocket > current.NetInPocket)
                return other;

            return other.NetInPocket == current.NetInPocket && other.Pay < current.Pay ? other : current;
        }

        private static decimal Value(PayLevel level) => level.KeepsReserve ? level.NetInPocket : decimal.MinValue;
    }
}

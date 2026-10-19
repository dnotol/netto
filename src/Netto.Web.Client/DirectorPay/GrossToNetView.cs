using Netto.Core.DirectorPay;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.DirectorPay;

// What the plan page draws from one gross-to-net result: each track as rows of bars on one scale
// shared by every track, so equal amounts are equally long, the next 1,000 as shares, and each
// step's amounts grouped under their sources. It only selects, orders and scales the engine's
// steps; every amount is the engine's.
internal sealed record GrossToNetView(
    IReadOnlyList<FunnelTrack> Tracks,
    IReadOnlyList<NextThousandPart> NextThousand,
    IReadOnlyList<StepDetail> Details)
{
    private const decimal NextSlice = 1_000m;

    // The next 1,000's bar: the parts with a width, since one of none would still draw the gap that
    // separates segments. The legend lists every part.
    public IReadOnlyList<NextThousandPart> NextThousandBar => [.. NextThousand.Where(part => part.Width > 0m)];

    public static GrossToNetView From(GrossToNetResult result)
    {
        var gross = new Line("gross_pay", RowKind.Gross, result.GrossPay);

        // What leaves the pocket, in the engine's order, down to the net.
        var cash = new List<Line> { gross };
        cash.AddRange(result.Steps.Where(step => step.Track == Track.PaidOut).Select(Deduction));
        cash.Add(new Line("net_in_pocket", RowKind.NetInPocket, result.NetInPocket));

        // What lowers the tax base, in the engine's order, down to the taxable income it closes on.
        var taxBase = new List<Line> { gross };
        taxBase.AddRange(result.Steps.TakeWhile(step => step.RuleKey != "taxable_income").Select(Deduction));
        taxBase.Add(new Line("taxable_income", RowKind.TaxableIncome, result.TaxableIncome));

        var tracks = new[] { (Key: "cash", Bars: Bars(cash)), (Key: "tax_base", Bars: Bars(taxBase)) };

        // From the lowest point, or zero, to the highest: below zero only when the net is negative.
        var ends = tracks.SelectMany(track => track.Bars).SelectMany(bar => new[] { bar.From, bar.To }).Append(0m).ToList();
        var (minimum, maximum) = (ends.Min(), ends.Max());
        decimal Percent(decimal amount) => maximum == minimum ? 0m : (amount - minimum) / (maximum - minimum) * 100m;

        return new GrossToNetView(
            tracks.Select(track => new FunnelTrack(
                track.Key,
                track.Bars.Select(bar => new FunnelRow(bar.Line.Key, bar.Line.Kind, bar.Line.Amount, Percent(bar.From), Percent(bar.To))).ToList()))
                .ToList(),
            Parts(
            [
                ("social_contributions", result.NextThousand.SocialContributions),
                ("income_tax", result.NextThousand.IncomeTax),
                ("municipal_surcharge", result.NextThousand.MunicipalSurcharge),
                ("low_earned_income_credit_lost", result.NextThousand.LowEarnedIncomeCredit),
                ("net_in_pocket", result.NextThousand.NetInPocket),
            ]),
            result.Steps.Select(StepDetail.From).ToList());
    }

    // A step paid out with a negative amount is money that comes back: the tax credit for low earned
    // income. The engine negates it, so a credit of nothing is a negative zero and still comes back.
    private static Line Deduction(Step step) => step.Track switch
    {
        Track.PaidOut when decimal.IsNegative(step.Amount) => new(step.RuleKey, RowKind.Back, -step.Amount),
        Track.PaidOut => new(step.RuleKey, RowKind.PaidOut, step.Amount),
        _ => new(step.RuleKey, RowKind.TaxBaseOnly, step.Amount),
    };

    // Where the credit rises, the next 1,000 gains some of it: that part is negative and the pocket's is
    // more than the 1,000. The bar then draws only the parts that take money, each as wide as its share
    // of them; where none is negative, that is its share of the 1,000.
    private static List<NextThousandPart> Parts(IReadOnlyList<(string Key, decimal Amount)> parts)
    {
        var drawn = parts.Sum(part => Math.Max(0m, part.Amount));
        return parts
            .Select(part => new NextThousandPart(part.Key, part.Amount, part.Amount / NextSlice, drawn == 0m ? 0m : Math.Max(0m, part.Amount) / drawn))
            .ToList();
    }

    // A total is drawn between zero and its amount, a deduction from the balance it leaves to the
    // balance before it, so each part paid out sits where the previous bar ended, and what comes back
    // from the balance it finds up to the balance it leaves.
    private static List<(Line Line, decimal From, decimal To)> Bars(IReadOnlyList<Line> lines)
    {
        var bars = new List<(Line, decimal, decimal)>();
        var balance = 0m;
        foreach (var line in lines)
        {
            if (line.IsDeduction)
            {
                bars.Add((line, balance - line.Amount, balance));
                balance -= line.Amount;
            }
            else if (line.Kind == RowKind.Back)
            {
                bars.Add((line, balance, balance + line.Amount));
                balance += line.Amount;
            }
            else
            {
                bars.Add((line, Math.Min(0m, line.Amount), Math.Max(0m, line.Amount)));
                balance = line.Amount;
            }
        }

        return bars;
    }

    private sealed record Line(string Key, RowKind Kind, decimal Amount)
    {
        public bool IsDeduction => Kind.IsDeduction();
    }
}

internal sealed record FunnelTrack(string Key, IReadOnlyList<FunnelRow> Rows);

// One bar of a track. BarStart and BarEnd are percentages of the shared scale's width.
internal sealed record FunnelRow(string Key, RowKind Kind, decimal Amount, decimal BarStart, decimal BarEnd)
{
    public bool IsDeduction => Kind.IsDeduction();
}

internal enum RowKind
{
    Gross,

    // Leaves the pocket: social contributions, income tax, municipal surcharge.
    PaidOut,

    // Comes back to the pocket after the tax: the tax credit for low earned income.
    Back,

    // Only lowers the tax base and is never paid out: the flat-rate costs.
    TaxBaseOnly,

    TaxableIncome,
    NetInPocket,
}

internal static class RowKindExtensions
{
    public static bool IsDeduction(this RowKind kind) => kind is RowKind.PaidOut or RowKind.TaxBaseOnly;
}

// Share is a fraction of the next 1,000: 0.16588 for 165.88, negative for credit gained. Width is the
// fraction of the bar it takes, the same as its share unless a part is negative.
internal sealed record NextThousandPart(string Key, decimal Amount, decimal Share, decimal Width);

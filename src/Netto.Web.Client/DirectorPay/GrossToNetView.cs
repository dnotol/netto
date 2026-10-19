using Netto.Core.DirectorPay;
using Netto.Core.Rules;

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
            [
                Part("social_contributions", result.NextThousand.SocialContributions),
                Part("income_tax", result.NextThousand.IncomeTax),
                Part("municipal_surcharge", result.NextThousand.MunicipalSurcharge),
                Part("net_in_pocket", result.NextThousand.NetInPocket),
            ],
            result.Steps.Select(Detail).ToList());
    }

    // The step's own source first, even with no amount of its own, then each other source in the
    // order its amounts appear.
    private static StepDetail Detail(Step step) =>
        new(step.RuleKey, step.Amount, step.AmountsUsed.Select(amount => amount.Source)
            .Prepend(step.Source)
            .Distinct()
            .Select(source => new SourceGroup(source, step.AmountsUsed.Where(amount => amount.Source == source).ToList()))
            .ToList());

    private static Line Deduction(Step step) =>
        new(step.RuleKey, step.Track == Track.PaidOut ? RowKind.PaidOut : RowKind.TaxBaseOnly, step.Amount);

    private static NextThousandPart Part(string key, decimal amount) => new(key, amount, amount / NextSlice);

    // A total is drawn between zero and its amount, a deduction from the balance it leaves to the
    // balance before it, so each part paid out sits where the previous bar ended.
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
        public bool IsDeduction => Kind is RowKind.PaidOut or RowKind.TaxBaseOnly;
    }
}

internal sealed record FunnelTrack(string Key, IReadOnlyList<FunnelRow> Rows);

// One bar of a track. BarStart and BarEnd are percentages of the shared scale's width.
internal sealed record FunnelRow(string Key, RowKind Kind, decimal Amount, decimal BarStart, decimal BarEnd);

internal enum RowKind
{
    Gross,

    // Leaves the pocket: social contributions, income tax, municipal surcharge.
    PaidOut,

    // Only lowers the tax base and is never paid out: the flat-rate costs.
    TaxBaseOnly,

    TaxableIncome,
    NetInPocket,
}

// Share is a fraction of the next 1,000: 0.16588 for 165.88.
internal sealed record NextThousandPart(string Key, decimal Amount, decimal Share);

internal sealed record StepDetail(string RuleKey, decimal Amount, IReadOnlyList<SourceGroup> Sources);

internal sealed record SourceGroup(Source Source, IReadOnlyList<RuleAmount> Amounts);

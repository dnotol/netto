using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the bar of buckets draws from one run of the river: where the money ends up, out of what the
// river starts from, the profit before pay or, with the company skipped, the gross pay. Taxes on pay
// are the director pay's steps paid out; taxes on profit, the company's and the dividend's, the
// refund on the exempt dividend coming back. It only regroups the engine's figures and steps, so the
// buckets add up to the start.
internal sealed record BucketsView(decimal Start, bool StartsAtPay, IReadOnlyList<Bucket> Buckets)
{
    // In the order the bar shows them, the pocket first; also when there is no figure to show.
    public static IReadOnlyList<string> Keys { get; } = ["pocket", "taxes_on_pay", "taxes_on_profit", "kept_in_company"];

    public static BucketsView? From(RiverRun run)
    {
        if (run.Pay is not { } pay)
            return null;

        var (start, pocket, kept, steps) = run.Year is { } year
            ? (year.ProfitBeforePay, year.NetInPocket, year.KeptInCompany, year.Steps)
            : (pay.GrossPay, pay.NetInPocket, 0m, pay.Steps);

        var amounts = Keys.Zip(new[]
        {
            pocket,
            PaidOut(steps, TaxableEvent.DirectorPay),
            PaidOut(steps, TaxableEvent.CompanyProfit) + PaidOut(steps, TaxableEvent.Dividend),
            kept,
        }, (key, amount) => (Key: key, Amount: amount)).ToList();
        var drawn = start > 0m && amounts.All(bucket => bucket.Amount >= 0m);

        return new BucketsView(
            start,
            run.Year is null,
            amounts.Select(bucket => new Bucket(bucket.Key, bucket.Amount, drawn ? bucket.Amount / start : null)).ToList());
    }

    private static decimal PaidOut(IReadOnlyList<Step> steps, TaxableEvent taxableEvent) =>
        steps.Where(step => step.Event == taxableEvent && step.Track == Track.PaidOut).Sum(step => step.Amount);
}

// Share is a fraction of what the river starts from, or null when shares would not mean anything:
// nothing to start from, or a bucket below zero (a loss kept in the company).
internal sealed record Bucket(string Key, decimal Amount, decimal? Share);

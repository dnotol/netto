using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the bar of buckets draws from one run of the river: where the money ends up, out of what the
// river starts from, what the company earns before it pays its director anything, pay, vouchers or
// expense allowances, with the profits kept from earlier years or, with the company skipped, what the
// pay costs the company, the gross plus the contributions it pays on the director's behalf when it
// does (CompanyPaysContributions). The vouchers, the allowances and the rent are in the pocket. Taxes
// on pay are the director's personal tax: the director pay's steps paid out and, once the company pays
// a rent, the rent's, which it is taxed with (TaxesOnPayIncludeRent); taxes on profit, the company's
// and the dividend's, the refund on the exempt dividend coming back. It only regroups the engine's
// figures and steps, so the buckets add up to the start.
internal sealed record BucketsView(
    decimal Start, decimal EarlierProfits, bool StartsAtPay, bool CompanyPaysContributions, bool TaxesOnPayIncludeRent, IReadOnlyList<Bucket> Buckets)
{
    // In the order the bar shows them, the pocket first; also when there is no figure to show.
    public static IReadOnlyList<string> Keys { get; } = ["pocket", "taxes_on_pay", "taxes_on_profit", "kept_in_company"];

    // What reaches the pocket out of what leaves the company: what the river starts from less what the
    // company keeps at year end, which is still the director's. None where the bar draws no shares.
    public decimal? Reach => Buckets.All(bucket => bucket.Share is not null)
        ? Shares.Of(AmountOf("pocket"), Start - AmountOf("kept_in_company"))
        : null;

    // The rest of it went to taxes: the whole less the reach as the bar rounds it, so the two read as
    // the whole.
    public decimal? TaxesShare => Reach is { } reach ? 1m - Math.Round(reach, 2, MidpointRounding.AwayFromZero) : null;

    public static BucketsView? From(RiverRun run)
    {
        if (run.Pay is not { } pay)
            return null;

        var earlierProfits = run.Year?.Scenario.EarlierProfits ?? 0m;
        var (start, pocket, kept, steps) = run.Year is { } year
            ? (year.Scenario.ProfitBeforeDirector + earlierProfits, year.NetInPocket, year.KeptInCompany, year.Steps)
            : (pay.CompanyCost, pay.NetInPocket, 0m, pay.Steps);

        var amounts = Keys.Zip(new[]
        {
            pocket,
            PaidOut(steps, TaxableEvent.DirectorPay) + PaidOut(steps, TaxableEvent.Rent),
            PaidOut(steps, TaxableEvent.CompanyProfit) + PaidOut(steps, TaxableEvent.Dividend),
            kept,
        }, (key, amount) => (Key: key, Amount: amount)).ToList();
        var drawn = start > 0m && amounts.All(bucket => bucket.Amount >= 0m);

        return new BucketsView(
            start,
            earlierProfits,
            run.Year is null,
            pay.ContributionsPaidByCompany > 0m,
            steps.Any(step => step.Event == TaxableEvent.Rent),
            amounts.Select(bucket => new Bucket(bucket.Key, bucket.Amount, drawn ? bucket.Amount / start : null)).ToList());
    }

    private decimal AmountOf(string key) => Buckets.Single(bucket => bucket.Key == key).Amount;

    private static decimal PaidOut(IReadOnlyList<Step> steps, TaxableEvent taxableEvent) =>
        steps.Where(step => step.Event == taxableEvent && step.Track == Track.PaidOut).Sum(step => step.Amount);
}

// Share is a fraction of what the river starts from, or null when shares would not mean anything:
// nothing to start from, or a bucket below zero (a loss kept in the company).
internal sealed record Bucket(string Key, decimal Amount, decimal? Share);

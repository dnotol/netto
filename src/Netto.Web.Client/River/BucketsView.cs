using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the bar of buckets draws from one run of the river: where the money ends up, out of what the
// river starts from, what the company earns before it pays its director anything, pay, vouchers,
// expense allowances, rent or a car, with the profits kept from earlier years or, with the company
// skipped, what the pay costs the company, the gross plus the contributions it pays on the director's
// behalf when it does (CompanyPaysContributions). The vouchers, the allowances and the rent are in the
// pocket, the director's contribution to a car out of it. Once a car counts, the muddy pocket holds
// what the company spends on it, right after the pocket. Taxes on pay are the director's personal
// tax: the director pay's steps paid out, a car's benefit and a royalty's part beyond its limits taxed
// in them, once the company pays a rent, the rent's, which it is taxed with (TaxesOnPayIncludeRent),
// and a royalty's tax apart (decided unattended: the director's income tax on money received besides
// the dividend, as the rent's is); taxes on profit, the company's and
// the dividend's, the refund on the exempt dividend coming back. It only regroups the engine's figures
// and steps, so the buckets add up to the start.
internal sealed record BucketsView(
    decimal Start, decimal EarlierProfits, bool StartsAtPay, bool CompanyPaysContributions, bool TaxesOnPayIncludeRent, IReadOnlyList<Bucket> Buckets)
{
    // In the order the bar shows them, the pocket first; also when there is no figure to show. A car
    // adds the muddy pocket after the pocket.
    public static IReadOnlyList<string> Keys { get; } = ["pocket", "taxes_on_pay", "taxes_on_profit", "kept_in_company"];

    public const string Muddy = "muddy";

    public bool HasMuddyPocket => Buckets.Any(bucket => bucket.Key == Muddy);

    // What reaches the pocket out of what leaves the company: what the river starts from less what the
    // company keeps at year end, which is still the director's. None where the bar draws no shares.
    public decimal? Reach => Drawn ? Shares.Of(AmountOf("pocket"), Leaves) : null;

    // What lands in the muddy pocket out of the same, once a car counts.
    public decimal? MuddyShare => Drawn && HasMuddyPocket ? Shares.Of(AmountOf(Muddy), Leaves) : null;

    // The rest of it went to taxes: the whole less the reach and the muddy share as the bar rounds them,
    // so the three read as the whole.
    public decimal? TaxesShare => Reach is { } reach ? 1m - Rounded(reach) - Rounded(MuddyShare ?? 0m) : null;

    public static BucketsView? From(RiverRun run)
    {
        if (run.Pay is not { } pay)
            return null;

        var earlierProfits = run.Year?.Scenario.EarlierProfits ?? 0m;
        var (start, pocket, kept, steps) = run.Year is { } year
            ? (year.Scenario.ProfitBeforeDirector + earlierProfits, year.NetInPocket, year.KeptInCompany, year.Steps)
            : (pay.CompanyCost, pay.NetInPocket, 0m, pay.Steps);

        var amounts = new List<(string Key, decimal Amount)> { ("pocket", pocket) };
        if (run.Year is { Scenario.Car.Any: true } withCar)
            amounts.Add((Muddy, withCar.MuddyPocket));
        amounts.AddRange(
        [
            ("taxes_on_pay", PaidOut(steps, TaxableEvent.DirectorPay) + PaidOut(steps, TaxableEvent.Rent) + PaidOut(steps, TaxableEvent.CopyrightRoyalty)),
            ("taxes_on_profit", PaidOut(steps, TaxableEvent.CompanyProfit) + PaidOut(steps, TaxableEvent.Dividend)),
            ("kept_in_company", kept),
        ]);
        var drawn = start > 0m && amounts.All(bucket => bucket.Amount >= 0m);

        return new BucketsView(
            start,
            earlierProfits,
            run.Year is null,
            pay.ContributionsPaidByCompany > 0m,
            steps.Any(step => step.Event == TaxableEvent.Rent),
            amounts.Select(bucket => new Bucket(bucket.Key, bucket.Amount, drawn ? bucket.Amount / start : null)).ToList());
    }

    private bool Drawn => Buckets.All(bucket => bucket.Share is not null);

    private decimal Leaves => Start - AmountOf("kept_in_company");

    private decimal AmountOf(string key) => Buckets.Single(bucket => bucket.Key == key).Amount;

    private static decimal Rounded(decimal share) => Math.Round(share, 2, MidpointRounding.AwayFromZero);

    private static decimal PaidOut(IReadOnlyList<Step> steps, TaxableEvent taxableEvent) =>
        steps.Where(step => step.Event == taxableEvent && step.Track == Track.PaidOut).Sum(step => step.Amount);
}

// Share is a fraction of what the river starts from, or null when shares would not mean anything:
// nothing to start from, or a bucket below zero (a loss kept in the company).
internal sealed record Bucket(string Key, decimal Amount, decimal? Share);

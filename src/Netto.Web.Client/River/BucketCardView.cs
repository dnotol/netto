using Netto.Web.Client.Tabs;

namespace Netto.Web.Client.River;

// What a bucket's card says about the bucket chosen on the map: its total (BucketsView), and one line per
// module sending money into it, net of what comes back out of it (the car's contribution out of the pocket,
// the refund on exempt dividends out of the taxes on profit), in the river's order, a module netting to
// nothing left out: one amount per source, and no share, the bar's reach being the one percentage. The lines
// are read from the map's links (MapLinks.Of), each figure to the cent as its card draws it (MapBundle.Sum):
// arithmetic of display, no tax figure, so they add up to the bucket in the bar (BucketCardsTests). The
// pocket line's running totals are the map's alone. None while the engine refuses an amount, as the bar has
// no figure then.
internal sealed record BucketCardView(
    string Key,
    decimal Amount,
    IReadOnlyList<BucketLine> Lines,
    bool TaxesOnPayIncludeRent,
    bool TaxesOnPayIncludeRoyalty)
{
    public static BucketCardView? From(RiverInputs inputs, RiverRun run, string key)
    {
        var into = MapEnd.InBucket(key);
        if (run.Error is not null || BucketsView.From(run) is not { } buckets)
            return null;

        var bucket = buckets.Buckets.Single(candidate => candidate.Key == key);
        var links = MapLinks.Of(inputs, run);
        var lines = links.Where(link => link.To == into || link.From == into)
            .GroupBy(link => link.Card)
            .OrderBy(group => MapModules.Order.ToList().IndexOf(group.Key))
            .Select(group => (Module: group.Key, Amount: MapBundle.Sum(group.Where(link => link.To == into)) - MapBundle.Sum(group.Where(link => link.From == into))))
            .Where(line => line.Amount != 0m)
            .Select(line => new BucketLine(line.Module, line.Amount))
            .ToList();

        return new BucketCardView(key, bucket.Amount, lines, buckets.TaxesOnPayIncludeRent, buckets.TaxesOnPayIncludeRoyalty);
    }
}

// One module's net sending into a bucket.
internal sealed record BucketLine(RiverModule Module, decimal Amount);

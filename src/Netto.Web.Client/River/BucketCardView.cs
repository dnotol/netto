using Netto.Web.Client.Tabs;

namespace Netto.Web.Client.River;

// What a bucket's card says about the bucket chosen on the map: its total and its share of the bar's start
// (BucketsView), and one line per module sending money into it, net of what comes back out of it (the car's
// contribution out of the pocket, the refund on exempt dividends out of the taxes on profit), in the river's
// order, a module netting to nothing left out. The lines are read from the map's links (MapLinks.Of), each
// figure to the cent as its card draws it (MapBundle.Sum): arithmetic of display, no tax figure, so they add
// up to the bucket in the bar (BucketCardsTests). The pocket's card also lists the pocket line's running
// totals, module by module (MapPocketLine), its last the pocket. None while the engine refuses an amount,
// as the bar has no figure then.
internal sealed record BucketCardView(
    string Key,
    decimal Amount,
    decimal? Share,
    IReadOnlyList<BucketLine> Lines,
    IReadOnlyList<PocketTotal> RunningTotals,
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
            .Select(line => new BucketLine(line.Module, line.Amount, bucket.Share is null ? null : Shares.Of(line.Amount, buckets.Start)))
            .ToList();

        return new BucketCardView(key, bucket.Amount, bucket.Share, lines, key == "pocket" ? PocketTotals(links) : [], buckets.TaxesOnPayIncludeRent, buckets.TaxesOnPayIncludeRoyalty);
    }

    // What has reached the director once each module's lines joined the pocket line or left it, from the top,
    // where the map says a total: the rule of MapView.SaidElsewhere, which marks a stretch unsaid out of a point
    // where money only leaves the line (the car's contribution), the next total under a join saying it, and
    // below nothing. BucketCardsTests holds the two to each other.
    private static List<PocketTotal> PocketTotals(IReadOnlyList<MapLink> links)
    {
        var onTheLine = MapPocketLine.Of(links);
        var joined = onTheLine.Where(link => link.To.PocketLine is not null && link.From.PocketLine is null).Select(link => link.To).ToHashSet();

        return [.. onTheLine
            .Where(link => link.From.PocketLine is not null && (link.To.PocketLine is not null || link.To.InPocket))
            .Where(stretch => joined.Contains(stretch.From) && stretch.Amount >= 0m)
            .Select(stretch => new PocketTotal(stretch.From.PocketLine!.Value, stretch.Amount!.Value))];
    }
}

// One module's net sending into a bucket, with its share of the bar's start where the bar draws shares.
internal sealed record BucketLine(RiverModule Module, decimal Amount, decimal? Share);

// What has reached the director after a module's point of the pocket line.
internal sealed record PocketTotal(RiverModule Module, decimal Total);

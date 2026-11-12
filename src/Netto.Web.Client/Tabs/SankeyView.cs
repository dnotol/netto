using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the Sankey draws of the one run of the river: the map's links of money (MapLinks.Of), each a line its card
// already shows, gathered into bands between nodes, a band the sum of the links it gathers (MapBundle.Sum). It
// selects and regroups the cards' figures and computes none; nothing here decides a tax outcome
// (docs/architecture/decisions/2026-11-11-my-plans-picture-is-the-map-or-a-sankey.md).
//
// The company's line folds into Company year: its stretches, from one point of it to the next, join Company year
// to itself and are no band, the last into the split staying Company year's band into « To share »; a lever's share
// runs from Company year to the lever. The car, which takes no share, has its costs run through it: from Company
// year to the car and on into the expenses, the car card's one figure twice. What comes back out of a bucket into
// a module that sends money into it, the refund on the exempt dividend, a tax credit, is netted on that band, which
// keeps what came back (Back); what comes back with no band to net it on, the director's contribution to a car, out
// of the pocket into the company's line, is the one loop, from the pocket into Company year. Earlier profits, which
// no link carries, are a source into Dividend, the Dividend card's amount (DividendView). Tax bases, conditions and
// the household move no money and are no band.
//
// Money is conserved at every node: what enters a node between the sources and the buckets leaves it, the sources
// send what the bar starts from and each bucket receives what the bar says (SankeyViewTests). None while the engine
// refuses an amount or the bar draws no shares (a loss), nor while a band nets to less than nothing: no honest
// picture.
internal sealed record SankeyView(IReadOnlyList<SankeyNode> Nodes, IReadOnlyList<SankeyBand> Bands, BucketsView Bar)
{
    public static SankeyView? From(RiverInputs inputs, RiverRun run) =>
        run.Error is null && BucketsView.From(run) is { } bar && bar.Buckets.All(bucket => bucket.Share is not null)
            ? Gather(MapLinks.Of(inputs, run), bar, run.Year is { } year ? DividendView.From(year).EarlierProfits : 0m)
            : null;

    // The bands of the map's links, for a bar that draws its shares, earlier profits a source into Dividend while
    // there are any. None while a band nets to less than nothing, or money comes back into a module with no band to
    // net it on but the car's contribution into the company's line, the one loop.
    internal static SankeyView? Gather(IEnumerable<MapLink> links, BucketsView bar, decimal earlierProfits)
    {
        var forward = new List<(SankeyNode From, SankeyNode To, MapLink Link)>();
        var back = new List<(SankeyNode Module, SankeyNode Bucket, MapLink Link)>();
        foreach (var link in links.Where(link => MapLinkStyle.IsMoney(link.Kind)))
        {
            if (link.From.Bucket is not null)
                back.Add((NodeOf(link.To), NodeOf(link.From), link));
            else
                forward.AddRange(Ends(link).Where(ends => ends.From != ends.To).Select(ends => (ends.From, ends.To, link)));
        }

        var pairs = forward.GroupBy(band => (band.From, band.To)).ToList();
        var bands = new List<SankeyBand>();
        foreach (var pair in pairs)
        {
            var returned = back.Where(line => (line.Module, line.Bucket) == pair.Key).Select(line => line.Link).ToList();
            bands.Add(new SankeyBand(pair.Key.From, pair.Key.To, MapBundle.Sum(pair.Select(band => band.Link)) - MapBundle.Sum(returned))
            {
                Back = MapBundle.Sum(returned),
                Links = [.. pair.Select(band => band.Link), .. returned],
            });
        }

        var netted = pairs.Select(pair => pair.Key).ToHashSet();
        bands.AddRange(back.Where(line => !netted.Contains((line.Module, line.Bucket))).GroupBy(line => (line.Bucket, line.Module)).Select(loop =>
            new SankeyBand(loop.Key.Bucket, loop.Key.Module, MapBundle.Sum(loop.Select(line => line.Link))) { Loop = true, Links = [.. loop.Select(line => line.Link)] }));

        if (earlierProfits > 0m)
            bands.Add(new SankeyBand(SankeyNode.EarlierProfits, SankeyNode.Of(RiverModule.Dividend), earlierProfits));

        if (bands.Any(band => band.Amount < 0m || (band.Loop && !IsCarContribution(band))))
            return null;

        var drawn = bands.Where(band => band.Amount != 0m).OrderBy(band => band.Loop).ThenBy(band => SankeyLayout.Rank(band.From)).ThenBy(band => SankeyLayout.Rank(band.To)).ToList();
        var nodes = drawn.SelectMany(band => new[] { band.From, band.To }).Concat(BucketsView.Keys.Select(SankeyNode.InBucket)).Distinct().OrderBy(SankeyLayout.Rank).ToList();
        return new SankeyView(nodes, drawn, bar);
    }

    // The one loop the Sankey draws: the director's contribution to a car, out of the pocket into the company's line.
    private static bool IsCarContribution(SankeyBand loop) =>
        loop.From == SankeyNode.InBucket("pocket") && loop.To == SankeyNode.Of(RiverModule.CompanyYear) && loop.Links.All(link => link.Card == RiverModule.Car);

    // What enters a node, the loop into it included.
    public decimal In(SankeyNode node) => Bands.Where(band => band.To == node).Sum(band => band.Amount);

    // What leaves a node, the loop out of it included.
    public decimal Out(SankeyNode node) => Bands.Where(band => band.From == node).Sum(band => band.Amount);

    // A node money starts from: no bucket, and nothing entering it but the loop, Company year, earlier profits, or
    // Director pay with the company skipped.
    public bool IsSource(SankeyNode node) => node.Bucket is null && Bands.All(band => band.To != node || band.Loop);

    // The amount a node's label says is the money its name names: a bucket the bar's; Corporate tax the tax it pays,
    // which is what it sends into the buckets, not the profit entering it; Dividend what is paid out, what enters it
    // less what the company keeps; Company year the profit its card says, before the levers, less what leaves it for
    // the company's expenses; a source what it sends of its own, not what the loop brings back; any other module what
    // it receives.
    public decimal Says(SankeyNode node) => node switch
    {
        { Bucket: { } key } => Bar.AmountOf(key),
        { Module: RiverModule.CorporateTax } => Bands.Where(band => band.From == node && band.To.Bucket is not null).Sum(band => band.Amount),
        { Module: RiverModule.Dividend } => In(node) - Bands.Where(band => band.From == node && band.To == SankeyNode.InBucket("kept_in_company")).Sum(band => band.Amount),
        { Module: RiverModule.CompanyYear } => Out(node) - In(node) - Bands.Where(band => band.From == node && band.To == SankeyNode.InBucket("expenses")).Sum(band => band.Amount),
        _ when IsSource(node) => Out(node) - In(node),
        _ => In(node),
    };

    // The nodes at either end of a link, the company's line folded into Company year; money a lever's card sends from
    // the company's line beside it into somewhere other than the line (the car's costs) runs through the lever.
    private static IEnumerable<(SankeyNode From, SankeyNode To)> Ends(MapLink link) =>
        link.From.Lever is { } lever && link.Card == lever && link.To != MapEnd.Of(lever) && !OnCompanysLine(link.To)
            ? [(SankeyNode.Of(RiverModule.CompanyYear), SankeyNode.Of(lever)), (SankeyNode.Of(lever), NodeOf(link.To))]
            : [(NodeOf(link.From), NodeOf(link.To))];

    private static bool OnCompanysLine(MapEnd end) => end.Lever is not null || end == MapEnd.Of(RiverModule.CompanyYear) || end == MapEnd.Of(RiverModule.Split);

    private static SankeyNode NodeOf(MapEnd end) => end switch
    {
        { Module: { } module } => SankeyNode.Of(module),
        { Bucket: { } bucket } => SankeyNode.InBucket(bucket),
        { Lever: not null } => SankeyNode.Of(RiverModule.CompanyYear),
        _ => throw new ArgumentOutOfRangeException(nameof(end), end.Id, "An end the Sankey has no node for."),
    };
}

// A node of the Sankey: a module, a bucket of the bar, or the profits kept from earlier years, which the Dividend card
// takes. Id names it in the drawing's elements, as the map names its own (MapEnd.Id).
internal sealed record SankeyNode
{
    private SankeyNode(RiverModule? module, string? bucket) => (Module, Bucket) = (module, bucket);

    public RiverModule? Module { get; }

    public string? Bucket { get; }

    public bool IsEarlierProfits => Module is null && Bucket is null;

    public static SankeyNode EarlierProfits { get; } = new(null, null);

    public static SankeyNode Of(RiverModule module) => new(module, null);

    public static SankeyNode InBucket(string key) => BucketsView.Keys.Contains(key)
        ? new(null, key)
        : throw new ArgumentOutOfRangeException(nameof(key), key, "A bucket the bar does not have.");

    public string Id => (Module, Bucket) switch
    {
        ({ } module, _) => MapModules.Name(module),
        (_, { } bucket) => bucket,
        _ => "earlier-profits",
    };

    // What choosing the node chooses, as the map does: its module or its bucket; earlier profits, the Dividend card,
    // where they are typed.
    public MapChoice Choice => (Module, Bucket) switch
    {
        ({ } module, _) => MapChoice.Of(module),
        (_, { } bucket) => MapChoice.InBucket(bucket),
        _ => MapChoice.Of(RiverModule.Dividend),
    };
}

// One band of the Sankey: the links it gathers between two nodes, its amount their sum less what came back out of
// the bucket it runs into (Back), netted; or the loop, money back out of a bucket with no band to net it on.
internal sealed record SankeyBand(SankeyNode From, SankeyNode To, decimal Amount)
{
    // What came back out of the bucket the band runs into, netted on it.
    public decimal Back { get; init; }

    // Money going back out of a bucket, drawn as the one dashed loop: the director's contribution to a car.
    public bool Loop { get; init; }

    // The map's links the band gathers, the ones coming back included; none for earlier profits, the Dividend card's
    // amount.
    public IReadOnlyList<MapLink> Links { get; init; } = [];

    // Whether choosing a module keeps the band drawn: a module at either end of it, or, for the loop, the card whose
    // line it is.
    public bool Touches(RiverModule module) => From.Module == module || To.Module == module || (Loop && Links.Any(link => link.Card == module));
}

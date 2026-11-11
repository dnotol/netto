using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// Where the Sankey draws its nodes, bands and labels: one ordering rule, no hand-placed node
// (docs/architecture/decisions/2026-11-11-my-plans-picture-is-the-map-or-a-sankey.md). The nodes stand in fixed
// stages left to right (Stages), a stage holding no node of the plan left out, each down its stage in the table's
// order; the bands out of a node stack down its bar in the order of the nodes they reach, the bands into it in the
// order of the nodes they come from, the loop last at both its ends. Every band is as thick as its money at one
// scale, the tallest stage's money taking MoneyPixels; a band thinner than MinimumPixels is drawn at it and flagged
// (AtMinimum), no longer to scale, which the key says. A node's bar is as tall as the larger of what enters it and
// what leaves it, its label, its name over its amount, right of it, centred on it, the node taking the taller of the
// two; the five buckets' labels stand in a gutter right of the bands. What runs across is in percent of the drawing's
// width, laid out at the narrowest map (MapLayout.NarrowestRem), where labels have the least room, so a wider drawing
// only gives them more; what runs down is in pixels, so a band keeps its thickness however wide it is drawn. The
// loop runs under every place (LoopY). SankeyLayoutTests hold it on every plan the Sankey draws.
internal static class SankeyLayout
{
    // The drawing's narrowest width, the map's, in pixels: what runs across is laid out at it.
    public const decimal NarrowestPixels = MapLayout.NarrowestRem * MapLayout.PixelsPerRem;

    // The thinnest a band is drawn, in pixels, whatever its money.
    public const decimal MinimumPixels = 2m;

    // How tall the tallest stage's money is drawn, in pixels: the one scale of every band.
    public const decimal MoneyPixels = 320m;

    // How wide a node's bar is at the narrowest drawing, in pixels.
    public const decimal NodePixels = 8m;

    // The room between two places of a stage, in pixels.
    public const decimal GapPixels = 8m;

    // The room each side of a label, in pixels.
    public const decimal LabelGapPixels = 4m;

    // The room under the places the loop runs in, in pixels.
    public const decimal LoopRoomPixels = 24m;

    // The share of the width, in percent, the buckets' labels stand in, right of the bands.
    public const decimal GutterPercent = 30m;

    // A label's line, in pixels: an amount's height on the map (MapAmounts), its text the same size.
    public static decimal LineHeight => MapAmounts.Height;

    // The stages, left to right, each node in its stage's order: what the company earns; what is left to share
    // first, then the levers in the river's order, the car last, so its costs run straight into the expenses; the
    // royalty, Corporate tax and earlier profits; Director pay and Dividend; the five buckets in the bar's order.
    public static IReadOnlyList<IReadOnlyList<SankeyNode>> Stages { get; } =
    [
        [SankeyNode.Of(RiverModule.CompanyYear)],
        [SankeyNode.Of(RiverModule.Split), SankeyNode.Of(RiverModule.Vouchers), SankeyNode.Of(RiverModule.Allowances), SankeyNode.Of(RiverModule.Rent), SankeyNode.Of(RiverModule.Car)],
        [SankeyNode.Of(RiverModule.Copyright), SankeyNode.Of(RiverModule.CorporateTax), SankeyNode.EarlierProfits],
        [SankeyNode.Of(RiverModule.DirectorPay), SankeyNode.Of(RiverModule.Dividend)],
        [.. BucketsView.Keys.Select(SankeyNode.InBucket)],
    ];

    // A node's stage and its place in it; a module no stage holds is refused.
    public static (int Stage, int Order) Rank(SankeyNode node)
    {
        for (var stage = 0; stage < Stages.Count; stage++)
        {
            var order = Stages[stage].ToList().IndexOf(node);
            if (order >= 0)
                return (stage, order);
        }

        throw new ArgumentOutOfRangeException(nameof(node), node.Id, "A node no stage of the Sankey holds.");
    }

    // How wide a label's line is drawn, in pixels: its text as the map's amounts measure it (MapAmounts.Width),
    // without their ground.
    public static decimal TextWidth(string text) => MapAmounts.Width(text) - 2m * MapAmounts.GroundRem * MapLayout.PixelsPerRem;

    // The picture of a view, each node labelled by its words: its name, wrapped at its spaces to its room, and its
    // amount on a line of its own.
    public static SankeyPicture Of(SankeyView view, Func<SankeyNode, (string Name, string Amount)> words)
    {
        var stages = view.Nodes.Select(node => Rank(node).Stage).Distinct().Order().ToList();
        var node = Across(NodePixels);
        var step = stages.Count > 1 ? (100m - GutterPercent - node) / (stages.Count - 1) : 0m;
        decimal LeftOf(int stage) => stages.IndexOf(stage) * step;
        decimal RoomOf(int stage) => (stage == stages[^1] ? 100m - LeftOf(stage) - node : step - node) * NarrowestPixels / 100m - 2m * LabelGapPixels;

        var scale = MoneyPixels / view.Nodes.GroupBy(each => Rank(each).Stage).Max(stage => stage.Sum(each => Math.Max(view.In(each), view.Out(each))));
        var thickness = view.Bands.ToDictionary(band => band, band => Math.Max(Thickness(band.Amount, scale), MinimumPixels));
        decimal Thick(SankeyBand band) => thickness[band];

        var places = new List<SankeyPlace>();
        foreach (var stage in stages)
        {
            var top = 0m;
            foreach (var each in view.Nodes.Where(each => Rank(each).Stage == stage))
            {
                var bar = Math.Max(Math.Max(view.Bands.Where(band => band.To == each).Sum(Thick), view.Bands.Where(band => band.From == each).Sum(Thick)), MinimumPixels);
                var (name, amount) = words(each);
                var lines = (IReadOnlyList<string>)[.. Wrap(name, RoomOf(stage)), amount];
                var label = lines.Count * LineHeight;
                var slot = Math.Max(bar, label);
                places.Add(new SankeyPlace(each, stage, LeftOf(stage), node, top + (slot - bar) / 2m, bar,
                    new SankeyLabel(LeftOf(stage) + node + Across(LabelGapPixels), top + (slot - label) / 2m, lines.Max(TextWidth), lines)));
                top += slot + GapPixels;
            }
        }

        var at = places.ToDictionary(place => place.Node);
        var fromTops = Stack(view, at, band => band.From, band => band.To, Thick);
        var toTops = Stack(view, at, band => band.To, band => band.From, Thick);
        var strips = view.Bands.Select(band => new SankeyStrip(
            band, at[band.From].Left + node, fromTops[band], at[band.To].Left, toTops[band], Thick(band), band.Amount * scale < MinimumPixels)).ToList();

        var bottom = places.Max(place => Math.Max(place.Top + place.Height, place.Label.Top + place.Label.Height));
        var loop = view.Bands.Any(band => band.Loop);
        return new SankeyPicture(places, strips, bottom + (loop ? LoopRoomPixels : 0m), scale) { LoopY = loop ? bottom + LoopRoomPixels / 2m : null };
    }

    // Where each band meets a node at one of its ends: stacked down the node's bar from its top, in the order of the
    // nodes at their other ends, the loop last.
    private static Dictionary<SankeyBand, decimal> Stack(
        SankeyView view, Dictionary<SankeyNode, SankeyPlace> at, Func<SankeyBand, SankeyNode> end, Func<SankeyBand, SankeyNode> other, Func<SankeyBand, decimal> thick)
    {
        var tops = new Dictionary<SankeyBand, decimal>();
        foreach (var node in view.Nodes)
        {
            var top = at[node].Top;
            foreach (var band in view.Bands.Where(band => end(band) == node).OrderBy(band => band.Loop).ThenBy(band => Rank(other(band))))
            {
                tops[band] = top;
                top += thick(band);
            }
        }

        return tops;
    }

    // A name's words on lines no wider than the room, as many to a line as it holds; a word wider than the room
    // alone on its line.
    private static IEnumerable<string> Wrap(string name, decimal room)
    {
        var line = "";
        foreach (var word in name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var longer = line.Length == 0 ? word : $"{line} {word}";
            if (line.Length > 0 && TextWidth(longer) > room)
            {
                yield return line;
                longer = word;
            }

            line = longer;
        }

        if (line.Length > 0)
            yield return line;
    }

    // A band's thickness at a scale, to the ten-thousandth of a pixel, so bands stacked down a bar add up exactly.
    public static decimal Thickness(decimal amount, decimal scale) => Math.Round(amount * scale, 4);

    private static decimal Across(decimal pixels) => pixels * 100m / NarrowestPixels;
}

// The Sankey as it is drawn: its places, its bands, its height in pixels, the scale of its money, and where the loop
// runs under its places, none without one.
internal sealed record SankeyPicture(IReadOnlyList<SankeyPlace> Places, IReadOnlyList<SankeyStrip> Strips, decimal Height, decimal PixelsPerEuro)
{
    public decimal? LoopY { get; init; }
}

// A node where it is drawn: its stage, its bar's left edge and width in percent of the drawing's width, its top and
// height in pixels, and its label.
internal sealed record SankeyPlace(SankeyNode Node, int Stage, decimal Left, decimal Width, decimal Top, decimal Height, SankeyLabel Label);

// A node's label: its left edge in percent of the drawing's width, its top in pixels, the width of its longest line
// in pixels, and its lines, its name's then its amount.
internal sealed record SankeyLabel(decimal Left, decimal Top, decimal Width, IReadOnlyList<string> Lines)
{
    public decimal Height => Lines.Count * SankeyLayout.LineHeight;
}

// A band where it is drawn: from the right edge of the bar it leaves to the left edge of the bar it reaches, in
// percent of the drawing's width, its top at each end and its thickness in pixels; at the minimum, thicker than its
// money, flagged.
internal sealed record SankeyStrip(SankeyBand Band, decimal FromX, decimal FromTop, decimal ToX, decimal ToTop, decimal Thickness, bool AtMinimum);

using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Where the map says what each line carries: every line's amount, a condition's word or two too, on a stretch
// of its own line or right beside it, where no other line of money runs along it, clear of the boxes, the quiet
// rows, the buckets, every arrowhead and every other amount, inside the map; a line crossing it does so square,
// under its ground, where the fewest do. Held on every plan MapLayoutTests routes and on those at the edges, on
// the map drawn at its smallest and its largest: 544 and 1,184 px wide, a chain row from 3.25rem to 4.6rem.
public class MapAmountsTests
{
    // What Inter draws of a text at 0.72rem with tabular figures, in pixels, as Edge measured it (2026-10-26):
    // the estimate is never less, and never more than a few percent above it.
    [Theory]
    [InlineData("€100,000.00", 73.641)]
    [InlineData("€1,180,000.00", 84.203)]
    [InlineData("−€1,234.56", 66.156)]
    [InlineData("€8.00", 33.188)]
    [InlineData("no pay keeps", 72.484)]
    [InlineData("not covered", 64.734)]
    [InlineData("no effect", 48.344)]
    [InlineData("covered", 44.156)]
    [InlineData("lowers", 35.844)]
    [InlineData("kept", 23.609)]
    [InlineData("lost", 19.547)]
    [InlineData("met", 20.578)]
    public void Width_aText_isWhatInterDrawsOfItAndItsGroundEachSide(string text, double measured)
    {
        var drawn = (double)(MapAmounts.Width(text) - 2m * MapAmounts.GroundRem * MapLayout.PixelsPerRem);

        (drawn >= measured, drawn <= measured * 1.04).ShouldBe((true, true));
    }

    [Fact]
    public void Height_isARem()
    {
        MapAmounts.Height.ShouldBe(16m);
    }

    // The stylesheet draws an amount at the size it is placed at: its text at 0.72rem, a rem high, its ground
    // each side, at weight 400 with tabular figures, on one line.
    [Fact]
    public void MapTabCss_drawsAnAmountAtTheSizeItIsPlacedAt()
    {
        var amount = Stylesheet.Block(Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css"), ".map-amount");

        amount.ShouldSatisfyAllConditions(
            () => amount.ShouldContain($"font-size: {Stylesheet.Rem(MapAmounts.FontRem)};"),
            () => amount.ShouldContain($"height: {Stylesheet.Rem(MapAmounts.HeightRem)};"),
            () => amount.ShouldContain($"line-height: {Stylesheet.Rem(MapAmounts.HeightRem)};"),
            () => amount.ShouldContain($"padding: 0 {Stylesheet.Rem(MapAmounts.GroundRem)};"),
            () => amount.ShouldContain("box-sizing: border-box;"),
            () => amount.ShouldContain("font-weight: 400;"),
            () => amount.ShouldContain("font-variant-numeric: tabular-nums;"),
            () => amount.ShouldContain("white-space: nowrap;"));
    }

    // The map's chain row is drawn at most 4.6rem high, the largest size the amounts are held at here.
    [Fact]
    public void MapTabCss_drawsAChainRowAtMostTheLargestTheAmountsAreHeldAt()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css");

        Stylesheet.Block(css, ".map").ShouldContain($"height: min(calc(var(--map-units) * {Stylesheet.Rem(LargestRowRem)}), var(--map-room));");
    }

    // Every line has its amount, a condition its word or two, on a stretch of its own line or beside it: its
    // point on the stretch, on it or over, under, left or right of it as the stretch runs, or past the end of a
    // line out of the river; and no other line of money runs along it there, at any size.
    [Theory]
    [MemberData(nameof(Plans))]
    public void Of_anyPlan_givesEveryLineItsAmountOnAStretchOfItsOwnLineNoOtherLineOfMoneyRunsAlong(string plan)
    {
        var (view, amounts) = Placed(plan);

        amounts.ShouldSatisfyAllConditions(
            () => view.Bundles().Where(line => !amounts.Any(amount => ReferenceEquals(amount.Bundle.Route, line.Route))).Select(Name).ShouldBeEmpty(),
            () => amounts.Count.ShouldBe(view.Bundles().Count),
            () => amounts.Where(amount => !OnItsStretch(amount)).Select(Name).ShouldBeEmpty(),
            () => Sizes(view).SelectMany(scale => amounts.SelectMany(amount => view.Bundles()
                .Where(other => !ReferenceEquals(other.Route, amount.Bundle.Route) && other.Kind is MapLinkKind.PassedOn or MapLinkKind.IntoBucket)
                .Where(other => Strokes(other, scale).Any(stroke => stroke.Across == Across(amount) && stroke.Area.Overlaps(amount.Area(scale))))
                .Select(other => $"{Name(amount)} along {Name(other)} at {scale}"))).ShouldBeEmpty());
    }

    // No amount is over a box, a quiet row, a bucket, an arrowhead or another amount, a pixel off each and two
    // off another amount, nor past the map's edge, at any size.
    [Theory]
    [MemberData(nameof(Plans))]
    public void Of_anyPlan_drawsNoAmountOverABoxAnArrowheadOrAnotherAmountNorPastTheMap(string plan)
    {
        var (view, amounts) = Placed(plan);

        Sizes(view).SelectMany(scale => Faults(view, amounts, scale)).ShouldBeEmpty();
    }

    // A line under an amount crosses it square to the stretch it stands on, under its ground, never along it:
    // every line, under every amount and every condition's word or two but one. The condition the split sets on
    // Corporate tax comes down between the split's money into Corporate tax and, with a company car, the car's
    // rules into it, too close to them for its word, which stands where they run along it the least.
    [Theory]
    [MemberData(nameof(Plans))]
    public void Of_anyPlan_crossesAnotherLineOnlySquareButTheCarsRulesBesideTheSplitsCondition(string plan)
    {
        var (view, amounts) = Placed(plan);

        Sizes(view).SelectMany(scale => amounts.SelectMany(amount => view.Bundles().Where(other => !ReferenceEquals(other.Route, amount.Bundle.Route))
                .Where(other => Strokes(other, scale).Any(stroke => stroke.Across == Across(amount) && stroke.Area.Overlaps(amount.Area(scale))))
                .Where(other => !(amount.Bundle.Kind == MapLinkKind.Condition && amount.Bundle.From == MapEnd.Of(RiverModule.Split) && amount.Bundle.To == MapEnd.Of(RiverModule.CorporateTax)
                    && other.Kind is MapLinkKind.TaxBase or MapLinkKind.Condition && other.From == MapEnd.Of(RiverModule.Car) && other.To == MapEnd.Of(RiverModule.CorporateTax)))
                .Select(other => $"{Name(amount)} along {Name(other)} at {scale}")))
            .ShouldBeEmpty();
    }

    // Of the places on its own line, an amount takes one no other line crosses where there is one: a line
    // crossing another at its middle leaves the other's amount aside, and its own too.
    [Fact]
    public void Of_twoLinesCrossingAtTheirMiddles_placesEachAmountWhereTheOtherDoesNotCrossIt()
    {
        MapLink[] links =
        [
            Money(RiverModule.Vouchers, new MapRoute([new MapPoint(10m, 50m), new MapPoint(90m, 50m)])),
            Money(RiverModule.Allowances, new MapRoute([new MapPoint(50m, 5m), new MapPoint(50m, 95m)])),
        ];
        var view = new MapView([], [], 10m) { Links = links };
        var scale = MapScale.Smallest(view.Units);

        var amounts = MapAmounts.Of(view, Say);

        amounts.ShouldSatisfyAllConditions(
            () => amounts.Count.ShouldBe(2),
            () => amounts.Where(amount => view.Bundles().Where(other => !ReferenceEquals(other.Route, amount.Bundle.Route))
                .Any(other => Strokes(other, scale).Any(stroke => stroke.Area.Overlaps(amount.Area(scale))))).Select(Name).ShouldBeEmpty());
    }

    // A refused amount leaves the map no line, and so no amount.
    [Fact]
    public void Of_aRefusedAmount_placesNone()
    {
        var river = MapLinksTests.Full.WithReserve(-1m);

        MapAmounts.Of(MapView.From(river, river.Run()), Say).ShouldBeEmpty();
    }

    // The plans the amounts are held on: every one MapLayoutTests routes, by its levers and those it leaves
    // empty, or with the company skipped, and those at the edges, by name.
    public static TheoryData<string> Plans()
    {
        var plans = new TheoryData<string>();
        foreach (var (bits, quiet, companySkipped) in MapLayoutTests.Plans())
            plans.Add(companySkipped ? $"levers {bits}, the company skipped" : $"levers {bits}, quiet {quiet}");
        foreach (var plan in MapLayoutTests.Extremes)
            plans.Add(plan);

        return plans;
    }

    // A plan's map, with its amounts placed.
    internal static (MapView View, IReadOnlyList<MapAmount> Amounts) Placed(string plan)
    {
        var river = Regex.Match(plan, "^levers (\\d+), (?:quiet (\\d+)|(the company skipped))$") is { Success: true } match
            ? MapLayoutTests.River(int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                match.Groups[2].Success ? int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) : 0, match.Groups[3].Success)
            : MapLayoutTests.Extreme(plan);
        var view = MapView.From(river, river.Run());
        return (view, MapAmounts.Of(view, Say));
    }

    // A condition's words, as the map's texts say them.
    internal static string Say(MapLabel label) => ResxText.For<MapTab>().Format(label.Key, [.. label.Args]);

    // The tallest a chain row is drawn (MapTab.razor.css), and the sizes the map is drawn at, from its
    // smallest to its largest: the narrowest map and 1,920 px's, each at its shortest and its tallest.
    internal const decimal LargestRowRem = 4.6m;

    internal static IEnumerable<MapScale> Sizes(MapView view) =>
        from width in MapLayoutTests.Sizes.Select(size => size.Width)
        from row in new[] { MapLayout.SmallestRowRem, LargestRowRem }
        select MapScale.Of(width, view.Units * row * MapLayout.PixelsPerRem);

    // Every amount over a box, a quiet row, a bucket, an arrowhead or another amount, or past the map's edge,
    // the map drawn at a scale.
    internal static IEnumerable<string> Faults(MapView view, IReadOnlyList<MapAmount> amounts, MapScale scale)
    {
        var boxes = view.Places().Select(place => (Name: string.Join('+', place.Nodes.Select(node => node.End.Id)), place.Slot))
            .Concat(view.Buckets.Select(bucket => (Name: bucket.End.Id, bucket.Slot))).Select(box => (box.Name, Area: MapArea.Of(box.Slot, scale))).ToList();
        var heads = view.Bundles().Where(bundle => bundle.Arrowhead).Select(bundle => (Name: Name(bundle), Area: MapLineWidths.Head(bundle.Route, scale))).ToList();
        var edge = new MapArea(0m, 0m, 100m * scale.X, 100m * scale.Y);
        foreach (var amount in amounts)
        {
            var area = amount.Area(scale);
            foreach (var box in boxes.Where(box => box.Area.Overlaps(area, 1m)))
                yield return $"{Name(amount)} over {box.Name} at {scale}";
            foreach (var head in heads.Where(head => head.Area.Overlaps(area, 1m)))
                yield return $"{Name(amount)} over {head.Name}'s arrowhead at {scale}";
            foreach (var other in amounts.Where(other => other != amount && other.Area(scale).Overlaps(area, 2m)))
                yield return $"{Name(amount)} over {Name(other)}'s amount at {scale}";
            if (area.Left < edge.Left || area.Top < edge.Top || area.Right > edge.Right || area.Bottom > edge.Bottom)
                yield return $"{Name(amount)} past the map's edge at {scale}";
        }
    }

    // Whether an amount's point is on the stretch it names, and it stands as that stretch runs: on it, over or
    // under one across, left or right of one down, or right of the end of a line out of the river, past it.
    private static bool OnItsStretch(MapAmount amount)
    {
        var points = amount.Bundle.Route.Points;
        if (amount.Stretch < 0 || amount.Stretch + 1 >= points.Count)
            return false;

        var (a, b) = (points[amount.Stretch], points[amount.Stretch + 1]);
        var across = a.Y == b.Y;
        var past = amount.Bundle.To.IsOut && amount.Stretch + 2 == points.Count && amount.At == b && across && amount.Side == MapAmountSide.Past && amount.Left > 0m;
        var on = across
            ? amount.At.Y == a.Y && amount.At.X >= Math.Min(a.X, b.X) && amount.At.X <= Math.Max(a.X, b.X) && amount.Side is MapAmountSide.On or MapAmountSide.Above or MapAmountSide.Below
            : amount.At.X == a.X && amount.At.Y >= Math.Min(a.Y, b.Y) && amount.At.Y <= Math.Max(a.Y, b.Y) && amount.Side is MapAmountSide.On or MapAmountSide.Left or MapAmountSide.Right;
        return past || on;
    }

    // Whether the stretch an amount stands on runs across the map.
    private static bool Across(MapAmount amount) => amount.Bundle.Route.Points[amount.Stretch].Y == amount.Bundle.Route.Points[amount.Stretch + 1].Y;

    // Each stretch of a line as drawn on the map at a scale, its halo with it, and whether it runs across.
    internal static IEnumerable<(bool Across, MapArea Area)> Strokes(MapBundle line, MapScale scale) =>
        line.Route.Points.Zip(line.Route.Points.Skip(1)).Where(stretch => stretch.First != stretch.Second)
            .Select(stretch => (stretch.First.Y == stretch.Second.Y, MapLineWidths.Stroke(stretch.First, stretch.Second, line.Route.Width, scale, MapLinkStyle.HaloGap)));

    internal static string Name(MapAmount amount) => $"{Name(amount.Bundle)}'s {amount.Text}";

    private static string Name(MapBundle line) => $"{line.Kind} {line.From.Id} to {line.To.Id}";

    private static MapLink Money(RiverModule module, MapRoute route) =>
        new(MapLinkKind.IntoBucket, MapEnd.Of(module), MapEnd.InBucket("pocket"), module, new MapLabel(MapText.Map, "Money", []), 1_000m) { Route = route };
}

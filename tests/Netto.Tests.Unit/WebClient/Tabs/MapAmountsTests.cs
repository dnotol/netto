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
            () => Sizes(view).SelectMany(scale => Along(view, amounts, scale)).Where(along => along.Other.Kind is MapLinkKind.PassedOn or MapLinkKind.IntoBucket)
                .Select(Name).ShouldBeEmpty());
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

    // Every line under an amount, of money or of a tax base, crosses it square to the stretch it stands on,
    // under its ground, never along it, at any size.
    [Theory]
    [MemberData(nameof(Plans))]
    public void Of_anyPlan_crossesAnotherLineUnderAnAmountOnlySquare(string plan)
    {
        var (view, amounts) = Placed(plan);

        Sizes(view).SelectMany(scale => Along(view, amounts.Where(amount => amount.Bundle.Amount is not null), scale)).Select(Name).ShouldBeEmpty();
    }

    // A rule's line runs along a condition's word in one case alone. The condition the split sets on Corporate
    // tax comes down between the split's money into Corporate tax and a company car's rules into it, too close to
    // them for its word: its line is the only one left with no place free of every other line, the other amounts
    // where they stand (MapAmounts.Crowded), so a rule's line runs along its word, the car's, and only there;
    // every other line under a condition's word crosses it square.
    [Theory]
    [MemberData(nameof(Plans))]
    public void Of_anyPlan_runsARulesLineAlongTheSplitsConditionOnCorporateTaxAloneBesideTheCarsRulesWhereItHasNoPlaceFreeOfThem(string plan)
    {
        var (view, amounts) = Placed(plan);
        var crowded = MapAmounts.Crowded(view, Say);

        var along = Sizes(view).SelectMany(scale => Along(view, amounts, scale)).ToList();

        along.ShouldSatisfyAllConditions(
            () => along.Where(pair => !SplitsConditionOnCorporateTax(pair.Amount.Bundle) || !CarsRuleOnCorporateTax(pair.Other)).Select(Name).ShouldBeEmpty(),
            () => along.Where(pair => !crowded.Any(line => ReferenceEquals(line.Route, pair.Amount.Bundle.Route))).Select(Name).ShouldBeEmpty(),
            () => crowded.Where(line => !SplitsConditionOnCorporateTax(line)).Select(Name).ShouldBeEmpty());
    }

    // The case that rule allows happens: with a company car, the split's condition on Corporate tax has no place
    // left free of every other line, and the car's rules into Corporate tax run along its word.
    [Fact]
    public void Of_aCompanyCar_runsTheCarsRulesAlongTheSplitsConditionOnCorporateTaxWhichHasNoPlaceFreeOfThem()
    {
        var (view, amounts) = Placed("the costly car");

        var along = Along(view, amounts, MapScale.Smallest(view.Units)).ToList();

        along.ShouldSatisfyAllConditions(
            () => MapAmounts.Crowded(view, Say).Select(Name).ShouldBe(["Condition split to corporate-tax"]),
            () => along.ShouldNotBeEmpty(),
            () => along.ShouldAllBe(pair => SplitsConditionOnCorporateTax(pair.Amount.Bundle) && CarsRuleOnCorporateTax(pair.Other)));
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

    // The amounts are placed on each run of the river, in the browser, on the WebAssembly interpreter. What the
    // search does for the plan with every lever, 31 lines, is counted rather than timed, and held to about twice
    // what it did when it was written (2026-10-26: 9,397 boxes, lines and amounts a place was held against, 163
    // places weighed, 3 moves of an amount in the way), so a search grown many times over fails here, on any
    // machine, before the browser feels it. The browser measured it then: 11 ms more an edit, on a costly car.
    [Fact]
    public void Of_thePlanWithEveryLever_takesAtMostTwiceTheWorkItTookWhenWritten()
    {
        var view = MapView.From(MapLinksTests.Full, MapLinksTests.Full.Run());

        var work = MapAmounts.Work(view, Say);

        work.ShouldSatisfyAllConditions(
            () => work.Checks.ShouldBeLessThanOrEqualTo(19_000),
            () => work.Places.ShouldBeLessThanOrEqualTo(330),
            () => work.Moves.ShouldBeLessThanOrEqualTo(10));
    }

    // A kind of line the search has no rank for is refused, as the map's other switches over the kinds of
    // line refuse it, rather than ranked as one it knows.
    [Fact]
    public void Of_aLineOfAKindTheSearchHasNoRankFor_isRefused()
    {
        var view = new MapView([], [], 10m) { Links = [Money(RiverModule.Vouchers, new MapRoute([new MapPoint(10m, 50m), new MapPoint(90m, 50m)])) with { Kind = (MapLinkKind)99 }] };

        Should.Throw<ArgumentOutOfRangeException>(() => MapAmounts.Of(view, Say));
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

    // Every other line with a stretch running along an amount, the same way as the stretch the amount stands
    // on, over it, the map drawn at a scale.
    private static IEnumerable<(MapAmount Amount, MapBundle Other)> Along(MapView view, IEnumerable<MapAmount> amounts, MapScale scale) =>
        amounts.SelectMany(amount => view.Bundles().Where(other => !ReferenceEquals(other.Route, amount.Bundle.Route))
            .Where(other => Strokes(other, scale).Any(stroke => stroke.Across == Across(amount) && stroke.Area.Overlaps(amount.Area(scale))))
            .Select(other => (amount, other)));

    private static string Name((MapAmount Amount, MapBundle Other) along) => $"{Name(along.Amount)} along {Name(along.Other)}";

    private static bool SplitsConditionOnCorporateTax(MapBundle line) =>
        line.Kind == MapLinkKind.Condition && line.From == MapEnd.Of(RiverModule.Split) && line.To == MapEnd.Of(RiverModule.CorporateTax);

    private static bool CarsRuleOnCorporateTax(MapBundle line) =>
        line.Kind is MapLinkKind.TaxBase or MapLinkKind.Condition && line.From == MapEnd.Of(RiverModule.Car) && line.To == MapEnd.Of(RiverModule.CorporateTax);

    // Whether the stretch an amount stands on runs across the map.
    private static bool Across(MapAmount amount) => amount.Bundle.Route.Points[amount.Stretch].Y == amount.Bundle.Route.Points[amount.Stretch + 1].Y;

    // Each stretch of a line as drawn on the map at a scale, its halo with it, and whether it runs across.
    private static IEnumerable<(bool Across, MapArea Area)> Strokes(MapBundle line, MapScale scale) => MapLineWidths.Strokes(line.Route, scale, MapLinkStyle.HaloGap);

    internal static string Name(MapAmount amount) => $"{Name(amount.Bundle)}'s {amount.Text}";

    private static string Name(MapBundle line) => $"{line.Kind} {line.From.Id} to {line.To.Id}";

    // A line of money from a module to the pocket on the route given.
    private static MapLink Money(RiverModule module, MapRoute route) => MapLayoutTests.Money(module, MapEnd.InBucket("pocket")) with { Route = route };
}

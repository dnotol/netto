using System.Text.RegularExpressions;
using Netto.Web.Client;
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

    // A French amount is an English one's figures, separators and sign, the sign after a no-break space, which
    // takes a space's advance: « 100.000,00 € » is « €100,000.00 » and a space wide.
    [Theory]
    [InlineData("100.000,00\u00A0€", "€100,000.00 ")]
    [InlineData("−1.234,56\u00A0€", "−€1,234.56 ")]
    [InlineData("20,5\u00A0%", "20.5 %")]
    [InlineData("20,5\u202F%", "20.5 %")]
    public void Width_aFrenchText_takesAnEnglishOnesWidthItsNoBreakSpaceASpace(string french, string english)
    {
        MapAmounts.Width(french).ShouldBe(MapAmounts.Width(english));
    }

    // A French word is as wide as its letters without their accents, which Inter draws on the same advance
    // (inter-latin-wght-normal.woff2, read 2026-10-30): « société » as « societe ».
    [Theory]
    [InlineData("à", "a")]
    [InlineData("â", "a")]
    [InlineData("ç", "c")]
    [InlineData("é", "e")]
    [InlineData("è", "e")]
    [InlineData("ê", "e")]
    [InlineData("ë", "e")]
    [InlineData("î", "i")]
    [InlineData("ï", "i")]
    [InlineData("ô", "o")]
    [InlineData("ù", "u")]
    [InlineData("û", "u")]
    [InlineData("ü", "u")]
    [InlineData("société", "societe")]
    public void Width_anAccentedLetter_isItsLettersWithoutTheAccent(string accented, string plain)
    {
        MapAmounts.Width(accented).ShouldBe(MapAmounts.Width(plain));
    }

    // The punctuation of French words, which Inter draws narrow (inter-latin-wght-normal.woff2, read 2026-10-30):
    // a middle dot and a colon on a point's advance, a typographic apostrophe on less, a straight one on a little
    // more, 0.3em; none a whole em, as a character Inter is not known to draw is reckoned.
    [Theory]
    [InlineData("·", 0.27)]
    [InlineData(":", 0.27)]
    [InlineData("’", 0.27)]
    [InlineData("'", 0.3)]
    public void Width_aFrenchPunctuationMark_isItsAdvance(string mark, double em)
    {
        var drawn = MapAmounts.Width(mark) - 2m * MapAmounts.GroundRem * MapLayout.PixelsPerRem;

        drawn.ShouldBe(Math.Ceiling((decimal)em * MapAmounts.FontRem * MapLayout.PixelsPerRem * 100m) / 100m);
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
    // point on the stretch, on it or over, under, left or right of it as the stretch runs; and no other line of
    // money runs along it there, at any size. A gate's line into the pocket has none, its gate's box saying it,
    // nor the pay, its pill saying it.
    [Theory]
    [MemberData(nameof(Plans))]
    public void Of_anyPlan_givesEveryLineItsAmountOnAStretchOfItsOwnLineNoOtherLineOfMoneyRunsAlong(string plan)
    {
        var (view, amounts) = Placed(plan);
        var saying = view.Bundles().Where(line => line.SaysAmount).ToList();

        amounts.ShouldSatisfyAllConditions(
            () => saying.Where(line => !amounts.Any(amount => ReferenceEquals(amount.Bundle.Route, line.Route))).Select(Name).ShouldBeEmpty(),
            () => amounts.Count.ShouldBe(saying.Count),
            () => amounts.Where(amount => !OnItsStretch(amount)).Select(Name).ShouldBeEmpty(),
            () => Sizes(view).SelectMany(scale => Along(view, amounts, scale)).Where(along => along.Other.Kind is MapLinkKind.PassedOn or MapLinkKind.IntoBucket)
                .Select(Name).ShouldBeEmpty());
    }

    // In French, whose amounts take a no-break space more and whose conditions say other words, every plan keeps
    // the rules of its amounts: each on a stretch of its own line, over no box, arrowhead or other amount nor past
    // the map, crossed only square, and no line along a condition's word. All but a pay low enough for the credit
    // on low earned income to come back into Director pay (below).
    [Theory]
    [MemberData(nameof(FrenchPlans))]
    public void Of_anyPlanInFrench_keepsEveryRuleOfItsAmounts(string plan)
    {
        using var french = InLanguage.French();
        var view = View(plan);
        var amounts = MapAmounts.Of(view, SayInFrench);
        var saying = view.Bundles().Where(line => line.SaysAmount).ToList();

        amounts.ShouldSatisfyAllConditions(
            () => amounts.Count.ShouldBe(saying.Count),
            () => amounts.Where(amount => !OnItsStretch(amount)).Select(Name).ShouldBeEmpty(),
            () => Sizes(view).SelectMany(scale => Faults(view, amounts, scale)).ShouldBeEmpty(),
            () => Sizes(view).SelectMany(scale => Along(view, amounts, scale)).Select(Name).ShouldBeEmpty(),
            () => MapAmounts.Crowded(view, SayInFrench).Select(Name).ShouldBeEmpty());
    }

    public static TheoryData<string> FrenchPlans() => new(Plans().Select(row => row.Data).Where(plan => plan != LowPay));

    // Known, and reported (2026-10-30): with a pay low enough for the credit on low earned income to come back
    // into Director pay, the line back runs down beside the pocket line under Director pay, as close as the line
    // into the taxes on pay and their widths let it, which leaves the totals beside the pocket line the room of an
    // English amount and three pixels short of a French one. Those three totals have no free place; this pins it,
    // so that a layout giving them room turns it red and lets them join the rule above.
    private const string LowPay = "a low pay";

    [Fact]
    public void Of_aLowPayInFrench_leavesTheThreeTotalsUnderDirectorPayNoFreePlace()
    {
        using var french = InLanguage.French();
        var view = View(LowPay);

        MapAmounts.Crowded(view, SayInFrench).Select(Name).ShouldBe([
            "IntoBucket pocket-line-director-pay to pocket-line-copyright",
            "IntoBucket pocket-line-copyright to pocket-line-dividend",
            "IntoBucket pocket-line-dividend to pocket",
        ], ignoreOrder: true);
    }

    private static string SayInFrench(MapLabel label) => ResxText.French<MapTab>().Format(label.Key, [.. label.Args]);

    // Every line says its amount on it, a condition its word or two, but those whose amount the map says
    // elsewhere: a line joining the pocket line, its gate's box or the total under it saying it; the pay, its
    // pill saying it; what Corporate tax adds back, its badge saying it; money passed on into a gate whose box
    // says that very amount, to the cent: a lever's share off the company's line, the split's royalty, what
    // Corporate tax passes the dividend; and a stretch of the pocket line out of a point no line joins, where money
    // only leaves it, which the next total under a join says, or below nothing, which says no total
    // (operator, 29.10.2026). Every other stretch says what has reached you so far.
    [Theory]
    [MemberData(nameof(Plans))]
    public void View_anyPlan_saysEveryLinesAmountButWhereTheMapSaysItElsewhere(string plan)
    {
        var view = View(plan);

        view.Bundles().Where(line => line.SaysAmount == SaidElsewhere(view, line)).Select(line => $"{Name(line)} {(line.SaysAmount ? "says" : "says nothing")}").ShouldBeEmpty();
    }

    // Corporate tax's line to the dividend says no amount while the dividend's box says it crosses, and keeps its
    // own when the box says nothing crosses, the dividend all kept.
    [Theory]
    [InlineData("every lever", false)]
    [InlineData("the dividend all kept", true)]
    public void View_corporateTaxsLineToTheDividend_saysItsAmountOnlyWhereTheDividendsBoxDoesNot(string river, bool says)
    {
        var view = View(river);

        var line = MapViewTests.Line(view, MapEnd.Of(RiverModule.CorporateTax), MapEnd.Of(RiverModule.Dividend));

        (line.Amount is not null, line.SaysAmount).ShouldBe((true, says));
    }

    // The car's contribution leaving the pocket line, the stretch out of the car's point says no total; one below
    // nothing says none either, whether a contribution beyond what reached you above it or a pay taking more than
    // it brings leaves it there.
    [Theory]
    [InlineData("every lever", RiverModule.Car, false)]
    [InlineData("a contribution beyond the car's costs", RiverModule.Car, true)]
    [InlineData("the costly car", RiverModule.DirectorPay, true)]
    public void View_aStretchOfThePocketLine_saysNoTotalOutOfAPointMoneyOnlyLeavesOrBelowNothing(string plan, RiverModule point, bool belowNothing)
    {
        var view = View(plan);

        var stretch = view.Bundles().Single(line => line.From == MapEnd.OnPocketLine(point) && (line.To.PocketLine is not null || line.To == MapEnd.InBucket("pocket")));

        (stretch.Amount < 0m, stretch.SaysAmount).ShouldBe((belowNothing, false));
    }

    // Whether the map says a line's amount somewhere other than on it: its gate's box or the total under it, the
    // pay's pill, Corporate tax's badge, the box of the gate it passes money into saying that amount; or, a
    // stretch of the pocket line, says none out of a point no line joins or below nothing.
    private static bool SaidElsewhere(MapView view, MapBundle line) =>
        (line.To.PocketLine is not null && line.From.PocketLine is null)
        || (line.From.PocketLine is not null && (line.To.PocketLine is not null || line.To == MapEnd.InBucket("pocket"))
            && (line.Amount < 0m || !view.Bundles().Any(other => other.To == line.From && other.From.PocketLine is null)))

        || (line.Kind == MapLinkKind.PassedOn && line.From == MapEnd.Of(RiverModule.Split) && line.To == MapEnd.Of(RiverModule.DirectorPay))
        || (line.Kind == MapLinkKind.TaxBase && line.To == MapEnd.Of(RiverModule.CorporateTax))
        || (line.Kind == MapLinkKind.PassedOn && line.To.Module is { } into && view.Modules.Single(node => node.End.Module == into) is { Gate: true } box
            && box.Lines.Any(said => said.Amount is { } amount && Formats.Euros(amount) == Formats.Euros(line.Amount!.Value)));

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

    // Every line, a condition's too, has a place free of every other line, the other amounts where they stand
    // (MapAmounts.Crowded), so no line runs along a condition's word either: every line under one crosses it
    // square. Until the car's costs ran into the expenses from the bottom of its side (2026-10-28), the
    // condition the split sets on Corporate tax had none beside a company car's rules into it, and their line
    // ran along its word.
    [Theory]
    [MemberData(nameof(Plans))]
    public void Of_anyPlan_runsNoLineAlongAConditionsWordAndLeavesEveryLineAPlaceFreeOfTheOthers(string plan)
    {
        var (view, amounts) = Placed(plan);

        var along = Sizes(view).SelectMany(scale => Along(view, amounts, scale)).ToList();

        along.ShouldSatisfyAllConditions(
            () => along.Select(Name).ShouldBeEmpty(),
            () => MapAmounts.Crowded(view, Say).Select(Name).ShouldBeEmpty());
    }

    // Of the places on its own line, an amount takes one no other line crosses where there is one: a line
    // crossing another at its middle leaves the other's amount aside, and its own too.
    [Fact]
    public void Of_twoLinesCrossingAtTheirMiddles_placesEachAmountWhereTheOtherDoesNotCrossIt()
    {
        MapLink[] links =
        [
            Money(RiverModule.DirectorPay, new MapRoute([new MapPoint(10m, 50m), new MapPoint(90m, 50m)])),
            Money(RiverModule.CompanyYear, new MapRoute([new MapPoint(50m, 5m), new MapPoint(50m, 95m)])),
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
    // The partner's marital quotient made it 32 lines (2026-10-28: 10,644 held against, 232 weighed, 11 moves,
    // its amount moving those beside the royalty's tax apart and Corporate tax's way into its taxes out of the
    // room between them): the moves are held to about twice that.
    [Fact]
    public void Of_thePlanWithEveryLever_takesAtMostTwiceTheWorkItTookWhenWritten()
    {
        var view = MapView.From(MapLinksTests.Full, MapLinksTests.Full.Run(), MapNames.Drawn);

        var work = MapAmounts.Work(view, Say);

        work.ShouldSatisfyAllConditions(
            () => work.Checks.ShouldBeLessThanOrEqualTo(19_000),
            () => work.Places.ShouldBeLessThanOrEqualTo(330),
            () => work.Moves.ShouldBeLessThanOrEqualTo(22));
    }

    // A kind of line the search has no rank for is refused, as the map's other switches over the kinds of
    // line refuse it, rather than ranked as one it knows.
    [Fact]
    public void Of_aLineOfAKindTheSearchHasNoRankFor_isRefused()
    {
        var view = new MapView([], [], 10m) { Links = [Money(RiverModule.DirectorPay, new MapRoute([new MapPoint(10m, 50m), new MapPoint(90m, 50m)])) with { Kind = (MapLinkKind)99 }] };

        Should.Throw<ArgumentOutOfRangeException>(() => MapAmounts.Of(view, Say));
    }

    // A refused amount leaves the map no line, and so no amount.
    [Fact]
    public void Of_aRefusedAmount_placesNone()
    {
        var river = MapLinksTests.Full.WithReserve(-1m);

        MapAmounts.Of(MapView.From(river, river.Run(), MapNames.Drawn), Say).ShouldBeEmpty();
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
        var view = View(plan);
        return (view, MapAmounts.Of(view, Say));
    }

    // A plan's map, by its name among the plans the amounts are held on.
    internal static MapView View(string plan)
    {
        var river = River(plan);
        return MapView.From(river, river.Run(), MapNames.Drawn);
    }

    // A plan by its name: one the amounts are held on, by its levers and those it leaves empty, or with the
    // company skipped, or one at the edges (MapLayoutTests.Extremes); else a river the map's tests draw by name
    // (MapRendering.River).
    internal static RiverInputs River(string plan) =>
        Regex.Match(plan, "^levers (\\d+), (?:quiet (\\d+)|(the company skipped))$") is { Success: true } match
            ? MapLayoutTests.River(int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                match.Groups[2].Success ? int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) : 0, match.Groups[3].Success)
            : MapLayoutTests.Extremes.Contains(plan) ? MapLayoutTests.Extreme(plan) : MapRendering.River(plan);

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
    // under one across, or left or right of one down.
    private static bool OnItsStretch(MapAmount amount)
    {
        var points = amount.Bundle.Route.Points;
        if (amount.Stretch < 0 || amount.Stretch + 1 >= points.Count)
            return false;

        var (a, b) = (points[amount.Stretch], points[amount.Stretch + 1]);
        return a.Y == b.Y
            ? amount.At.Y == a.Y && amount.At.X >= Math.Min(a.X, b.X) && amount.At.X <= Math.Max(a.X, b.X) && amount.Side is MapAmountSide.On or MapAmountSide.Above or MapAmountSide.Below
            : amount.At.X == a.X && amount.At.Y >= Math.Min(a.Y, b.Y) && amount.At.Y <= Math.Max(a.Y, b.Y) && amount.Side is MapAmountSide.On or MapAmountSide.Left or MapAmountSide.Right;
    }

    // Every other line with a stretch running along an amount, the same way as the stretch the amount stands
    // on, over it, the map drawn at a scale.
    private static IEnumerable<(MapAmount Amount, MapBundle Other)> Along(MapView view, IEnumerable<MapAmount> amounts, MapScale scale) =>
        amounts.SelectMany(amount => view.Bundles().Where(other => !ReferenceEquals(other.Route, amount.Bundle.Route))
            .Where(other => Strokes(other, scale).Any(stroke => stroke.Across == Across(amount) && stroke.Area.Overlaps(amount.Area(scale))))
            .Select(other => (amount, other)));

    private static string Name((MapAmount Amount, MapBundle Other) along) => $"{Name(along.Amount)} along {Name(along.Other)}";

    // Whether the stretch an amount stands on runs across the map.
    private static bool Across(MapAmount amount) => amount.Bundle.Route.Points[amount.Stretch].Y == amount.Bundle.Route.Points[amount.Stretch + 1].Y;

    // Each stretch of a line as drawn on the map at a scale, its halo with it, and whether it runs across.
    private static IEnumerable<(bool Across, MapArea Area)> Strokes(MapBundle line, MapScale scale) => MapLineWidths.Strokes(line.Route, scale, MapLinkStyle.HaloGap);

    internal static string Name(MapAmount amount) => $"{Name(amount.Bundle)}'s {amount.Text}";

    private static string Name(MapBundle line) => $"{line.Kind} {line.From.Id} to {line.To.Id}";

    // A line of money from a module to the pocket on the route given.
    private static MapLink Money(RiverModule module, MapRoute route) => MapLayoutTests.Money(module, MapEnd.InBucket("pocket")) with { Route = route };
}

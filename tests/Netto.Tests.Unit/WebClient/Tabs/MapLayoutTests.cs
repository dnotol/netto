using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Where the map draws its modules, its buckets and its lines: fixed slots in the river's shape, rows
// closing up over a lever out of the plan, the levers that move no money of their own in one quiet row,
// and an orthogonal route for every link, the conditions one module sets on another among them, that
// crosses no box and no quiet row but the one the chain's line runs through, ends on its two boxes and
// shares no stretch with another link's, whatever the plan holds and whichever of its levers it leaves
// empty; and at the widths its lines are drawn, apart from one another, off the boxes and within the sides
// they meet, on the map at 1,201 px, where it is narrowest, and at 1,920 px.
public class MapLayoutTests
{
    // The conditions one module sets on another, as the engine will name them: the pay keeping the
    // reduced rate, a car's benefit within 20% of the pay, a rent leaving no office allowance, a royalty
    // lowering the pay that keeps the rate, and the reserve the dividend keeps.
    private static readonly (RiverModule From, RiverModule To)[] Conditions =
    [
        (RiverModule.Split, RiverModule.CorporateTax),
        (RiverModule.Car, RiverModule.CorporateTax),
        (RiverModule.Allowances, RiverModule.Rent),
        (RiverModule.Copyright, RiverModule.Split),
        (RiverModule.Dividend, RiverModule.Split),
    ];

    // Every module and bucket a card's line can join: a line runs either way between them, money into the
    // bucket or, a refund, a credit, a contribution or a bonus beyond its surcharge, back out of it.
    private static readonly (RiverModule Module, string Bucket)[] DrainEnds =
    [
        (RiverModule.Vouchers, "pocket"),
        (RiverModule.Allowances, "pocket"),
        (RiverModule.Rent, "taxes_on_pay"),
        (RiverModule.Rent, "pocket"),
        (RiverModule.Car, "pocket"),
        (RiverModule.Copyright, "taxes_on_pay"),
        (RiverModule.Copyright, "pocket"),
        (RiverModule.DirectorPay, "taxes_on_pay"),
        (RiverModule.DirectorPay, "pocket"),
        (RiverModule.CorporateTax, "taxes_on_profit"),
        (RiverModule.Dividend, "taxes_on_profit"),
        (RiverModule.Dividend, "pocket"),
        (RiverModule.Dividend, "kept_in_company"),
    ];

    public static TheoryData<int> EveryDrainEnds => new(Enumerable.Range(0, DrainEnds.Length));

    // The most badges a module's box can hold, which the routes here are worked out with, so its box is the
    // tallest the map draws: a badge for each condition acting on it (Conditions, the office rule's
    // leaving none) and one for what its tax base gains, Corporate tax's from the vouchers and the car,
    // Director pay's from the car's benefit. MapBadgesTests holds every plan within it.
    internal static readonly IReadOnlyDictionary<RiverModule, int> MostBadges = new Dictionary<RiverModule, int>
    {
        [RiverModule.Split] = 2,
        [RiverModule.CorporateTax] = 3,
        [RiverModule.DirectorPay] = 1,
    };

    [Fact]
    public void Place_everyModuleOfTheMap_hasASlotOfItsOwn()
    {
        MapModules.Order.Select(MapLayout.Place).Distinct().Count().ShouldBe(MapModules.Order.Count);
    }

    [Theory]
    [InlineData(RiverModule.River)]
    [InlineData((RiverModule)99)]
    public void Place_aModuleTheMapHasNoPlaceFor_isRefused(RiverModule module)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapLayout.Place(module));
    }

    [Fact]
    public void Grid_theExample_closesUpOverTheLeversOutOfThePlan()
    {
        MapLayout.Grid(MapView.ModulesOf(RiverInputs.Example)).Rows
            .ShouldBe([MapRow.CompanyYear, MapRow.Split, MapRow.StreamTop, MapRow.StreamBottom, MapRow.Buckets]);
    }

    [Fact]
    public void Grid_everyLever_drawsEveryRow()
    {
        MapLayout.Grid(MapView.ModulesOf(MapLinksTests.Full)).Rows.ShouldBe(Enum.GetValues<MapRow>());
    }

    [Fact]
    public void Grid_companySkipped_isCompanyYearAndDirectorPayOverTheBuckets()
    {
        MapLayout.Grid(MapView.ModulesOf(MapLinksTests.Full.SkipCompany())).Rows.ShouldBe([MapRow.CompanyYear, MapRow.StreamBottom, MapRow.Buckets]);
    }

    // A module's badges grow its box a line each, 0.3 of a chain row (MapLayout.BadgeRem and BadgeGapRem
    // at the smallest scale), and its row keeps round its tallest box the room a chain row keeps round its
    // box: the split's two badges add their lines to its row, every other row keeping its height.
    [Fact]
    public void Grid_aBoxsBadges_growItALineEachAndItsRowWithIt()
    {
        var modules = MapView.ModulesOf(MapLinksTests.Full);
        var (without, with) = (MapLayout.Grid(modules), MapLayout.Grid(modules, badgeLines: new Dictionary<RiverModule, int> { [RiverModule.Split] = 2 }));

        with.ShouldSatisfyAllConditions(
            () => Units(with, RiverModule.Split).ShouldBe(0.5m + 2 * 0.3m),
            () => (with.Units - without.Units).ShouldBe(0.6m),
            () => modules.Where(module => module != RiverModule.Split).Select(module => Units(with, module))
                .ShouldBe(modules.Where(module => module != RiverModule.Split).Select(module => Units(without, module))));
    }

    // A stream's row is taller than its box needs, for the words of its many lines: badges grow it only
    // once its tallest box takes more than the room the row has round it.
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0.3)]
    public void Grid_badgesInAStreamsBox_growItsRowOnlyPastItsRoom(int lines, double growth)
    {
        var modules = MapView.ModulesOf(MapLinksTests.Full);

        var with = MapLayout.Grid(modules, badgeLines: new Dictionary<RiverModule, int> { [RiverModule.CorporateTax] = lines });

        (Units(with, RiverModule.CorporateTax), with.Units - MapLayout.Grid(modules).Units).ShouldBe((0.5m + lines * 0.3m, (decimal)growth));
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Grid_anyPlan_drawsNoBoxOverAnother(int bits, int quiet, bool companySkipped)
    {
        var boxes = Boxes(River(bits, quiet, companySkipped));

        boxes.SelectMany((a, i) => boxes.Skip(i + 1).Where(b => Overlaps(Area(a.Slot), Area(b.Slot))).Select(b => $"{a.Id} over {b.Id}")).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_runsEveryLineAcrossOrDownOnly(int bits, int quiet, bool companySkipped)
    {
        Routes(River(bits, quiet, companySkipped)).Where(route => Segments(route.Route).Any(segment => segment.A.X != segment.B.X && segment.A.Y != segment.B.Y))
            .Select(route => route.Name).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_endsEveryLineOnItsTwoBoxes(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);
        var grid = Grid(river);

        Routes(river).Where(route => !OnEdge(grid, route.From, route.Route.Points[0]) || !EndsOn(grid, route.From, route.To, route.Route.End))
            .Select(route => route.Name).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_crossesNoBox(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);
        var boxes = Boxes(river);

        Routes(river).SelectMany(route => Segments(route.Route).SelectMany(segment =>
                boxes.Where(box => Enters(segment, box.Slot) && !RunsThrough(route.Through, box.Modules)).Select(box => $"{route.Name} through {box.Id}")))
            .ShouldBeEmpty();
    }

    // What the map itself draws, its boxes holding this plan's badges: no line through a box, but the
    // chain's through its own quiet row.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void From_anyPlan_drawsNoLineThroughABox(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);
        var view = MapView.From(river, river.Run());
        var boxes = view.Places().Select(place => (Id: string.Join('+', place.Nodes.Select(node => node.End.Id)), place.Slot, Modules: place.Nodes.Select(node => node.End.Module!.Value).ToList()))
            .Concat(view.Buckets.Select(bucket => (Id: bucket.End.Id, bucket.Slot, Modules: new List<RiverModule>()))).ToList();

        view.Links.SelectMany(link => Segments(RouteOf(link)).SelectMany(segment =>
                boxes.Where(box => Enters(segment, box.Slot) && !RunsThrough(link.Through, box.Modules)).Select(box => $"{link.Kind} {link.From.Id} to {link.To.Id} through {box.Id}")))
            .ShouldBeEmpty();
    }

    // The levers the routes here are worked out with as quiet, which the map draws quiet, are those the plan
    // leaves empty: every one of them, but for Expense allowances beside a rent paid (below).
    [Theory]
    [MemberData(nameof(RiversWithoutEmptyAllowancesBesideAPaidRent))]
    public void Quiet_anyPlan_isTheLeversItLeavesEmpty(int bits, int quiet, bool companySkipped)
    {
        Quiet(River(bits, quiet, companySkipped)).ShouldBe(Levers(quiet).Select(MapModules.Of), ignoreOrder: true);
    }

    // Empty Expense allowances beside a rent paid keep their box: the rule that leaves no office allowance
    // beside a rent links them to it, a condition, which counts as any link does.
    [Theory]
    [MemberData(nameof(RiversWithEmptyAllowancesBesideAPaidRent))]
    public void Quiet_emptyAllowancesBesideAPaidRent_isTheOtherLeversItLeavesEmpty(int bits, int quiet, bool companySkipped)
    {
        Quiet(River(bits, quiet, companySkipped)).ShouldBe(Levers(quiet).Where(lever => lever != OptionalModule.Allowances).Select(MapModules.Of), ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_sharesNoStretchOfLineBetweenTwoLinks(int bits, int quiet, bool companySkipped)
    {
        var routes = Routes(River(bits, quiet, companySkipped));

        routes.SelectMany((a, i) => routes.Skip(i + 1)
                .Where(b => Segments(a.Route).Any(sa => Segments(b.Route).Any(sb => Shared(sa, sb))))
                .Select(b => $"{a.Name} with {b.Name}"))
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_staysInsideTheMap(int bits, int quiet, bool companySkipped)
    {
        Routes(River(bits, quiet, companySkipped)).Where(route => route.Route.Points.Any(point => point.X is < 0m or > 100m || point.Y is < 0m or > 100m))
            .Select(route => route.Name).ShouldBeEmpty();
    }

    // Lines that run side by side, down a lane, along a side of a box or across a gap, are apart by their
    // half-widths and a halo's gap, so none covers another, on the map at 1,201 px, where it is narrowest,
    // and at 1,920 px.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_keepsLinesSideBySideApartAtTheirWidths(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);

        Crowded(Routes(river), Grid(river).Units).ShouldBeEmpty();
    }

    // No line crosses a box at its width, nor runs past the map's edge, but the chain's through its own
    // quiet row.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_crossesNoBoxAtItsWidth(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);

        Crossings(Routes(river), Boxes(river), Grid(river).Units).ShouldBeEmpty();
    }

    // A line meets each of its boxes within the side it meets, its whole width on it.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_meetsItsBoxesWithinTheirSidesAtItsWidth(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);

        Overhangs(Routes(river), Grid(river)).ShouldBeEmpty();
    }

    // Each arrowhead, sized by its line (MapLineWidths), is clear of every other arrowhead and of every other
    // line, and no longer than the stretch of line it ends, so it reads as an arrowhead and never runs past a
    // turn: on the narrowest map, where it takes the largest share of it.
    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_drawsEachArrowheadClearOfTheOthersAndWithinItsLastStretch(int bits, int quiet, bool companySkipped)
    {
        var river = River(bits, quiet, companySkipped);

        Arrowheads(Routes(river), Grid(river).Units).ShouldBeEmpty();
    }

    // Plans at the edges of what a plan holds keep their lines apart and off the boxes too, and their
    // arrowheads clear: the pay at its ceiling, a pay low enough for the tax credit on low earned income to
    // come back beside its taxes, a royalty past its limits, the costly car, a car costly to run, whose stub
    // out of the river ends in a large arrowhead beside the drops into Corporate tax, and the company skipped
    // with a high pay or a low one.
    [Theory]
    [MemberData(nameof(ExtremeRivers))]
    public void Routed_aPlanAtTheEdges_keepsItsLinesApartAndOffTheBoxesAtTheirWidths(string plan)
    {
        var river = Extreme(plan);

        var routes = Routes(river);

        routes.ShouldSatisfyAllConditions(
            () => river.Run().Error.ShouldBeNull(),
            () => Crowded(routes, Grid(river).Units).ShouldBeEmpty(),
            () => Crossings(routes, Boxes(river), Grid(river).Units).ShouldBeEmpty(),
            () => Overhangs(routes, Grid(river)).ShouldBeEmpty(),
            () => Arrowheads(routes, Grid(river).Units).ShouldBeEmpty());
    }

    // The way into the taxes and what comes back out of them turn along the band over the buckets, a row each,
    // what comes back along the row under the way in and nested inside it, so that the two never cross; the
    // dividend's way to the pocket runs along the band's last row, under them; Director pay's and the dividend's
    // ways in share a row, and so do their ways back. A pay low enough for the credit on low earned income to come
    // back beside the taxes on pay, the withholding refunded on the exempt dividend beside the taxes on profit.
    [Fact]
    public void Routed_theWaysIntoTheTaxesAndWhatComesBack_turnAlongTheBandsRowsNestedAboveTheDividendsWayToThePocket()
    {
        var view = MapView.From(Extreme("a low pay"), Extreme("a low pay").Run());

        var (payIn, payBack) = (Line(view, MapEnd.Of(RiverModule.DirectorPay), MapEnd.InBucket("taxes_on_pay")), Line(view, MapEnd.InBucket("taxes_on_pay"), MapEnd.Of(RiverModule.DirectorPay)));
        var (dividendIn, dividendBack) = (Line(view, MapEnd.Of(RiverModule.Dividend), MapEnd.InBucket("taxes_on_profit")), Line(view, MapEnd.InBucket("taxes_on_profit"), MapEnd.Of(RiverModule.Dividend)));
        var pocket = Line(view, MapEnd.Of(RiverModule.Dividend), MapEnd.InBucket("pocket"));

        view.ShouldSatisfyAllConditions(
            () => new[] { payIn, payBack, dividendIn, dividendBack, pocket }.Select(Turns).ShouldAllBe(turns => turns),
            () => (Row(payIn) == Row(dividendIn), Row(payBack) == Row(dividendBack)).ShouldBe((true, true)),
            () => (Row(payIn) < Row(payBack), Row(payBack) < Row(pocket)).ShouldBe((true, true)),
            () => (Segments(payIn).Any(a => Segments(payBack).Any(b => Cross(a, b))), Segments(dividendIn).Any(a => Segments(dividendBack).Any(b => Cross(a, b)))).ShouldBe((false, false)));
    }

    // Corporate tax's way into its taxes comes down clear of the dividend's ways into them and back, crossing none
    // of the band's rows they turn along.
    [Theory]
    [MemberData(nameof(ExtremeRivers))]
    public void Routed_corporateTaxsWayIntoItsTaxes_crossesNoneOfTheDividendsWaysIntoThemAndBack(string plan)
    {
        var river = Extreme(plan);
        var view = MapView.From(river, river.Run());

        var corporate = view.Bundles().Where(line => line.From == MapEnd.Of(RiverModule.CorporateTax) && line.To == MapEnd.InBucket("taxes_on_profit")).ToList();
        var dividend = view.Bundles().Where(line => line.Touches(RiverModule.Dividend) && (line.From == MapEnd.InBucket("taxes_on_profit") || line.To == MapEnd.InBucket("taxes_on_profit"))).ToList();

        corporate.SelectMany(a => dividend.Where(b => Segments(a).Any(sa => Segments(b).Any(sb => Cross(sa, sb)))).Select(b => $"{b.From.Id} to {b.To.Id}"))
            .ShouldBeEmpty();
    }

    // The royalty's part paid apart leaves its bottom and turns along a row of its own under it to the pocket's
    // lane, where its amount stands; its line on to Director pay comes down beside it, crossing it nowhere.
    [Fact]
    public void Routed_theRoyaltysWayToThePocket_leavesItsBottomAlongARowOfItsOwnCrossingNoneOfItsLines()
    {
        var view = MapView.From(MapLinksTests.Full, MapLinksTests.Full.Run());
        var (royalty, pay) = (view.Modules.Single(node => node.End.Module == RiverModule.Copyright).Slot, view.Modules.Single(node => node.End.Module == RiverModule.DirectorPay).Slot);

        var pocket = Line(view, MapEnd.Of(RiverModule.Copyright), MapEnd.InBucket("pocket"));
        var others = view.Bundles().Where(line => line.From == MapEnd.Of(RiverModule.Copyright) && line.To != MapEnd.InBucket("pocket")).ToList();

        pocket.ShouldSatisfyAllConditions(
            () => (pocket.Route.Points[0].Y, Turns(pocket)).ShouldBe((royalty.Bottom, true)),
            () => (Row(pocket) > royalty.Bottom, Row(pocket) < pay.Top).ShouldBe((true, true)),
            () => others.Where(other => Segments(pocket).Any(a => Segments(other).Any(b => Cross(a, b)))).Select(other => other.To.Id).ShouldBeEmpty());
    }

    // The condition a royalty sets on the split leaves the royalty's right side, so it never climbs beside the
    // split's line into the royalty's top.
    [Fact]
    public void Routed_theConditionARoyaltySetsOnTheSplit_leavesTheRoyaltysRightSide()
    {
        var view = MapView.From(MapLinksTests.Full, MapLinksTests.Full.Run());
        var royalty = view.Modules.Single(node => node.End.Module == RiverModule.Copyright).Slot;

        var condition = view.Bundles().Single(line => line.Kind == MapLinkKind.Condition && line.From == MapEnd.Of(RiverModule.Copyright));

        condition.Route.Points[0].X.ShouldBe(royalty.Right);
    }

    // The band over the buckets grows the map only by what the streams' last row lacks under its boxes: its three
    // rows with a dividend, its first 0.1 of a chain row under the lowest box, each 0.35 under the one before, the
    // buckets 0.2 under the last, a whole chain row; two with Director pay alone, 0.65. Every lever: Director pay
    // holds a badge, its row keeps 0.4 under it, the buckets' row grows from 0.9 to 0.7 and the 0.6 the band lacks,
    // 10.7 chain rows to 11.1; the example: 0.55 under its boxes, 6.1 to 6.35; the company skipped: 0.55 under its
    // box covers the band, 3.5 as before.
    [Theory]
    [InlineData("every lever", 11.1)]
    [InlineData("the example", 6.35)]
    [InlineData("the company skipped", 3.5)]
    public void From_aPlan_growsByTheBandItsStreamsLackRoomFor(string plan, double units)
    {
        var river = plan switch
        {
            "every lever" => MapLinksTests.Full,
            "the example" => RiverInputs.Example,
            _ => MapLinksTests.Full.SkipCompany(),
        };

        var view = MapView.From(river, river.Run());

        view.Units.ShouldBe((decimal)units);
    }

    // The one line of the map between two ends, of the kind money has between them.
    private static MapBundle Line(MapView view, MapEnd from, MapEnd to) =>
        view.Bundles().Single(bundle => bundle.From == from && bundle.To == to && bundle.Kind != MapLinkKind.Condition);

    // Whether a line turns once along a row: down, across, down.
    private static bool Turns(MapBundle line) =>
        line.Route.Points is [var a, var b, var c, var d] && a.X == b.X && b.Y == c.Y && c.X == d.X && b.X != c.X;

    // The y of the row a line turns along.
    private static decimal Row(MapBundle line) => line.Route.Points[1].Y;

    private static IEnumerable<(MapPoint A, MapPoint B)> Segments(MapBundle line) => Segments(line.Route);

    // Whether two segments, one across and one down, cross inside both.
    private static bool Cross((MapPoint A, MapPoint B) a, (MapPoint A, MapPoint B) b)
    {
        var (across, down) = a.A.Y == a.B.Y ? (a, b) : (b, a);
        return across.A.Y == across.B.Y && down.A.X == down.B.X
            && Math.Min(across.A.X, across.B.X) < down.A.X && down.A.X < Math.Max(across.A.X, across.B.X)
            && Math.Min(down.A.Y, down.B.Y) < across.A.Y && across.A.Y < Math.Max(down.A.Y, down.B.Y);
    }

    // A lane its lines cannot all fit at their widths squeezes them into it, never past it: five lines down
    // the pocket's lane, each as large as the run's largest line, cross no box and land on the pocket.
    [Fact]
    public void Routed_aLaneTooNarrowForItsLines_squeezesThemIntoItCrossingNoBox()
    {
        var river = MapLinksTests.Full;
        var grid = Grid(river);
        MapLink[] links =
        [
            Money(RiverModule.CompanyYear, MapEnd.Of(RiverModule.Vouchers)),
            .. new[] { RiverModule.Vouchers, RiverModule.Allowances, RiverModule.Rent, RiverModule.Car, RiverModule.Copyright }.Select(module => Money(module, MapEnd.InBucket("pocket"))),
        ];

        var routes = RoutesOf(MapLayout.Routed(grid, links));

        routes.ShouldSatisfyAllConditions(
            () => routes.Select(route => route.Route.Width).Distinct().ShouldBe([MapLineWidths.Thickest]),
            () => Crossings(routes, Boxes(river), grid.Units).ShouldBeEmpty(),
            () => Overhangs(routes, grid).ShouldBeEmpty());
    }

    // The ends a card's line joins in these plans are among those the routes are held to below.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void DrainEnds_everyModuleAndBucketALineJoins_isHeldToItsRoutes(bool companyPays, bool companySkipped)
    {
        var river = companyPays ? MapLinksTests.Full.WithContributionsPaidBy(Netto.Core.SocialContributions.ContributionsPaidBy.Company) : MapLinksTests.Full;
        river = companySkipped ? river.SkipCompany() : river;

        MapLinks.Of(river, river.Run()).Where(link => link.From.Bucket is not null || link.To.Bucket is not null)
            .Select(link => ((link.From.Module ?? link.To.Module)!.Value, link.From.Bucket ?? link.To.Bucket!))
            .Where(ends => !DrainEnds.Contains(ends)).ShouldBeEmpty();
    }

    // A line between a module and a bucket, either way, beside every other line of the plan: it ends on
    // its two boxes, crosses none, runs across or down only and shares no stretch with another, and at its
    // width keeps apart from the others and off the boxes.
    [Theory]
    [MemberData(nameof(EveryDrainEnds))]
    public void Routed_aLineEitherWayBetweenAModuleAndABucket_endsOnBothAndCrossesNoBox(int ends)
    {
        var (module, bucket) = DrainEnds[ends];
        var river = MapLinksTests.Full;
        var grid = Grid(river);
        MapLink[] both =
        [
            new(MapLinkKind.IntoBucket, MapEnd.Of(module), MapEnd.InBucket(bucket), module, new MapLabel(MapText.Map, "Into", []), 1m),
            new(MapLinkKind.IntoBucket, MapEnd.InBucket(bucket), MapEnd.Of(module), module, new MapLabel(MapText.Map, "Back", []), 1m),
        ];

        var routes = Routes(river, both);

        var boxes = Boxes(river);
        routes.ShouldSatisfyAllConditions(
            () => routes.Where(route => !OnEdge(grid, route.From, route.Route.Points[0]) || !EndsOn(grid, route.From, route.To, route.Route.End))
                .Select(route => route.Name).ShouldBeEmpty(),
            () => routes.SelectMany(route => Segments(route.Route).SelectMany(segment =>
                boxes.Where(box => Enters(segment, box.Slot)).Select(box => $"{route.Name} through {box.Id}"))).ShouldBeEmpty(),
            () => routes.Where(route => Segments(route.Route).Any(segment => segment.A.X != segment.B.X && segment.A.Y != segment.B.Y))
                .Select(route => route.Name).ShouldBeEmpty(),
            () => routes.SelectMany((a, i) => routes.Skip(i + 1)
                .Where(b => Segments(a.Route).Any(sa => Segments(b.Route).Any(sb => Shared(sa, sb))))
                .Select(b => $"{a.Name} with {b.Name}")).ShouldBeEmpty(),
            () => Crowded(routes, grid.Units).ShouldBeEmpty(),
            () => Crossings(routes, boxes, grid.Units).ShouldBeEmpty(),
            () => Overhangs(routes, grid).ShouldBeEmpty(),
            () => Arrowheads(routes, grid.Units).ShouldBeEmpty());
    }

    // A line back out of a bucket ends on its module, its arrow pointing into it.
    [Theory]
    [MemberData(nameof(EveryDrainEnds))]
    public void Routed_aLineBackOutOfABucket_endsOnItsModule(int ends)
    {
        var (module, bucket) = DrainEnds[ends];
        var river = MapLinksTests.Full;
        var grid = Grid(river);
        var back = new MapLink(MapLinkKind.IntoBucket, MapEnd.InBucket(bucket), MapEnd.Of(module), module, new MapLabel(MapText.Map, "Back", []), 1m);

        var route = RouteOf(MapLayout.Routed(grid, [back]).Single());

        (OnEdge(grid, MapEnd.InBucket(bucket), route.Points[0]), OnEdge(grid, MapEnd.Of(module), route.End)).ShouldBe((true, true));
    }

    // Links of the same ends and kind are one line on the map, their words drawn together.
    [Fact]
    public void Routed_linksOfTheSameEndsAndKind_shareOneRoute()
    {
        var river = MapLinksTests.Full;

        var links = MapLayout.Routed(MapLayout.Grid(MapView.ModulesOf(river)), MapLinks.Of(river, river.Run()));

        links.GroupBy(link => (link.From, link.To, link.Kind)).Where(bundle => bundle.Select(link => link.Route).Distinct().Count() != 1)
            .Select(bundle => $"{bundle.Key.From.Id} to {bundle.Key.To.Id}").ShouldBeEmpty();
    }

    // What the car's costs leave the river by is a stub out of its right side.
    [Fact]
    public void Routed_theCarsCosts_runOutOfItsRightSide()
    {
        var river = MapLinksTests.Full;
        var grid = Grid(river);

        var route = RouteOf(MapLayout.Routed(grid, MapLinks.Of(river, river.Run())).Single(link => link.To.IsOut));

        (route.Points[0].X, route.End.X > grid.Slot(MapEnd.Of(RiverModule.Car)).Right, route.Arrow).ShouldBe((grid.Slot(MapEnd.Of(RiverModule.Car)).Right, true, MapDirection.Right));
    }

    // Every composition of the levers, with the company's year and without it, and, with it, every set of
    // the levers in the plan left empty (quiet, by the bits of a number as the composition's), which the
    // map draws quiet but for Expense allowances beside a rent paid.
    public static TheoryData<int, int, bool> Rivers() => Data(Plans());

    public static TheoryData<int, int, bool> RiversWithoutEmptyAllowancesBesideAPaidRent() => Data(Plans().Where(plan => !EmptyAllowancesBesideAPaidRent(plan)));

    public static TheoryData<int, int, bool> RiversWithEmptyAllowancesBesideAPaidRent() => Data(Plans().Where(EmptyAllowancesBesideAPaidRent));

    private static IEnumerable<(int Bits, int Quiet, bool CompanySkipped)> Plans()
    {
        foreach (var bits in Enumerable.Range(0, 32))
        {
            foreach (var quiet in Enumerable.Range(0, 32).Where(quiet => (quiet & ~bits) == 0))
                yield return (bits, quiet, false);
            yield return (bits, 0, true);
        }
    }

    private static bool EmptyAllowancesBesideAPaidRent((int Bits, int Quiet, bool CompanySkipped) plan) =>
        Levers(plan.Quiet).Contains(OptionalModule.Allowances) && Levers(plan.Bits).Contains(OptionalModule.Rent) && !Levers(plan.Quiet).Contains(OptionalModule.Rent);

    private static TheoryData<int, int, bool> Data(IEnumerable<(int Bits, int Quiet, bool CompanySkipped)> plans)
    {
        var data = new TheoryData<int, int, bool>();
        foreach (var (bits, quiet, companySkipped) in plans)
            data.Add(bits, quiet, companySkipped);

        return data;
    }

    internal static RiverInputs River(int bits, int quiet, bool companySkipped)
    {
        var river = MapQuietTests.Emptied(MapLinksTests.Full with { Composition = MapModulesTests.Composition(bits) }, Levers(quiet));
        return companySkipped ? river.SkipCompany() : river;
    }

    private static OptionalModule[] Levers(int bits) => [.. PlanComposition.Modules.Where((_, index) => (bits & (1 << index)) != 0)];

    // The levers the map draws quiet, read off the map itself, the conditions of its year among its links.
    private static HashSet<RiverModule> Quiet(RiverInputs river) =>
        MapView.From(river, river.Run()).Modules.Where(node => node.Quiet).Select(node => node.End.Module!.Value).ToHashSet();

    // The plan's grid with the levers the map draws quiet and every box holding the most badges it can.
    private static MapLayout.MapGrid Grid(RiverInputs river) => MapLayout.Grid(MapView.ModulesOf(river), Quiet(river), MostBadges);

    // How tall a module's box is, in chain rows.
    private static decimal Units(MapLayout.MapGrid grid, RiverModule module) => Math.Round(grid.Slot(MapEnd.Of(module)).Height * grid.Units / 100m, 12);

    // Each box of the map, a quiet row's pill once, with the levers in it.
    private static IReadOnlyList<(string Id, MapSlot Slot, IReadOnlyList<RiverModule> Modules)> Boxes(RiverInputs river)
    {
        var grid = Grid(river);
        return
        [
            .. MapView.ModulesOf(river).GroupBy(module => grid.Slot(MapEnd.Of(module)))
                .Select(place => (string.Join('+', place.Select(MapModules.Name)), place.Key, (IReadOnlyList<RiverModule>)[.. place])),
            .. BucketsView.Keys.Select(key => (key, grid.Slot(MapEnd.InBucket(key)), (IReadOnlyList<RiverModule>)[])),
        ];
    }

    // The chain's line past quiet levers runs through their pill, and through no other box.
    private static bool RunsThrough(IReadOnlyList<RiverModule> through, IReadOnlyList<RiverModule> box) => box.Count > 0 && box.All(through.Contains);

    // Each line of the plan, the run's links and the conditions between the modules it draws with a box,
    // folded past the levers the map draws quiet as the map folds them, once.
    private static IReadOnlyList<RoutedLine> Routes(RiverInputs river, params MapLink[] more)
    {
        var run = river.Run();
        var modules = MapView.ModulesOf(river);
        var quiet = Quiet(river);
        var conditions = Conditions
            .Where(condition => modules.Contains(condition.From) && modules.Contains(condition.To) && !quiet.Contains(condition.From) && !quiet.Contains(condition.To))
            .Select(condition => new MapLink(MapLinkKind.Condition, MapEnd.Of(condition.From), MapEnd.Of(condition.To), condition.From,
                new MapLabel(MapText.Map, "Condition", []), null));

        return RoutesOf(MapLayout.Routed(MapLayout.Grid(modules, quiet, MostBadges), MapQuiet.Fold([.. MapLinks.Of(river, run), .. conditions, .. more], modules, quiet)));
    }

    // Each line of links routed, once.
    private static IReadOnlyList<RoutedLine> RoutesOf(IReadOnlyList<MapLink> links) =>
    [
        .. links.GroupBy(link => link.Route).Select(bundle => bundle.First())
            .Select(link => new RoutedLine($"{link.Kind} {link.From.Id} to {link.To.Id}", link.From, link.To, RouteOf(link), link.Through)),
    ];

    // The route the layout gave a link, which every link it routed has.
    private static MapRoute RouteOf(MapLink link) =>
        link.Route ?? throw new InvalidOperationException($"The layout gave {link.Kind} {link.From.Id} to {link.To.Id} no route.");

    // A line as the layout routes it, named by its kind and its ends.
    private sealed record RoutedLine(string Name, MapEnd From, MapEnd To, MapRoute Route, IReadOnlyList<RiverModule> Through);

    // The sizes the map is drawn at, in pixels, each with a chain row at its smallest, 3.25rem
    // (MapLayout.SmallestRowRem), the shortest the map is ever drawn: the narrowest, a column 34rem wide
    // beside the chosen module's from 1,201 px (MapLayout.NarrowestRem), and its 1,184 px at 1,920 px.
    private static readonly (string Name, decimal Width)[] Sizes = [("1,201 px", MapLayout.NarrowestRem * 16m), ("1,920 px", 1_184m)];

    private const decimal RowPixels = MapLayout.SmallestRowRem * 16m;

    // A hundredth of a pixel, for the rounding of the routes' percents.
    private const decimal Tolerance = 0.01m;

    // Every two lines running side by side closer than their half-widths and a halo's gap, at each size.
    private static List<string> Crowded(IReadOnlyList<RoutedLine> routes, decimal units)
    {
        var faults = new List<string>();
        foreach (var (size, width) in Sizes)
        {
            var scale = (X: width / 100m, Y: RowPixels * units / 100m);
            faults.AddRange(routes.SelectMany((a, i) => routes.Skip(i + 1)
                .Where(b => Segments(a.Route).Any(sa => Segments(b.Route).Any(sb => Closer(sa, sb, (a.Route.Width + b.Route.Width) / 2m + MapLinkStyle.HaloGap, scale))))
                .Select(b => $"{a.Name} with {b.Name} at {size}")));
        }

        return faults;
    }

    // Whether two segments, both across or both down, run side by side along a stretch they share, closer
    // than the pixels given.
    private static bool Closer((MapPoint A, MapPoint B) a, (MapPoint A, MapPoint B) b, decimal apart, (decimal X, decimal Y) scale)
    {
        if (a.A.X == a.B.X && b.A.X == b.B.X)
            return Along(a.A.Y, a.B.Y, b.A.Y, b.B.Y) && Math.Abs(a.A.X - b.A.X) * scale.X < apart - Tolerance;
        if (a.A.Y == a.B.Y && b.A.Y == b.B.Y)
            return Along(a.A.X, a.B.X, b.A.X, b.B.X) && Math.Abs(a.A.Y - b.A.Y) * scale.Y < apart - Tolerance;
        return false;
    }

    // Whether two stretches of one axis share a part of it.
    private static bool Along(decimal a1, decimal a2, decimal b1, decimal b2) =>
        Math.Max(Math.Min(a1, a2), Math.Min(b1, b2)) < Math.Min(Math.Max(a1, a2), Math.Max(b1, b2));

    // Every line whose stroke, at its width, enters a box, but the chain's its own quiet row, or runs past
    // the map's edge, at each size.
    private static List<string> Crossings(IReadOnlyList<RoutedLine> routes, IReadOnlyList<(string Id, MapSlot Slot, IReadOnlyList<RiverModule> Modules)> boxes, decimal units)
    {
        var faults = new List<string>();
        foreach (var (size, width) in Sizes)
        {
            var scale = (X: width / 100m, Y: RowPixels * units / 100m);
            var map = Area(new MapSlot(0m, 0m, 100m, 100m), scale);
            foreach (var route in routes)
            {
                foreach (var stroke in Segments(route.Route).Select(segment => Stroke(segment, route.Route.Width, scale)))
                {
                    faults.AddRange(boxes.Where(box => !RunsThrough(route.Through, box.Modules) && Overlaps(stroke, Area(box.Slot, scale)))
                        .Select(box => $"{route.Name} through {box.Id} at {size}"));
                    if (stroke.Left < map.Left - Tolerance || stroke.Right > map.Right + Tolerance || stroke.Top < map.Top - Tolerance || stroke.Bottom > map.Bottom + Tolerance)
                        faults.Add($"{route.Name} past the map's edge at {size}");
                }
            }
        }

        return faults;
    }

    // What a segment covers at its width, in pixels: its length, and half its width each side of it.
    private static (decimal Left, decimal Top, decimal Right, decimal Bottom) Stroke((MapPoint A, MapPoint B) segment, decimal width, (decimal X, decimal Y) scale)
    {
        var (half, a, b) = (width / 2m, (X: segment.A.X * scale.X, Y: segment.A.Y * scale.Y), (X: segment.B.X * scale.X, Y: segment.B.Y * scale.Y));
        return a.X == b.X
            ? (a.X - half, Math.Min(a.Y, b.Y), a.X + half, Math.Max(a.Y, b.Y))
            : (Math.Min(a.X, b.X), a.Y - half, Math.Max(a.X, b.X), a.Y + half);
    }

    // What a box covers, in percent of the map, or in pixels at the scale given.
    private static (decimal Left, decimal Top, decimal Right, decimal Bottom) Area(MapSlot slot, (decimal X, decimal Y)? scale = null)
    {
        var (x, y) = scale ?? (1m, 1m);
        return (slot.Left * x, slot.Top * y, slot.Right * x, slot.Bottom * y);
    }

    // Whether two areas share more than an edge, a hundredth of their unit left for rounding.
    private static bool Overlaps((decimal Left, decimal Top, decimal Right, decimal Bottom) a, (decimal Left, decimal Top, decimal Right, decimal Bottom) b) =>
        a.Left < b.Right - Tolerance && b.Left < a.Right - Tolerance && a.Top < b.Bottom - Tolerance && b.Top < a.Bottom - Tolerance;

    // Every line whose width hangs past the side of a box it meets, where it leaves it and where it ends,
    // the end of a line out of the river left out: on the narrowest map, where a side is the fewest pixels.
    private static List<string> Overhangs(IReadOnlyList<RoutedLine> routes, MapLayout.MapGrid grid)
    {
        var scale = (X: Sizes[0].Width / 100m, Y: RowPixels * grid.Units / 100m);
        return
        [
            .. routes.Where(route => Hangs(grid.Slot(route.From), route.Route.Points[0], route.Route.Width, scale)).Select(route => $"{route.Name} off {route.From.Id}"),
            .. routes.Where(route => !route.To.IsOut && Hangs(grid.Slot(route.To), route.Route.End, route.Route.Width, scale)).Select(route => $"{route.Name} off {route.To.Id}"),
        ];
    }

    // Whether a line meeting a box at a point of its edge hangs past the side the point is on, at its width.
    private static bool Hangs(MapSlot box, MapPoint at, decimal width, (decimal X, decimal Y) scale)
    {
        var half = width / 2m - Tolerance;
        return at.X == box.Left || at.X == box.Right
            ? (at.Y - box.Top) * scale.Y < half || (box.Bottom - at.Y) * scale.Y < half
            : (at.X - box.Left) * scale.X < half || (box.Right - at.X) * scale.X < half;
    }

    // Every two arrowheads that overlap, every arrowhead over another line, and every arrowhead longer than
    // the stretch of line it ends, on the narrowest map: an arrowhead is drawn where a line ends on a module
    // or out of the river, none into a bucket.
    private static List<string> Arrowheads(IReadOnlyList<RoutedLine> routes, decimal units)
    {
        var scale = (X: Sizes[0].Width / 100m, Y: RowPixels * units / 100m);
        var heads = routes.Where(route => route.To.Bucket is null)
            .Select(route => (route.Name, Area: Head(route.Route, scale), Long: MapLineWidths.ArrowLength(route.Route.Width) > Last(route.Route, scale) + Tolerance)).ToList();
        return
        [
            .. heads.Where(head => head.Long).Select(head => $"{head.Name}'s arrowhead past its last stretch"),
            .. heads.SelectMany((a, i) => heads.Skip(i + 1).Where(b => Overlaps(a.Area, b.Area)).Select(b => $"{a.Name}'s arrowhead over {b.Name}'s")),
            .. heads.SelectMany(head => routes.Where(route => route.Name != head.Name)
                .Where(route => Segments(route.Route).Any(segment => Overlaps(head.Area, Stroke(segment, route.Route.Width, scale))))
                .Select(route => $"{head.Name}'s arrowhead over {route.Name}")),
        ];
    }

    // What an arrowhead covers, in pixels, its point on the line's end.
    private static (decimal Left, decimal Top, decimal Right, decimal Bottom) Head(MapRoute route, (decimal X, decimal Y) scale)
    {
        var (half, length, x, y) = (MapLineWidths.ArrowHalf(route.Width), MapLineWidths.ArrowLength(route.Width), route.End.X * scale.X, route.End.Y * scale.Y);
        return route.Arrow switch
        {
            MapDirection.Down => (x - half, y - length, x + half, y),
            MapDirection.Up => (x - half, y, x + half, y + length),
            MapDirection.Right => (x - length, y - half, x, y + half),
            _ => (x, y - half, x + length, y + half),
        };
    }

    // How long the last stretch of a line is, in pixels.
    private static decimal Last(MapRoute route, (decimal X, decimal Y) scale) =>
        Math.Abs(route.Points[^1].X - route.Points[^2].X) * scale.X + Math.Abs(route.Points[^1].Y - route.Points[^2].Y) * scale.Y;

    // A line of money as large as the run's largest, from a module to an end.
    private static MapLink Money(RiverModule module, MapEnd to) =>
        new(to.Module is null ? MapLinkKind.IntoBucket : MapLinkKind.PassedOn, MapEnd.Of(module), to, module, new MapLabel(MapText.Map, "Money", []), 100_000m);

    // The plans at the edges of what a plan holds, by name.
    public static TheoryData<string> ExtremeRivers() =>
        new("the pay at its ceiling", "a low pay", "a royalty past its limits", "the costly car", "a car costly to run", "the company skipped, a high pay", "the company skipped, a low pay");

    private static RiverInputs Extreme(string plan) => plan switch
    {
        "the pay at its ceiling" => MapLinksTests.Full.WithDirectorPay(10_000_000m),
        "a low pay" => MapLinksTests.Full.WithDirectorPay(12_000m),
        "a royalty past its limits" => MapLinksTests.Full.WithRoyalty(60_000m),
        "the costly car" => MapViewConditionsTests.CostlyCar,
        "a car costly to run" => MapLinksTests.Full.WithCarCosts(40_000m),
        "the company skipped, a high pay" => MapLinksTests.Full.SkipCompany().WithDirectorPay(200_000m),
        "the company skipped, a low pay" => MapLinksTests.Full.SkipCompany().WithDirectorPay(12_000m),
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, "A plan the tests have no inputs for."),
    };

    private static IEnumerable<(MapPoint A, MapPoint B)> Segments(MapRoute route) => route.Points.Zip(route.Points.Skip(1));

    // Whether a segment, across or down, runs through the inside of a box, its edges left out.
    private static bool Enters((MapPoint A, MapPoint B) segment, MapSlot box)
    {
        var (left, right) = (Math.Min(segment.A.X, segment.B.X), Math.Max(segment.A.X, segment.B.X));
        var (top, bottom) = (Math.Min(segment.A.Y, segment.B.Y), Math.Max(segment.A.Y, segment.B.Y));
        return left == right
            ? left > box.Left && left < box.Right && Math.Max(top, box.Top) < Math.Min(bottom, box.Bottom)
            : top > box.Top && top < box.Bottom && Math.Max(left, box.Left) < Math.Min(right, box.Right);
    }

    // Whether two segments lie on one line and overlap along a stretch of it.
    private static bool Shared((MapPoint A, MapPoint B) a, (MapPoint A, MapPoint B) b)
    {
        if (a.A.X == a.B.X && b.A.X == b.B.X && a.A.X == b.A.X)
            return Math.Max(Math.Min(a.A.Y, a.B.Y), Math.Min(b.A.Y, b.B.Y)) < Math.Min(Math.Max(a.A.Y, a.B.Y), Math.Max(b.A.Y, b.B.Y));
        if (a.A.Y == a.B.Y && b.A.Y == b.B.Y && a.A.Y == b.A.Y)
            return Math.Max(Math.Min(a.A.X, a.B.X), Math.Min(b.A.X, b.B.X)) < Math.Min(Math.Max(a.A.X, a.B.X), Math.Max(b.A.X, b.B.X));
        return false;
    }

    private static bool OnEdge(MapLayout.MapGrid grid, MapEnd end, MapPoint point)
    {
        var box = grid.Slot(end);
        var withinX = point.X >= box.Left && point.X <= box.Right;
        var withinY = point.Y >= box.Top && point.Y <= box.Bottom;
        return (withinX && (point.Y == box.Top || point.Y == box.Bottom)) || (withinY && (point.X == box.Left || point.X == box.Right));
    }

    // A line ends on its end's box; one out of the river ends past its module's right side, in its row.
    private static bool EndsOn(MapLayout.MapGrid grid, MapEnd from, MapEnd to, MapPoint point)
    {
        if (!to.IsOut)
            return OnEdge(grid, to, point);

        var box = grid.Slot(from);
        return point.X > box.Right && point.Y > box.Top && point.Y < box.Bottom;
    }
}

using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Where the map draws its modules, its buckets and its lines: fixed slots in the river's shape, rows
// closing up over a lever out of the plan, and an orthogonal route for every link, the conditions one
// module sets on another among them, that crosses no box, ends on its two boxes and shares no stretch
// with another link's, whatever the plan holds.
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

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Grid_anyPlan_drawsNoBoxOverAnother(int bits, bool companySkipped)
    {
        var boxes = Boxes(River(bits, companySkipped));

        boxes.SelectMany((a, i) => boxes.Skip(i + 1).Where(b => Overlap(a.Slot, b.Slot)).Select(b => $"{a.Id} over {b.Id}")).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_runsEveryLineAcrossOrDownOnly(int bits, bool companySkipped)
    {
        Routes(River(bits, companySkipped)).Where(route => Segments(route.Route).Any(segment => segment.A.X != segment.B.X && segment.A.Y != segment.B.Y))
            .Select(route => route.Name).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_endsEveryLineOnItsTwoBoxes(int bits, bool companySkipped)
    {
        var river = River(bits, companySkipped);
        var grid = MapLayout.Grid(MapView.ModulesOf(river));

        Routes(river).Where(route => !OnEdge(grid, route.From, route.Route.Points[0]) || !EndsOn(grid, route.From, route.To, route.Route.End))
            .Select(route => route.Name).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_crossesNoBox(int bits, bool companySkipped)
    {
        var river = River(bits, companySkipped);
        var boxes = Boxes(river);

        Routes(river).SelectMany(route => Segments(route.Route).SelectMany(segment =>
                boxes.Where(box => Enters(segment, box.Slot)).Select(box => $"{route.Name} through {box.Id}")))
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_sharesNoStretchOfLineBetweenTwoLinks(int bits, bool companySkipped)
    {
        var routes = Routes(River(bits, companySkipped));

        routes.SelectMany((a, i) => routes.Skip(i + 1)
                .Where(b => Segments(a.Route).Any(sa => Segments(b.Route).Any(sb => Shared(sa, sb))))
                .Select(b => $"{a.Name} with {b.Name}"))
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(Rivers))]
    public void Routed_anyPlan_staysInsideTheMap(int bits, bool companySkipped)
    {
        Routes(River(bits, companySkipped)).Where(route => route.Route.Points.Any(point => point.X is < 0m or > 100m || point.Y is < 0m or > 100m))
            .Select(route => route.Name).ShouldBeEmpty();
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
    // its two boxes, crosses none, runs across or down only and shares no stretch with another.
    [Theory]
    [MemberData(nameof(EveryDrainEnds))]
    public void Routed_aLineEitherWayBetweenAModuleAndABucket_endsOnBothAndCrossesNoBox(int ends)
    {
        var (module, bucket) = DrainEnds[ends];
        var river = MapLinksTests.Full;
        var grid = MapLayout.Grid(MapView.ModulesOf(river));
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
                .Select(b => $"{a.Name} with {b.Name}")).ShouldBeEmpty());
    }

    // A line back out of a bucket ends on its module, its arrow pointing into it.
    [Theory]
    [MemberData(nameof(EveryDrainEnds))]
    public void Routed_aLineBackOutOfABucket_endsOnItsModule(int ends)
    {
        var (module, bucket) = DrainEnds[ends];
        var river = MapLinksTests.Full;
        var grid = MapLayout.Grid(MapView.ModulesOf(river));
        var back = new MapLink(MapLinkKind.IntoBucket, MapEnd.InBucket(bucket), MapEnd.Of(module), module, new MapLabel(MapText.Map, "Back", []), 1m);

        var route = MapLayout.Routed(grid, [back]).Single().Route!;

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
        var grid = MapLayout.Grid(MapView.ModulesOf(river));

        var route = MapLayout.Routed(grid, MapLinks.Of(river, river.Run())).Single(link => link.To.IsOut).Route!;

        (route.Points[0].X, route.End.X > grid.Slot(MapEnd.Of(RiverModule.Car)).Right, route.Arrow).ShouldBe((grid.Slot(MapEnd.Of(RiverModule.Car)).Right, true, MapDirection.Right));
    }

    // Every composition of the levers, with the company's year and without it.
    public static TheoryData<int, bool> Rivers()
    {
        var data = new TheoryData<int, bool>();
        foreach (var bits in Enumerable.Range(0, 32))
        {
            data.Add(bits, false);
            data.Add(bits, true);
        }

        return data;
    }

    private static RiverInputs River(int bits, bool companySkipped)
    {
        var river = MapLinksTests.Full with { Composition = MapModulesTests.Composition(bits) };
        return companySkipped ? river.SkipCompany() : river;
    }

    private static IReadOnlyList<(string Id, MapSlot Slot)> Boxes(RiverInputs river)
    {
        var grid = MapLayout.Grid(MapView.ModulesOf(river));
        return [.. MapView.ModulesOf(river).Select(MapEnd.Of).Concat(BucketsView.Keys.Select(MapEnd.InBucket)).Select(end => (end.Id, grid.Slot(end)))];
    }

    // Each line of the plan, the run's links and the conditions between the modules it draws, once.
    private static IReadOnlyList<(string Name, MapEnd From, MapEnd To, MapRoute Route)> Routes(RiverInputs river, params MapLink[] more)
    {
        var modules = MapView.ModulesOf(river);
        var conditions = Conditions.Where(condition => modules.Contains(condition.From) && modules.Contains(condition.To))
            .Select(condition => new MapLink(MapLinkKind.Condition, MapEnd.Of(condition.From), MapEnd.Of(condition.To), condition.From,
                new MapLabel(MapText.Map, "Condition", []), null));

        var links = MapLayout.Routed(MapLayout.Grid(modules), [.. MapLinks.Of(river, river.Run()), .. conditions, .. more]);

        return [.. links.GroupBy(link => link.Route).Select(bundle => bundle.First())
            .Select(link => ($"{link.Kind} {link.From.Id} to {link.To.Id}", link.From, link.To, link.Route!))];
    }

    private static IEnumerable<(MapPoint A, MapPoint B)> Segments(MapRoute route) => route.Points.Zip(route.Points.Skip(1));

    private static bool Overlap(MapSlot a, MapSlot b) => a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;

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

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
    public void Grid_companySkipped_isDirectorPayOverTheBuckets()
    {
        MapLayout.Grid(MapView.ModulesOf(MapLinksTests.Full.SkipCompany())).Rows.ShouldBe([MapRow.StreamBottom, MapRow.Buckets]);
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
    private static IReadOnlyList<(string Name, MapEnd From, MapEnd To, MapRoute Route)> Routes(RiverInputs river)
    {
        var modules = MapView.ModulesOf(river);
        var conditions = Conditions.Where(condition => modules.Contains(condition.From) && modules.Contains(condition.To))
            .Select(condition => new MapLink(MapLinkKind.Condition, MapEnd.Of(condition.From), MapEnd.Of(condition.To), condition.From,
                new MapLabel(MapText.Map, "Condition", []), null));

        var links = MapLayout.Routed(MapLayout.Grid(modules), [.. MapLinks.Of(river, river.Run()), .. conditions]);

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

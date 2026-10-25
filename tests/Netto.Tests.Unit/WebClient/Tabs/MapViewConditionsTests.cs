using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The engine's conditions on the map the run draws (MapView): among its links, each routed, between
// modules on the map, and none on a run without a year.
public class MapViewConditionsTests
{
    // The map draws the run's conditions among its links, each routed: on every lever with the costly car,
    // whose benefit is beyond 20% of the pay.
    [Fact]
    public void From_aRun_drawsTheYearsConditionsAmongItsLinks()
    {
        var river = CostlyCar;
        var run = river.Run();

        var conditions = MapView.From(river, run).Links.Where(link => link.Kind == MapLinkKind.Condition).ToList();

        conditions.ShouldSatisfyAllConditions(
            () => conditions.Select(Shape).ShouldBe(MapConditions.From(YearConditions.Of(run.Year!), MapView.ModulesOf(river)).Select(Shape)),
            () => conditions.ShouldAllBe(link => link.Route != null),
            () => conditions.Single(link => link.From.Module == RiverModule.Car).Holds.ShouldBe(false));
    }

    // A link as a viewer reads it, its words' arguments by value.
    private static (MapLinkKind, string, string, string, string, bool?) Shape(MapLink link) =>
        (link.Kind, link.From.Id, link.To.Id, link.Carries.Key, string.Join('|', link.Carries.Args), link.Holds);

    // A refused amount leaves the map without a link, and the company skipped leaves no year to read.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void From_aRunWithoutAYear_drawsNoCondition(bool refused)
    {
        var river = refused ? MapLinksTests.Full.WithReserve(-1m) : MapLinksTests.Full.SkipCompany();

        MapView.From(river, river.Run()).Links.Where(link => link.Kind == MapLinkKind.Condition).ShouldBeEmpty();
    }

    // With the allowances out of the plan the rent sets no condition the map can draw.
    [Fact]
    public void From_allowancesOutOfThePlan_drawsNoConditionOnTheRent()
    {
        var river = MapLinksTests.Full.TakeOutOfPlan(OptionalModule.Allowances);

        MapView.From(river, river.Run()).Links.Where(link => link is { Kind: MapLinkKind.Condition, To.Module: RiverModule.Rent }).ShouldBeEmpty();
    }

    // Every lever, its car at a catalogue value of 1,000,000.
    internal static RiverInputs CostlyCar => MapLinksTests.Full.WithCatalogueValue(1_000_000m);
}

using Netto.Tests.WebClient.Tabs;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// A quiet lever on the Map tab, chosen and given its amounts on its card beside the map, as the page runs
// it: the map draws it with its box again, in its place, at once, the quiet levers around it each in their
// own row, and it stays chosen.
public class PlanMapQuietTests
{
    // Every lever of the chain left empty, the royalty kept.
    private static readonly RiverInputs ChainEmpty = MapQuietTests.Emptied(MapLinksTests.Full,
        OptionalModule.Vouchers, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);

    [Fact]
    public async Task Plan_aQuietLeverChosenGivenItsAmounts_getsItsBoxBackInItsPlaceAndStaysChosen()
    {
        await using var page = LivePlan.At("http://localhost/plan/map", new BrowserStorage(), river: ChainEmpty);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Allowances));

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(ChainEmpty with { Allowances = MapLinksTests.Full.Allowances }));

        var shown = await page.FindAsync<MapTab>();
        shown.ShouldSatisfyAllConditions(
            () => (shown.Chosen, page.Errors.Count).ShouldBe((RiverModule.Allowances, 0)),
            () => shown.ShownMap.Places().Select(place => (string.Join('+', place.Nodes.Select(node => node.End.Id)), place.Quiet)).Take(4).ShouldBe(
                [("company-year", false), ("vouchers", true), ("allowances", false), ("rent+car", true)]));
    }
}

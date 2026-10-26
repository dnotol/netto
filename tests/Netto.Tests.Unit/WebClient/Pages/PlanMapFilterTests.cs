using MudBlazor;
using Netto.Tests.WebClient.Tabs;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The kinds of link the viewer hides on the Map tab, as the page keeps them: a box ticked or unticked is
// remembered in the browser, which the page opened again reads before it draws (MapFilterTests), and the
// tab draws its map again without the links of the kind, as the page's MapTab is read to draw them (as its
// Chosen is). A click is the browser's; here the box's own callback is invoked, as its event would.
public class PlanMapFilterTests
{
    // The first visit: the two kinds of rule are unticked, and nothing is stored until the viewer ticks.
    [Fact]
    public async Task Plan_firstVisit_hasTheTwoKindsOfRuleUntickedAndStoresNothing()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan/map", storage, river: MapLinksTests.Full);

        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();

        map.ShouldSatisfyAllConditions(
            () => map.DrawnLines.Select(line => line.Bundle.Kind).Distinct().Order().ShouldBe([MapLinkKind.PassedOn, MapLinkKind.IntoBucket]),
            () => map.ShownMap.Hidden.Select(link => link.Kind).Distinct().Order().ShouldBe([MapLinkKind.TaxBase, MapLinkKind.Condition]),
            () => storage.Get(MapLinkFilter.StorageKey).ShouldBeNull(),
            () => page.Errors.ShouldBeEmpty());
    }

    // A kind ticked is kept in the browser, whole with the one still unticked, and the tab draws its lines.
    [Fact]
    public async Task Plan_aRuleKindTickedOnTheMap_isKeptInTheBrowserAndDrawnByTheTab()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan/map", storage, river: MapLinksTests.Full);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(true));

        map.ShouldSatisfyAllConditions(
            () => boxes.Count.ShouldBe(Enum.GetValues<MapLinkKind>().Length),
            () => storage.Get(MapLinkFilter.StorageKey).ShouldBe("condition"),
            () => map.DrawnLines.Select(line => line.Bundle.Kind).Distinct().Order().ShouldBe([MapLinkKind.PassedOn, MapLinkKind.IntoBucket, MapLinkKind.TaxBase]),
            () => page.Errors.ShouldBeEmpty());
    }

    [Fact]
    public async Task Plan_aKindUntickedOnTheMap_isKeptInTheBrowserAndNothingFails()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan/map", storage);
        await page.LoadAsync("map");
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.PassedOn)].ValueChanged.InvokeAsync(false));

        (storage.Get(MapLinkFilter.StorageKey), page.Errors.Count).ShouldBe(("passed_on;tax_base;condition", 0));
    }

    // The box ticked again is shown again: what stays hidden is the kind left unticked, for the next visit.
    [Fact]
    public async Task Plan_aKindTickedAgainOnTheMap_leavesOnlyTheOtherOneKept()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan/map", storage);
        await page.LoadAsync("map");
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(true));
        await page.InvokeAsync(() => boxes[Index(MapLinkKind.Condition)].ValueChanged.InvokeAsync(true));
        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));

        (storage.Get(MapLinkFilter.StorageKey), page.Errors.Count).ShouldBe(("tax_base", 0));
    }

    // The tab draws again at a tick: the lines and the words beside them lose the links of the kind unticked,
    // the others stay as they were, and nothing else of the map changes, the chosen module's neighbourhood
    // included, which lists all its links whatever the legend ticks.
    [Fact]
    public async Task Plan_aKindUntickedOnTheMap_isDrawnNoMoreByTheTab()
    {
        await using var page = LivePlan.At("http://localhost/plan/map", MapRendering.EveryKindShown(), river: MapLinksTests.Full);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Car));
        var (lines, words, listed, modules) = (Lines(map), Words(map), MapNeighbourhoodHtml.Listed(map), map.ShownMap.Modules);
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));

        map.ShouldSatisfyAllConditions(
            () => lines.Select(line => line.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => words.Select(word => word.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => listed.ShouldContain(line => line.StartsWith("rule:", StringComparison.Ordinal)),
            () => Lines(map).ShouldBe(lines.Where(line => line.Kind != MapLinkKind.TaxBase)),
            () => Words(map).ShouldBe(words.Where(word => word.Kind != MapLinkKind.TaxBase)),
            () => MapNeighbourhoodHtml.Listed(map).ShouldBe(listed),
            () => map.ShownMap.Hidden.Select(link => link.Kind).Distinct().ShouldBe([MapLinkKind.TaxBase]),
            () => map.ShownMap.Modules.ShouldBe(modules),
            () => page.Errors.ShouldBeEmpty());
    }

    [Fact]
    public async Task Plan_aKindTickedAgainOnTheMap_isDrawnAgainByTheTab()
    {
        await using var page = LivePlan.At("http://localhost/plan/map", MapRendering.EveryKindShown(), river: MapLinksTests.Full);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Car));
        var (lines, words, listed) = (Lines(map), Words(map), MapNeighbourhoodHtml.Listed(map));
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();
        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));
        Lines(map).Count.ShouldBeLessThan(lines.Count);

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(true));

        map.ShouldSatisfyAllConditions(
            () => Lines(map).ShouldBe(lines),
            () => Words(map).ShouldBe(words),
            () => MapNeighbourhoodHtml.Listed(map).ShouldBe(listed),
            () => map.ShownMap.Hidden.ShouldBeEmpty());
    }

    // Each line the tab draws, by its kind and its route, and whether it is faded; each bundle of words
    // beside them; each line and rule in the neighbourhood of the module chosen.
    private static List<(MapLinkKind Kind, MapRoute Route, bool Faded)> Lines(MapTab map) =>
        [.. map.DrawnLines.Select(line => (line.Bundle.Kind, line.Bundle.Route, line.Faded))];

    private static List<(MapLinkKind Kind, MapRoute Route)> Words(MapTab map) =>
        [.. map.DrawnWords.SelectMany(block => block.Bundles).Select(bundle => (bundle.Kind, bundle.Route))];

    // The box a kind is, in the order of the kinds.
    private static int Index(MapLinkKind kind) => Array.IndexOf(Enum.GetValues<MapLinkKind>(), kind);
}

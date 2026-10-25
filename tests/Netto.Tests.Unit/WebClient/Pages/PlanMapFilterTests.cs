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
    [Fact]
    public async Task Plan_aKindUntickedOnTheMap_isKeptInTheBrowserAndNothingFails()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan/map", storage);
        await page.LoadAsync("map");
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));

        (boxes.Count, storage.Get(MapLinkFilter.StorageKey), page.Errors.Count).ShouldBe((Enum.GetValues<MapLinkKind>().Length, "tax_base", 0));
    }

    // The box ticked again is shown again: what stays hidden is the kind left unticked, for the next visit.
    [Fact]
    public async Task Plan_aKindTickedAgainOnTheMap_leavesOnlyTheOtherOneKept()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan/map", storage);
        await page.LoadAsync("map");
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));
        await page.InvokeAsync(() => boxes[Index(MapLinkKind.Condition)].ValueChanged.InvokeAsync(false));
        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(true));

        (storage.Get(MapLinkFilter.StorageKey), page.Errors.Count).ShouldBe(("condition", 0));
    }

    // The tab draws again at a tick: the lines, the words beside them and the chosen module's list lose the
    // links of the kind unticked, the others stay as they were, and nothing else of the map changes.
    [Fact]
    public async Task Plan_aKindUntickedOnTheMap_isDrawnNoMoreByTheTab()
    {
        await using var page = LivePlan.At("http://localhost/plan/map", new BrowserStorage(), river: MapLinksTests.Full);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Car));
        var (lines, words, listed, modules) = (Lines(map), Words(map), Listed(map), map.ShownMap.Modules);
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));

        map.ShouldSatisfyAllConditions(
            () => lines.Select(line => line.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => words.Select(word => word.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => listed.Select(link => link.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => Lines(map).ShouldBe(lines.Where(line => line.Kind != MapLinkKind.TaxBase)),
            () => Words(map).ShouldBe(words.Where(word => word.Kind != MapLinkKind.TaxBase)),
            () => Listed(map).ShouldBe(listed.Where(link => link.Kind != MapLinkKind.TaxBase)),
            () => map.ShownMap.Hidden.Select(link => link.Kind).Distinct().ShouldBe([MapLinkKind.TaxBase]),
            () => map.ShownMap.Modules.ShouldBe(modules),
            () => page.Errors.ShouldBeEmpty());
    }

    [Fact]
    public async Task Plan_aKindTickedAgainOnTheMap_isDrawnAgainByTheTab()
    {
        await using var page = LivePlan.At("http://localhost/plan/map", new BrowserStorage(), river: MapLinksTests.Full);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Car));
        var (lines, words, listed) = (Lines(map), Words(map), Listed(map));
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();
        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));
        Lines(map).Count.ShouldBeLessThan(lines.Count);

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(true));

        map.ShouldSatisfyAllConditions(
            () => Lines(map).ShouldBe(lines),
            () => Words(map).ShouldBe(words),
            () => Listed(map).ShouldBe(listed),
            () => map.ShownMap.Hidden.ShouldBeEmpty());
    }

    // Each line the tab draws, by its kind and its route, and whether it is faded; each bundle of words
    // beside them; each link listed in words for the module chosen.
    private static List<(MapLinkKind Kind, MapRoute Route, bool Faded)> Lines(MapTab map) =>
        [.. map.DrawnLines.Select(line => (line.Bundle.Kind, line.Bundle.Route, line.Faded))];

    private static List<(MapLinkKind Kind, MapRoute Route)> Words(MapTab map) =>
        [.. map.DrawnWords.SelectMany(block => block.Bundles).Select(bundle => (bundle.Kind, bundle.Route))];

    private static List<MapLink> Listed(MapTab map) => [.. map.ShownMap.LinksOf(map.Chosen!.Value)];

    // The box a kind is, in the order of the kinds.
    private static int Index(MapLinkKind kind) => Array.IndexOf(Enum.GetValues<MapLinkKind>(), kind);
}

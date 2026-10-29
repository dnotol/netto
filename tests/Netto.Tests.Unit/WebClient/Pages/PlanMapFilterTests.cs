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
    // The first visit: the conditions are unticked, left out of the lines the tab draws though the run's map
    // holds them, a tax base drawn, and nothing is stored until the viewer ticks.
    [Fact]
    public async Task Plan_firstVisit_hasTheConditionsUntickedAndStoresNothing()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage, river: MapLinksTests.Full);

        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();

        var run = MapView.From(MapLinksTests.Full, MapLinksTests.Full.Run());

        map.ShouldSatisfyAllConditions(
            () => map.DrawnLines.Select(line => line.Bundle.Kind).Distinct().Order().ShouldBe([MapLinkKind.PassedOn, MapLinkKind.IntoBucket, MapLinkKind.TaxBase]),
            () => map.ShownMap.Links.Select(link => link.Kind).Distinct().Order().ShouldBe([MapLinkKind.PassedOn, MapLinkKind.IntoBucket, MapLinkKind.TaxBase]),
            () => run.Links.Select(link => link.Kind).Distinct().Except(map.ShownMap.Links.Select(link => link.Kind)).Order().ShouldBe([MapLinkKind.Condition]),
            () => storage.Get(MapLinkFilter.StorageKey).ShouldBeNull(),
            () => page.Errors.ShouldBeEmpty());
    }

    // A kind ticked is kept in the browser, whole, and the tab draws its lines.
    [Fact]
    public async Task Plan_aRuleKindTickedOnTheMap_isKeptInTheBrowserAndDrawnByTheTab()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage, river: MapLinksTests.Full);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.Condition)].ValueChanged.InvokeAsync(true));

        map.ShouldSatisfyAllConditions(
            () => boxes.Count.ShouldBe(Enum.GetValues<MapLinkKind>().Length),
            () => storage.Get(MapLinkFilter.StorageKey).ShouldBe(""),
            () => map.DrawnLines.Select(line => line.Bundle.Kind).Distinct().Order().ShouldBe([MapLinkKind.PassedOn, MapLinkKind.IntoBucket, MapLinkKind.TaxBase, MapLinkKind.Condition]),
            () => page.Errors.ShouldBeEmpty());
    }

    [Fact]
    public async Task Plan_aKindUntickedOnTheMap_isKeptInTheBrowserAndNothingFails()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage);
        await page.LoadAsync();
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.PassedOn)].ValueChanged.InvokeAsync(false));

        (storage.Get(MapLinkFilter.StorageKey), page.Errors.Count).ShouldBe(("passed_on;condition", 0));
    }

    // The box ticked again is shown again: what stays hidden is the kind left unticked, for the next visit.
    [Fact]
    public async Task Plan_aKindTickedAgainOnTheMap_leavesOnlyTheOtherOneKept()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage);
        await page.LoadAsync();
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(true));
        await page.InvokeAsync(() => boxes[Index(MapLinkKind.Condition)].ValueChanged.InvokeAsync(true));
        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));

        (storage.Get(MapLinkFilter.StorageKey), page.Errors.Count).ShouldBe(("tax_base", 0));
    }

    // The tab draws again at a tick: the lines and the amounts on them lose the links of the kind unticked,
    // the others stay as they were, and nothing else of the map changes, what the chosen module receives
    // included, which is read from all its links whatever the legend ticks.
    [Fact]
    public async Task Plan_aKindUntickedOnTheMap_isDrawnNoMoreByTheTab()
    {
        await using var page = LivePlan.At("http://localhost/plan", MapRendering.EveryKindShown(), river: MapLinksTests.Full);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.DirectorPay));
        var (lines, words, listed, modules) = (Lines(map), Words(map), MapReceivedHtml.Listed(map), map.ShownMap.Modules);
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));

        map.ShouldSatisfyAllConditions(
            () => lines.Select(line => line.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => words.Select(word => word.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => listed.ShouldNotBeEmpty(),
            () => Lines(map).ShouldBe(lines.Where(line => line.Kind != MapLinkKind.TaxBase)),
            () => Words(map).ShouldBe(words.Where(word => word.Kind != MapLinkKind.TaxBase)),
            () => MapReceivedHtml.Listed(map).ShouldBe(listed),
            () => map.ShownMap.Links.Select(link => link.Kind).ShouldNotContain(MapLinkKind.TaxBase),
            () => map.ShownMap.Modules.ShouldBe(modules),
            () => page.Errors.ShouldBeEmpty());
    }

    [Fact]
    public async Task Plan_aKindTickedAgainOnTheMap_isDrawnAgainByTheTab()
    {
        await using var page = LivePlan.At("http://localhost/plan", MapRendering.EveryKindShown(), river: MapLinksTests.Full);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.DirectorPay));
        var (lines, words, listed, links) = (Lines(map), Words(map), MapReceivedHtml.Listed(map), map.ShownMap.Links);
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();
        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(false));
        Lines(map).Count.ShouldBeLessThan(lines.Count);

        await page.InvokeAsync(() => boxes[Index(MapLinkKind.TaxBase)].ValueChanged.InvokeAsync(true));

        map.ShouldSatisfyAllConditions(
            () => Lines(map).ShouldBe(lines),
            () => Words(map).ShouldBe(words),
            () => MapReceivedHtml.Listed(map).ShouldBe(listed),
            () => map.ShownMap.Links.ShouldBe(links));
    }

    // Each line the tab draws, by its kind and its route, and whether it is faded; each amount on them, by
    // its line's kind and route.
    private static List<(MapLinkKind Kind, MapRoute Route, bool Faded)> Lines(MapTab map) =>
        [.. map.DrawnLines.Select(line => (line.Bundle.Kind, line.Bundle.Route, line.Faded))];

    private static List<(MapLinkKind Kind, MapRoute Route)> Words(MapTab map) =>
        [.. map.DrawnAmounts.Select(amount => (amount.Bundle.Kind, amount.Bundle.Route))];

    // The box a kind is, in the order of the kinds.
    private static int Index(MapLinkKind kind) => Array.IndexOf(Enum.GetValues<MapLinkKind>(), kind);
}

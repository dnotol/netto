using MudBlazor;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The kinds of link the viewer hides on the Map tab, as the page keeps them: a box ticked or unticked is
// remembered in the browser, which the page opened again reads before it draws (MapFilterTests). A click is
// the browser's; here the box's own callback is invoked, as its event would.
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

    // The box a kind is, in the order of the kinds.
    private static int Index(MapLinkKind kind) => Array.IndexOf(Enum.GetValues<MapLinkKind>(), kind);
}

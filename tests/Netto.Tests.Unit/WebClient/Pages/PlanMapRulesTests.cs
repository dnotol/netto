using Netto.Tests.WebClient.Tabs;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The lines the page's map draws, as the page keeps the module chosen: every line of money always, a tax base's
// and a condition's for the module chosen alone, drawn again as the choice changes, as the page's MapTab is
// read to draw them (as its Chosen is). Nothing of the lines is a viewer's preference: nothing is kept in the
// browser for them, and what an earlier version kept there is read by no one and left as it was. A click is the
// browser's; here the tab's own callback is invoked, as its event would.
public class PlanMapRulesTests
{
    // The key an earlier version kept the kinds of line hidden under, which nothing reads now.
    private const string EarlierKey = "netto.map.lines";

    // The first visit, Company year chosen on arriving: the money, and Company year's rules, of which it has none;
    // nothing is kept in the browser.
    [Fact]
    public async Task Plan_firstVisit_drawsTheMoneyAndStoresNothingOfTheLines()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage, river: MapLinksTests.Full);

        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();

        map.ShouldSatisfyAllConditions(
            () => map.DrawnLines.Select(line => line.Bundle.Kind).Distinct().Order().ShouldBe([MapLinkKind.PassedOn, MapLinkKind.IntoBucket]),
            () => storage.Get(EarlierKey).ShouldBeNull(),
            () => page.Errors.ShouldBeEmpty());
    }

    // What an earlier version kept of the kinds hidden is read by no one, so it hides nothing, and is left as
    // it was.
    [Fact]
    public async Task Plan_aChoiceAnEarlierVersionKept_isIgnoredAndLeftAsItWas()
    {
        var storage = new BrowserStorage();
        storage.Set(EarlierKey, "passed_on;into_bucket");
        var writes = storage.Writes;
        await using var page = LivePlan.At("http://localhost/plan", storage, river: MapLinksTests.Full);

        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();

        map.ShouldSatisfyAllConditions(
            () => map.DrawnLines.Select(line => line.Bundle.Kind).Distinct().Order().ShouldBe([MapLinkKind.PassedOn, MapLinkKind.IntoBucket]),
            () => (storage.Get(EarlierKey), storage.Writes).ShouldBe(("passed_on;into_bucket", writes)));
    }

    // Choosing a module draws its tax bases' and conditions' lines and their words; choosing another draws its
    // own instead; the lines of money and their amounts stay where they were, and so do the modules.
    [Fact]
    public async Task Plan_aModuleChosen_drawsItsRulesLinesAndAnotherTheirsInstead()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: MapLinksTests.Full);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        var modules = map.ShownMap.Modules;

        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Car));
        var car = Rules(map);
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Split));
        var split = Rules(map);

        map.ShouldSatisfyAllConditions(
            () => car.ShouldNotBeEmpty(),
            () => car.ShouldAllBe(line => line.Touches(RiverModule.Car)),
            () => split.ShouldNotBeEmpty(),
            () => split.ShouldAllBe(line => line.Touches(RiverModule.Split)),
            () => split.ShouldNotContain(line => line.Card == RiverModule.Car && !line.Touches(RiverModule.Split)),
            () => map.ShownMap.Modules.ShouldBe(modules),
            () => page.Errors.ShouldBeEmpty());
    }

    // The tax bases' and conditions' lines the tab draws.
    private static List<MapBundle> Rules(MapTab map) =>
        [.. map.DrawnLines.Select(line => line.Bundle).Where(bundle => !MapLinkStyle.IsMoney(bundle.Kind))];
}

using Netto.Tests.WebClient.Tabs;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The line over the chosen module's card, saying what it receives and from where, as the page runs it: an end
// that is a module on the map is chosen as the map's own box is, and its card takes the focus on its heading,
// so a keyboard can walk the money upstream a step at a time. The click is the one the browser sends to the
// end's button.
public class PlanMapReceivedTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // Choosing an end of the line is choosing that module on the map: it is the page's choice, its card
    // replaces the old one beside the map and takes the focus, once, and what it receives replaces the old.
    [Fact]
    public async Task Plan_anEndChosenOnTheLine_isChosenAsOnTheMapAndItsCardTakesTheFocus()
    {
        await using var page = await MapAsync(Full, RiverModule.Dividend);
        var before = page.Focuses;

        await page.PressAsync("data-received-from", "corporate-tax");

        var (map, panel) = (await page.FindAsync<MapTab>(), await page.FindAsync<MapPanel>());
        (map.Chosen, panel.Module, MapReceivedHtml.Listed(map).Single().EndsWith($"from {MapTabTests.CardTitle(RiverModule.Split)}", StringComparison.Ordinal), page.Focuses - before, page.Errors.Count)
            .ShouldBe((RiverModule.CorporateTax, RiverModule.CorporateTax, true, 1, 0));
    }

    // The same state, the same focus, as a choice on the map.
    [Fact]
    public async Task Plan_anEndChosenOnTheLine_doesWhatAChoiceOnTheMapDoes()
    {
        await using var fromTheLine = await MapAsync(Full, RiverModule.Dividend);
        await using var fromTheMap = await MapAsync(Full, RiverModule.Dividend);
        var map = await fromTheMap.FindAsync<MapTab>();
        var (beforeLine, beforeMap) = (fromTheLine.Focuses, fromTheMap.Focuses);

        await fromTheLine.PressAsync("data-received-from", "corporate-tax");
        await fromTheMap.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.CorporateTax));

        var (shownFromTheLine, shownFromTheMap) = (await fromTheLine.FindAsync<MapTab>(), await fromTheMap.FindAsync<MapTab>());
        (shownFromTheLine.Chosen, fromTheLine.Focuses - beforeLine).ShouldBe((shownFromTheMap.Chosen, fromTheMap.Focuses - beforeMap));
        MapReceivedHtml.Listed(shownFromTheLine).ShouldBe(MapReceivedHtml.Listed(shownFromTheMap));
    }

    // Walking the money upstream by its ends: from the dividend to Corporate tax, to the split; each step is a
    // choice whose card takes the focus.
    [Fact]
    public async Task Plan_walkingTheEndsOfTheLine_followsTheMoneyUpstreamAChoiceAtATime()
    {
        await using var page = await MapAsync(Full, RiverModule.Dividend);
        var (before, visited) = (page.Focuses, new List<RiverModule?>());

        foreach (var next in new[] { RiverModule.CorporateTax, RiverModule.Split })
        {
            (await page.FindAsync<MapTab>()).Received.ShouldNotBeNull().Lines.Select(line => line.Module).ShouldContain(next);
            await page.PressAsync("data-received-from", MapModules.Name(next));
            visited.Add((await page.FindAsync<MapTab>()).Chosen);
        }

        visited.ShouldBe([RiverModule.CorporateTax, RiverModule.Split]);
        (page.Focuses - before, page.Errors.Count).ShouldBe((2, 0));
    }

    // Letting the module go leaves nothing said received.
    [Fact]
    public async Task Plan_theModuleLetGo_saysNothingReceived()
    {
        await using var page = await MapAsync(Full, RiverModule.Car);
        var map = await page.FindAsync<MapTab>();

        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(null));

        (await page.FindAsync<MapTab>()).Received.ShouldBeNull();
    }

    // The Map tab opened with a module chosen.
    private static async Task<LivePlan> MapAsync(RiverInputs river, RiverModule chosen)
    {
        var page = LivePlan.At("http://localhost/plan", MapRendering.EveryKindShown(), river: river);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(chosen));
        return page;
    }
}

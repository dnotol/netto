using MudBlazor;
using Netto.Tests.WebClient.Tabs;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The neighbourhood of the module chosen on the Map tab, as the page runs it: an end of a line that is a
// module on the map is chosen as the map's own box is, and its card takes the focus on its heading; the
// legend changes nothing of it. A click is the browser's; here the neighbourhood's callback is invoked, as
// its button's event would.
public class PlanMapNeighbourhoodTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // Choosing an end in the neighbourhood is choosing that module on the map: it is the page's choice, its
    // card replaces the old one beside the map and takes the focus, once, and its neighbourhood replaces the
    // old.
    [Fact]
    public async Task Plan_anEndChosenInTheNeighbourhood_isChosenAsOnTheMapAndItsCardTakesTheFocus()
    {
        await using var page = await MapAsync(Full, RiverModule.Split);
        var before = page.Focuses;
        var hood = await page.FindAsync<MapNeighbourhood>();

        await page.InvokeAsync(() => hood.OnChoose.InvokeAsync(RiverModule.CorporateTax));

        var (map, panel) = (await page.FindAsync<MapTab>(), await page.FindAsync<MapPanel>());
        (map.Chosen, panel.Module, map.Neighbourhood!.Module, page.Focuses - before, page.Errors.Count)
            .ShouldBe((RiverModule.CorporateTax, RiverModule.CorporateTax, RiverModule.CorporateTax, 1, 0));
    }

    // The same state, the same focus, as a choice on the map.
    [Fact]
    public async Task Plan_anEndChosenInTheNeighbourhood_doesWhatAChoiceOnTheMapDoes()
    {
        await using var fromTheNeighbourhood = await MapAsync(Full, RiverModule.Split);
        await using var fromTheMap = await MapAsync(Full, RiverModule.Split);
        var (hood, map) = (await fromTheNeighbourhood.FindAsync<MapNeighbourhood>(), await fromTheMap.FindAsync<MapTab>());
        var (beforeNeighbourhood, beforeMap) = (fromTheNeighbourhood.Focuses, fromTheMap.Focuses);

        await fromTheNeighbourhood.InvokeAsync(() => hood.OnChoose.InvokeAsync(RiverModule.DirectorPay));
        await fromTheMap.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.DirectorPay));

        var (shownFromTheNeighbourhood, shownFromTheMap) = (await fromTheNeighbourhood.FindAsync<MapTab>(), await fromTheMap.FindAsync<MapTab>());
        (shownFromTheNeighbourhood.Chosen, fromTheNeighbourhood.Focuses - beforeNeighbourhood).ShouldBe((shownFromTheMap.Chosen, fromTheMap.Focuses - beforeMap));
        Listed(shownFromTheNeighbourhood).ShouldBe(Listed(shownFromTheMap));
    }

    // Walking the money by its ends: from the split to Corporate tax, to the dividend; each step is a choice
    // whose card takes the focus.
    [Fact]
    public async Task Plan_walkingTheEndsOfTheNeighbourhood_followsTheMoneyAChoiceAtATime()
    {
        await using var page = await MapAsync(Full, RiverModule.Split);
        var (before, visited) = (page.Focuses, new List<RiverModule?>());

        foreach (var next in new[] { RiverModule.CorporateTax, RiverModule.Dividend })
        {
            var (map, hood) = (await page.FindAsync<MapTab>(), await page.FindAsync<MapNeighbourhood>());
            map.Neighbourhood!.GoesOut.Select(row => row.To.Module).ShouldContain(next);
            await page.InvokeAsync(() => hood.OnChoose.InvokeAsync(next));
            visited.Add((await page.FindAsync<MapTab>()).Chosen);
        }

        visited.ShouldBe([RiverModule.CorporateTax, RiverModule.Dividend]);
        (page.Focuses - before, page.Errors.Count).ShouldBe((2, 0));
    }

    // The legend ticks the lines of the map: the neighbourhood of the module chosen keeps every line and
    // every rule it had, whichever box is ticked or unticked.
    [Fact]
    public async Task Plan_aKindTickedOrUntickedInTheLegend_leavesTheNeighbourhoodAsItWas()
    {
        await using var page = await MapAsync(Full, RiverModule.Car);
        var before = Listed(await page.FindAsync<MapTab>());
        var boxes = await page.FindAllAsync<MudCheckBox<bool>>();

        foreach (var kind in Enum.GetValues<MapLinkKind>())
        {
            await page.InvokeAsync(() => boxes[Array.IndexOf(Enum.GetValues<MapLinkKind>(), kind)].ValueChanged.InvokeAsync(false));
            await page.InvokeAsync(() => boxes[Array.IndexOf(Enum.GetValues<MapLinkKind>(), kind)].ValueChanged.InvokeAsync(true));
        }

        Listed(await page.FindAsync<MapTab>()).ShouldBe(before);
        before.ShouldContain(line => line.StartsWith("rule:", StringComparison.Ordinal));
        page.Errors.ShouldBeEmpty();
    }

    // Letting the module go leaves the column saying how to choose.
    [Fact]
    public async Task Plan_theModuleLetGo_leavesNoNeighbourhood()
    {
        await using var page = await MapAsync(Full, RiverModule.Car);
        var map = await page.FindAsync<MapTab>();

        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(null));

        (await page.FindAsync<MapTab>()).Neighbourhood.ShouldBeNull();
        (await page.FindAsync<MapNeighbourhood>()).Hood.ShouldBeNull();
    }

    // The Map tab opened with a module chosen.
    private static async Task<LivePlan> MapAsync(RiverInputs river, RiverModule chosen)
    {
        var page = LivePlan.At("http://localhost/plan/map", MapRendering.EveryKindShown(), river: river);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(chosen));
        return page;
    }

    // Each line and rule the neighbourhood lists, as a viewer tells them apart.
    private static List<string> Listed(MapTab map)
    {
        var hood = map.Neighbourhood!;
        return
        [
            .. hood.ComesIn.Concat(hood.PassesThrough).Concat(hood.GoesOut).Select(row => $"{row.Words} {row.Amount} {row.From.Name} {row.To.Name}"),
            .. hood.Rules.Select(rule => $"rule: {rule.Words} {rule.Amount} {rule.Other.Name}"),
        ];
    }
}

using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The card of the module chosen beside the map, in the Map tab as the page draws it (the card in the panel is
// MapPanelTests', the page's work PlanMapPanelTests'): in the column after the neighbourhood, the room kept while none
// is chosen, no card for a module off the map.
public class MapTabPanelTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // The module chosen has its own card in the side column, after its neighbourhood.
    [Fact]
    public async Task Tab_aModuleChosen_hasItsCardInTheColumnAfterItsNeighbourhood()
    {
        var html = await RenderTabAsync(Full, RiverModule.Car);

        var (side, links, panel, card) = (html.IndexOf("class=\"map-tab__side\"", StringComparison.Ordinal), html.IndexOf("class=\"map-hood\"", StringComparison.Ordinal),
            html.IndexOf("class=\"map-tab__panel\"", StringComparison.Ordinal), html.IndexOf("id=\"card-car\"", StringComparison.Ordinal));
        (side < links, links < panel, panel < card, Regex.Count(html, "<section id=\"card-")).ShouldBe((true, true, true, 1));
    }

    // Nothing chosen: the room is kept, in words, so the map does not move when a module is chosen.
    [Fact]
    public async Task Tab_nothingChosen_keepsTheCardsRoomWithItsHint()
    {
        var html = await RenderTabAsync(Full);

        var panel = Regex.Match(html, "<div class=\"map-tab__panel\".*", RegexOptions.Singleline).Value;
        (panel.Contains("map-panel__hint"), panel.Contains("module-card")).ShouldBe((true, false));
    }

    // A module left off the map is chosen no more, so the panel draws no card for it.
    [Fact]
    public async Task Tab_aModuleChosenOffTheMap_hasNoCard()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverModule.Car);

        Regex.IsMatch(html, "<section id=\"card-").ShouldBeFalse();
    }
}

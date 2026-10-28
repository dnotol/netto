using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The conditions on the Map tab as a viewer meets them, drawn without a browser (MapRendering): every
// lever with the costly car, whose benefit is beyond 20% of the pay. Each is a dashed line that says in a
// word or two on the map, on its line, whether it holds, never by a colour, and a badge on the module it acts
// on (MapTabTests); its sentence is on no link: the cards say it with their figures (operator, 2026-10-28).
public class MapConditionsRenderingTests
{
    private static readonly RiverInputs River = MapViewConditionsTests.CostlyCar;

    [Fact]
    public async Task MapTab_aPlansConditions_areDashedLines()
    {
        var conditions = MapView.From(River, River.Run()).Bundles().Count(bundle => bundle.Kind == MapLinkKind.Condition);

        var html = await MapRendering.RenderTabAsync(River);

        var dashes = Regex.Matches(html, "<polyline [^>]*stroke-dasharray=\"([^\"]*)\"[^>]*data-link-kind=\"Condition\"").Select(match => match.Groups[1].Value).ToList();
        (conditions > 0, dashes.Count, string.Join('|', dashes.Distinct())).ShouldBe((true, conditions, MapLinkStyle.Dash(MapLinkKind.Condition)));
    }

    // On the map, on its line, the car's condition says in a word that the rate is lost.
    [Fact]
    public async Task MapTab_theCarsCondition_saysOnTheMapThatTheRateIsLost()
    {
        var html = await MapRendering.RenderTabAsync(River);

        ConditionWords(html).ShouldContain("lost");
    }

    // Its module chosen, the line still says its word or two on the map.
    [Fact]
    public async Task MapTab_theCarChosen_saysItsConditionInAWordOnTheMap()
    {
        var html = await MapRendering.RenderTabAsync(River, RiverModule.Car);

        var map = Regex.Match(html, "<div class=\"map\".*?<div class=\"map-tab__side\"", RegexOptions.Singleline).Value;
        ConditionWords(map).ShouldContain("lost");
    }

    // The words on the map of every condition's line, each trimmed.
    private static List<string> ConditionWords(string html) =>
        [.. Regex.Matches(html, "<span class=\"map-amount map-amount--state\" data-link-kind=\"Condition\"[^>]*>([^<]*)</span>").Select(line => line.Groups[1].Value.Trim())];
}

using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The conditions on the Map tab as a viewer meets them, drawn without a browser (MapRendering): every
// lever with the costly car, whose benefit is beyond 20% of the pay. Each is a dashed line that says no word
// on the map, its rule and state being the Effects list's, and a badge on the module it acts on where it is lost
// (MapTabTests); its sentence is on no link: the cards say it with their figures (operator, 2026-10-28).
public class MapConditionsRenderingTests
{
    private static readonly RiverInputs River = MapViewConditionsTests.CostlyCar;

    // The car chosen, the conditions it sets or meets are its kind's dashed lines.
    [Fact]
    public async Task MapTab_aPlansConditions_areDashedLines()
    {
        var conditions = MapView.From(River, River.Run(), MapNames.English).Bundles().Count(bundle => bundle.Kind == MapLinkKind.Condition && bundle.Touches(RiverModule.Car));

        var html = await MapRendering.RenderTabAsync(River, MapChoice.Of(RiverModule.Car));

        var dashes = Regex.Matches(html, "<path [^>]*stroke-dasharray=\"([^\"]*)\"[^>]*data-link-kind=\"Condition\"").Select(match => match.Groups[1].Value).ToList();
        (conditions > 0, dashes.Count, string.Join('|', dashes.Distinct())).ShouldBe((true, conditions, MapLinkStyle.Dash(MapLinkKind.Condition)));
    }

    // Nothing chosen, no condition's line is drawn, nor its word: the badge on the module it acts on says it.
    [Fact]
    public async Task MapTab_nothingChosen_saysNoConditionOnTheMapButItsBadge()
    {
        var html = await MapRendering.RenderTabAsync(River);

        (ConditionWords(html).Count, html.Contains("data-badge-kind=\"Condition\"", StringComparison.Ordinal)).ShouldBe((0, true));
    }

    // Its module chosen, the line says no word on the map: its rule and state are the Effects list's and its title's.
    [Fact]
    public async Task MapTab_theCarChosen_saysNoWordOnTheConditionsLine()
    {
        var html = await MapRendering.RenderTabAsync(River, MapChoice.Of(RiverModule.Car));

        var map = Regex.Match(html, "<div class=\"map\".*?<div class=\"map-tab__side\"", RegexOptions.Singleline).Value;
        map.Contains("data-link-kind=\"Condition\"", StringComparison.Ordinal).ShouldBeTrue();
        ConditionWords(map).ShouldBeEmpty();
    }

    // The words on the map of every condition's line, each trimmed.
    private static List<string> ConditionWords(string html) =>
        [.. Regex.Matches(html, "<span class=\"map-amount\" data-link-kind=\"Condition\"[^>]*>([^<]*)</span>").Select(line => line.Groups[1].Value.Trim())];
}

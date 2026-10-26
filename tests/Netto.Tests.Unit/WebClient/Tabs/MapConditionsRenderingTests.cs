using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The conditions on the Map tab as a viewer meets them, drawn without a browser (MapRendering): every
// lever with the costly car, whose benefit is beyond 20% of the pay. Each is a dashed line that says in a
// word or two beside the map whether it holds, and in its sentence, its module chosen, in its
// neighbourhood, never by a colour.
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

    // Beside the map the car's condition says in a word that the rate is lost; its sentence is not on the
    // map, where it would run past the edge.
    [Fact]
    public async Task MapTab_theCarsCondition_saysBesideTheMapThatTheRateIsLost()
    {
        var words = Words(River, RiverModule.Car);

        var html = await MapRendering.RenderTabAsync(River);

        (words.EndsWith("reduced rate lost", StringComparison.Ordinal), ConditionWords(html).Contains("lost"), html.Contains(words, StringComparison.Ordinal))
            .ShouldBe((true, true, false));
    }

    // Its module chosen, the line still says its word or two on the map, and its neighbourhood beside it
    // its sentence.
    [Fact]
    public async Task MapTab_theCarChosen_saysItsConditionInAWordOnTheMapAndItsSentenceInItsNeighbourhood()
    {
        var html = await MapRendering.RenderTabAsync(River, RiverModule.Car);

        var map = Regex.Match(html, "<div class=\"map\".*?<div class=\"map-tab__side\"", RegexOptions.Singleline).Value;
        (ConditionWords(map).Contains("lost"), map.Contains(Words(River, RiverModule.Car), StringComparison.Ordinal), html.Contains(Words(River, RiverModule.Car), StringComparison.Ordinal))
            .ShouldBe((true, false, true));
    }

    // The words on the map of every condition's line, each trimmed.
    private static List<string> ConditionWords(string html) =>
        [.. Regex.Matches(html, "<span class=\"map-amount map-amount--state\" data-link-kind=\"Condition\"[^>]*>([^<]*)</span>").Select(line => line.Groups[1].Value.Trim())];

    [Fact]
    public async Task MapTab_theCarChosen_listsItsConditionInWordsWithItsModuleAndThatItDoesNotHold()
    {
        var html = await MapRendering.RenderTabAsync(River, RiverModule.Car);

        MapNeighbourhoodHtml.Rows(html, "rules").ShouldContain($"Sets on Corporate tax {Words(River, RiverModule.Car)} Does not hold");
    }

    // The words of the condition the module sets, as the map's texts say them.
    private static string Words(RiverInputs river, RiverModule from)
    {
        var link = MapView.From(river, river.Run()).Links.Single(link => link.Kind == MapLinkKind.Condition && link.From.Module == from);
        return ResxText.For<MapTab>().Format(link.Carries.Key, [.. link.Carries.Args]);
    }
}

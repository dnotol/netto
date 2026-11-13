using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The switch over My plan's picture, first among the map's tools: a group of two buttons, the picture drawn pressed;
// the map unless the viewer kept the Sankey, which then takes the map's place and its key's.
public class MapPictureSwitchTests
{
    [Theory]
    [InlineData(PlanPicture.Map, "true", "false")]
    [InlineData(PlanPicture.Sankey, "false", "true")]
    public async Task Switch_isAGroupOfTwoButtons_thePictureDrawnPressed(PlanPicture current, string map, string sankey)
    {
        var html = await RenderSwitchAsync(current);

        (Group(html), string.Join(' ', Buttons(html))).ShouldBe(("Picture of the plan", $"(map, Map, {map}) (sankey, Sankey, {sankey})"));
    }

    [Fact]
    public async Task Switch_inFrench_saysCarteAndSankey()
    {
        using var french = InLanguage.French();

        var html = await RenderSwitchAsync(PlanPicture.Map);

        (Group(html), string.Join(' ', Buttons(html).Select(button => button.Name))).ShouldBe(("Image du plan", "Carte Sankey"));
    }

    [Fact]
    public async Task MapTab_nothingKept_drawsTheMap_theSwitchFirstInItsTools()
    {
        var html = await RenderTabAsync(MapLinksTests.Full);

        var tools = Regex.Match(html, "<div class=\"map-tab__tools\"[^>]*>(.*?)<div class=\"map-tab__frame\"", RegexOptions.Singleline).Groups[1].Value;
        (tools.IndexOf("map-picture-switch", StringComparison.Ordinal), tools.Contains("map-tab__key", StringComparison.Ordinal),
            html.Contains("<div class=\"map\"", StringComparison.Ordinal), html.Contains("class=\"sankey", StringComparison.Ordinal)).ShouldBe((ToolsStart(tools), false, true, false));
    }

    [Fact]
    public async Task MapTab_sankeyKept_drawsTheSankey_inTheMapsPlace_withoutTheMapsKey()
    {
        var storage = new BrowserStorage();
        storage.Set(PicturePreference.StorageKey, "sankey");

        var html = await RenderTabAsync(MapLinksTests.Full, more: services => services.AddSingleton(new PicturePreference(storage)));

        (html.Contains("<div class=\"map\"", StringComparison.Ordinal), html.Contains("map-tab__key", StringComparison.Ordinal),
            html.Contains("<div class=\"sankey\"", StringComparison.Ordinal), html.Contains("class=\"map-tab__panel\"", StringComparison.Ordinal)).ShouldBe((false, false, true, true));
    }

    // The switch is the first thing among the tools: nothing but whitespace and its own wrapper before it.
    private static int ToolsStart(string tools) => tools.IndexOf("map-picture-switch", StringComparison.Ordinal) is var at and >= 0
        && !tools[..at].Contains("<button", StringComparison.Ordinal) && !tools[..at].Contains("map-tab__key", StringComparison.Ordinal)
            ? at
            : -1;

    private static async Task<string> RenderSwitchAsync(PlanPicture current) =>
        WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(MapPictureSwitch), new Folds(new BrowserStorage()), new()
        {
            [nameof(MapPictureSwitch.Current)] = current,
        }));

    private static string Group(string html) =>
        Regex.Match(html, "<div class=\"map-picture-switch\" role=\"group\" aria-label=\"([^\"]*)\"").Groups[1].Value;

    private static IReadOnlyList<(string Picture, string Name, string Pressed)> Buttons(string html) =>
        [.. Regex.Matches(html, "<button type=\"button\" class=\"map-picture-switch__button\" aria-pressed=\"(\\w+)\" data-picture=\"(\\w+)\"[^>]*>([^<]*)</button>")
            .Select(match => (match.Groups[2].Value, match.Groups[3].Value.Trim(), match.Groups[1].Value))];
}

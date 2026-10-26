using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab drawn without a browser, as a viewer meets it (RiverRendering): on a river's own run, with
// a module chosen or none, and any service a test adds (more), registered after the render's own so its
// own wins: the viewer's filter of the kinds of link, which shows every kind unless a test gives another,
// the map's first visit, with its rules' lines left out, being the tests of that first visit's own.
// Its HTML decoded to the text a viewer reads.
internal static class MapRendering
{
    // A browser's storage in which the viewer chose to see every kind of link (the key present and empty):
    // what a test that holds a line, its arrowhead or its words to the map starts from.
    public static BrowserStorage EveryKindShown()
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, "");
        return storage;
    }

    public static async Task<string> RenderTabAsync(RiverInputs river, RiverModule? chosen = null, Action<IServiceCollection>? more = null) =>
        WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(MapTab), new Folds(new BrowserStorage()), new()
        {
            [nameof(MapTab.Inputs)] = river,
            [nameof(MapTab.Run)] = river.Run(),
            [nameof(MapTab.Chosen)] = chosen,
        }, services =>
        {
            services.AddSingleton(new MapLinkFilter(EveryKindShown()));
            more?.Invoke(services);
        }));

    // The inner HTML of each kind's item of the legend in a rendered page, in the order drawn: its box and the
    // bubble beside it.
    public static List<string> LegendItems(string html) =>
        [.. Regex.Matches(html, "<li class=\"map-filter__kind\"[^>]*>(.*?)</li>", RegexOptions.Singleline).Select(match => match.Groups[1].Value)];

    // Each box of the legend: the kind it is, whether it is ticked, and its label as a screen reader reads
    // it, what the eye alone reads of it (the count's number) left out.
    public static List<(MapLinkKind Kind, bool Ticked, string Text)> LegendBoxes(string html) =>
    [
        .. LegendItems(html).Select(item => (
            Enum.Parse<MapLinkKind>(Regex.Match(item, "data-link-kind=\"(\\w+)\"").Groups[1].Value),
            Regex.IsMatch(item, "<input [^>]*type=\"checkbox\"[^>]*\\schecked"),
            Regex.Replace(Regex.Replace(Regex.Replace(Regex.Match(item, "<label.*?</label>", RegexOptions.Singleline).Value,
                "<span [^>]*aria-hidden=\"true\"[^>]*>[^<]*</span>", ""), "<[^>]*>", " "), "\\s+", " ").Trim())),
    ];
}

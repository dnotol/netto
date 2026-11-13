using System.Net;
using System.Text.RegularExpressions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The key to the map's lines as a viewer meets it on the Map tab, drawn without a browser, compact among the
// map's tools: a list named for a screen reader, an item for every style of line in its order, each with a
// sample of the map's own line and its short name, nothing to tick; beside the last, one info bubble saying
// what each is. The items are one row that wraps only when the column has not the room for it.
public class MapKeyTests
{
    [Fact]
    public async Task Key_isAListNamedForAScreenReaderAnItemALine()
    {
        var html = await RenderAsync();

        html.ShouldSatisfyAllConditions(
            () => Regex.IsMatch(html, "^<ul class=\"map-key\" aria-label=\"Key to the lines\"[^>]*>").ShouldBeTrue(),
            () => Names(html).ShouldBe(["Passed on", "Into a bucket", "Into your pocket", "Tax base", "Conditions"]));
    }

    // The key says what the lines are; it shows and hides none: nothing to tick.
    [Fact]
    public async Task Key_hasNothingToTick()
    {
        var html = await RenderAsync();

        (Regex.IsMatch(html, "<input|<fieldset|mud-checkbox|aria-pressed"), Regex.Count(html, "<button")).ShouldBe((false, 1));
    }

    // What each line is goes in one info bubble after the items, beside the last one, in its item, so the two
    // never break apart when the row wraps; named "More about" the key and saying each line's name and what it is.
    [Fact]
    public async Task Key_theLinesShareOneBubbleBesideTheLastSayingWhatEachIs()
    {
        var html = await RenderAsync();

        var items = Items(html);
        var bubble = Regex.Match(items[^1], "<span class=\"info-bubble\"[^>]*>\\s*<button [^>]*aria-label=\"([^\"]*)\"", RegexOptions.Singleline);
        var glosses = Regex.Matches(html, "<span class=\"map-key__gloss\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value).ToList();
        var texts = ResxText.For<MapKey>();

        html.ShouldSatisfyAllConditions(
            () => Regex.Count(html, "class=\"info-bubble\"").ShouldBe(1),
            () => items[..^1].ShouldAllBe(item => !item.Contains("info-bubble", StringComparison.Ordinal)),
            () => bubble.Groups[1].Value.ShouldBe("More about Key to the lines"),
            () => glosses.ShouldBe(MapKeyView.Lines.Select(line => $"{texts.Get(MapKeyText.Name(line))}: {texts.Get(MapKeyText.Gloss(line))}")));
    }

    // The items are one row, a flex row that wraps only when the column has not the room, each as wide as it
    // asks, with no frame of its own.
    [Fact]
    public void Stylesheet_theLines_areOneCompactRowThatWrapsOnlyWhenItMust()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapKey.razor.css");

        var row = Stylesheet.Block(css, ".map-key").Split(';').Select(declaration => declaration.Trim())
            .Where(declaration => declaration.StartsWith("display", StringComparison.Ordinal) || declaration.StartsWith("flex-wrap", StringComparison.Ordinal));
        css.ShouldSatisfyAllConditions(
            () => row.ShouldBe(["display: flex", "flex-wrap: wrap"]),
            () => Stylesheet.Block(css, ".map-key").ShouldContain("list-style: none"),
            () => Stylesheet.Block(css, ".map-key__line").ShouldContain("flex-wrap: nowrap"),
            () => Stylesheet.Block(css, ".map-key__line").ShouldNotContain("border:"));
    }

    // The names read as quietly as the line holding them: normal weight, the muted text colour; the samples keep
    // their own inks, set on each.
    [Fact]
    public void Stylesheet_theNames_areNormalWeightAndMuted()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapKey.razor.css");

        var name = Stylesheet.Block(css, ".map-key__name").Split(';').Select(declaration => declaration.Trim())
            .Where(declaration => declaration.StartsWith("font-weight", StringComparison.Ordinal) || declaration.StartsWith("color", StringComparison.Ordinal));
        name.ShouldBe(["color: var(--netto-text-muted)", "font-weight: 400"]);
    }

    // Each sample is the map's own line of its style, hidden from a screen reader, which hears the name: its
    // dash, width and ink, and its end, an arrowhead in its ink or a bar its line runs into, a bucket's edge;
    // what reaches the director in the map's gold, running into the pocket.
    [Fact]
    public async Task Key_everySample_isDrawnAsTheMapDrawsItsLines()
    {
        var html = await RenderAsync();

        Samples(html).ShouldBe(MapKeyView.Lines.Select(line => (line.ToString(), MapKeyView.Width(line), MapKeyView.Dash(line), $"stroke: {MapKeyView.Ink(line)}",
            MapKeyView.End(line) == MapLineEnd.Arrowhead ? $"fill: {MapKeyView.Ink(line)}" : null,
            MapKeyView.End(line) == MapLineEnd.Bucket ? $"stroke: {MapLinkStyle.BucketEdge}" : null)));
    }

    // Nothing here is a live region, and no resx key shows through for want of a text.
    [Fact]
    public async Task Key_isNoLiveRegionAndLeaksNoKey()
    {
        var html = await RenderAsync();

        (Regex.IsMatch(html, "aria-live|role=\"(status|alert)\""), Regex.IsMatch(html, "(Name|Gloss)_[A-Z]|GlossLine|\"Key\"")).ShouldBe((false, false));
    }

    private static async Task<string> RenderAsync() =>
        WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(MapKey), new Folds(new BrowserStorage()), []));

    // The inner HTML of each line's item, in the order drawn.
    internal static List<string> Items(string html) =>
        [.. Regex.Matches(html, "<li class=\"map-key__line\"[^>]*>(.*?)</li>", RegexOptions.Singleline).Select(match => match.Groups[1].Value)];

    // The name of each line's item, as a screen reader hears it.
    internal static List<string> Names(string html) =>
        [.. Items(html).Select(item => Regex.Match(item, "<span class=\"map-key__name\"[^>]*>([^<]*)</span>").Groups[1].Value)];

    // Each sample of the key: its line, its stroke's width, dash and ink, its arrowhead's ink and its bar's, if
    // it has them.
    private static List<(string Line, string Width, string? Dash, string Ink, string? Arrowhead, string? Bucket)> Samples(string html) =>
    [
        .. Regex.Matches(html, "<svg class=\"map-key__swatch\" data-key-line=\"(\\w+)\"[^>]*aria-hidden=\"true\"[^>]*>(.*?)</svg>", RegexOptions.Singleline).Select(match => (
            match.Groups[1].Value,
            Regex.Match(match.Groups[2].Value, "<line [^>]*stroke-width=\"([\\d.]+)\"").Groups[1].Value,
            Regex.Match(match.Groups[2].Value, "<line [^>]*stroke-dasharray=\"([^\"]*)\"") is { Success: true } dash ? dash.Groups[1].Value : null,
            Regex.Match(match.Groups[2].Value, "<line [^>]*style=\"([^\"]*)\"").Groups[1].Value,
            Regex.Match(match.Groups[2].Value, "<polygon class=\"map-key__arrow\"[^>]*style=\"([^\"]*)\"") is { Success: true } arrow ? arrow.Groups[1].Value : null,
            Regex.Match(match.Groups[2].Value, "<line class=\"map-key__bucket\"[^>]*style=\"([^\"]*)\"") is { Success: true } bar ? bar.Groups[1].Value : null)),
    ];
}

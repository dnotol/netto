using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The legend of the kinds of link as a viewer meets it on the Map tab, drawn without a browser: a group
// named by its legend, a box for every kind in the order of the kinds, ticked while the kind is shown, each
// with a sample of the map's own line for it, its name, how many links the plan has of it and what it is.
// Ticking a box is an event, which MapLinkFilterTests holds the effect of; the tab drawing its map again is
// the tab's (MapTabTests).
public class MapFilterTests
{
    private static readonly Dictionary<MapLinkKind, int> Counts = new()
    {
        [MapLinkKind.PassedOn] = 10,
        [MapLinkKind.IntoBucket] = 1,
        [MapLinkKind.TaxBase] = 6,
    };

    [Fact]
    public async Task Filter_isAGroupNamedByItsLegend()
    {
        var html = await RenderAsync(new MapLinkFilter(new BrowserStorage()), Counts);

        (Regex.IsMatch(html, "^<fieldset class=\"map-filter\"[^>]*><legend[^>]*>Kinds of link shown</legend>"), Regex.Count(html, "<fieldset")).ShouldBe((true, 1));
    }

    [Fact]
    public async Task Filter_everyKind_isABoxWithItsNameItsCountAndWhatItIs()
    {
        var html = await RenderAsync(new MapLinkFilter(new BrowserStorage()), Counts);

        Boxes(html).Select(box => box.Text).ShouldBe(
        [
            "Money passed on (10 links) Money handed from one module to the next.",
            "Money into a bucket (1 link) Money that ends in one of the bar's buckets, leaves the river, or comes back out of a bucket.",
            "Tax base (6 links) What changes the amount a tax is worked out on without moving money: a benefit taxed with the pay, what Corporate tax adds back.",
            "Conditions (none in this plan) A condition one module sets on another.",
        ]);
    }

    // The box is a native checkbox inside its label, so it is named by it and operated by the keyboard.
    [Fact]
    public async Task Filter_everyBox_isACheckboxInsideItsLabel()
    {
        var html = await RenderAsync(new MapLinkFilter(new BrowserStorage()), Counts);

        Regex.Count(html, "<label class=\"mud-checkbox[^>]*>(?:(?!</label>).)*<input [^>]*type=\"checkbox\"[^>]*/>.*?</label>", RegexOptions.Singleline).ShouldBe(Enum.GetValues<MapLinkKind>().Length);
    }

    [Fact]
    public async Task Filter_nothingHidden_ticksEveryBox()
    {
        var html = await RenderAsync(new MapLinkFilter(new BrowserStorage()), Counts);

        Boxes(html).Select(box => box.Ticked).ShouldAllBe(ticked => ticked);
    }

    // A kind hidden in an earlier visit is unticked in the very first draw, the filter having read it from
    // the storage before the tab was drawn.
    [Fact]
    public async Task Filter_kindsHiddenBefore_areUntickedInTheFirstDraw()
    {
        var storage = new BrowserStorage();
        var before = new MapLinkFilter(storage);
        before.Set(MapLinkKind.TaxBase, shown: false);
        before.Set(MapLinkKind.Condition, shown: false);

        var html = await RenderAsync(new MapLinkFilter(storage), Counts);

        Boxes(html).Select(box => (box.Kind, box.Ticked)).ShouldBe(
            [(MapLinkKind.PassedOn, true), (MapLinkKind.IntoBucket, true), (MapLinkKind.TaxBase, false), (MapLinkKind.Condition, false)]);
    }

    // The counts are the plan's, whatever is hidden: a box is named the same ticked or not.
    [Fact]
    public async Task Filter_aKindHidden_keepsItsCount()
    {
        var shown = await RenderAsync(new MapLinkFilter(new BrowserStorage()), Counts);
        var hidden = new MapLinkFilter(new BrowserStorage());
        hidden.Set(MapLinkKind.TaxBase, shown: false);

        Boxes(await RenderAsync(hidden, Counts)).Select(box => box.Text).ShouldBe(Boxes(shown).Select(box => box.Text));
    }

    // The sample of a line is the map's own dash and width, hidden from a screen reader, which hears the
    // name; no two share a style, so the kinds are told apart with no colour.
    [Fact]
    public async Task Filter_everyBox_hasASampleOfItsKindsLine()
    {
        var html = await RenderAsync(new MapLinkFilter(new BrowserStorage()), Counts);

        var samples = Regex.Matches(html, "<svg class=\"map-filter__swatch\" data-link-kind=\"(\\w+)\"[^>]*aria-hidden=\"true\"[^>]*><line [^>]*stroke-width=\"([\\d.]+)\"(?: stroke-dasharray=\"([^\"]*)\")?[^>]*>")
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Success ? match.Groups[3].Value : null)).ToList();
        samples.ShouldBe(Enum.GetValues<MapLinkKind>().Select(kind => (kind.ToString(), MapLinkStyle.Width(kind), MapLinkStyle.Dash(kind))));
        samples.Distinct().Count().ShouldBe(samples.Count);
    }

    [Fact]
    public async Task Filter_thePlansLinks_saysHowManyAreShown()
    {
        var hidden = new MapLinkFilter(new BrowserStorage());
        hidden.Set(MapLinkKind.TaxBase, shown: false);

        var (all, some) = (Summary(await RenderAsync(new MapLinkFilter(new BrowserStorage()), Counts)), Summary(await RenderAsync(hidden, Counts)));

        (all, some).ShouldBe(("Showing 17 of 17 links", "Showing 11 of 17 links"));
    }

    [Fact]
    public async Task Filter_everyKindHidden_saysToTickOne()
    {
        var filter = new MapLinkFilter(new BrowserStorage());
        foreach (var kind in Enum.GetValues<MapLinkKind>())
            filter.Set(kind, shown: false);

        var html = await RenderAsync(filter, Counts);

        (Summary(html), Boxes(html).Any(box => box.Ticked)).ShouldBe(("Every kind of link is hidden: tick one to see its links.", false));
    }

    // A plan without a link, a refused amount leaves none, has nothing to count; the boxes stay.
    [Fact]
    public async Task Filter_aPlanWithoutALink_drawsNoSummaryButEveryBox()
    {
        var html = await RenderAsync(new MapLinkFilter(new BrowserStorage()), new Dictionary<MapLinkKind, int>());

        (html.Contains("map-filter__summary"), Boxes(html).Count, Boxes(html).Select(box => box.Text).Distinct().Count()).ShouldBe((false, 4, 4));
        Boxes(html).Select(box => box.Text).ShouldAllBe(text => text.Contains("(none in this plan)"));
    }

    // Nothing here is a live region, and no resx key shows through for want of a text.
    [Fact]
    public async Task Filter_isNoLiveRegionAndLeaksNoKey()
    {
        var html = await RenderAsync(new MapLinkFilter(new BrowserStorage()), Counts);

        (Regex.IsMatch(html, "aria-live|role=\"(status|alert)\""), Regex.IsMatch(html, "(Name|Gloss)_[A-Z]|Count_(None|One|Many)|Summary_(One|Many)|AllHidden|Hidden_(One|Many)|>Legend<"))
            .ShouldBe((false, false));
    }

    private static async Task<string> RenderAsync(MapLinkFilter filter, Dictionary<MapLinkKind, int> counts) =>
        WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(MapFilter), new Folds(new BrowserStorage()), new() { [nameof(MapFilter.Counts)] = counts }, services => services.AddSingleton(filter)));

    // Each box: the kind it is, whether it is ticked, and its label as a screen reader reads it.
    private static List<(MapLinkKind Kind, bool Ticked, string Text)> Boxes(string html) =>
    [
        .. Regex.Matches(html, "<li class=\"map-filter__kind\"[^>]*>(.*?)</li>", RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .Select(box => (
                Enum.Parse<MapLinkKind>(Regex.Match(box, "data-link-kind=\"(\\w+)\"").Groups[1].Value),
                Regex.IsMatch(box, "<input [^>]*type=\"checkbox\"[^>]*\\schecked"),
                Regex.Replace(Regex.Replace(Regex.Match(box, "<span class=\"mud-typography[^>]*>(.*)</span>", RegexOptions.Singleline).Groups[1].Value, "<[^>]*>", " "), "\\s+", " ").Trim())),
    ];

    private static string Summary(string html) =>
        Regex.Match(html, "<p class=\"map-filter__summary\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value.Trim();
}

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The legend of the kinds of link as a viewer meets it on the Map tab, drawn without a browser, compact
// among the map's tools: a group named by its legend, out of sight, a box for every kind in the order of the
// kinds, ticked while the kind is shown, each with a sample of the map's own line for it, its short name and,
// for a screen reader alone, how many links the plan has of it; after the boxes, one info bubble saying what
// each kind is. The boxes are one row that wraps only when the column has not the room for it. Ticking a box
// is an event: MapLinkFilterTests holds its effect on the filter, MapTabFilterTests what the tab draws of a
// filter, and PlanMapFilterTests that the tab draws again at a tick.
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
        var html = await RenderAsync(EveryKind(), Counts);

        (Regex.IsMatch(html, "^<fieldset class=\"map-filter\"[^>]*><legend[^>]*>Kinds of link shown</legend>"), Regex.Count(html, "<fieldset")).ShouldBe((true, 1));
    }

    // The label is the sample, the name and the count and nothing more, as a screen reader reads it: the
    // count's words, not the number the eye reads.
    [Fact]
    public async Task Filter_everyKind_isABoxWithItsNameAndItsCount()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        LegendBoxes(html).Select(box => box.Text).ShouldBe(["Passed on (10 links)", "Into a bucket (1 link)", "Tax base (6 links)", "Conditions (none in this plan)"]);
    }

    // A count is for a screen reader alone, which would hear a bare number as no count: the eye reads the
    // sample and the name, which keeps the four boxes and « Add to plan » on one line.
    [Fact]
    public async Task Filter_everyCount_isWordsForAScreenReaderAlone()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldNotContain("map-filter__count"),
            () => Regex.Matches(html, "<span class=\"map-filter__spoken\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)
                .ShouldBe(["(10 links)", "(1 link)", "(6 links)", "(none in this plan)"]));
    }

    // What each kind is goes in one info bubble after the boxes, named "More about" the legend and described
    // by each kind's name and what it is; never inside a box's label, whose text would then carry it, nor a
    // button inside a label.
    [Fact]
    public async Task Filter_theKinds_shareOneBubbleAfterTheBoxesSayingWhatEachIs()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        var bubble = Regex.Match(html, "</ul>\\s*<span class=\"info-bubble\"[^>]*>\\s*<button [^>]*aria-label=\"([^\"]*)\"");
        var glosses = Regex.Matches(html, "<span class=\"map-filter__gloss\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value).ToList();

        html.ShouldSatisfyAllConditions(
            () => Regex.Count(html, "class=\"info-bubble\"").ShouldBe(1),
            () => LegendItems(html).ShouldAllBe(item => !item.Contains("info-bubble", StringComparison.Ordinal)),
            () => bubble.Groups[1].Value.ShouldBe("More about Kinds of link shown"),
            () => glosses.ShouldBe(
            [
                "Passed on: money handed from one module to the next. The thicker the line, the more money.",
                "Into a bucket: money that ends in one of the bar's buckets, its line running into it, or that comes back out of a bucket, its line then ending in an arrowhead. The thicker the line, the more money.",
                "Tax base: what changes the amount a tax is worked out on without moving money, a benefit taxed with the pay, what Corporate tax adds back.",
                "Conditions: a condition one module sets on another.",
            ]));
    }

    // The four boxes are one row, a flex row that wraps only when the column has not the room, and each
    // box takes the room its content asks, not a column of a grid's; compact, with no frame of its own, and
    // the group's legend out of sight, its words for a screen reader.
    [Fact]
    public void Stylesheet_theKinds_areOneCompactRowThatWrapsOnlyWhenItMust()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapFilter.razor.css");

        var row = Stylesheet.Block(css, ".map-filter__kinds").Split(';').Select(declaration => declaration.Trim()).Where(declaration => declaration.StartsWith("display", StringComparison.Ordinal) || declaration.StartsWith("flex-wrap", StringComparison.Ordinal));
        css.ShouldSatisfyAllConditions(
            () => row.ShouldBe(["display: flex", "flex-wrap: wrap"]),
            () => Stylesheet.Block(css, ".map-filter__kind").ShouldNotContain("width: 100%"),
            () => Stylesheet.Block(css, ".map-filter__kind").ShouldNotContain("border:"),
            () => Stylesheet.Block(css, ".map-filter__legend").ShouldContain("clip-path: inset(50%)"),
            () => Stylesheet.Block(css, ".map-filter__legend").ShouldContain("position: absolute"),
            () => Stylesheet.Block(css, ".map-filter").ShouldContain("margin: 0;"));
    }

    // Each box is a dense checkbox, whose button is 32 px square and no more (MudBlazor's 48 px would make
    // the four boxes 966 px with their gaps and wrap the last at 1,024 px); a target of at least 24 px.
    [Fact]
    public async Task Filter_everyBox_isADenseCheckbox()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        LegendItems(html).Select(item => Regex.IsMatch(item, "<span class=\"mud-button-root [^\"]*mud-checkbox-dense[^\"]*\"")).ShouldBe([true, true, true, true]);
    }

    // The box is a native checkbox inside its label, so it is named by it and operated by the keyboard.
    [Fact]
    public async Task Filter_everyBox_isACheckboxInsideItsLabel()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        Regex.Count(html, "<label class=\"mud-checkbox[^>]*>(?:(?!</label>).)*<input [^>]*type=\"checkbox\"[^>]*/>.*?</label>", RegexOptions.Singleline).ShouldBe(Enum.GetValues<MapLinkKind>().Length);
    }

    // A viewer who never ticked a box meets the kinds of money ticked and the two of rules not.
    [Fact]
    public async Task Filter_firstVisit_ticksTheMoneyAndLeavesTheRulesUnticked()
    {
        var html = await RenderAsync(new MapLinkFilter(new BrowserStorage()), Counts);

        LegendBoxes(html).Select(box => (box.Kind, box.Ticked)).ShouldBe(
            [(MapLinkKind.PassedOn, true), (MapLinkKind.IntoBucket, true), (MapLinkKind.TaxBase, false), (MapLinkKind.Condition, false)]);
    }

    [Fact]
    public async Task Filter_nothingHidden_ticksEveryBox()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        LegendBoxes(html).Select(box => box.Ticked).ShouldAllBe(ticked => ticked);
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

        LegendBoxes(html).Select(box => (box.Kind, box.Ticked)).ShouldBe(
            [(MapLinkKind.PassedOn, true), (MapLinkKind.IntoBucket, true), (MapLinkKind.TaxBase, false), (MapLinkKind.Condition, false)]);
    }

    // The counts are the plan's, whatever is hidden: a box is named the same ticked or not.
    [Fact]
    public async Task Filter_aKindHidden_keepsItsCount()
    {
        var shown = await RenderAsync(EveryKind(), Counts);
        var hidden = EveryKind();
        hidden.Set(MapLinkKind.TaxBase, shown: false);

        LegendBoxes(await RenderAsync(hidden, Counts)).Select(box => box.Text).ShouldBe(LegendBoxes(shown).Select(box => box.Text));
    }

    // The sample of a line is the map's own dash and width, hidden from a screen reader, which hears the
    // name; no two share a style, so the kinds are told apart with no colour.
    [Fact]
    public async Task Filter_everyBox_hasASampleOfItsKindsLine()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        var samples = Regex.Matches(html, "<svg class=\"map-filter__swatch\" data-link-kind=\"(\\w+)\"[^>]*aria-hidden=\"true\"[^>]*><line [^>]*stroke-width=\"([\\d.]+)\"(?: stroke-dasharray=\"([^\"]*)\")?[^>]*>")
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Success ? match.Groups[3].Value : null)).ToList();
        samples.ShouldBe(Enum.GetValues<MapLinkKind>().Select(kind => (kind.ToString(), MapLinkStyle.Sample(kind), MapLinkStyle.Dash(kind))));
        samples.Distinct().Count().ShouldBe(samples.Count);
    }

    // Each sample is drawn in the very ink its kind's lines are drawn in on the map (MapLinkStyle.Ink), so
    // the legend can never say another colour than the map; its arrowhead too.
    [Fact]
    public async Task Filter_everySample_isInkedAsItsKindsLinesOnTheMap()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        html.ShouldSatisfyAllConditions(
            () => Regex.Matches(html, "<svg class=\"map-filter__swatch\" data-link-kind=\"(\\w+)\"[^>]*><line [^>]*style=\"([^\"]*)\"")
                .Select(match => (match.Groups[1].Value, match.Groups[2].Value))
                .ShouldBe(Enum.GetValues<MapLinkKind>().Select(kind => (kind.ToString(), $"stroke: {MapLinkStyle.Ink(kind)}"))),
            () => Samples(html).Where(sample => sample.Arrowhead is not null).Select(sample => (sample.Kind, sample.Arrowhead))
                .ShouldBe(Enum.GetValues<MapLinkKind>().Where(kind => MapLinkStyle.SampleEnd(kind) == MapLineEnd.Arrowhead).Select(kind => (kind.ToString(), (string?)$"fill: {MapLinkStyle.Ink(kind)}"))));
    }

    // Each sample ends as the lines it stands for end on the map (MapLinkStyle.SampleEnd), so the legend tells
    // money passed on from money into a bucket by their ends, not by a width that is an amount's: money
    // passed on, a tax base and a condition in an arrowhead, money into a bucket in a bar its line runs into,
    // the edge of a bucket, drawn in a bucket's frame; that the kind's lines leaving the river or coming back
    // out of a bucket end in an arrowhead is its bubble's to say.
    [Fact]
    public async Task Filter_everySample_endsAsTheLinesItStandsForEnd()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        Samples(html).Select(sample => (sample.Kind, sample.Arrowhead is not null, sample.Bucket))
            .ShouldBe(Enum.GetValues<MapLinkKind>().Select(kind => (kind.ToString(), MapLinkStyle.SampleEnd(kind) == MapLineEnd.Arrowhead,
                MapLinkStyle.SampleEnd(kind) == MapLineEnd.Bucket ? $"stroke: {MapLinkStyle.BucketEdge}" : null)));
    }

    // Each sample of the legend: its kind, its arrowhead's ink if it has one, and its bar's if it has one.
    private static List<(string Kind, string? Arrowhead, string? Bucket)> Samples(string html) =>
    [
        .. Regex.Matches(html, "<svg class=\"map-filter__swatch\" data-link-kind=\"(\\w+)\"[^>]*>(.*?)</svg>", RegexOptions.Singleline).Select(match => (
            match.Groups[1].Value,
            Regex.Match(match.Groups[2].Value, "<polygon class=\"map-filter__arrow\"[^>]*style=\"([^\"]*)\"") is { Success: true } arrow ? arrow.Groups[1].Value : null,
            Regex.Match(match.Groups[2].Value, "<line class=\"map-filter__bucket\"[^>]*style=\"([^\"]*)\"") is { Success: true } bar ? bar.Groups[1].Value : null)),
    ];

    // The line under the boxes is for the viewer who has ticked none: nothing to read off the map until one
    // is. With one ticked, or all, it is left out, and so is any line of how many links are shown, which
    // the boxes' own counts and states already say.
    [Fact]
    public async Task Filter_everyKindHidden_saysToTickOne()
    {
        var filter = EveryKind();
        foreach (var kind in Enum.GetValues<MapLinkKind>())
            filter.Set(kind, shown: false);

        var html = await RenderAsync(filter, Counts);

        (Note(html), LegendBoxes(html).Any(box => box.Ticked)).ShouldBe(("Every kind of link is hidden: tick one to see its links.", false));
    }

    [Fact]
    public async Task Filter_aKindStillTicked_saysNothingBelowTheBoxes()
    {
        var some = EveryKind();
        some.Set(MapLinkKind.TaxBase, shown: false);
        some.Set(MapLinkKind.Condition, shown: false);

        var (all, part) = (await RenderAsync(EveryKind(), Counts), await RenderAsync(some, Counts));

        (Note(all), Note(part)).ShouldBe(("", ""));
    }

    // A plan without a link, a refused amount leaves none, has nothing to count; the boxes stay, each
    // counted as none.
    [Fact]
    public async Task Filter_aPlanWithoutALink_drawsEveryBoxCountedAsNone()
    {
        var html = await RenderAsync(EveryKind(), new Dictionary<MapLinkKind, int>());

        (LegendBoxes(html).Count, LegendBoxes(html).Select(box => box.Text).Distinct().Count()).ShouldBe((4, 4));
        LegendBoxes(html).Select(box => box.Text).ShouldAllBe(text => text.Contains("(none in this plan)"));
    }

    // Nothing here is a live region, and no resx key shows through for want of a text.
    [Fact]
    public async Task Filter_isNoLiveRegionAndLeaksNoKey()
    {
        var html = await RenderAsync(EveryKind(), Counts);

        (Regex.IsMatch(html, "aria-live|role=\"(status|alert)\""), Regex.IsMatch(html, "(Name|Gloss)_[A-Z]|Count_(None|One|Many)|AllHidden|Hidden_(One|Many)|>Legend<"))
            .ShouldBe((false, false));
    }

    // The filter of a viewer who ticked every box.
    private static MapLinkFilter EveryKind() => new(EveryKindShown());

    private static async Task<string> RenderAsync(MapLinkFilter filter, Dictionary<MapLinkKind, int> counts) =>
        WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(MapFilter), new Folds(new BrowserStorage()), new() { [nameof(MapFilter.Counts)] = counts }, services => services.AddSingleton(filter)));

    private static string Note(string html) =>
        Regex.Match(html, "<p class=\"map-filter__note\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value.Trim();
}

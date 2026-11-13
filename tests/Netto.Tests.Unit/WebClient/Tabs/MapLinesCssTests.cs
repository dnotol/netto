using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the Map tab's stylesheet draws the map's lines on is what the layout keeps them apart for: a map no
// narrower than a column 34rem wide at any width (MapLayout.NarrowestRem), beside the card from 1,201 px and
// over it below, where it scrolls sideways only once the window has not that room, and never shorter than a
// chain row to 3.25rem (MapQuietTests holds that one); a bucket a line runs on under, and an arrowhead sized
// by its line.
public class MapLinesCssTests
{
    private static readonly string Css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css");

    // One width for every window: the map's own rule holds it, and no media query sets another.
    [Fact]
    public void MapTabCss_atEveryWidth_drawsTheMapNoNarrowerThanTheLayoutKeepsItsLinesApartFor()
    {
        var mapRules = Stylesheet.Rules(Css).Where(rule => rule.Selector == ".map").Select(rule => rule.Declarations).ToList();

        mapRules.ShouldSatisfyAllConditions(
            () => mapRules.Count(declarations => declarations.Contains("min-width", StringComparison.Ordinal)).ShouldBe(1),
            () => mapRules.ShouldContain(declarations => declarations.Contains($"min-width: {Stylesheet.Rem(MapLayout.NarrowestRem)};", StringComparison.Ordinal)));
    }

    // Below the breakpoint the map scrolls sideways inside its column once the window is narrower than it: in
    // a frame of its own, which holds the map alone, so the tools over it, and the panel of « Edit plan »
    // that drops from them, are never clipped by a scroll area, nor the map's column given one (a box that
    // scrolls one way scrolls the other too).
    [Fact]
    public void MapTabCss_belowTheBreakpoint_scrollsTheMapSidewaysInAFrameOfItsOwn()
    {
        var rules = Stylesheet.Rules(Css);

        rules.ShouldSatisfyAllConditions(
            () => Stylesheet.Block(Stylesheet.MediaBlock(Css, "(max-width: 1200px)"), ".map-tab__frame").ShouldContain("overflow-x: auto;"),
            () => rules.Where(rule => rule.Selector is ".map-tab__map" or ".map-tab__tools" or ".map-tab__add").ShouldAllBe(rule => !rule.Declarations.Contains("overflow")));
    }

    // The frame holds the map and nothing else: the tools are over it, outside it.
    [Fact]
    public async Task MapTab_framesTheMapAloneAndNotItsTools()
    {
        var html = await MapRendering.RenderTabAsync(MapLinksTests.Full);

        var frame = System.Text.RegularExpressions.Regex.Match(html, "<div class=\"map-tab__frame\"[^>]*>\\s*<div class=\"map\" ");
        (frame.Success, html.IndexOf("class=\"map-tab__tools\"", StringComparison.Ordinal) < frame.Index).ShouldBe((true, true));
    }

    // A bucket is drawn over the lines that run on under it, on the map's ground under its wash, so a line
    // into it meets its edge, rounded corners and all, whatever runs beneath it.
    [Fact]
    public void MapTabCss_drawsABucketOpaqueOverTheLinesUnderIt()
    {
        Stylesheet.Block(Css, ".map-bucket").ShouldContain("background: linear-gradient(var(--netto-wash), var(--netto-wash)), var(--netto-page);");
    }

    // A bucket is a button as a module is: in the map's own type, not the browser's, its size its own after
    // that, a pointer over it, gold where the pointer or the choice is on it, as a module is.
    [Fact]
    public void MapTabCss_drawsABucketAsAButtonGoldWhereThePointerOrTheChoiceIsOnIt()
    {
        var bucket = Stylesheet.Block(Css, ".map-bucket");

        Css.ShouldSatisfyAllConditions(
            () => bucket.ShouldContain("font: inherit;"),
            () => bucket.IndexOf("font: inherit;", StringComparison.Ordinal).ShouldBeLessThan(bucket.IndexOf("font-size: 0.8rem;", StringComparison.Ordinal)),
            () => bucket.ShouldContain("cursor: pointer;"),
            () => Stylesheet.Block(Css, ".map-bucket:hover").ShouldContain("border-color: var(--netto-gold);"),
            () => Stylesheet.Block(Css, ".map-bucket--chosen").ShouldContain("border-color: var(--netto-gold);"),
            () => Stylesheet.Block(Css, ".map-bucket--chosen").ShouldContain("box-shadow: inset 0 0 0 1px var(--netto-gold);"));
    }

    // An arrowhead is as large as its line asks, the sizes set on it: half its base the border each side,
    // its length the border it points with, whichever way it points.
    [Fact]
    public void MapTabCss_sizesAnArrowheadByWhatIsSetOnIt()
    {
        Css.ShouldSatisfyAllConditions(
            () => Stylesheet.Block(Css, ".map-arrow").ShouldContain("border: var(--map-arrow-half) solid transparent;"),
            () => Ways.Where(way => !Stylesheet.Block(Css, $".map-arrow--{way}").Contains("var(--map-arrow-length) solid var(--map-arrow)", StringComparison.Ordinal))
                .ShouldBeEmpty());
    }

    // An arrowhead is drawn on the map's ground, its box the size of its base by its length, over the end
    // of its line, so the line, however thick, stops at the arrowhead's base and the arrowhead reads as a
    // point at any size of the map, its line's width never showing beside its front.
    [Fact]
    public void MapTabCss_drawsAnArrowheadOnTheGroundOverTheEndOfItsLine()
    {
        Stylesheet.Block(Css, ".map-arrow").ShouldContain("background: var(--netto-page);");
    }

    // The pocket line's steps are square-ended, so each join reads as the line widening rather than as a round cap
    // bulging into the next step, its halo the ground, both keeping their width as the map stretches; nothing of it
    // transitions, and only the whole of it fades, never a step.
    [Fact]
    public void MapTabCss_drawsThePocketLineSquareEndedFadingOnlyWhole()
    {
        var shape = Stylesheet.Rules(Css).Single(rule => rule.Selector.Contains(".map-pocket-line__halo", StringComparison.Ordinal)
            && rule.Selector.Contains(".map-pocket-line__step", StringComparison.Ordinal)).Declarations;

        Css.ShouldSatisfyAllConditions(
            () => shape.ShouldContain("stroke-linecap: butt;"),
            () => shape.ShouldContain("vector-effect: non-scaling-stroke;"),
            () => Stylesheet.Block(Css, ".map-pocket-line__halo").ShouldContain("stroke: var(--netto-page);"),
            () => Stylesheet.Block(Css, ".map-pocket-line--faded").ShouldContain("opacity: 0.18;"),
            () => Stylesheet.Rules(Css).Where(rule => rule.Selector.Contains("map-pocket-line", StringComparison.Ordinal))
                .Where(rule => rule.Declarations.Contains("transition", StringComparison.Ordinal) || (rule.Selector != ".map-pocket-line--faded" && rule.Declarations.Contains("opacity", StringComparison.Ordinal)))
                .Select(rule => rule.Selector).ShouldBeEmpty());
    }

    // The ways an arrowhead points.
    private static readonly string[] Ways = ["down", "up", "right", "left"];
}

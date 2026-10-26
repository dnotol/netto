using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the Map tab's stylesheet draws the map's lines on is what the layout keeps them apart for: a map no
// narrower than a column 34rem wide from 1,201 px, where the column beside it leaves it the least room, and
// 60rem below, where it scrolls sideways instead (MapLayout.NarrowestRem), and never shorter than a chain
// row to 3.25rem (MapQuietTests holds that one); a bucket a line runs on under, and an arrowhead sized by
// its line.
public class MapLinesCssTests
{
    private static readonly string Css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css");

    [Fact]
    public void MapTabCss_fromTheBreakpoint_drawsTheMapNoNarrowerThanTheLayoutKeepsItsLinesApartFor()
    {
        Stylesheet.Block(Stylesheet.MediaBlock(Css, "(min-width: 1201px)"), ".map").ShouldContain($"min-width: {Stylesheet.Rem(MapLayout.NarrowestRem)};");
    }

    [Fact]
    public void MapTabCss_belowTheBreakpoint_drawsTheMapWiderThanThat()
    {
        Stylesheet.Block(Stylesheet.MediaBlock(Css, "(max-width: 1200px)"), ".map").ShouldContain("min-width: 60rem;");
    }

    // A bucket is drawn over the lines that run on under it, on the map's ground under its wash, so a line
    // into it meets its edge, rounded corners and all, whatever runs beneath it.
    [Fact]
    public void MapTabCss_drawsABucketOpaqueOverTheLinesUnderIt()
    {
        Stylesheet.Block(Css, ".map-bucket").ShouldContain("background: linear-gradient(var(--netto-wash), var(--netto-wash)), var(--netto-page);");
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

    // The ways an arrowhead points.
    private static readonly string[] Ways = ["down", "up", "right", "left"];
}

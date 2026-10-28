using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The layout that keeps the Map tab's map in view beside the card of the module chosen, and the look of a
// skipped Company year's box. How it looks in a window is the browser's check; these hold what it rests on:
// the map and its card share the page's column and never leave it; from 1,201 px the map is sticky under the
// band, as tall as the window leaves it, its tools above it, and the card beside it is a plain block the page
// scrolls, with no scroll area of its own or around it; below it the card goes under the map.
public class MapPanelLayoutTests
{
    // The map and the card take the page's column and no room past it: nothing reaches into the margins.
    [Fact]
    public void Layout_keepsTheMapAndTheCardInsideThePagesColumn()
    {
        var css = MapTabCss();

        css.ShouldSatisfyAllConditions(
            () => css.ShouldNotContain("margin-inline"),
            () => css.ShouldNotContain("100vw"),
            () => Rule(Stylesheet.MediaBlock(css, "(min-width: 1201px)"), ".map-tab").ShouldContain("grid-template-columns: minmax(0, 1fr) minmax(34rem, 0.6fr)"));
    }

    // From 1,201 px the map stays under the band while the page scrolls past it, never taller than the window
    // leaves under the band, one custom property saying how much that is; its tools take their line and the
    // map shrinks to what they leave it, never below its smallest scale.
    [Fact]
    public void Layout_fromTheBreakpoint_keepsTheMapAndItsToolsWithinWhatTheWindowLeavesUnderTheBand()
    {
        var css = MapTabCss();
        var wide = Stylesheet.MediaBlock(css, "(min-width: 1201px)");

        css.ShouldSatisfyAllConditions(
            () => Regex.Matches(css, @"--map-room\s*:\s*([^;]+);").Select(match => match.Groups[1].Value).ShouldHaveSingleItem().ShouldContain("var(--netto-under-bar"),
            () => Rule(wide, ".map-tab__map").ShouldContain("position: sticky"),
            () => Rule(wide, ".map-tab__map").ShouldContain("top: var(--netto-under-bar)"),
            () => Rule(wide, ".map-tab__map").ShouldContain("display: flex"),
            () => Rule(wide, ".map-tab__map").ShouldContain("flex-direction: column"),
            () => Rule(wide, ".map-tab__map").ShouldContain("max-height: var(--map-room)"),
            () => Rule(wide, ".map-tab__tools").ShouldContain("flex: none"),
            () => Rule(wide, ".map-tab__frame").ShouldContain("flex: 0 1 auto"),
            () => Rule(wide, ".map-tab__frame").ShouldContain("display: flex"),
            () => Rule(wide, ".map").ShouldContain("flex: 0 1 auto"),
            () => Rule(css, ".map").ShouldContain("var(--map-room)"));
    }

    // The tools are one line, the legend taking what « Add to plan » leaves it and wrapping its boxes inside
    // that room, never « Add to plan » onto a line of its own: so the row is two lines at most where the
    // column is narrowest, 1,201 px wide.
    [Fact]
    public void Tools_keepAddToPlanOnTheLegendsFirstLine()
    {
        var css = MapTabCss();

        css.ShouldSatisfyAllConditions(
            () => Rule(css, ".map-tab__tools").ShouldContain("flex-wrap: nowrap"),
            () => Rule(css, ".map-tab__tools").ShouldNotContain("flex-wrap: wrap"),
            () => Rule(css, ".map-tab__filter").ShouldContain("flex: 1 1 auto"),
            () => Rule(css, ".map-tab__filter").ShouldContain("min-width: 0"),
            () => Rule(css, ".map-tab__add").ShouldContain("flex: none"));
    }

    // The card beside the map is the page's to scroll: no rule makes the column or the room of the card a
    // scroll area, sticky or capped, at any width, and the grid keeps no height of its own.
    [Fact]
    public void Layout_atEveryWidth_givesTheCardNoScrollAreaOfItsOwnAndNoneAroundIt()
    {
        var rules = Stylesheet.Rules(File.ReadAllText(MapTabCssPath())).Where(rule => rule.Selector is ".map-tab" or ".map-tab__side" or ".map-tab__panel").ToList();

        rules.ShouldSatisfyAllConditions(
            () => rules.ShouldNotBeEmpty(),
            () => rules.ShouldAllBe(rule => !rule.Declarations.Contains("overflow") && !rule.Declarations.Contains("max-height")
                && !rule.Declarations.Contains("sticky") && !rule.Declarations.Contains("min-height")));
    }

    // The column under the map, below the breakpoint, is as tall as its card: nothing sticks there.
    [Fact]
    public void Layout_belowTheBreakpoint_sticksNothing()
    {
        var narrow = MapTabCss().Replace(Stylesheet.MediaBlock(MapTabCss(), "(min-width: 1201px)"), "");

        narrow.ShouldNotContain("sticky");
    }

    // A module's title keeps to its box on the narrowest map: its size follows the map's width (the map is a
    // container of its inline size), from 0.72rem on a map 34rem wide to 0.85rem on a wide one, and it wraps
    // onto a second line where it must, two lines at its smallest size and line height fitting the smallest
    // box, half a chain row of 3.25rem (MapLayout.SmallestRowRem).
    [Fact]
    public void Module_title_scalesWithTheMapAndFitsTwoLinesInTheSmallestBox()
    {
        var (map, module) = (Rule(MapTabCss(), ".map"), Rule(MapTabCss(), ".map-module"));
        var font = Regex.Match(module, @"font-size:\s*clamp\(([\d.]+)rem,\s*[\d.]+cqi,\s*([\d.]+)rem\)");
        var lineHeight = decimal.Parse(Regex.Match(module, @"line-height:\s*([\d.]+);").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        module.ShouldSatisfyAllConditions(
            () => map.ShouldContain("container-type: inline-size"),
            () => (font.Groups[1].Value, font.Groups[2].Value).ShouldBe(("0.72", "0.85")),
            () => (2 * 0.72m * lineHeight).ShouldBeLessThanOrEqualTo(0.5m * Netto.Web.Client.Tabs.MapLayout.SmallestRowRem));
    }

    // A Company year the viewer skipped is drawn as the river draws its skipped card: the same outline, not a
    // filled box.
    [Fact]
    public void Module_skipped_isDrawnAsTheRiverDrawsItsSkippedCard()
    {
        var card = Rule(Without(File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web.Client", "River", "ModuleCard.razor.css"))), ".module-card--skipped");

        Rule(MapTabCss(), ".map-module--skipped").ShouldSatisfyAllConditions(
            () => card.ShouldContain("border-style: dashed"),
            () => card.ShouldContain("background: transparent"),
            () => Rule(MapTabCss(), ".map-module--skipped").ShouldContain("border-style: dashed"),
            () => Rule(MapTabCss(), ".map-module--skipped").ShouldContain("background: transparent"));
    }

    private static string MapTabCssPath() => RepositoryFile.FindUp("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css");

    // The stylesheet with its comments left out.
    private static string MapTabCss() => Without(File.ReadAllText(MapTabCssPath()));

    private static string Without(string css) => Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);

    // The declarations of every rule for exactly that selector in the css given.
    private static string Rule(string css, string selector)
    {
        var matches = Regex.Matches(css, $@"(?<![\w-]){Regex.Escape(selector)}\s*\{{([^}}]*)\}}");
        matches.Count.ShouldBeGreaterThan(0, selector);
        return string.Join(' ', matches.Select(match => match.Groups[1].Value));
    }
}

using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The layout that keeps the Map tab's map in view beside the card of the module chosen, and the look of a
// skipped Company year's box. How it looks in a window is the browser's check; these hold what it rests on:
// from 1,201 px the column of the neighbourhood and the card is sticky under the bar and never taller than
// the window leaves, its two regions scrolling apart so that a long card never pushes the neighbourhood out of
// sight, and below it the column goes under the map as tall as its card.
public class MapPanelLayoutTests
{
    // From 1,201 px the column is sticky under the bar and never taller than the window leaves, so a long card
    // scrolls inside it and the map keeps its place; the grid is at least that tall, and one custom property
    // says how much the window leaves, for the map, the column and the grid alike.
    [Fact]
    public void Layout_fromTheBreakpoint_keepsTheColumnWithinWhatTheWindowLeavesUnderTheBar()
    {
        var css = Stylesheet();
        var wide = MediaBlock(css, "(min-width: 1201px)");

        css.ShouldSatisfyAllConditions(
            () => Regex.Matches(css, @"--map-room\s*:\s*([^;]+);").Select(match => match.Groups[1].Value).ShouldHaveSingleItem().ShouldContain("var(--netto-under-bar"),
            () => Rule(wide, ".map-tab__side").ShouldContain("position: sticky"),
            () => Rule(wide, ".map-tab__side").ShouldContain("top: var(--netto-under-bar)"),
            () => Rule(wide, ".map-tab__side").ShouldContain("max-height: var(--map-room)"),
            () => Rule(wide, ".map-tab__side").ShouldContain("overflow-y: auto"),
            () => Rule(wide, ".map-tab").ShouldContain("min-height: var(--map-room)"),
            () => Rule(css, ".map").ShouldContain("var(--map-room)"));
    }

    // The column is two regions one above the other, the neighbourhood of the module chosen and its card, each
    // scrolling inside itself: the neighbourhood takes what it needs up to half of what the window leaves and
    // does not shrink, the card the rest, so a card as long as it likes scrolls under a neighbourhood that
    // stays in sight, and a neighbourhood too long for its half scrolls inside it.
    [Fact]
    public void Layout_fromTheBreakpoint_keepsTheNeighbourhoodInSightWhateverTheLengthOfTheCard()
    {
        var wide = MediaBlock(Stylesheet(), "(min-width: 1201px)");

        wide.ShouldSatisfyAllConditions(
            () => Rule(wide, ".map-tab__side").ShouldContain("display: flex"),
            () => Rule(wide, ".map-tab__side").ShouldContain("flex-direction: column"),
            () => Rule(wide, ".map-tab__hood").ShouldContain("flex: 0 0 auto"),
            () => Rule(wide, ".map-tab__hood").ShouldContain("max-height: calc(var(--map-room) / 2)"),
            () => Rule(wide, ".map-tab__hood").ShouldContain("overflow-y: auto"),
            () => Rule(wide, ".map-tab__panel").ShouldContain("flex: 1 1 auto"),
            () => Rule(wide, ".map-tab__panel").ShouldContain("min-height: 0"),
            () => Rule(wide, ".map-tab__panel").ShouldContain("overflow-y: auto"));
    }

    // Below it the column goes under the map, as tall as its card: nothing sticky, nothing scrolling inside,
    // the neighbourhood above the card with room between.
    [Fact]
    public void Layout_belowTheBreakpoint_leavesTheColumnUnderTheMapAsTallAsItsCard()
    {
        var narrow = Stylesheet().Replace(MediaBlock(Stylesheet(), "(min-width: 1201px)"), "");

        narrow.ShouldSatisfyAllConditions(
            () => narrow.ShouldNotContain(".map-tab__side"),
            () => narrow.ShouldNotContain(".map-tab__panel"),
            () => narrow.ShouldNotContain("overflow-y"),
            () => narrow.ShouldNotContain("max-height"),
            () => Rule(narrow, ".map-tab__hood").ShouldContain("margin-bottom: 1rem"));
    }

    // A Company year the viewer skipped is drawn as the river draws its skipped card: the same outline, not a
    // filled box.
    [Fact]
    public void Module_skipped_isDrawnAsTheRiverDrawsItsSkippedCard()
    {
        var card = Rule(Without(File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web.Client", "River", "ModuleCard.razor.css"))), ".module-card--skipped");

        Rule(Stylesheet(), ".map-module--skipped").ShouldSatisfyAllConditions(
            () => card.ShouldContain("border-style: dashed"),
            () => card.ShouldContain("background: transparent"),
            () => Rule(Stylesheet(), ".map-module--skipped").ShouldContain("border-style: dashed"),
            () => Rule(Stylesheet(), ".map-module--skipped").ShouldContain("background: transparent"));
    }

    // The stylesheet with its comments left out.
    private static string Stylesheet() =>
        Without(File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css")));

    private static string Without(string css) => Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);

    // A media query's whole block, from its opening brace to the one that closes it.
    private static string MediaBlock(string css, string query)
    {
        var start = css.IndexOf($"@media {query}", StringComparison.Ordinal);
        start.ShouldBeGreaterThan(-1, query);
        var open = css.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < css.Length; index++)
        {
            depth += css[index] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0)
                return css[start..(index + 1)];
        }

        throw new InvalidOperationException($"The block of {query} is never closed.");
    }

    // The declarations of every rule for exactly that selector in the css given.
    private static string Rule(string css, string selector)
    {
        var matches = Regex.Matches(css, $@"(?<![\w-]){Regex.Escape(selector)}\s*\{{([^}}]*)\}}");
        matches.Count.ShouldBeGreaterThan(0, selector);
        return string.Join(' ', matches.Select(match => match.Groups[1].Value));
    }
}

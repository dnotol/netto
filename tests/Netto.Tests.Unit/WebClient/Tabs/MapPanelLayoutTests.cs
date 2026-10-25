using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The layout that keeps the Map tab's map in view beside the card of the module chosen. How it looks in a
// window is the browser's check; these hold what it rests on: from 1,201 px the column of links and card is
// sticky under the bar and never taller than the window leaves, so a long card scrolls inside it, and below
// it the column goes under the map as tall as its card.
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

    // Below it the column goes under the map, as tall as its card: nothing sticky, nothing scrolling inside.
    [Fact]
    public void Layout_belowTheBreakpoint_leavesTheColumnUnderTheMapAsTallAsItsCard()
    {
        var narrow = Stylesheet().Replace(MediaBlock(Stylesheet(), "(min-width: 1201px)"), "");

        narrow.ShouldSatisfyAllConditions(
            () => narrow.ShouldNotContain(".map-tab__side"),
            () => narrow.ShouldNotContain("overflow-y"));
    }

    // The stylesheet with its comments left out.
    private static string Stylesheet() =>
        Regex.Replace(File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css")), @"/\*.*?\*/", "", RegexOptions.Singleline);

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

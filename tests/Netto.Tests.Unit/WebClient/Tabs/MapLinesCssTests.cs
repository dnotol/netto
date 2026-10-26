using System.Text.RegularExpressions;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the Map tab's stylesheet draws the map's lines on is what the layout keeps them apart for: a map no
// narrower than a column 34rem wide from 1,201 px, where the column beside it leaves it the least room, and
// 60rem below, where it scrolls sideways instead (MapLayout.NarrowestRem), and never shorter than a chain
// row to 3.25rem (MapQuietTests holds that one).
public class MapLinesCssTests
{
    private static readonly string Css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css");

    [Fact]
    public void MapTabCss_fromTheBreakpoint_drawsTheMapNoNarrowerThanTheLayoutKeepsItsLinesApartFor()
    {
        Regex.IsMatch(Css, $@"@media \(min-width: 1201px\) \{{(?:(?!@media).)*?\n\s*\.map \{{[^}}]*min-width: {Regex.Escape(Stylesheet.Rem(MapLayout.NarrowestRem))};", RegexOptions.Singleline)
            .ShouldBeTrue();
    }

    [Fact]
    public void MapTabCss_belowTheBreakpoint_drawsTheMapWiderThanThat()
    {
        Regex.IsMatch(Css, @"@media \(max-width: 1200px\) \{(?:(?!@media).)*?\n\s*\.map \{[^}]*min-width: 60rem;", RegexOptions.Singleline).ShouldBeTrue();
    }
}

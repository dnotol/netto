using System.Text.RegularExpressions;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// How each kind of link is drawn, told apart at a glance and with no colour needed: money passed on solid
// and heavy, money into a bucket solid and light, what only changes a tax base dotted, a condition dashed.
// The map's lines and the filter's swatches read the one style.
public class MapLinkStyleTests
{
    [Fact]
    public void Style_noTwoKinds_shareOne()
    {
        Enum.GetValues<MapLinkKind>().Select(kind => (MapLinkStyle.Dash(kind), MapLinkStyle.Width(kind))).Distinct().Count()
            .ShouldBe(Enum.GetValues<MapLinkKind>().Length);
    }

    [Fact]
    public void Style_money_isSolidAndHeavierPassedOnThanIntoABucket()
    {
        (MapLinkStyle.Dash(MapLinkKind.PassedOn), MapLinkStyle.Dash(MapLinkKind.IntoBucket),
                decimal.Parse(MapLinkStyle.Width(MapLinkKind.PassedOn), System.Globalization.CultureInfo.InvariantCulture)
                > decimal.Parse(MapLinkStyle.Width(MapLinkKind.IntoBucket), System.Globalization.CultureInfo.InvariantCulture))
            .ShouldBe((null, null, true));
    }

    [Fact]
    public void Style_aTaxBaseAndACondition_areBrokenLinesOfTheirOwn()
    {
        (MapLinkStyle.Dash(MapLinkKind.TaxBase), MapLinkStyle.Dash(MapLinkKind.Condition)).ShouldBe(("1 4", "7 5"));
    }

    // Every line is drawn over a halo of the map's ground, its own width and two pixels each side, so a line
    // drawn over another leaves a gap on each side of it: the crossing reads as over and under.
    [Theory]
    [InlineData(MapLinkKind.PassedOn)]
    [InlineData(MapLinkKind.IntoBucket)]
    [InlineData(MapLinkKind.TaxBase)]
    [InlineData(MapLinkKind.Condition)]
    public void Halo_everyKind_isItsLinesWidthWithTwoPixelsEachSide(MapLinkKind kind)
    {
        (Pixels(MapLinkStyle.Halo(kind)) - Pixels(MapLinkStyle.Width(kind))).ShouldBe(2 * 2m);
    }

    [Fact]
    public void Style_aKindItHasNoStyleFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapLinkStyle.Dash((MapLinkKind)99));
    }

    // Each kind's ink, which its lines, their arrowheads and the legend's sample of it are drawn in, is a
    // token both themes declare, so both draw it: money passed on the strongest, money into a bucket and a
    // tax base lighter, a condition muted; what reaches the pocket is the map's one gold.
    [Fact]
    public void Ink_everyKind_isATokenBothThemesDeclare()
    {
        var css = File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web", "wwwroot", "app.css"));
        var (dark, light) = (Declared(css, @":root\s*\{"), Declared(css, @":root\[data-theme=""light""\]\s*\{"));
        string[] tokens = [.. Enum.GetValues<MapLinkKind>().Select(MapLinkStyle.Ink).Select(Token)];

        tokens.ShouldSatisfyAllConditions(
            () => tokens.ShouldBe(["--netto-data-strong", "--netto-data-high", "--netto-data-high", "--netto-text-muted"]),
            () => tokens.Where(token => !dark.Contains(token) || !light.Contains(token)).ShouldBeEmpty());
    }

    [Fact]
    public void PocketInk_isTheGold()
    {
        MapLinkStyle.PocketInk.ShouldBe("var(--netto-gold)");
    }

    [Fact]
    public void Ink_aKindItHasNoInkFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapLinkStyle.Ink((MapLinkKind)99));
    }

    // The ink is the style's alone: neither the map's stylesheet nor the legend's gives a line, an
    // arrowhead or a sample an ink of its own, by kind or not, so the two can never drift apart.
    [Theory]
    [InlineData("MapTab.razor.css")]
    [InlineData("MapFilter.razor.css")]
    public void Stylesheet_inksNoLineArrowheadOrSampleOfItsOwn(string stylesheet)
    {
        var css = File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web.Client", "Tabs", stylesheet));

        Regex.Matches(Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline), @"([^{}]+)\{([^}]*)\}")
            .Where(rule => Regex.IsMatch(rule.Groups[1].Value, @"\.map-(line|arrow)(--[a-z-]+)?(?![\w-])|\.map-filter__swatch(?![\w-])|data-link-kind"))
            .Where(rule => Regex.IsMatch(rule.Groups[2].Value, @"(?<![\w-])(stroke|--map-arrow|color)\s*:"))
            .Select(rule => rule.Groups[1].Value.Trim()).ShouldBeEmpty();
    }

    private static string Token(string ink) => Regex.Match(ink, @"^var\((--netto-[a-z-]+)\)$") is { Success: true } match ? match.Groups[1].Value : ink;

    // The tokens a theme's block of app.css declares.
    private static HashSet<string> Declared(string css, string selector) =>
    [
        .. Regex.Matches(Regex.Match(css, selector + @"([^}]*)\}").Groups[1].Value, @"(--netto-[a-z-]+)\s*:").Select(match => match.Groups[1].Value),
    ];

    private static decimal Pixels(string width) => decimal.Parse(width, System.Globalization.CultureInfo.InvariantCulture);
}

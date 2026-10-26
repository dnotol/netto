using System.Text.RegularExpressions;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// How each kind of link is drawn, told apart at a glance and with no colour needed: money passed on solid,
// ending in an arrowhead, money into a bucket solid, running into it, what only changes a tax base dotted, a
// condition dashed. The map's lines and the filter's samples read the one style; a line of money is as
// thick as its amount (MapLineWidths), a line carrying none its kind's width.
public class MapLinkStyleTests
{
    // By their dash and their end alone, neither a width, which is an amount's, nor an ink.
    [Fact]
    public void Style_noTwoKinds_shareOne()
    {
        Enum.GetValues<MapLinkKind>().Select(kind => (MapLinkStyle.Dash(kind), MapLinkStyle.SampleEnd(kind))).Distinct().Count()
            .ShouldBe(Enum.GetValues<MapLinkKind>().Length);
    }

    [Fact]
    public void Style_money_isSolidPassedOnEndingInAnArrowheadIntoABucketInABar()
    {
        (MapLinkStyle.Dash(MapLinkKind.PassedOn), MapLinkStyle.Dash(MapLinkKind.IntoBucket), MapLinkStyle.SampleEnd(MapLinkKind.PassedOn), MapLinkStyle.SampleEnd(MapLinkKind.IntoBucket))
            .ShouldBe((null, null, MapLineEnd.Arrowhead, MapLineEnd.Bucket));
    }

    // A rule's line ends in an arrowhead on the module it reaches.
    [Theory]
    [InlineData(MapLinkKind.TaxBase)]
    [InlineData(MapLinkKind.Condition)]
    public void SampleEnd_aRulesLine_isAnArrowhead(MapLinkKind kind)
    {
        MapLinkStyle.SampleEnd(kind).ShouldBe(MapLineEnd.Arrowhead);
    }

    // A sample's end is the end the map draws for the lines it stands for: every line passed on, every tax
    // base and every condition ends in an arrowhead; of the money into a bucket, the lines ending in a
    // bucket run on under it, as the sample's bar shows, and the others, leaving the river or coming back
    // out of a bucket, end in an arrowhead, as the kind's bubble says.
    [Fact]
    public void SampleEnd_ofEachKind_isTheEndTheMapDrawsForTheLinesItStandsFor()
    {
        var river = MapViewConditionsTests.CostlyCar;

        var bundles = MapView.From(river, river.Run()).Bundles();

        var (intoABucket, besides) = (bundles.Where(bundle => bundle.Kind == MapLinkKind.IntoBucket && bundle.To.Bucket is not null).ToList(),
            bundles.Where(bundle => bundle.Kind == MapLinkKind.IntoBucket && bundle.To.Bucket is null).ToList());
        bundles.ShouldSatisfyAllConditions(
            () => bundles.Where(bundle => MapLinkStyle.SampleEnd(bundle.Kind) == MapLineEnd.Arrowhead).ShouldAllBe(bundle => bundle.Arrowhead),
            () => (MapLinkStyle.SampleEnd(MapLinkKind.IntoBucket), intoABucket.Count > 0, intoABucket.All(bundle => !bundle.Arrowhead)).ShouldBe((MapLineEnd.Bucket, true, true)),
            () => besides.Select(bundle => bundle.To.IsOut ? "out of the river" : "back out of a bucket").Distinct().Order().ShouldBe(["back out of a bucket", "out of the river"]),
            () => besides.ShouldAllBe(bundle => bundle.Arrowhead));
    }

    // The legend's samples of money are of one width, saying the kind, not an amount: what tells them
    // apart is their end.
    [Fact]
    public void Sample_ofMoneyEitherWay_isOneWidth()
    {
        MapLinkStyle.Sample(MapLinkKind.PassedOn).ShouldBe(MapLinkStyle.Sample(MapLinkKind.IntoBucket));
    }

    // The bar a sample of money into a bucket runs into is a bucket's edge, in a bucket's frame, a token both
    // themes declare.
    [Fact]
    public void BucketEdge_isTheFrameOfABucketBothThemesDeclare()
    {
        (MapLinkStyle.BucketEdge, Stylesheet.ThemeTokens("dark").ContainsKey("--netto-line-input"), Stylesheet.ThemeTokens("light").ContainsKey("--netto-line-input"))
            .ShouldBe(("var(--netto-line-input)", true, true));
    }

    [Fact]
    public void Style_aTaxBaseAndACondition_areBrokenLinesOfTheirOwn()
    {
        (MapLinkStyle.Dash(MapLinkKind.TaxBase), MapLinkStyle.Dash(MapLinkKind.Condition)).ShouldBe(("1 4", "7 5"));
    }

    // A line that carries no money keeps its kind's width whatever the plan: a tax base 2 px, a condition
    // 1.75 px; a line of money has none of its kind's, being as thick as its amount.
    [Fact]
    public void Fixed_aTaxBaseAndAConditionKeepTheirWidth_moneyNone()
    {
        Enum.GetValues<MapLinkKind>().Select(MapLinkStyle.Fixed).ShouldBe([null, null, 2m, 1.75m]);
    }

    // A line of money is the one whose kind has no width of its own, as thick as its amount; every other
    // kind is a rule, a line that carries none. The widths, the placing of the amounts and the
    // neighbourhood's lines and rules all ask this one question.
    [Fact]
    public void IsMoney_aKindWithNoFixedWidth_isMoneyAndEveryOtherKindIsARule()
    {
        var kinds = Enum.GetValues<MapLinkKind>();

        kinds.ShouldSatisfyAllConditions(
            () => kinds.Where(MapLinkStyle.IsMoney).ShouldBe(kinds.Where(kind => MapLinkStyle.Fixed(kind) is null)),
            () => kinds.Where(kind => !MapLinkStyle.IsMoney(kind)).ShouldBe(kinds.Where(kind => MapLinkStyle.Fixed(kind) is not null)),
            () => kinds.Where(MapLinkStyle.IsMoney).ShouldBe([MapLinkKind.PassedOn, MapLinkKind.IntoBucket]),
            () => kinds.Where(kind => !MapLinkStyle.IsMoney(kind)).ShouldBe([MapLinkKind.TaxBase, MapLinkKind.Condition]));
    }

    [Fact]
    public void IsMoney_aKindItHasNoStyleFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapLinkStyle.IsMoney((MapLinkKind)99));
    }

    // The legend's sample of a kind that carries no money is as wide as its lines on the map.
    [Theory]
    [InlineData(MapLinkKind.TaxBase)]
    [InlineData(MapLinkKind.Condition)]
    public void Sample_aKindThatCarriesNoMoney_isAsWideAsItsLines(MapLinkKind kind)
    {
        ((decimal?)Pixels(MapLinkStyle.Sample(kind))).ShouldBe(MapLinkStyle.Fixed(kind));
    }

    // Every line is drawn over a halo of the map's ground, its own width and two pixels each side, so a line
    // drawn over another leaves a gap on each side of it: the crossing reads as over and under, the thinnest
    // line's as the thickest's.
    [Theory]
    [InlineData(1.5)]
    [InlineData(4.75)]
    [InlineData(8)]
    public void Halo_everyLine_isItsWidthWithTwoPixelsEachSide(double width)
    {
        (Pixels(MapLinkStyle.Halo((decimal)width)) - Pixels(MapLinkStyle.Width((decimal)width))).ShouldBe(2 * 2m);
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
        var (dark, light) = (Stylesheet.ThemeTokens("dark"), Stylesheet.ThemeTokens("light"));
        string[] tokens = [.. Enum.GetValues<MapLinkKind>().Select(MapLinkStyle.Ink).Select(Token)];

        tokens.ShouldSatisfyAllConditions(
            () => tokens.ShouldBe(["--netto-data-strong", "--netto-data-high", "--netto-data-high", "--netto-text-muted"]),
            () => tokens.Where(token => !dark.ContainsKey(token) || !light.ContainsKey(token)).ShouldBeEmpty());
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
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", stylesheet);

        Stylesheet.Rules(css)
            .Where(rule => Regex.IsMatch(rule.Selector, @"\.map-(line|arrow)(--[a-z-]+)?(?![\w-])|\.map-filter__swatch(?![\w-])|data-link-kind"))
            .Where(rule => Regex.IsMatch(rule.Declarations, @"(?<![\w-])(stroke|--map-arrow|color)\s*:"))
            .Select(rule => rule.Selector).ShouldBeEmpty();
    }

    private static string Token(string ink) => Regex.Match(ink, @"^var\((--netto-[a-z-]+)\)$") is { Success: true } match ? match.Groups[1].Value : ink;

    private static decimal Pixels(string width) => decimal.Parse(width, System.Globalization.CultureInfo.InvariantCulture);
}

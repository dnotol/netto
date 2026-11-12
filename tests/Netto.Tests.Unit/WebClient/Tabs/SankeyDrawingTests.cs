using System.Net;
using System.Text.RegularExpressions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The Sankey as a viewer meets it: every node a button named by its label, in the view's order, the buckets last; the
// bands for the eye only, each in its place, those not touching the choice faded; the car's contribution a dashed
// loop said in words under the drawing; no key and no « net »; the modules without a band
// buttons of their own; one line where there is no picture. What a click does is the browser's.
public class SankeyDrawingTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    [Fact]
    public async Task Drawing_everyNode_isAButtonNamedByItsLabel_inTheViewsOrder()
    {
        var view = SankeyView.From(Full, Full.Run())!;
        var picture = SankeyLayout.Of(view, node => SankeyLayoutTests.Words(view, node));

        var html = await RenderAsync(Full);

        Nodes(html).ShouldBe(picture.Places.Select(place => (place.Node.Id, string.Join(' ', place.Label.Lines), "false")));
    }

    [Fact]
    public async Task Drawing_aBucketsLabel_isTheBarsAmount_withNoWordAfterIt()
    {
        var html = await RenderAsync(Full);

        Nodes(html).Single(node => node.Id == "taxes_on_profit").Name.ShouldEndWith(Formats.Euros(BucketsView.From(Full.Run())!.AmountOf("taxes_on_profit")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Drawing_says_nothingOfHowItIsDrawn(bool french)
    {
        using var language = french ? InLanguage.French() : null;

        var html = await RenderAsync(Full);

        (html.Contains("sankey-key", StringComparison.Ordinal), Regex.IsMatch(html, @"2 px|net", RegexOptions.IgnoreCase)).ShouldBe((false, false));
    }

    [Fact]
    public async Task Drawing_earlierProfits_areNamedAsTheDividendCardsField()
    {
        var river = Full.WithEarlierProfits(20_000m);

        var html = await RenderAsync(river);

        Nodes(html).Single(node => node.Id == "earlier-profits").Name.ShouldStartWith("Profits kept from earlier years, after tax");
    }

    [Fact]
    public async Task Drawing_theBandsAndBars_areForTheEyeOnly()
    {
        var html = await RenderAsync(Full);

        Regex.Match(html, "<svg class=\"sankey__bands\"[^>]*>").Value.ShouldContain("aria-hidden=\"true\"");
    }

    [Fact]
    public async Task Drawing_aBand_eachStripOfThePicture_theLoopApart()
    {
        var view = SankeyView.From(Full, Full.Run())!;

        var html = await RenderAsync(Full);

        (Regex.Count(html, "<path class=\"sankey-band[ \"]"), Regex.Count(html, "<path class=\"sankey-loop[ \"]"))
            .ShouldBe((view.Bands.Count(band => !band.Loop), 1));
    }

    [Fact]
    public async Task Drawing_aBandAtTheMinimum_isMarkedSo()
    {
        var view = SankeyView.From(Full, Full.Run())!;
        var picture = SankeyLayout.Of(view, node => SankeyLayoutTests.Words(view, node));

        var html = await RenderAsync(Full);

        Regex.Count(html, "data-at-minimum=\"true\"").ShouldBe(picture.Strips.Count(strip => strip.AtMinimum && !strip.Band.Loop));
    }

    [Fact]
    public async Task Drawing_theCarContribution_isSaidUnderTheDrawing_withItsAmount()
    {
        var html = await RenderAsync(Full);

        Text(html, "sankey__loop").ShouldContain(Formats.Euros(Full.Run().Year!.Car.Contribution));
    }

    [Fact]
    public async Task Drawing_theCarContributionSentence_readsAloneWithoutALegend()
    {
        var html = await RenderAsync(Full);

        Text(html, "sankey__loop").ShouldBe($"Your contribution to the car, {Formats.Euros(Full.Run().Year!.Car.Contribution)}, goes back from your pocket to the company.");
    }

    [Fact]
    public async Task Drawing_noLoop_saysNone()
    {
        var river = MapRendering.River("a car without contribution");

        var html = await RenderAsync(river);

        html.Contains("sankey__loop", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task Drawing_aModuleChosen_isPressed_andTheBandsNotTouchingItFade()
    {
        var view = SankeyView.From(Full, Full.Run())!;

        var html = await RenderAsync(Full, MapChoice.Of(RiverModule.Car));

        (Nodes(html).Single(node => node.Id == "car").Pressed, Faded(html)).ShouldBe(("true", view.Bands.Count(band => !band.Touches(RiverModule.Car))));
    }

    [Fact]
    public async Task Drawing_aBucketChosen_keepsTheBandsAtItsEnds()
    {
        var view = SankeyView.From(Full, Full.Run())!;
        var pocket = SankeyNode.InBucket("pocket");

        var html = await RenderAsync(Full, MapChoice.InBucket("pocket"));

        Faded(html).ShouldBe(view.Bands.Count(band => band.From != pocket && band.To != pocket));
    }

    [Fact]
    public async Task Drawing_nothingChosen_fadesNothing()
    {
        var html = await RenderAsync(Full);

        Faded(html).ShouldBe(0);
    }

    [Fact]
    public async Task Drawing_aModuleWithoutABand_isAButtonUnderTheDrawing()
    {
        var html = await RenderAsync(Full, MapChoice.Of(RiverModule.Household));

        AlsoInPlan(html).ShouldBe([("household", "Household", "true")]);
    }

    [Fact]
    public async Task Drawing_whatMovesNoMoney_isSaidToBeBesideTheCard()
    {
        var html = await RenderAsync(Full);

        Text(html, "sankey__no-money").ShouldContain("beside the card");
    }

    [Fact]
    public async Task Drawing_noPicture_saysSo_everyModuleAButtonUnderIt()
    {
        var river = Full.WithReserve(-1m);

        var html = await RenderAsync(river);

        (Text(html, "sankey__none").Length > 0, Nodes(html).Count, string.Join(' ', AlsoInPlan(html).Select(module => module.Id)))
            .ShouldBe((true, 0, string.Join(' ', MapView.ModulesOf(river).Select(MapModules.Name))));
    }

    [Fact]
    public async Task Drawing_inFrench_namesItsNodesAndSaysItsLinesInFrench()
    {
        using var french = InLanguage.French();

        var html = await RenderAsync(Full.WithEarlierProfits(20_000m));

        (Nodes(html).Single(node => node.Id == "earlier-profits").Name.StartsWith("Réserves taxées des années précédentes", StringComparison.Ordinal),
            Nodes(html).Single(node => node.Id == "split").Name.StartsWith("À partager", StringComparison.Ordinal),
            Text(html, "sankey__no-money").Contains("carte", StringComparison.Ordinal)).ShouldBe((true, true, true));
    }

    [Fact]
    public void Stylesheet_transitionsNothing_andInksOnlyWithTheSitesTokens()
    {
        var css = Regex.Replace(Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "SankeyDrawing.razor.css"), "/\\*.*?\\*/", "", RegexOptions.Singleline);

        (Regex.IsMatch(css, "transition[a-z-]*\\s*:"), Regex.IsMatch(css, "#[0-9a-fA-F]{3,8}\\b|rgba?\\(|hsla?\\("), Regex.Count(css, "var\\(--netto-") > 5)
            .ShouldBe((false, false, true));
    }

    // A band the choice does not touch still reads as money: faded by one value, never as far as the map's rules lines.
    [Fact]
    public void Stylesheet_aFadedBand_keepsAtLeastFortyPercentOfItsInk_byOneValue()
    {
        var css = Stylesheet.WithoutComments(Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "SankeyDrawing.razor.css"));

        var faded = decimal.Parse(Regex.Match(Stylesheet.Block(css, ".sankey"), "--sankey-faded:\\s*([0-9.]+);").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        (faded >= 0.4m, Stylesheet.Rules(css).Single(rule => rule.Selector.Contains(".sankey-band--faded", StringComparison.Ordinal)).Declarations.Contains("opacity: var(--sankey-faded);", StringComparison.Ordinal)).ShouldBe((true, true));
    }

    internal static async Task<string> RenderAsync(RiverInputs river, MapChoice? chosen = null) =>
        WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(SankeyDrawing), new Folds(new BrowserStorage()), new()
        {
            [nameof(SankeyDrawing.Inputs)] = river,
            [nameof(SankeyDrawing.Run)] = river.Run(),
            [nameof(SankeyDrawing.Chosen)] = chosen,
        }));

    // Each node's button: its node, the words it is named by, whether it is pressed.
    internal static IReadOnlyList<(string Id, string Name, string Pressed)> Nodes(string html) =>
        [.. Regex.Matches(html, "<button type=\"button\" class=\"sankey-node[^\"]*\" aria-pressed=\"(\\w+)\" data-node=\"([a-z_-]+)\"[^>]*>(.*?)</button>", RegexOptions.Singleline)
            .Select(match => (match.Groups[2].Value, Words(match.Groups[3].Value), match.Groups[1].Value))];

    private static IReadOnlyList<(string Id, string Name, string Pressed)> AlsoInPlan(string html) =>
        [.. Regex.Matches(html, "<button type=\"button\" class=\"sankey__also-module[^\"]*\" aria-pressed=\"(\\w+)\" data-module=\"([a-z-]+)\"[^>]*>(.*?)</button>", RegexOptions.Singleline)
            .Select(match => (match.Groups[2].Value, Words(match.Groups[3].Value), match.Groups[1].Value))];

    private static int Faded(string html) => Regex.Count(html, "class=\"sankey-(?:band|loop)[^\"]*--faded");

    private static string Text(string html, string block) =>
        Words(Regex.Match(html, $"<p class=\"{block}\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value);

    private static string Words(string html) => Regex.Replace(Regex.Replace(html, "<[^>]+>", ""), "\\s+", " ").Trim();
}

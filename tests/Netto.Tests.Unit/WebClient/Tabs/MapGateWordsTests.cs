using System.Net;
using System.Text.RegularExpressions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// One line of a gate's box, or the pay's pill, as a viewer reads it: its words with its amount among them,
// what reaches the viewer the map's one gold, every other amount in the words' ink.
public class MapGateWordsTests
{
    [Fact]
    public async Task Render_whatReachesYou_drawsItsAmountInGold()
    {
        var html = await Render(new MapGateLine(MapGateText.ToYou, 981.32m, ToYou: true));

        html.ShouldBe("<span class=\"map-gate-words\"><span class=\"map-gate-words__amount map-gate-words__amount--to-you\">€981.32</span> to you</span>");
    }

    [Fact]
    public async Task Render_anotherAmount_drawsItInTheWordsInk()
    {
        var html = await Render(new MapGateLine(MapGateText.YouPay, 600m, ToYou: false));

        html.ShouldBe("<span class=\"map-gate-words\">you pay <span class=\"map-gate-words__amount\">€600.00</span></span>");
    }

    // A figure below nothing reads as the card reads it.
    [Fact]
    public async Task Render_anAmountBelowNothing_drawsItsSign()
    {
        var html = await Render(new MapGateLine(MapGateText.ToYou, -109m, ToYou: true));

        html.ShouldContain(">−€109.00</span> to you");
    }

    [Fact]
    public async Task Render_wordsWithoutAnAmount_drawsTheWordsAlone()
    {
        var html = await Render(new MapGateLine(MapGateText.Untaxed, null, ToYou: false));

        html.ShouldBe("<span class=\"map-gate-words\">untaxed</span>");
    }

    [Fact]
    public async Task Render_thePill_saysThePayAndItsAmount()
    {
        var html = await Render(new MapGateLine(MapGateText.Pay(withContributions: false), 50_000m, ToYou: false));

        CardHtml.TextInSight(html).ShouldBe("Pay · €50,000.00");
    }

    // The map's one gold is what reaches the viewer, through the theme's token; the words are muted.
    [Fact]
    public void MapGateWordsCss_drawsWhatReachesYouInGoldAndTheWordsMuted()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapGateWords.razor.css");

        css.ShouldSatisfyAllConditions(
            () => Stylesheet.Block(css, ".map-gate-words").ShouldContain("color: var(--netto-text-muted);"),
            () => Stylesheet.Block(css, ".map-gate-words__amount--to-you").ShouldContain("color: var(--netto-gold);"));
    }

    // The line drawn, decoded to the text a viewer reads, without the attribute its scoped stylesheet adds.
    private static async Task<string> Render(MapGateLine line) =>
        Regex.Replace(WebUtility.HtmlDecode(await RiverRendering.RenderAsync<MapGateWords>(new Folds(new BrowserStorage()), new() { ["Line"] = line })), @"\s+b-[a-z0-9]+(="")?", "").Trim();
}

using System.Text.RegularExpressions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab as a viewer meets it: every module of the plan a button in the river's order, the bar's four
// buckets, a line for each link of the run with its words and amounts beside it, each line and word saying
// its kind; a module chosen marked, its links brought out and listed in words; a refused amount leaving no
// line and no amount, the module holding it marked. What a click does is the browser's.
public class MapTabTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    [Fact]
    public async Task Tab_everyModule_isAButtonNamedByItsCardsTitleInTheRiversOrder()
    {
        var html = await RenderTabAsync(Full);

        Buttons(html).Select(button => (button.Module, button.Name, button.Pressed)).ShouldBe(
            MapView.ModulesOf(Full).Select(module => (MapModules.Name(module), CardTitle(module), "false")));
    }

    // The buckets are not modules: nothing to choose, named as the bar names them.
    [Fact]
    public async Task Tab_buckets_areTheBarsFourByTheBarsNames()
    {
        var html = await RenderTabAsync(Full);

        Regex.Matches(html, "<div class=\"map-bucket[^\"]*\" data-bucket=\"([a-z_]+)\"[^>]*>([^<]*)</div>").Select(match => (match.Groups[1].Value, match.Groups[2].Value.Trim()))
            .ShouldBe([
                ("pocket", "Your pocket"), ("taxes_on_pay", "Contributions and tax on pay and rent"), ("taxes_on_profit", "Taxes on profit"),
                ("kept_in_company", "Kept in company")]);
    }

    // One line for each route, each saying its kind, the kinds told apart by their stroke.
    [Fact]
    public async Task Tab_everyLine_saysItsKindAndTakesItsStroke()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full);

        Lines(html).ShouldBe(view.Drawn(null).Select(line => (line.Bundle.Kind.ToString(), MapLinkStyle.Dash(line.Bundle.Kind), MapLinkStyle.Width(line.Bundle.Kind))));
    }

    // The words beside the lines: every amount drawn is a link's, and every link's is drawn.
    [Fact]
    public async Task Tab_wordsBesideTheLines_drawEveryLinksAmountAndNoOther()
    {
        var links = MapLinks.Of(Full, Full.Run());

        var html = await RenderTabAsync(Full);

        Regex.Matches(html, "<span class=\"map-words__amount\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)
            .ShouldBe(links.Select(link => Formats.Euros(link.Amount!.Value)), ignoreOrder: true);
    }

    [Fact]
    public async Task Tab_wordsBesideTheLines_sayTheirKind()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full);

        Regex.Matches(html, "<div class=\"map-words__bundle[^\"]*\" data-link-kind=\"(\\w+)\"").Select(match => match.Groups[1].Value)
            .ShouldBe(view.Bundles().Select(bundle => bundle.Kind.ToString()), ignoreOrder: true);
    }

    // Nothing chosen: no button pressed, no line faded, and the list says how to choose.
    [Fact]
    public async Task Tab_nothingChosen_fadesNothingAndSaysHowToChoose()
    {
        var html = await RenderTabAsync(Full);

        (html.Contains("map-line--faded"), Regex.Count(html, "<li class=\"map-links__link\""), Heading(html), html.Contains("Choose a module on the map"))
            .ShouldBe((false, 0, "A module's links", true));
    }

    // A module chosen is pressed, its lines stay and the others fade, and its links are listed in words,
    // what it carries, its amount and where from and to, each saying its kind.
    [Fact]
    public async Task Tab_aModuleChosen_marksItBringsOutItsLinesAndListsThemInWords()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full, RiverModule.Car);

        var links = view.LinksOf(RiverModule.Car).ToList();
        html.ShouldSatisfyAllConditions(
            () => Buttons(html).Where(button => button.Pressed == "true").Select(button => button.Module).ShouldBe(["car"]),
            () => Regex.Count(html, "<polyline class=\"map-line[^\"]*map-line--faded").ShouldBe(view.Bundles().Count(bundle => !bundle.Touches(RiverModule.Car))),
            () => Heading(html).ShouldBe("Links of Company car"),
            () => Words(html).Select(word => word.Kind).ShouldBe(links.Select(link => link.Kind.ToString())),
            () => Words(html).Select(word => word.Text).ShouldContain($"The car's yearly costs: {Formats.Euros(Full.Run().Year!.Car.Costs)}, from Company car to out of the river"));
    }

    // What comes back is listed from where it comes.
    [Fact]
    public async Task Tab_aModuleChosen_listsWhatComesBackFromItsBucket()
    {
        var html = await RenderTabAsync(Full, RiverModule.Car);

        Words(html).Select(word => word.Text).ShouldContain(text => text.StartsWith("Your contribution", StringComparison.Ordinal) && text.EndsWith(", from Your pocket to Company car", StringComparison.Ordinal));
    }

    // A module chosen that has left the map is none.
    [Fact]
    public async Task Tab_aModuleChosenOffTheMap_choosesNone()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverModule.Car);

        (Buttons(html).Any(button => button.Pressed == "true"), Heading(html)).ShouldBe((false, "A module's links"));
    }

    // A refused amount leaves the map without a line or an amount, as it leaves the bar; the module
    // holding it is marked in words, and the refusal said above the map.
    [Fact]
    public async Task Tab_aRefusedAmount_drawsNoLineNoAmountAndMarksTheModuleHoldingIt()
    {
        var html = await RenderTabAsync(Full.WithReserve(-1m));

        var map = Regex.Match(html, "<div class=\"map\".*?<div class=\"map-tab__side\"", RegexOptions.Singleline).Value;
        html.ShouldSatisfyAllConditions(
            () => (Regex.Count(map, "<polyline"), map.Contains('€'), Buttons(html).Count).ShouldBe((0, false, MapView.ModulesOf(Full).Count)),
            () => Buttons(html).Where(button => button.Refused).Select(button => (button.Module, button.Name)).ShouldBe([("dividend", "Dividend refused")]),
            () => html.ShouldContain("refused-message"));
    }

    // Nothing on the map is a live region: the bar's is the river's only announcement.
    [Fact]
    public async Task Tab_aModuleChosen_isSaidInNoLiveRegion()
    {
        var html = await RenderTabAsync(Full, RiverModule.Car);

        Regex.IsMatch(Regex.Match(html, "<div class=\"map-tab\".*", RegexOptions.Singleline).Value, "aria-live|role=\"(status|alert)\"").ShouldBeFalse();
    }

    // Room under the head for the filter of the kinds of link, and beside the map for the chosen module's
    // card, after the words of its links.
    [Fact]
    public async Task Tab_leavesRoomForTheFilterAndTheCard()
    {
        var html = await RenderTabAsync(Full);

        var (head, filter, map, words, panel) = (html.IndexOf("</header>", StringComparison.Ordinal), html.IndexOf("map-tab__filter", StringComparison.Ordinal),
            html.IndexOf("class=\"map\"", StringComparison.Ordinal), html.IndexOf("map-links", StringComparison.Ordinal), html.IndexOf("map-tab__panel", StringComparison.Ordinal));
        (head < filter, filter < map, map < words, words < panel).ShouldBe((true, true, true, true));
    }

    // Company year skipped: the map starts at Director pay, over the buckets.
    [Fact]
    public async Task Tab_companySkipped_drawsDirectorPayAlone()
    {
        var html = await RenderTabAsync(Full.SkipCompany());

        Buttons(html).Select(button => button.Module).ShouldBe(["director-pay"]);
    }

    // Each button: the module it is, its name as a screen reader hears it, its pressed state, and whether
    // it is marked refused.
    private static List<(string Module, string Name, string Pressed, bool Refused)> Buttons(string html) =>
    [
        .. Regex.Matches(html, "<button type=\"button\" class=\"(map-module[^\"]*)\"[^>]*aria-pressed=\"(\\w+)\"[^>]*data-module=\"([a-z-]+)\"[^>]*>(.*?)</button>", RegexOptions.Singleline)
            .Select(match => (match.Groups[3].Value, Regex.Replace(Regex.Replace(match.Groups[4].Value, "<[^>]*>", " "), "\\s+", " ").Trim(), match.Groups[2].Value,
                match.Groups[1].Value.Contains("map-module--refused", StringComparison.Ordinal))),
    ];

    // Each line: its kind, its dash and its width.
    private static List<(string Kind, string? Dash, string Width)> Lines(string html) =>
    [
        .. Regex.Matches(html, "<polyline [^>]*>").Select(match => (
            Regex.Match(match.Value, "data-link-kind=\"(\\w+)\"").Groups[1].Value,
            Regex.Match(match.Value, "stroke-dasharray=\"([^\"]*)\"") is { Success: true } dash ? dash.Groups[1].Value : null,
            Regex.Match(match.Value, "stroke-width=\"([^\"]*)\"").Groups[1].Value)),
    ];

    private static List<(string Kind, string Text)> Words(string html) =>
    [
        .. Regex.Matches(html, "<li class=\"map-links__link\" data-link-kind=\"(\\w+)\"[^>]*>(.*?)</li>", RegexOptions.Singleline)
            .Select(match => (match.Groups[1].Value, Regex.Replace(match.Groups[2].Value, "\\s+", " ").Trim())),
    ];

    private static string Heading(string html) =>
        Regex.Match(html, "<h2 [^>]*class=\"map-links__title\"[^>]*>(.*?)</h2>", RegexOptions.Singleline).Groups[1].Value.Trim();

    private static string CardTitle(RiverModule module) => ResxText.For(MapTabTextTests.Card(module)).Get("Title")!;
}

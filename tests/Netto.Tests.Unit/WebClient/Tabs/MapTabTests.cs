using System.Text.RegularExpressions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab as a viewer meets it: every module of the plan a button in the river's order, the bar's four
// buckets, one line for each route of the run's links, drawn over a halo of the ground, with one amount
// beside it, a condition's with its word or two, each line and amount saying its kind; a module chosen
// marked, its lines brought out with each card line's words and amount, the others' words and amounts gone,
// and its links listed in words; a refused amount leaving no line and no amount, the module holding it
// marked. What a click does is the browser's.
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

    // Each line and its arrowhead are drawn in their kind's ink, the style's own (MapLinkStyle.Ink), or in
    // the gold of what reaches the pocket: no stylesheet keeps an ink of its own for them.
    [Fact]
    public async Task Tab_everyLineAndItsArrowhead_takeTheirKindsInkOrThePocketsGold()
    {
        var inks = MapView.From(Full, Full.Run()).Drawn(null)
            .Select(line => (line.Bundle.Kind.ToString(), line.Bundle.To == MapEnd.InBucket("pocket") ? MapLinkStyle.PocketInk : MapLinkStyle.Ink(line.Bundle.Kind))).ToList();

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => inks.Select(ink => ink.Item2).ShouldContain(MapLinkStyle.PocketInk),
            () => Regex.Matches(html, "<polyline class=\"map-line[^>]*>").Select(match => (Attribute(match.Value, "data-link-kind"), Attribute(match.Value, "style")))
                .ShouldBe(inks.Select(ink => ((string?)ink.Item1, (string?)$"stroke: {ink.Item2}"))),
            () => Regex.Matches(html, "<span class=\"map-arrow [^>]*>").Select(match => Regex.Match(Attribute(match.Value, "style") ?? "", "--map-arrow: ([^;]*)$").Groups[1].Value)
                .ShouldBe(inks.Select(ink => ink.Item2)));
    }

    // Nothing chosen: one amount beside each line of money or of a tax base, the sum of the card's lines it
    // holds, and no words; a condition's line says in a word or two whether it holds.
    [Fact]
    public async Task Tab_nothingChosen_drawsOneAmountPerLineAndNoWords()
    {
        var view = MapView.From(Full, Full.Run());

        var map = Map(await RenderTabAsync(Full));

        map.ShouldSatisfyAllConditions(
            () => view.Bundles().ShouldContain(bundle => bundle.Links.Count > 1),
            () => Amounts(map).ShouldBe(view.Bundles().Where(bundle => bundle.Amount is not null).Select(bundle => Formats.Euros(bundle.Amount!.Value)), ignoreOrder: true),
            () => map.ShouldNotContain("map-words__line"),
            () => States(map).ShouldBe(view.Links.Where(link => link.Kind == MapLinkKind.Condition).Select(State), ignoreOrder: true));
    }

    // What the line to the pocket carries is drawn in its gold, the map's one; no other amount is.
    [Fact]
    public async Task Tab_nothingChosen_drawsTheAmountsIntoThePocketInGold()
    {
        var view = MapView.From(Full, Full.Run());

        var map = Map(await RenderTabAsync(Full));

        Regex.Matches(map, "<span class=\"map-words__amount map-words__amount--pocket\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)
            .ShouldBe(view.Bundles().Where(bundle => bundle.To == MapEnd.InBucket("pocket")).Select(bundle => Formats.Euros(bundle.Amount!.Value)), ignoreOrder: true);
    }

    // Nothing chosen, the map holds no sentence, only amounts and a condition's word or two, so nothing runs
    // past its edge: a condition's sentence is said where the chosen module's links are listed.
    [Fact]
    public async Task Tab_nothingChosen_drawsNoSentenceOnTheMap()
    {
        var states = ResxText.For<MapTab>().Keys.Where(key => key.StartsWith("State_", StringComparison.Ordinal)).Select(key => ResxText.For<MapTab>().Get(key)).ToHashSet();

        var map = Map(await RenderTabAsync(MapViewConditionsTests.CostlyCar));

        Bundles(map).Where(text => !Regex.IsMatch(text, "^−?€[0-9,]+\\.[0-9]{2}$") && !states.Contains(text)).ShouldBeEmpty();
    }

    // A module chosen: only its own lines have words on the map, each of its card's lines what it carries
    // and its amount, as its card says it, a condition still its word or two; no other line has a word or
    // an amount.
    [Fact]
    public async Task Tab_aModuleChosen_wordsOnlyItsOwnLinesEachCardLineWithItsAmount()
    {
        var view = MapView.From(Full, Full.Run());
        var own = view.Bundles().Where(bundle => bundle.Touches(RiverModule.Car)).ToList();

        var map = Map(await RenderTabAsync(Full, RiverModule.Car));

        map.ShouldSatisfyAllConditions(
            () => Regex.Matches(map, "<div class=\"map-words__bundle\" data-link-kind=\"(\\w+)\"").Select(match => match.Groups[1].Value)
                .ShouldBe(own.Select(bundle => bundle.Kind.ToString()), ignoreOrder: true),
            () => Amounts(map).ShouldBe(own.Where(bundle => bundle.Amount is not null).SelectMany(bundle => bundle.Links).Select(link => Formats.Euros(link.Amount!.Value)), ignoreOrder: true),
            () => WordLines(map).Count.ShouldBe(own.Where(bundle => bundle.Amount is not null).Sum(bundle => bundle.Links.Count)),
            () => WordLines(map).ShouldContain($"The car's yearly costs {Formats.Euros(Full.Run().Year!.Car.Costs)}"),
            () => States(map).ShouldBe(own.SelectMany(bundle => bundle.Links).Where(link => link.Kind == MapLinkKind.Condition).Select(State), ignoreOrder: true));
    }

    // Every line is drawn over a halo of the map's ground, right before it, on its points and wider by the
    // gap each side, so a line drawn over another leaves a gap on each side of it; a faded line's halo fades
    // with it.
    [Fact]
    public async Task Tab_everyLine_isDrawnOverAHaloOfTheGround()
    {
        var drawn = MapView.From(Full, Full.Run()).Drawn(RiverModule.Car);

        var html = await RenderTabAsync(Full, RiverModule.Car);

        Regex.Matches(html, "<polyline [^>]*>").Select(match => match.Value).Chunk(2)
            .Select(pair => (Attribute(pair[0], "class"), Attribute(pair[0], "points") == Attribute(pair[^1], "points"), Attribute(pair[0], "stroke-width"),
                Attribute(pair[0], "stroke-dasharray"), Attribute(pair[^1], "data-link-kind")))
            .ShouldBe(drawn.Select(line => ((string?)(line.Faded ? "map-halo map-halo--faded" : "map-halo"), true, (string?)MapLinkStyle.Halo(line.Bundle.Kind), (string?)null,
                (string?)line.Bundle.Kind.ToString())));
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

    // While an amount is refused there is no figure to list: the list says so, never that no money moves
    // through the module chosen.
    [Fact]
    public async Task Tab_aModuleChosenWhileAnAmountIsRefused_saysThereIsNoFigure()
    {
        var html = await RenderTabAsync(Full.WithReserve(-1m), RiverModule.Car);

        (Heading(html), Note(html)).ShouldBe(("Links of Company car", "No figures while an amount is refused."));
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

    // Company year skipped: it stays on the map, drawn dashed and said skipped in words, so that it can be
    // chosen to enter its year again; the cards that only wait for it are not there, and Director pay is.
    [Fact]
    public async Task Tab_companySkipped_drawsCompanyYearSkippedAndDirectorPay()
    {
        var html = await RenderTabAsync(Full.SkipCompany());

        Buttons(html).Select(button => (button.Module, button.Name, button.Skipped)).ShouldBe(
            [("company-year", "Company year skipped", true), ("director-pay", "Director pay", false)]);
    }

    // A skipped Company year chosen: its card goes beside the map, its box is marked chosen, and the list of
    // its links says no money moves through it.
    [Fact]
    public async Task Tab_companySkippedAndCompanyYearChosen_listsNoLinkAndHasItsSkippedCard()
    {
        var html = await RenderTabAsync(Full.SkipCompany(), RiverModule.CompanyYear);

        (Heading(html), Note(html), Words(html).Count, html.Contains("module-card--skipped"), Buttons(html).Single(button => button.Pressed == "true").Module)
            .ShouldBe(("Links of Company year", "No money moves through it in this plan.", 0, true, "company-year"));
    }

    // Entered, Company year is a box like the others.
    [Fact]
    public async Task Tab_companyEntered_drawsNoModuleSkipped()
    {
        var html = await RenderTabAsync(Full);

        Buttons(html).Any(button => button.Skipped).ShouldBeFalse();
    }

    // Each button: the module it is, its name as a screen reader hears it, its pressed state, and whether
    // it is marked refused.
    internal static List<(string Module, string Name, string Pressed, bool Refused, bool Skipped)> Buttons(string html) =>
    [
        .. Regex.Matches(html, "<button type=\"button\" class=\"(map-module[^\"]*)\"[^>]*aria-pressed=\"(\\w+)\"[^>]*data-module=\"([a-z-]+)\"[^>]*>(.*?)</button>", RegexOptions.Singleline)
            .Select(match => (match.Groups[3].Value, Regex.Replace(Regex.Replace(match.Groups[4].Value, "<[^>]*>", " "), "\\s+", " ").Trim(), match.Groups[2].Value,
                match.Groups[1].Value.Contains("map-module--refused", StringComparison.Ordinal), match.Groups[1].Value.Contains("map-module--skipped", StringComparison.Ordinal))),
    ];

    // What the map draws, the list beside it left out.
    internal static string Map(string html) => Regex.Match(html, "<div class=\"map\".*?<div class=\"map-tab__side\"", RegexOptions.Singleline).Value;

    // The amounts drawn beside the lines, and the words of the conditions' lines.
    internal static List<string> Amounts(string map) =>
        [.. Regex.Matches(map, "<span class=\"map-words__amount[^\"]*\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)];

    private static List<string> States(string map) =>
        [.. Regex.Matches(map, "<span class=\"map-words__state\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value.Trim())];

    // What each line of the map says beside it, as a viewer reads it.
    private static List<string> Bundles(string map) =>
        [.. Regex.Matches(map, "<div class=\"map-words__bundle\"[^>]*>(.*?)</div>", RegexOptions.Singleline).Select(match => Text(match.Groups[1].Value))];

    // Each card line's words and amount beside a line of the module chosen.
    internal static List<string> WordLines(string map) =>
        [.. Regex.Matches(map, "<span class=\"map-words__line\"[^>]*>(.*?)</span></span>", RegexOptions.Singleline).Select(match => Text(match.Groups[1].Value))];

    private static string Text(string html) => Regex.Replace(Regex.Replace(html, "<[^>]*>", ""), "\\s+", " ").Trim();

    // The word or two a condition says of whether it holds.
    private static string State(MapLink link) => ResxText.For<MapTab>().Get(link.State!.Key)!;

    private static string? Attribute(string element, string name) =>
        Regex.Match(element, $" {name}=\"([^\"]*)\"") is { Success: true } match ? match.Groups[1].Value : null;

    // Each line: its kind, its dash and its width.
    private static List<(string Kind, string? Dash, string Width)> Lines(string html) =>
    [
        .. Regex.Matches(html, "<polyline class=\"map-line[^>]*>").Select(match => (
            Regex.Match(match.Value, "data-link-kind=\"(\\w+)\"").Groups[1].Value,
            Regex.Match(match.Value, "stroke-dasharray=\"([^\"]*)\"") is { Success: true } dash ? dash.Groups[1].Value : null,
            Regex.Match(match.Value, "stroke-width=\"([^\"]*)\"").Groups[1].Value)),
    ];

    internal static List<(string Kind, string Text)> Words(string html) =>
    [
        .. Regex.Matches(html, "<li class=\"map-links__link\" data-link-kind=\"(\\w+)\"[^>]*>(.*?)</li>", RegexOptions.Singleline)
            .Select(match => (match.Groups[1].Value, Regex.Replace(match.Groups[2].Value, "\\s+", " ").Trim())),
    ];

    private static string Note(string html) =>
        Regex.Match(html, "<p class=\"map-links__note\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value.Trim();

    private static string Heading(string html) =>
        Regex.Match(html, "<h2 [^>]*class=\"map-links__title\"[^>]*>(.*?)</h2>", RegexOptions.Singleline).Groups[1].Value.Trim();

    internal static string CardTitle(RiverModule module) => ResxText.For(MapTabTextTests.Card(module)).Get("Title")!;
}

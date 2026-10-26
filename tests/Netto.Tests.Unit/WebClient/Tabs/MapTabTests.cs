using System.Text.RegularExpressions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapNeighbourhoodHtml;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab as a viewer meets it: every module of the plan a button in the river's order, the bar's four
// buckets, one line for each route of the run's links, drawn over a halo of the ground, with its amount on
// it where MapAmounts places it, a condition's word or two instead, each line and amount saying its kind; a
// module chosen marked, its lines brought out with their amounts where they were, the others' amounts gone and
// no words on the map, its neighbourhood beside it (MapNeighbourhoodRenderingTests'); a refused amount leaving
// no line and no amount, the module holding it marked. What a click does is the browser's.
public class MapTabTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // Each module is a button named by its card's title and the words of the badges in its box, which a
    // screen reader hears with it: every module the map draws, each with the badges MapBadges finds for it.
    [Fact]
    public async Task Tab_everyModule_isAButtonNamedByItsCardsTitleAndItsBadgesInTheRiversOrder()
    {
        var badges = MapBadgesTests.Of(Full);

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => badges.ShouldNotBeEmpty(),
            () => Buttons(html).Select(button => (button.Module, button.Name, button.Pressed)).ShouldBe(MapView.ModulesOf(Full).Select(module =>
                (MapModules.Name(module), string.Join(' ', [CardTitle(module), .. badges.GetValueOrDefault(module, []).Select(BadgeWords)]), "false"))));
    }

    // Each module a condition or a tax base reaches holds its badges in its box, inside its button, in the
    // river's order and each module's badges in MapBadges' order, each drawn in its tone: with the costly
    // car the benefits lose Corporate tax its rate.
    [Fact]
    public async Task Tab_aModuleAConditionOrATaxBaseReaches_drawsItsBadgesInItsButton()
    {
        var river = MapViewConditionsTests.CostlyCar;
        var badges = MapBadgesTests.Of(river);

        var html = await RenderTabAsync(river);

        html.ShouldSatisfyAllConditions(
            () => badges.Values.SelectMany(list => list).Select(badge => badge.Tone).ShouldContain(MapBadgeTone.Lost),
            () => ButtonBadges(html).Select(badge => (badge.Module, badge.Tone, badge.Words)).ShouldBe(MapView.ModulesOf(river)
                .SelectMany(module => badges.GetValueOrDefault(module, []).Select(badge => (MapModules.Name(module), MapBadgeStyle.Class(badge.Tone), BadgeWords(badge))))));
    }

    // A rule lost is said in its words, an icon and an outline, never the colour alone: each badge draws
    // its tone's icon (MapBadgeStyle), out of a screen reader's hearing, before its words; what a tax base
    // gains, an amount, none.
    [Fact]
    public async Task Tab_aBadge_drawsItsTonesIconBeforeItsWords()
    {
        var html = await RenderTabAsync(MapViewConditionsTests.CostlyCar);

        var badges = ButtonBadges(html);
        badges.ShouldSatisfyAllConditions(
            () => badges.Select(badge => badge.Tone).ShouldContain("lost"),
            () => badges.Select(badge => badge.Tone).ShouldContain("amount"),
            () => badges.Where(badge => MapBadgeStyle.Icon(Enum.Parse<MapBadgeTone>(badge.Tone, ignoreCase: true)) is { } icon
                    ? !Regex.IsMatch(badge.Icon, "^<svg [^>]*aria-hidden=\"true\"") || !badge.Icon.Contains(icon, StringComparison.Ordinal)
                    : badge.Icon.Length > 0)
                .Select(badge => $"{badge.Module} {badge.Words}").ShouldBeEmpty());
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

    // One line for each route, each saying its kind, the kinds told apart by their dash, each as thick as its
    // route: a line of money as its amount, a line carrying none as its kind.
    [Fact]
    public async Task Tab_everyLine_saysItsKindAndTakesItsStroke()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => view.Drawn(null).Select(line => line.Bundle.Route.Width).Distinct().Count().ShouldBeGreaterThan(Enum.GetValues<MapLinkKind>().Length),
            () => Lines(html).ShouldBe(view.Drawn(null).Select(line => (line.Bundle.Kind.ToString(), MapLinkStyle.Dash(line.Bundle.Kind), MapLinkStyle.Width(line.Bundle.Route.Width)))));
    }

    // Each line and its arrowhead are drawn in their kind's ink, the style's own (MapLinkStyle.Ink), or in
    // the gold of what reaches the pocket: no stylesheet keeps an ink of its own for them.
    [Fact]
    public async Task Tab_everyLineAndItsArrowhead_takeTheirKindsInkOrThePocketsGold()
    {
        var drawn = MapView.From(Full, Full.Run()).Drawn(null);
        var inks = drawn.Select(line => (line.Bundle.Kind.ToString(), Ink: line.Bundle.To == MapEnd.InBucket("pocket") ? MapLinkStyle.PocketInk : MapLinkStyle.Ink(line.Bundle.Kind))).ToList();

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => inks.Select(ink => ink.Ink).ShouldContain(MapLinkStyle.PocketInk),
            () => Regex.Matches(html, "<polyline class=\"map-line[^>]*>").Select(match => (Attribute(match.Value, "data-link-kind"), Attribute(match.Value, "style")))
                .ShouldBe(inks.Select(ink => ((string?)ink.Item1, (string?)$"stroke: {ink.Ink}"))),
            () => Regex.Matches(html, "<span class=\"map-arrow [^>]*>").Select(match => Regex.Match(Attribute(match.Value, "style") ?? "", "--map-arrow: ([^;]*)$").Groups[1].Value)
                .ShouldBe(drawn.Zip(inks).Where(pair => pair.First.Bundle.Arrowhead).Select(pair => pair.Second.Ink)));
    }

    // An arrowhead ends every line that reaches a module or leaves the river, sized by its line, half its
    // base and its length set on it (MapLineWidths); a line into a bucket has none, the gold lines into the
    // pocket among them.
    [Fact]
    public async Task Tab_anArrowhead_endsEachLineIntoAModuleOrOutOfTheRiverSizedByItsLine()
    {
        var drawn = MapView.From(Full, Full.Run()).Drawn(null);

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => drawn.Where(line => !line.Bundle.Arrowhead).ShouldContain(line => line.Bundle.To == MapEnd.InBucket("pocket")),
            () => Regex.Matches(html, "<span class=\"map-arrow [^>]*>")
                .Select(match => Regex.Match(Attribute(match.Value, "style") ?? "", "--map-arrow-half: ([^;]*); --map-arrow-length: ([^;]*);") is { Success: true } size ? (size.Groups[1].Value, size.Groups[2].Value) : default)
                .ShouldBe(drawn.Where(line => line.Bundle.Arrowhead)
                    .Select(line => ($"{MapLinkStyle.Width(MapLineWidths.ArrowHalf(line.Bundle.Route.Width))}px", $"{MapLinkStyle.Width(MapLineWidths.ArrowLength(line.Bundle.Route.Width))}px"))));
    }

    // Every line is drawn on its points as drawn: a line into a bucket on under it to its middle, the bucket
    // drawn over it (MapRoute.Drawn).
    [Fact]
    public async Task Tab_everyLine_isDrawnOnItsPointsUnderTheBucketItMeets()
    {
        var drawn = MapView.From(Full, Full.Run()).Drawn(null);

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => drawn.Any(line => line.Bundle.Route.Drawn[^1] != line.Bundle.Route.End).ShouldBeTrue(),
            () => Regex.Matches(html, "<polyline class=\"map-line[^>]*>").Select(match => Attribute(match.Value, "points"))
                .ShouldBe(drawn.Select(line => (string?)string.Join(' ', line.Bundle.Route.Drawn.Select(point => $"{Number(point.X)},{Number(point.Y)}")))));
    }

    // Nothing chosen: one amount on each line of money or of a tax base, the sum of the card's lines it holds,
    // and no words; a condition's line says in a word or two whether it holds.
    [Fact]
    public async Task Tab_nothingChosen_drawsOneAmountPerLineAndNoWords()
    {
        var view = MapView.From(Full, Full.Run());

        var map = Map(await RenderTabAsync(Full));

        map.ShouldSatisfyAllConditions(
            () => view.Bundles().ShouldContain(bundle => bundle.Links.Count > 1),
            () => Amounts(map).ShouldBe(view.Bundles().Where(bundle => bundle.Amount is not null).Select(bundle => Formats.Euros(bundle.Amount!.Value)), ignoreOrder: true),
            () => map.ShouldNotContain("map-words"),
            () => States(map).ShouldBe(view.Links.Where(link => link.Kind == MapLinkKind.Condition).Select(State), ignoreOrder: true));
    }

    // Each amount is drawn where MapAmounts places it: at its point of its line, in percent of the map, its box
    // as wide as its text and its ground and moved from that point by its pixels, so it hugs its line however
    // large the map is drawn; out of a screen reader's hearing, the neighbourhood saying it.
    [Fact]
    public async Task Tab_eachAmount_isDrawnWhereMapAmountsPlacesIt()
    {
        var view = MapView.From(Full, Full.Run());

        var map = Map(await RenderTabAsync(Full));

        Placed(map).ShouldBe(MapAmounts.Of(view, MapAmountsTests.Say).Select(amount => (amount.Bundle.Kind.ToString(), Style(amount), "true", amount.Text)), ignoreOrder: true);
    }

    // What the line to the pocket carries is drawn in its gold, the map's one; no other amount is.
    [Fact]
    public async Task Tab_nothingChosen_drawsTheAmountsIntoThePocketInGold()
    {
        var view = MapView.From(Full, Full.Run());

        var map = Map(await RenderTabAsync(Full));

        Regex.Matches(map, "<span class=\"map-amount map-amount--pocket\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)
            .ShouldBe(view.Bundles().Where(bundle => bundle.To == MapEnd.InBucket("pocket")).Select(bundle => Formats.Euros(bundle.Amount!.Value)), ignoreOrder: true);
    }

    // Nothing chosen, the map holds no sentence, only amounts and a condition's word or two, so nothing runs
    // past its edge: a condition's sentence is said in the neighbourhood of the chosen module.
    [Fact]
    public async Task Tab_nothingChosen_drawsNoSentenceOnTheMap()
    {
        var states = ResxText.For<MapTab>().Keys.Where(key => key.StartsWith("State_", StringComparison.Ordinal)).Select(key => ResxText.For<MapTab>().Get(key)).ToHashSet();

        var map = Map(await RenderTabAsync(MapViewConditionsTests.CostlyCar));

        Bundles(map).Where(text => !Regex.IsMatch(text, "^−?€[0-9,]+\\.[0-9]{2}$") && !states.Contains(text)).ShouldBeEmpty();
    }

    // A module chosen: its own lines keep their amounts where they were, a condition its word or two, and no
    // other line has one; the map has no words, the neighbourhood beside it saying what each line carries.
    [Fact]
    public async Task Tab_aModuleChosen_keepsItsOwnLinesAmountsWhereTheyWereAndNoWords()
    {
        var own = MapView.From(Full, Full.Run()).Bundles().Where(bundle => bundle.Touches(RiverModule.Car)).Select(bundle => bundle.Kind.ToString()).ToList();
        var unchosen = Placed(Map(await RenderTabAsync(Full)));

        var map = Map(await RenderTabAsync(Full, RiverModule.Car));

        map.ShouldSatisfyAllConditions(
            () => Placed(map).Count.ShouldBe(own.Count),
            () => Placed(map).Select(amount => amount.Kind).ShouldBe(own, ignoreOrder: true),
            () => Placed(map).ShouldBeSubsetOf(unchosen),
            () => map.ShouldNotContain("map-words"),
            () => map.ShouldNotContain("The car's yearly costs"));
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
            .ShouldBe(drawn.Select(line => ((string?)(line.Faded ? "map-halo map-halo--faded" : "map-halo"), true, (string?)MapLinkStyle.Halo(line.Bundle.Route.Width), (string?)null,
                (string?)line.Bundle.Kind.ToString())));
    }

    [Fact]
    public async Task Tab_amountsOnTheLines_sayTheirKind()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full);

        Placed(Map(html)).Select(amount => amount.Kind).ShouldBe(view.Bundles().Select(bundle => bundle.Kind.ToString()), ignoreOrder: true);
    }

    // Nothing chosen: no button pressed, no line faded, and the column says how to choose.
    [Fact]
    public async Task Tab_nothingChosen_fadesNothingAndSaysHowToChoose()
    {
        var html = await RenderTabAsync(Full);

        (html.Contains("map-line--faded"), Groups(html).Count, Heading(html), Note(html).StartsWith("Choose a module on the map", StringComparison.Ordinal))
            .ShouldBe((false, 0, "Around a module", true));
    }

    // A module chosen is pressed, its lines stay and the others fade, and its neighbourhood is beside the map,
    // each of its lines with what it carries, its amount and where it goes.
    [Fact]
    public async Task Tab_aModuleChosen_marksItBringsOutItsLinesAndDrawsItsNeighbourhood()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full, RiverModule.Car);

        html.ShouldSatisfyAllConditions(
            () => Buttons(html).Where(button => button.Pressed == "true").Select(button => button.Module).ShouldBe(["car"]),
            () => Regex.Count(html, "<polyline class=\"map-line[^\"]*map-line--faded").ShouldBe(view.Bundles().Count(bundle => !bundle.Touches(RiverModule.Car))),
            () => Heading(html).ShouldBe("Around Company car"),
            () => Rows(html, "goes-out").ShouldContain($"The car's yearly costs {Formats.Euros(Full.Run().Year!.Car.Costs)} to out of the river"));
    }

    // What comes back is listed from where it comes.
    [Fact]
    public async Task Tab_aModuleChosen_listsWhatComesBackFromItsBucket()
    {
        var html = await RenderTabAsync(Full, RiverModule.Car);

        Rows(html, "comes-in").ShouldContain(row => row.StartsWith("Your contribution", StringComparison.Ordinal) && row.EndsWith("from Your pocket", StringComparison.Ordinal));
    }

    // A module chosen that has left the map is none.
    [Fact]
    public async Task Tab_aModuleChosenOffTheMap_choosesNone()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverModule.Car);

        (Buttons(html).Any(button => button.Pressed == "true"), Heading(html)).ShouldBe((false, "Around a module"));
    }

    // A refused amount leaves the map without a line or an amount, as it leaves the bar; the module
    // holding it is marked in words, and the refusal said above the map.
    [Fact]
    public async Task Tab_aRefusedAmount_drawsNoLineNoAmountAndMarksTheModuleHoldingIt()
    {
        var html = await RenderTabAsync(Full.WithReserve(-1m));

        var map = Regex.Match(html, "<div class=\"map\".*?<div class=\"map-tab__side\"", RegexOptions.Singleline).Value;
        html.ShouldSatisfyAllConditions(
            () => (Regex.Count(map, "<polyline"), map.Contains('€'), Regex.Count(map, "map-amount"), map.Contains("map-badge", StringComparison.Ordinal), Buttons(html).Count)
                .ShouldBe((0, false, 0, false, MapView.ModulesOf(Full).Count)),
            () => Buttons(html).Where(button => !button.Refused).Select(button => button.Name).ShouldBe(
                MapView.ModulesOf(Full).Where(module => module != RiverModule.Dividend).Select(CardTitle)),
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
    // card, after its neighbourhood.
    [Fact]
    public async Task Tab_leavesRoomForTheFilterAndTheCard()
    {
        var html = await RenderTabAsync(Full);

        var (head, filter, map, neighbourhood, panel) = (html.IndexOf("</header>", StringComparison.Ordinal), html.IndexOf("map-tab__filter", StringComparison.Ordinal),
            html.IndexOf("class=\"map\"", StringComparison.Ordinal), html.IndexOf("class=\"map-hood\"", StringComparison.Ordinal), html.IndexOf("map-tab__panel", StringComparison.Ordinal));
        (head < filter, filter < map, map < neighbourhood, neighbourhood < panel).ShouldBe((true, true, true, true));
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

    // A skipped Company year chosen: its card goes beside the map, its box is marked chosen, and its
    // neighbourhood says no money moves through it.
    [Fact]
    public async Task Tab_companySkippedAndCompanyYearChosen_listsNoLinkAndHasItsSkippedCard()
    {
        var html = await RenderTabAsync(Full.SkipCompany(), RiverModule.CompanyYear);

        (Heading(html), Note(html), Rows(html).Count, html.Contains("module-card--skipped"), Buttons(html).Single(button => button.Pressed == "true").Module)
            .ShouldBe(("Around Company year", "No money moves through it in this plan.", 0, true, "company-year"));
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

    // Each badge drawn in a module's button: the module, its tone, the icon before its words, and its words.
    private static List<(string Module, string Tone, string Icon, string Words)> ButtonBadges(string html) =>
    [
        .. Regex.Matches(html, "<button type=\"button\" class=\"map-module[^\"]*\"[^>]*data-module=\"([a-z-]+)\"[^>]*>(.*?)</button>", RegexOptions.Singleline)
            .SelectMany(button => Regex.Matches(button.Groups[2].Value, "<span class=\"map-badge map-badge--([a-z]+)\"[^>]*>(.*?)<span class=\"map-badge__words\"[^>]*>([^<]*)</span>\\s*</span>", RegexOptions.Singleline)
                .Select(badge => (button.Groups[1].Value, badge.Groups[1].Value, badge.Groups[2].Value.Trim(), badge.Groups[3].Value.Trim()))),
    ];

    // A badge's words, as the map's texts say them.
    internal static string BadgeWords(MapBadge badge) => ResxText.For<MapTab>().Format(badge.Words.Key, [.. badge.Words.Args]);

    // What the map draws, the column beside it left out.
    internal static string Map(string html) => Regex.Match(html, "<div class=\"map\".*?<div class=\"map-tab__side\"", RegexOptions.Singleline).Value;

    // Each amount drawn on the lines, a condition's words among them: its kind, its style, whether it is out of a
    // screen reader's hearing, and its text.
    internal static List<(string Kind, string Style, string Hidden, string Text)> Placed(string map) =>
    [
        .. Regex.Matches(map, "<span class=\"map-amount[^\"]*\" data-link-kind=\"(\\w+)\" style=\"([^\"]*)\" aria-hidden=\"(\\w+)\"[^>]*>([^<]*)</span>")
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, match.Groups[4].Value)),
    ];

    // The amounts drawn on the lines of money and of the tax bases, and the words of the conditions' lines.
    internal static List<string> Amounts(string map) =>
        [.. Regex.Matches(map, "<span class=\"map-amount(?: map-amount--pocket)?\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)];

    private static List<string> States(string map) =>
        [.. Regex.Matches(map, "<span class=\"map-amount map-amount--state\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value.Trim())];

    // What each line of the map says on it, as a viewer reads it.
    private static List<string> Bundles(string map) => [.. Placed(map).Select(amount => amount.Text)];

    // Where the tab draws an amount: its point in percent to the thousandth, its width and its box's corner
    // from that point in pixels.
    internal static string Style(MapAmount amount) =>
        $"left: {Number(amount.At.X)}%; top: {Number(amount.At.Y)}%; width: {Pixels(amount.Width)}; transform: translate({Pixels(amount.Left)}, {Pixels(amount.Top)})";

    private static string Pixels(decimal value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "px";

    // The word or two a condition says of whether it holds.
    private static string State(MapLink link) => ResxText.For<MapTab>().Get(link.State!.Key)!;

    private static string? Attribute(string element, string name) =>
        Regex.Match(element, $" {name}=\"([^\"]*)\"") is { Success: true } match ? match.Groups[1].Value : null;

    // A coordinate of the map as the tab writes it, in percent to the thousandth.
    private static string Number(decimal value) => Math.Round(value, 3).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    // Each line: its kind, its dash and its width.
    private static List<(string Kind, string? Dash, string Width)> Lines(string html) =>
    [
        .. Regex.Matches(html, "<polyline class=\"map-line[^>]*>").Select(match => (
            Regex.Match(match.Value, "data-link-kind=\"(\\w+)\"").Groups[1].Value,
            Regex.Match(match.Value, "stroke-dasharray=\"([^\"]*)\"") is { Success: true } dash ? dash.Groups[1].Value : null,
            Regex.Match(match.Value, "stroke-width=\"([^\"]*)\"").Groups[1].Value)),
    ];

    internal static string CardTitle(RiverModule module) => ResxText.For(MapTabTextTests.Card(module)).Get("Title")!;
}

using System.Text.RegularExpressions;
using Netto.Tests.WebClient.River;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapReceivedHtml;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab as a viewer meets it: every module of the plan a button in the river's order, the bar's five
// buckets, one line for each route of the run's links, drawn over a halo of the ground, with its amount on
// it where MapAmounts places it, a condition's word or two instead, each line and amount saying its kind; a
// module chosen marked, its lines brought out with their amounts where they were, the others' amounts gone and
// no words on the map, the line of what it receives beside it, over its card; a refused amount leaving
// no line and no amount, the module holding it marked. What a click does is the browser's.
public class MapTabTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // What the tab draws, in the order it draws it: its head, the map's tools and the key among them, the
    // map, the column beside it, the line of what the chosen module receives and its card.
    private static readonly string[] ToolsAndCard =
        ["</header>", "class=\"map-tab__tools\"", "map-tab__key", "class=\"map\"", "class=\"map-tab__side\"", "class=\"map-tab__receives\"", "map-tab__panel"];

    // Each module is a button named by its card's title, the lines its gate says and the words of the badges in
    // its box, which a screen reader hears with it: every module the map draws, each gate with the lines MapGates
    // finds for it, each box with the badges MapBadges finds for it.
    [Fact]
    public async Task Tab_everyModule_isAButtonNamedByItsCardsTitleItsGatesLinesAndItsBadgesInTheRiversOrder()
    {
        var badges = MapBadgesTests.Of(Full);
        var gates = MapGates.Of(Full, Full.Run());

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => badges.ShouldNotBeEmpty(),
            () => Buttons(html).Select(button => (button.Module, button.Name, button.Pressed)).ShouldBe(MapView.ModulesOf(Full).Select(module =>
                (MapModules.Name(module), string.Join(' ', [CardTitle(module), .. gates.For(module).Select(GateWords), .. badges.GetValueOrDefault(module, []).Select(BadgeWords)]),
                    "false"))));
    }

    // A way across is a gate: its box dashed, holding a line for each line MapGates says of it, in its order;
    // every other module a box of its own.
    [Fact]
    public async Task Tab_aWayAcross_isAGateHoldingTheLinesItSays()
    {
        var gates = MapGates.Of(Full, Full.Run());

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => Regex.Matches(html, "<button type=\"button\" class=\"map-module map-module--gate[^\"]*\"[^>]*data-module=\"([a-z-]+)\"").Select(match => match.Groups[1].Value)
                .ShouldBe(["vouchers", "allowances", "rent", "car", "copyright", "dividend"]),
            () => GateLines(html).ShouldBe(gates.Gates.SelectMany(gate => gate.Lines.Select(line => (MapModules.Name(gate.Module), GateWords(line))))));
    }

    // The map's three columns, framed and labelled over their top by whose money they hold, left to right, out of
    // a screen reader's hearing, the buttons in the river's order saying the modules: each frame drawn in its slot.
    [Fact]
    public async Task Tab_theColumns_areLabelledByWhoseMoneyTheyHold()
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English);

        var html = await RenderTabAsync(Full);

        Regex.Matches(html, "<div class=\"map-column map-column--([a-z]+)\" style=\"([^\"]*)\" aria-hidden=\"true\"[^>]*><span class=\"map-column__label\"[^>]*>([^<]*)</span></div>")
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value))
            .ShouldBe(view.Columns.Select(column => (column.Owner.ToString().ToLowerInvariant(), Box(column.Slot),
                ResxText.For<MapTab>().Get(MapTabText.Column(column.Owner))!)));
    }

    // The pay crosses into Director pay under its pill, in the gates' column, saying what crosses, out of a screen
    // reader's hearing, the cards saying it; its line says no amount of its own.
    [Fact]
    public async Task Tab_thePay_crossesUnderItsPill()
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English);
        var pill = view.Pill.ShouldNotBeNull();

        var map = Map(await RenderTabAsync(Full));

        map.ShouldSatisfyAllConditions(
            () => Regex.Matches(map, "<div class=\"map-pill\" style=\"([^\"]*)\" aria-hidden=\"true\"[^>]*>(.*?)</div>", RegexOptions.Singleline)
                .Select(match => (match.Groups[1].Value, Regex.Replace(Regex.Replace(match.Groups[2].Value, "<[^>]*>", ""), "\\s+", " ").Trim()))
                .ShouldBe([(Box(pill.Slot), GateWords(pill.Words))]),
            () => Amounts(map).ShouldNotContain(Formats.Euros(pill.Words.Amount!.Value)));
    }

    // Each column's label fits over the top row's boxes, in the room the row keeps over them on the narrowest map,
    // at the size the stylesheet draws it.
    [Fact]
    public void MapTabCss_drawsTheColumnsLabelsInTheRoomOverTheTopRow()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css");

        css.ShouldSatisfyAllConditions(
            () => Stylesheet.Block(css, ".map-column__label").ShouldContain($"top: {Stylesheet.Rem(MapLayout.LabelTopRem)};"),
            () => Stylesheet.Block(css, ".map-column__label").ShouldContain($"font-size: {Stylesheet.Rem(MapLayout.LabelRem)};"),
            () => Stylesheet.Block(css, ".map-column__label").ShouldContain("line-height: 1;"),
            () => (MapLayout.LabelTopRem + MapLayout.LabelRem).ShouldBeLessThan((1m - 0.5m) / 2m * MapLayout.SmallestRowRem));
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
            () => badges.Where(badge => MapBadgeStyle.Icon(Enum.GetValues<MapBadgeTone>().Single(tone => MapBadgeStyle.Class(tone) == badge.Tone)) is { } icon
                    ? !Regex.IsMatch(badge.Icon, "^<svg [^>]*aria-hidden=\"true\"") || !badge.Icon.Contains(icon, StringComparison.Ordinal)
                    : badge.Icon.Length > 0)
                .Select(badge => $"{badge.Module} {badge.Words}").ShouldBeEmpty());
    }

    // What a tax base gains shows as its amount alone in the box it marks, so it reads whole on the narrowest map;
    // its words are heard with it, out of sight, so the button's name keeps them whole. A condition's badge is
    // seen whole.
    [Fact]
    public async Task Tab_aTaxBasesBadge_showsItsAmountAndLeavesItsWordsHeard()
    {
        var html = await RenderTabAsync(MapViewConditionsTests.CostlyCar);

        var badges = ButtonBadges(html);
        badges.ShouldSatisfyAllConditions(
            () => badges.Where(badge => badge.Tone == "amount").Select(badge => (badge.Module, badge.Seen.StartsWith('+'), badge.Seen.Contains(' ', StringComparison.Ordinal), badge.Heard))
                .ShouldBe([("director-pay", true, false, " to tax base"), ("corporate-tax", true, false, " to tax base")]),
            () => badges.Where(badge => badge.Tone != "amount").ShouldAllBe(badge => badge.Heard.Length == 0 && badge.Seen.Length > 0));
    }

    // What a badge leaves heard is out of sight, as a quiet name's state is.
    [Fact]
    public void MapTabCss_hidesWhatABadgeLeavesHeard()
    {
        var heard = Stylesheet.Block(Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css"), ".map-badge__heard");

        heard.ShouldSatisfyAllConditions(
            () => heard.ShouldContain("position: absolute;"),
            () => heard.ShouldContain("clip-path: inset(50%);"),
            () => heard.ShouldContain("overflow: hidden;"));
    }

    // The buckets are not modules: nothing to choose, named as the bar names them.
    [Fact]
    public async Task Tab_buckets_areTheBarsFiveByTheBarsNames()
    {
        var html = await RenderTabAsync(Full);

        Regex.Matches(html, "<div class=\"map-bucket[^\"]*\" data-bucket=\"([a-z_]+)\"[^>]*>([^<]*)</div>").Select(match => (match.Groups[1].Value, match.Groups[2].Value.Trim()))
            .ShouldBe([
                ("pocket", "Your pocket"), ("taxes_on_pay", "Contributions and tax on pay and rent"), ("taxes_on_profit", "Taxes on profit"),
                ("kept_in_company", "Kept in company"), ("expenses", "Expenses")]);
    }

    // One line for each route, each saying its kind, the kinds told apart by their dash, each as thick as its
    // route: a line of money as its amount, a line carrying none as its kind.
    [Fact]
    public async Task Tab_everyLine_saysItsKindAndTakesItsStroke()
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English).ShowingFor(null);

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
        var drawn = MapView.From(Full, Full.Run(), MapNames.English).ShowingFor(null).Drawn(null);
        var inks = drawn.Select(line => (line.Bundle.Kind.ToString(), Ink: line.Bundle.To.InPocket ? MapLinkStyle.PocketInk : MapLinkStyle.Ink(line.Bundle.Kind))).ToList();

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => inks.Select(ink => ink.Ink).ShouldContain(MapLinkStyle.PocketInk),
            () => Regex.Matches(html, "<polyline class=\"map-line[^>]*>").Select(match => (Attribute(match.Value, "data-link-kind"), Attribute(match.Value, "style")))
                .ShouldBe(inks.Select(ink => ((string?)ink.Item1, (string?)$"stroke: {ink.Ink}"))),
            () => Regex.Matches(html, "<span class=\"map-arrow [^>]*>").Select(match => Regex.Match(Attribute(match.Value, "style") ?? "", "--map-arrow: ([^;]*)$").Groups[1].Value)
                .ShouldBe(drawn.Zip(inks).Where(pair => pair.First.Bundle.Arrowhead).Select(pair => pair.Second.Ink)));
    }

    // An arrowhead ends every line that reaches a module, sized by its line, half its
    // base and its length set on it (MapLineWidths); a line into a bucket has none, the gold lines into the
    // pocket among them.
    [Fact]
    public async Task Tab_anArrowhead_endsEachLineIntoAModuleOrOutOfTheRiverSizedByItsLine()
    {
        var drawn = MapView.From(Full, Full.Run(), MapNames.English).ShowingFor(null).Drawn(null);

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
        var drawn = MapView.From(Full, Full.Run(), MapNames.English).ShowingFor(null).Drawn(null);

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => drawn.Any(line => line.Bundle.Route.Drawn[^1] != line.Bundle.Route.End).ShouldBeTrue(),
            () => Regex.Matches(html, "<polyline class=\"map-line[^>]*>").Select(match => Attribute(match.Value, "points"))
                .ShouldBe(drawn.Select(line => (string?)string.Join(' ', line.Bundle.Route.Drawn.Select(point => $"{Number(point.X)},{Number(point.Y)}")))));
    }

    // Nothing chosen: one amount on each line of money that says one, the sum of the card's lines it holds,
    // and no words, no tax base's or condition's line being drawn (MapTabRulesTests).
    [Fact]
    public async Task Tab_nothingChosen_drawsOneAmountPerLineAndNoWords()
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English).ShowingFor(null);

        var map = Map(await RenderTabAsync(Full));

        map.ShouldSatisfyAllConditions(
            () => view.Bundles().ShouldContain(bundle => bundle.Links.Count > 1),
            () => Amounts(map).ShouldBe(view.Bundles().Where(bundle => bundle.Amount is not null && bundle.SaysAmount).Select(bundle => Formats.Euros(bundle.Amount!.Value)), ignoreOrder: true),
            () => map.ShouldNotContain("map-words"),
            () => States(map).ShouldBeEmpty());
    }

    // Each amount is drawn where MapAmounts places it, nothing chosen those of the lines of money: at its point of its line, in percent of the map, its box
    // as wide as its text and its ground and moved from that point by its pixels, so it hugs its line however
    // large the map is drawn; out of a screen reader's hearing, the cards saying their figures.
    [Fact]
    public async Task Tab_eachAmount_isDrawnWhereMapAmountsPlacesIt()
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English);

        var map = Map(await RenderTabAsync(Full));

        Placed(map).ShouldBe(MapAmounts.Of(view, MapAmountsTests.Say).Where(amount => MapLinkStyle.IsMoney(amount.Bundle.Kind))
            .Select(amount => (amount.Bundle.Kind.ToString(), Style(amount), "true", amount.Text)), ignoreOrder: true);
    }

    // What has reached the pocket is drawn in the gold of the pocket line, the map's one, where a stretch of it
    // says it: what has reached you so far; a line joining it says none, its gate's box or the total under it
    // saying it. No other amount is.
    [Fact]
    public async Task Tab_nothingChosen_drawsTheAmountsIntoThePocketInGold()
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English);

        var map = Map(await RenderTabAsync(Full));

        Regex.Matches(map, "<span class=\"map-amount map-amount--pocket\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)
            .ShouldBe(view.Bundles().Where(bundle => bundle.To.InPocket && bundle.SaysAmount).Select(bundle => Formats.Euros(bundle.Amount!.Value)), ignoreOrder: true);
    }

    // Nothing chosen, the map holds no sentence, only amounts and a condition's word or two, so nothing runs
    // past its edge: a condition's sentence is not on the tab (MapConditionsRenderingTests).
    [Fact]
    public async Task Tab_nothingChosen_drawsNoSentenceOnTheMap()
    {
        var states = ResxText.For<MapTab>().Keys.Where(key => key.StartsWith("State_", StringComparison.Ordinal)).Select(key => ResxText.For<MapTab>().Get(key)).ToHashSet();

        var map = Map(await RenderTabAsync(MapViewConditionsTests.CostlyCar));

        Bundles(map).Where(text => !Regex.IsMatch(text, "^−?€[0-9,]+\\.[0-9]{2}$") && !states.Contains(text)).ShouldBeEmpty();
    }

    // A module chosen: its own lines keep their amounts where they were, a condition its word or two, and no
    // other line has one; the map has no words, the card beside it saying what each line carries.
    [Fact]
    public async Task Tab_aModuleChosen_keepsItsOwnLinesAmountsWhereTheyWereAndNoWords()
    {
        var own = MapView.From(Full, Full.Run(), MapNames.English).Bundles().Where(bundle => bundle.Touches(RiverModule.Car) && bundle.SaysAmount).Select(bundle => bundle.Kind.ToString()).ToList();
        var placed = MapAmounts.Of(MapView.From(Full, Full.Run(), MapNames.English), MapAmountsTests.Say).Select(amount => (amount.Bundle.Kind.ToString(), Style(amount), "true", amount.Text)).ToList();

        var map = Map(await RenderTabAsync(Full, RiverModule.Car));

        map.ShouldSatisfyAllConditions(
            () => Placed(map).Count.ShouldBe(own.Count),
            () => Placed(map).Select(amount => amount.Kind).ShouldBe(own, ignoreOrder: true),
            () => Placed(map).ShouldBeSubsetOf(placed),
            () => map.ShouldNotContain("map-words"),
            () => map.ShouldNotContain("The car's yearly costs"));
    }

    // Every line is drawn over a halo of the map's ground, right before it, on its points and wider by the
    // gap each side, so a line drawn over another leaves a gap on each side of it; a faded line's halo fades
    // with it.
    [Fact]
    public async Task Tab_everyLine_isDrawnOverAHaloOfTheGround()
    {
        var drawn = MapView.From(Full, Full.Run(), MapNames.English).ShowingFor(RiverModule.Car).Drawn(RiverModule.Car);

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
        var view = MapView.From(Full, Full.Run(), MapNames.English).ShowingFor(null);

        var html = await RenderTabAsync(Full);

        Placed(Map(html)).Select(amount => amount.Kind).ShouldBe(view.Bundles().Where(bundle => bundle.SaysAmount).Select(bundle => bundle.Kind.ToString()), ignoreOrder: true);
    }

    // Nothing chosen: no button pressed, no line faded, no line of what is received, and the card's room says
    // how to choose.
    [Fact]
    public async Task Tab_nothingChosen_fadesNothingAndSaysHowToChoose()
    {
        var html = await RenderTabAsync(Full);

        (html.Contains("map-line--faded"), Count(html), html.Contains("map-panel__hint", StringComparison.Ordinal)).ShouldBe((false, 0, true));
    }

    // A module chosen is pressed, its lines stay and the others fade, and one line over its card says what
    // it receives and from where, each part the amount the map says on its line: the rent, its share off the
    // company's line, from Company year.
    [Fact]
    public async Task Tab_aModuleChosen_marksItBringsOutItsLinesAndSaysWhatItReceives()
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English).ShowingFor(RiverModule.Rent);
        var received = MapReceivedView.Of(view, RiverModule.Rent, end => end.Id).Lines;

        var html = await RenderTabAsync(Full, RiverModule.Rent);

        html.ShouldSatisfyAllConditions(
            () => Buttons(html).Where(button => button.Pressed == "true").Select(button => button.Module).ShouldBe(["rent"]),
            () => Regex.Count(html, "<polyline class=\"map-line[^\"]*map-line--faded").ShouldBe(view.Bundles().Count(bundle => !bundle.Touches(RiverModule.Rent))),
            () => received.Select(line => line.From).ShouldBe(["company-year"]),
            () => Line(html).ShouldBe($"Receives {received[0].Amount} from {CardTitle(RiverModule.CompanyYear)}"));
    }

    // The module chosen has no list of its lines and no second box of its own beside the map: its card's
    // drain list says where its money goes, and the map's button is its one button.
    [Fact]
    public async Task Tab_aModuleChosen_drawsNoListOfItsLinesAndNoSecondButtonForIt()
    {
        var html = await RenderTabAsync(Full, RiverModule.Split);

        (html.Contains("map-hood", StringComparison.Ordinal), Count(html), Regex.Count(html, "data-module=\"split\"")).ShouldBe((false, 1, 1));
    }

    // An end of the line that is a module on the map is a button choosing it, as its box does; a bucket is
    // named only.
    [Fact]
    public async Task Tab_aModuleChosen_namesTheModulesItReceivesFromAsButtons()
    {
        var (split, rent, dividend) = (await RenderTabAsync(RiverInputs.Example, RiverModule.Split), await RenderTabAsync(Full, RiverModule.Rent),
            await RenderTabAsync(Full, RiverModule.Dividend));

        split.ShouldSatisfyAllConditions(
            () => GoButtons(split).ShouldBe([("company-year", "Company year")]),
            () => GoButtons(rent).ShouldBe([("company-year", "Company year")]),
            () => GoButtons(dividend).ShouldBe([("corporate-tax", CardTitle(RiverModule.CorporateTax))]));
    }

    // Company year receives nothing: no line over its card.
    [Fact]
    public async Task Tab_companyYearChosen_saysNothingReceived()
    {
        var html = await RenderTabAsync(Full, RiverModule.CompanyYear);

        (Count(html), html.Contains("id=\"card-company-year\"", StringComparison.Ordinal)).ShouldBe((0, true));
    }

    // A module chosen that has left the map is none.
    [Fact]
    public async Task Tab_aModuleChosenOffTheMap_choosesNone()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverModule.Car);

        (Buttons(html).Any(button => button.Pressed == "true"), Count(html)).ShouldBe((false, 0));
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

    // Under the head, the map's tools over the map, the filter of the kinds of link among them; beside the
    // map, the line of what the chosen module receives, then its card.
    [Fact]
    public async Task Tab_leavesRoomForTheToolsAndTheCard()
    {
        var html = await RenderTabAsync(Full, RiverModule.Rent);

        var order = ToolsAndCard.Select(marker => html.IndexOf(marker, StringComparison.Ordinal)).ToList();
        (order.All(index => index >= 0), order.SequenceEqual(order.Order())).ShouldBe((true, true));
    }

    // Company year skipped: it stays on the map, drawn dashed and said skipped in words, so that it can be
    // chosen to enter its year again; the cards that only wait for it are not there, and Director pay is, with
    // the partner, whose quotient the pay alone is taxed with.
    [Fact]
    public async Task Tab_companySkipped_drawsCompanyYearSkippedDirectorPayAndThePartner()
    {
        var html = await RenderTabAsync(Full.SkipCompany());

        var quotient = Formats.Euros(MapGates.Of(Full.SkipCompany(), Full.SkipCompany().Run()).For(RiverModule.Household)[1].Amount!.Value);
        Buttons(html).Select(button => (button.Module, button.Name, button.Skipped)).ShouldBe(
            [("company-year", "Company year skipped", true), ("director-pay", "Director pay", false), ("household", $"Household quotient {quotient}", false)]);
    }

    // A skipped Company year chosen: its card goes beside the map, its box is marked chosen, and nothing is
    // said received.
    [Fact]
    public async Task Tab_companySkippedAndCompanyYearChosen_saysNothingReceivedAndHasItsSkippedCard()
    {
        var html = await RenderTabAsync(Full.SkipCompany(), RiverModule.CompanyYear);

        (Count(html), html.Contains("module-card--skipped"), Buttons(html).Single(button => button.Pressed == "true").Module)
            .ShouldBe((0, true, "company-year"));
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

    // Each badge drawn in a module's button: the module, its tone, the icon before its words, and its words, those
    // seen and those only heard, out of sight.
    private static List<(string Module, string Tone, string Icon, string Words, string Seen, string Heard)> ButtonBadges(string html) =>
    [
        .. Regex.Matches(html, "<button type=\"button\" class=\"map-module[^\"]*\"[^>]*data-module=\"([a-z-]+)\"[^>]*>(.*?)</button>", RegexOptions.Singleline)
            .SelectMany(button => Regex.Matches(button.Groups[2].Value,
                    "<span class=\"map-badge map-badge--([a-z-]+)\"[^>]*>(.*?)<span class=\"map-badge__words\"[^>]*>([^<]*)</span>(?:\\s*<span class=\"map-badge__heard\"[^>]*>([^<]*)</span>)?\\s*</span>",
                    RegexOptions.Singleline)
                .Select(badge => (button.Groups[1].Value, badge.Groups[1].Value, badge.Groups[2].Value.Trim(), (badge.Groups[3].Value + badge.Groups[4].Value).Trim(),
                    badge.Groups[3].Value, badge.Groups[4].Value))),
    ];

    // A badge's words, as the map's texts say them.
    internal static string BadgeWords(MapBadge badge) => ResxText.For<MapTab>().Format(badge.Words.Key, [.. badge.Words.Args]);

    // A gate's line, or the pay's pill, in its words, its amount among them as the cards format it.
    internal static string GateWords(MapGateLine line) =>
        ResxText.For<MapGateWords>().Format(line.Key, line.Amount is { } amount ? [Formats.Euros(amount)] : []).Trim();

    // Each line a gate's button draws, by its module, in its words.
    private static List<(string Module, string Words)> GateLines(string html) =>
    [
        .. Regex.Matches(html, "<button type=\"button\" class=\"map-module[^\"]*\"[^>]*data-module=\"([a-z-]+)\"[^>]*>(.*?)</button>", RegexOptions.Singleline)
            .SelectMany(button => Regex.Matches(button.Groups[2].Value, "<span class=\"map-gate-words\"[^>]*>(.*?)</span>(?=\\s*(<span class=\"map-gate-words\"|<span class=\"map-module__|$))",
                    RegexOptions.Singleline)
                .Select(line => (button.Groups[1].Value, Regex.Replace(Regex.Replace(line.Groups[1].Value, "<[^>]*>", ""), "\\s+", " ").Trim()))),
    ];

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

    // A box's place as the tab styles it, in percent of the map.
    private static string Box(MapSlot slot) => $"left: {Number(slot.Left)}%; top: {Number(slot.Top)}%; width: {Number(slot.Width)}%; height: {Number(slot.Height)}%";

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

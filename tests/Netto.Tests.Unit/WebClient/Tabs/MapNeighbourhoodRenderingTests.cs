using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapNeighbourhoodHtml;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The neighbourhood of the module chosen as a viewer meets it on the Map tab, drawn without a browser
// (MapRendering), in the column beside the map above the module's card: what comes into it, what goes out of
// it, the rules it sets or is under, each line on its own with its words, its amount and the other end of it
// (a button where it is a module on the map), whatever the legend ticks. What a click on an end does is
// PlanMapNeighbourhoodTests'; how it looks is the browser's.
public class MapNeighbourhoodRenderingTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // The costly car's benefit is beyond 20% of the pay: a condition that does not hold.
    private static readonly RiverInputs CostlyCar = MapViewConditionsTests.CostlyCar;

    private static readonly RiverInputs ChainQuiet = MapQuietTests.Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);

    private static readonly ResxText Texts = ResxText.For<MapTab>();

    private static readonly ResxText Neighbourhood = ResxText.For<MapNeighbourhood>();

    public static TheoryData<MapLinkKind> Kinds => new(Enum.GetValues<MapLinkKind>());

    // A link as the tab words it: by the texts that word its line on its card.
    private static string Words(MapLabel label) => ResxText.For(MapTabText.Texts(label.Text)).Format(label.Key, [.. label.Args]);

    // An end as the tab names it: a module by its card's title, a bucket by the bar's name for it, the
    // river's edge as such.
    private static string EndName(MapView view, MapEnd end) => end switch
    {
        { Module: { } module } => Texts.Get(MapTabText.Module(module))!,
        { Bucket: { } bucket } => ResxText.For<BucketBar>().Get(BucketBarText.Bucket(bucket, view.TaxesOnPayIncludeRent))!,
        _ => Texts.Get(MapTabText.OutOfRiver)!,
    };

    private static bool IsMoney(MapLink link) => link.Kind is MapLinkKind.PassedOn or MapLinkKind.IntoBucket;

    // What a row says of a line: what it carries, its amount, and where it comes from or goes to.
    private static string Row(MapView view, MapLink link, string group)
    {
        var line = $"{Words(link.Carries)} {Formats.Euros(link.Amount!.Value)}";
        var ends = group switch
        {
            "comes-in" => $"{Neighbourhood.Get("From")} {EndName(view, link.From)}",
            "goes-out" => $"{Neighbourhood.Get("To")} {EndName(view, link.To)}",
            _ => $"{Neighbourhood.Get("From")} {EndName(view, link.From)} {Neighbourhood.Get("To")} {EndName(view, link.To)}",
        };
        var past = link.Through.Count > 0 && group != "passes-through"
            ? $" {Neighbourhood.Format("Past", string.Join(", ", link.Through.Select(module => Texts.Get(MapTabText.Module(module)))))}"
            : "";

        return $"{line} {ends}{past}";
    }

    // The head: the module's name in a heading of the column, then what comes in, the module itself and what
    // goes out, then the rules, each group with its own heading below the section's.
    [Fact]
    public async Task Tab_aModuleChosen_namesItsNeighbourhoodAndDrawsItsGroupsInOrder()
    {
        var html = await RenderTabAsync(Full, RiverModule.Car);

        var section = Section(html);
        html.ShouldSatisfyAllConditions(
            () => Heading(html).ShouldBe("Around Company car"),
            () => Groups(html).ShouldBe(["comes-in", "goes-out", "rules"]),
            () => GroupHeadings(html).ShouldBe(["Comes in", "Goes out", "Rules"]),
            () => (section.IndexOf("data-group=\"comes-in\"", StringComparison.Ordinal) < section.IndexOf("map-hood__centre", StringComparison.Ordinal)).ShouldBeTrue(),
            () => (section.IndexOf("map-hood__centre", StringComparison.Ordinal) < section.IndexOf("data-group=\"goes-out\"", StringComparison.Ordinal)).ShouldBeTrue(),
            () => (section.IndexOf("data-group=\"goes-out\"", StringComparison.Ordinal) < section.IndexOf("data-group=\"rules\"", StringComparison.Ordinal)).ShouldBeTrue(),
            () => Regex.Match(section, "<p class=\"map-hood__centre\"[^>]*>(.*?)</p>", RegexOptions.Singleline).Groups[1].Value.Trim().ShouldBe("Company car"));
    }

    // Each line of money, on its own row: what it carries, its amount, the other end.
    [Theory]
    [InlineData(RiverModule.Car)]
    [InlineData(RiverModule.DirectorPay)]
    [InlineData(RiverModule.Split)]
    [InlineData(RiverModule.Dividend)]
    public async Task Tab_theMoneyOfAModule_isARowForEachLineWithItsWordsItsAmountAndTheOtherEnd(RiverModule module)
    {
        var view = MapView.From(Full, Full.Run());
        var money = view.LinksOf(module).Where(IsMoney).ToList();

        var html = await RenderTabAsync(Full, module);

        html.ShouldSatisfyAllConditions(
            () => Rows(html, "comes-in").ShouldBe(money.Where(link => link.To.Module == module).Select(link => Row(view, link, "comes-in"))),
            () => Rows(html, "goes-out").ShouldBe(money.Where(link => link.From.Module == module).Select(link => Row(view, link, "goes-out"))));
    }

    // The pay's contributions and taxes into one bucket are one line on the map and a row each here.
    [Fact]
    public async Task Tab_directorPay_hasARowForEachCardLineIntoTheTaxesOnPay()
    {
        var view = MapView.From(Full, Full.Run());
        var taxes = view.LinksOf(RiverModule.DirectorPay).Where(link => link is { Kind: MapLinkKind.IntoBucket, To.Bucket: "taxes_on_pay" }).ToList();

        var html = await RenderTabAsync(Full, RiverModule.DirectorPay);

        var rows = Rows(html, "goes-out");
        html.ShouldSatisfyAllConditions(
            () => taxes.Count.ShouldBeGreaterThan(2),
            () => rows.Where(row => row.EndsWith($"{Neighbourhood.Get("To")} {EndName(view, MapEnd.InBucket("taxes_on_pay"))}", StringComparison.Ordinal)).Count().ShouldBe(taxes.Count));
    }

    // An end that is a module on the map is a button that chooses it; a bucket and the river's edge are
    // named and nothing else.
    [Fact]
    public async Task Tab_theOtherEnds_areButtonsForModulesAndNamedOnlyForBucketsAndTheRiversEdge()
    {
        var view = MapView.From(Full, Full.Run());
        var links = view.LinksOf(RiverModule.Car).ToList();

        var html = await RenderTabAsync(Full, RiverModule.Car);

        var expected = links.Where(IsMoney).Where(link => link.To.Module == RiverModule.Car).Select(link => link.From)
            .Concat(links.Where(IsMoney).Where(link => link.From.Module == RiverModule.Car).Select(link => link.To))
            .Concat(links.Where(link => !IsMoney(link)).Select(link => link.From.Module == RiverModule.Car ? link.To : link.From))
            .Where(end => end.Module is not null).Select(end => (MapModules.Name(end.Module!.Value), EndName(view, end))).ToList();
        html.ShouldSatisfyAllConditions(
            () => expected.ShouldNotBeEmpty(),
            () => GoButtons(html).ShouldBe(expected),
            () => Rows(html, "comes-in").ShouldContain(row => row.Contains($"{Neighbourhood.Get("From")} Your pocket", StringComparison.Ordinal)),
            () => Rows(html, "goes-out").ShouldContain(row => row.EndsWith($"{Neighbourhood.Get("To")} out of the river", StringComparison.Ordinal)),
            () => GoButtons(html).Select(button => button.Name).Intersect([.. BucketsView.Keys.Select(key => EndName(view, MapEnd.InBucket(key))), EndName(view, MapEnd.OutOfRiver)]).ShouldBeEmpty());
    }

    // Each bar is drawn by the share of its row, out of sight of a screen reader, which has the amount.
    [Fact]
    public async Task Tab_aRowsBar_isItsShareOfTheLargestAmountListed()
    {
        var view = MapView.From(Full, Full.Run());
        var money = view.LinksOf(RiverModule.DirectorPay).Where(IsMoney).OrderBy(link => link.To.Module == RiverModule.DirectorPay ? 0 : 1).ToList();
        var largest = money.Max(link => Math.Abs(link.Amount!.Value));

        var html = await RenderTabAsync(Full, RiverModule.DirectorPay);

        html.ShouldSatisfyAllConditions(
            () => BarWidths(html).ShouldBe(money.Select(link => Formats.Percentage(Shares.Of(Math.Abs(link.Amount!.Value), largest)!.Value))),
            () => BarWidths(html).ShouldContain("100%"),
            () => Regex.Matches(Section(html), "<span class=\"map-hood__bar[^\"]*\"[^>]*>").ShouldAllBe(bar => bar.Value.Contains("aria-hidden=\"true\"", StringComparison.Ordinal)));
    }

    // What reaches the pocket is the one gold bar of a module's lines.
    [Fact]
    public async Task Tab_theLineToThePocket_isTheGoldBar()
    {
        var html = await RenderTabAsync(Full, RiverModule.DirectorPay);

        var gold = RowTags(html).Where(row => row.Tag.Contains("map-hood__row--pocket", StringComparison.Ordinal)).Select(row => MapNeighbourhoodHtml.Text(row.Inner)).ToList();
        gold.ShouldHaveSingleItem().ShouldStartWith(Words(new MapLabel(MapText.DrainList, DrainListText.Drain("net_pay"), [])));
    }

    // The rules a module sets or is under: a tax base with its amount, a condition in its whole sentence.
    [Fact]
    public async Task Tab_theRulesOfAModule_areEachInWordsWithWhoSetsThemAndTheirAmountWhereATaxBase()
    {
        var view = MapView.From(CostlyCar, CostlyCar.Run());
        var rules = view.LinksOf(RiverModule.Car).Where(link => !IsMoney(link)).ToList();

        var html = await RenderTabAsync(CostlyCar, RiverModule.Car);

        Rows(html, "rules").ShouldBe(rules.Select(link => string.Join(' ', new[]
        {
            Neighbourhood.Get("SetsOn"), EndName(view, link.To), Words(link.Carries), link.Amount is { } amount ? Formats.Euros(amount) : null,
            link.Holds == false ? Neighbourhood.Get("NotHolding") : null,
        }.OfType<string>())));
    }

    [Fact]
    public async Task Tab_aModuleUnderRules_saysWhoSetsThemOnIt()
    {
        var view = MapView.From(CostlyCar, CostlyCar.Run());
        var rules = view.LinksOf(RiverModule.CorporateTax).Where(link => !IsMoney(link)).ToList();

        var html = await RenderTabAsync(CostlyCar, RiverModule.CorporateTax);

        Rows(html, "rules").ShouldBe(rules.Select(link => string.Join(' ', new[]
        {
            Neighbourhood.Get("SetBy"), EndName(view, link.From), Words(link.Carries), link.Amount is { } amount ? Formats.Euros(amount) : null,
            link.Holds == false ? Neighbourhood.Get("NotHolding") : null,
        }.OfType<string>())));
    }

    // The one rule that does not hold is marked in words, and no rule that holds is.
    [Fact]
    public async Task Tab_aRuleNotHolding_isMarkedInWordsAndNoOtherRuleIs()
    {
        var view = MapView.From(CostlyCar, CostlyCar.Run());
        var lost = view.LinksOf(RiverModule.CorporateTax).Where(link => link.Holds == false).ToList();

        var html = await RenderTabAsync(CostlyCar, RiverModule.CorporateTax);

        var marked = RowTags(html, "rules").Where(row => MapNeighbourhoodHtml.Text(row.Inner).EndsWith(Neighbourhood.Get("NotHolding")!, StringComparison.Ordinal)).ToList();
        html.ShouldSatisfyAllConditions(
            () => lost.ShouldNotBeEmpty(),
            () => marked.Count.ShouldBe(lost.Count),
            () => RowTags(html, "rules").Count.ShouldBeGreaterThan(lost.Count),
            () => marked.ShouldAllBe(row => row.Tag.Contains("map-hood__row--lost", StringComparison.Ordinal)),
            () => RowTags(html, "rules").Except(marked).ShouldNotContain(row => row.Tag.Contains("map-hood__row--lost", StringComparison.Ordinal)));
    }

    // Nothing chosen: how to choose.
    [Fact]
    public async Task Tab_nothingChosen_saysHowToChoose()
    {
        var html = await RenderTabAsync(Full);

        (Heading(html), Note(html), Groups(html).Count).ShouldBe(("Around a module",
            "Choose a module on the map to see what comes into it, what goes out and the rules it sets or is under.", 0));
    }

    // A module that has left the map is chosen no more.
    [Fact]
    public async Task Tab_aModuleChosenOffTheMap_saysHowToChoose()
    {
        var html = await RenderTabAsync(RiverInputs.Example, RiverModule.Car);

        (Heading(html), Groups(html).Count).ShouldBe(("Around a module", 0));
    }

    // While the engine refuses an amount there is no figure to list, never that no money moves.
    [Fact]
    public async Task Tab_aModuleChosenWhileAnAmountIsRefused_saysThereIsNoFigure()
    {
        var html = await RenderTabAsync(Full.WithReserve(-1m), RiverModule.Car);

        (Heading(html), Note(html), Groups(html).Count).ShouldBe(("Around Company car", "No figures while an amount is refused.", 0));
    }

    // Company year skipped has no money to move.
    [Fact]
    public async Task Tab_companySkippedAndCompanyYearChosen_saysNoMoneyMoves()
    {
        var html = await RenderTabAsync(Full.SkipCompany(), RiverModule.CompanyYear);

        (Heading(html), Note(html), Groups(html).Count).ShouldBe(("Around Company year", "No money moves through it in this plan.", 0));
    }

    // A royalty that comes to nothing has no line and no rule.
    [Fact]
    public async Task Tab_aRoyaltyComingToNothingChosen_saysNoMoneyMoves()
    {
        var html = await RenderTabAsync(MapQuietTests.Emptied(Full, OptionalModule.Copyright), RiverModule.Copyright);

        (Note(html), Groups(html).Count).ShouldBe(("No money moves through it in this plan.", 0));
    }

    // The river starts at Company year: nothing comes in, and the group is left out rather than empty.
    [Fact]
    public async Task Tab_aGroupWithNothingInIt_isLeftOut()
    {
        var html = await RenderTabAsync(Full, RiverModule.CompanyYear);

        Groups(html).ShouldBe(["goes-out"]);
    }

    // The legend ticks the lines of the map and the neighbourhood is the module's whole: a rule whose line is
    // not drawn is listed, a kind unticked leaves nothing out, and nothing says that links are hidden.
    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Tab_whatTheLegendTicks_leavesTheNeighbourhoodAsItIs(MapLinkKind hidden)
    {
        var all = await RenderTabAsync(Full, RiverModule.Car);
        var storage = EveryKindShown();
        new MapLinkFilter(storage).Set(hidden, shown: false);

        var html = await RenderTabAsync(Full, RiverModule.Car, services => services.AddSingleton(new MapLinkFilter(storage)));

        html.ShouldSatisfyAllConditions(
            () => Section(html).ShouldBe(Section(all)),
            () => Section(html).ShouldNotContain("unticked"),
            () => Rows(html, "rules").ShouldNotBeEmpty());
    }

    // The first visit, the map quiet: the rules' lines are not drawn and their words are all there.
    [Fact]
    public async Task Tab_firstVisit_listsTheRulesThoughTheirLinesAreNotDrawn()
    {
        var all = await RenderTabAsync(Full, RiverModule.Car);

        var html = await RenderTabAsync(Full, RiverModule.Car, services => services.AddSingleton(new MapLinkFilter(new BrowserStorage())));

        (Section(html), Regex.IsMatch(html, "<polyline [^>]*data-link-kind=\"(TaxBase|Condition)\"")).ShouldBe((Section(all), false));
    }

    // A quiet lever moves no money of its own: the line of the chain running past it, with its words and
    // amount, passes through, and both its ends are buttons.
    [Fact]
    public async Task Tab_aQuietLeverChosen_listsTheLineRunningPastItAsPassingThrough()
    {
        var view = MapView.From(ChainQuiet, ChainQuiet.Run());
        var past = view.LinksOf(RiverModule.Rent).ShouldHaveSingleItem();

        var html = await RenderTabAsync(ChainQuiet, RiverModule.Rent);

        html.ShouldSatisfyAllConditions(
            () => Heading(html).ShouldBe("Around Home office rent"),
            () => Groups(html).ShouldBe(["passes-through"]),
            () => GroupHeadings(html).ShouldBe(["Passes through"]),
            () => Rows(html, "passes-through").ShouldBe([Row(view, past, "passes-through")]),
            () => GoButtons(html).ShouldBe([("vouchers", "Vouchers"), ("split", "The split")]));
    }

    // The two modules a line runs between, which would seem to skip the quiet levers between them, say it.
    [Fact]
    public async Task Tab_theEndsOfALineRunningPastQuietLevers_nameThemOnTheRow()
    {
        var view = MapView.From(ChainQuiet, ChainQuiet.Run());
        var past = view.LinksOf(RiverModule.Rent).Single();

        var vouchers = await RenderTabAsync(ChainQuiet, RiverModule.Vouchers);
        var split = await RenderTabAsync(ChainQuiet, RiverModule.Split);

        var note = Neighbourhood.Format("Past", "Expense allowances, Home office rent, Company car");
        Rows(vouchers, "goes-out").ShouldContain(Row(view, past, "goes-out"));
        Rows(split, "comes-in").ShouldContain(Row(view, past, "comes-in"));
        Row(view, past, "goes-out").ShouldEndWith(note);
    }

    // The neighbourhood is read by keyboard and by screen reader as a heading with lists under headings, its
    // ends buttons, with no live region and no bar a screen reader would stop at.
    [Fact]
    public async Task Tab_theNeighbourhood_isReachableByKeyboardAndSaidInNoLiveRegion()
    {
        var html = await RenderTabAsync(Full, RiverModule.DirectorPay);

        var section = Section(html);
        html.ShouldSatisfyAllConditions(
            () => Regex.Count(section, "<h2 ").ShouldBe(1),
            () => Regex.Count(section, "<h3 ").ShouldBe(Groups(html).Count),
            () => Regex.Count(section, "<ul ").ShouldBe(Groups(html).Count),
            () => Regex.Matches(section, "<button [^>]*>").ShouldAllBe(button => button.Value.Contains("type=\"button\"", StringComparison.Ordinal)),
            () => Regex.IsMatch(section, "tabindex|aria-live|role=\"(status|alert)\"").ShouldBeFalse(),
            () => Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"").ShouldBeFalse(),
            () => section.ShouldContain("aria-labelledby=\"map-hood-title\""));
    }
}

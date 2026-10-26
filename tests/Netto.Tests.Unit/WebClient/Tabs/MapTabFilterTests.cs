using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab under the viewer's filter of the kinds of link: the legend in its place under the head, and a
// kind hidden left out of the lines, of the arrowheads and the amounts on them; the modules, the buckets, the
// lines left and their amounts are where they were, and the neighbourhood of the module chosen, which lists the
// module's every link, is as it was (MapNeighbourhoodRenderingTests). The choice is read from the browser's
// storage before the first draw, so a kind hidden in an earlier visit is hidden at once.
public class MapTabFilterTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private const string ModuleButton = "<button type=\"button\" class=\"map-module";

    private const string BucketBox = "<div class=\"map-bucket";

    public static TheoryData<MapLinkKind> KindsTheFullPlanDraws
    {
        get
        {
            var kinds = new TheoryData<MapLinkKind>();
            foreach (var kind in Enum.GetValues<MapLinkKind>())
                kinds.Add(kind);
            return kinds;
        }
    }

    // Kinds hidden in an earlier visit by a viewer who had ticked every box: written by one filter, read by
    // the one the tab is drawn with.
    private static Action<IServiceCollection> Hiding(params MapLinkKind[] kinds)
    {
        var storage = EveryKindShown();
        var before = new MapLinkFilter(storage);
        foreach (var kind in kinds)
            before.Set(kind, shown: false);

        var filter = new MapLinkFilter(storage);
        return services => services.AddSingleton(filter);
    }

    // A viewer who has never ticked a box: the filter over a storage with nothing in it.
    private static void FirstVisit(IServiceCollection services) => services.AddSingleton(new MapLinkFilter(new BrowserStorage()));

    [Fact]
    public async Task Tab_underTheHead_holdsTheLegendOfTheKindsOfLink()
    {
        var html = await RenderTabAsync(Full);

        var (head, slot, legend, map) = (html.IndexOf("</header>", StringComparison.Ordinal), html.IndexOf("class=\"map-tab__filter\"", StringComparison.Ordinal),
            html.IndexOf("<fieldset class=\"map-filter\"", StringComparison.Ordinal), html.IndexOf("class=\"map\"", StringComparison.Ordinal));
        (head < slot, slot < legend, legend < map, Regex.Count(html, "<fieldset")).ShouldBe((true, true, true, 1));
    }

    // The legend counts the plan's links of each kind: what the map would draw with every kind ticked.
    [Fact]
    public async Task Tab_legend_countsThePlansLinksOfEachKind()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full);

        Regex.Matches(html, "<span class=\"map-filter__spoken\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value).ShouldBe(
            Enum.GetValues<MapLinkKind>().Select(kind => view.Links.Count(link => link.Kind == kind) is var count and > 0 ? $"({count} link{(count == 1 ? "" : "s")})" : "(none in this plan)"));
    }

    [Fact]
    public async Task Tab_nothingHidden_drawsEveryLine()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full, RiverModule.Car);

        LineKinds(html).ShouldBe(view.Drawn(RiverModule.Car).Select(line => line.Bundle.Kind.ToString()));
    }

    [Theory]
    [MemberData(nameof(KindsTheFullPlanDraws))]
    public async Task Tab_aKindHidden_drawsNoLineNoArrowheadAndNoWordOfIt(MapLinkKind hidden)
    {
        var view = MapView.From(Full, Full.Run());
        var kept = view.Bundles().Where(bundle => bundle.Kind != hidden).ToList();

        var html = await RenderTabAsync(Full, more: Hiding(hidden));

        html.ShouldSatisfyAllConditions(
            () => view.Bundles().Any(bundle => bundle.Kind == hidden).ShouldBeTrue(),
            () => LineKinds(html).ShouldBe(kept.Select(bundle => bundle.Kind.ToString())),
            () => Regex.Count(html, "<span class=\"map-arrow ").ShouldBe(kept.Count(bundle => bundle.Arrowhead)),
            () => MapTabTests.Placed(MapTabTests.Map(html)).Select(amount => amount.Kind).ShouldBe(kept.Select(bundle => bundle.Kind.ToString()), ignoreOrder: true),
            () => MapTabTests.Amounts(MapTabTests.Map(html)).ShouldBe(kept.Where(bundle => bundle.Amount is not null).Select(bundle => Formats.Euros(bundle.Amount!.Value)), ignoreOrder: true));
    }

    // Hiding a kind moves no amount: those left are drawn where they were with every kind shown.
    [Theory]
    [MemberData(nameof(KindsTheFullPlanDraws))]
    public async Task Tab_aKindHidden_movesNoAmountOfTheOthers(MapLinkKind hidden)
    {
        var all = MapTabTests.Placed(MapTabTests.Map(await RenderTabAsync(Full)));

        var html = await RenderTabAsync(Full, more: Hiding(hidden));

        MapTabTests.Placed(MapTabTests.Map(html)).ShouldBe(all.Where(amount => amount.Kind != hidden.ToString()));
    }

    // Hiding moves nothing: the modules and the buckets are all there, where the map puts them.
    [Fact]
    public async Task Tab_aKindHidden_leavesTheModulesAndTheBucketsWhereTheyAre()
    {
        var all = await RenderTabAsync(Full);

        var html = await RenderTabAsync(Full, more: Hiding(MapLinkKind.IntoBucket, MapLinkKind.TaxBase));

        html.ShouldSatisfyAllConditions(
            () => Boxes(html, ModuleButton).ShouldBe(Boxes(all, ModuleButton)),
            () => Boxes(html, BucketBox).ShouldBe(Boxes(all, BucketBox)),
            () => Boxes(html, ModuleButton).ShouldNotBeEmpty(),
            () => Boxes(html, BucketBox).Count.ShouldBe(BucketsView.Keys.Count));
    }

    // The legend keeps the counts and shows the kind unticked, so the viewer can bring it back.
    [Fact]
    public async Task Tab_aKindHidden_staysInTheLegendUntickedWithItsCount()
    {
        var shown = await RenderTabAsync(Full);

        var html = await RenderTabAsync(Full, more: Hiding(MapLinkKind.TaxBase));

        html.ShouldSatisfyAllConditions(
            () => LegendBoxes(html).Select(box => box.Text).ShouldBe(LegendBoxes(shown).Select(box => box.Text)),
            () => LegendBoxes(html).Select(box => box.Ticked).ShouldBe([true, true, false, true]));
    }

    // The first visit, nothing stored: the map is quiet. No line, arrowhead or word is drawn for a tax base
    // or a condition, the boxes of the modules they reach say them in their badges, and the legend shows the
    // two kinds unticked, with their counts, to bring them back.
    [Fact]
    public async Task Tab_firstVisit_drawsNoLineArrowheadOrWordOfTheRulesButEveryBadge()
    {
        var view = MapView.From(Full, Full.Run());
        var kept = view.Bundles().Where(bundle => bundle.Kind is MapLinkKind.PassedOn or MapLinkKind.IntoBucket).ToList();

        var html = await RenderTabAsync(Full, more: FirstVisit);
        var everyKind = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => view.Bundles().Select(bundle => bundle.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => view.Bundles().Select(bundle => bundle.Kind).ShouldContain(MapLinkKind.Condition),
            () => LineKinds(html).ShouldBe(kept.Select(bundle => bundle.Kind.ToString())),
            () => Regex.Count(html, "<span class=\"map-arrow ").ShouldBe(kept.Count(bundle => bundle.Arrowhead)),
            () => MapTabTests.Placed(MapTabTests.Map(html)).Select(amount => amount.Kind).Distinct().Order()
                .ShouldBe(kept.Select(bundle => bundle.Kind.ToString()).Distinct().Order()),
            () => Regex.Count(html, "<span class=\"map-badge ").ShouldBe(view.Modules.Sum(node => node.Badges.Count)),
            () => view.Modules.Sum(node => node.Badges.Count).ShouldBeGreaterThan(0),
            () => LegendBoxes(html).Select(box => box.Ticked).ShouldBe([true, true, false, false]),
            () => LegendBoxes(html).Select(box => box.Text).ShouldBe(LegendBoxes(everyKind).Select(box => box.Text)));
    }

    // Ticking a rule's kind draws its lines, its arrowheads and its words again, the other kind left out,
    // and the badges are what they were.
    [Theory]
    [InlineData(MapLinkKind.TaxBase, MapLinkKind.Condition)]
    [InlineData(MapLinkKind.Condition, MapLinkKind.TaxBase)]
    public async Task Tab_aRuleKindTicked_drawsItsLinesAndNotTheOthers(MapLinkKind ticked, MapLinkKind stillHidden)
    {
        var storage = new BrowserStorage();
        new MapLinkFilter(storage).Set(ticked, shown: true);
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full, more: services => services.AddSingleton(new MapLinkFilter(storage)));

        html.ShouldSatisfyAllConditions(
            () => LineKinds(html).ShouldContain(ticked.ToString()),
            () => LineKinds(html).ShouldNotContain(stillHidden.ToString()),
            () => Regex.Count(html, "<span class=\"map-badge ").ShouldBe(view.Modules.Sum(node => node.Badges.Count)));
    }

    // Every kind hidden, a module chosen: no line is drawn and its neighbourhood lists all it did.
    [Fact]
    public async Task Tab_aModuleChosenEveryKindHidden_drawsNoLineAndStillListsItsNeighbourhood()
    {
        var all = await RenderTabAsync(Full, RiverModule.Car);

        var html = await RenderTabAsync(Full, RiverModule.Car, Hiding([.. Enum.GetValues<MapLinkKind>()]));

        html.ShouldSatisfyAllConditions(
            () => Regex.Count(html, "<polyline").ShouldBe(0),
            () => MapNeighbourhoodHtml.Rows(html).ShouldNotBeEmpty(),
            () => MapNeighbourhoodHtml.Rows(html).ShouldBe(MapNeighbourhoodHtml.Rows(all)),
            () => html.ShouldNotContain("No money moves through it in this plan."));
    }

    // While the engine refuses an amount there is no link: the column says there is no figure.
    [Fact]
    public async Task Tab_aRefusedAmountAKindHidden_saysNoFigure()
    {
        var html = await RenderTabAsync(Full.WithReserve(-1m), RiverModule.Car, Hiding(MapLinkKind.TaxBase));

        MapNeighbourhoodHtml.Note(html).ShouldBe("No figures while an amount is refused.");
    }

    // The filter changes no live region: the tab has none.
    [Fact]
    public async Task Tab_withTheLegend_isSaidInNoLiveRegion()
    {
        var html = await RenderTabAsync(Full, RiverModule.Car, Hiding(MapLinkKind.TaxBase));

        Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"").ShouldBeFalse();
    }

    private static List<string> LineKinds(string html) =>
        [.. Regex.Matches(html, "<polyline [^>]*data-link-kind=\"(\\w+)\"[^>]*>").Select(match => match.Groups[1].Value)];

    // The boxes drawn of a kind of element, as their opening tags: where the map puts them.
    private static List<string> Boxes(string html, string opening) =>
        [.. Regex.Matches(html, Regex.Escape(opening) + "[^>]*style=\"([^\"]*)\"").Select(match => match.Groups[1].Value)];
}

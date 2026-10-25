using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab under the viewer's filter of the kinds of link: the legend in its place under the head, and a
// kind hidden left out of the lines, of the arrowheads and the words beside them, and of the chosen module's
// list in words, which says how many of its links are hidden; the modules, the buckets and what the lines
// left are where they were. The choice is read from the browser's storage before the first draw, so a kind
// hidden in an earlier visit is hidden at once.
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

    // Kinds hidden in an earlier visit: written by one filter, read by the one the tab is drawn with.
    private static Action<IServiceCollection> Hiding(params MapLinkKind[] kinds)
    {
        var storage = new BrowserStorage();
        var before = new MapLinkFilter(storage);
        foreach (var kind in kinds)
            before.Set(kind, shown: false);

        var filter = new MapLinkFilter(storage);
        return services => services.AddSingleton(filter);
    }

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

        Regex.Matches(html, "<span class=\"map-filter__count\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value).ShouldBe(
            Enum.GetValues<MapLinkKind>().Select(kind => view.Links.Count(link => link.Kind == kind) is var count and > 0 ? $"({count} link{(count == 1 ? "" : "s")})" : "(none in this plan)"));
    }

    [Fact]
    public async Task Tab_nothingHidden_drawsEveryLineAndSaysNothingOfHiddenLinks()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full, RiverModule.Car);

        html.ShouldSatisfyAllConditions(
            () => LineKinds(html).ShouldBe(view.Drawn(RiverModule.Car).Select(line => line.Bundle.Kind.ToString())),
            () => html.ShouldNotContain("data-hidden-links"),
            () => html.ShouldNotContain("hidden by the kinds"));
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
            () => Regex.Count(html, "<span class=\"map-arrow ").ShouldBe(kept.Count),
            () => Regex.Matches(html, "<div class=\"map-words__bundle[^\"]*\" data-link-kind=\"(\\w+)\"").Select(match => match.Groups[1].Value).ShouldBe(kept.Select(bundle => bundle.Kind.ToString()), ignoreOrder: true),
            () => Regex.Matches(html, "<span class=\"map-words__amount\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)
                .ShouldBe(kept.SelectMany(bundle => bundle.Links).Where(link => link.Amount is not null).Select(link => Formats.Euros(link.Amount!.Value)), ignoreOrder: true));
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
            () => Legend(html).Select(box => box.Text).ShouldBe(Legend(shown).Select(box => box.Text)),
            () => Legend(html).Select(box => box.Ticked).ShouldBe([true, true, false, true]));
    }

    // The module chosen: its links in words are the shown ones, and the line says how many the kinds turned
    // off hide, so a short list is not read as a module with little to say.
    [Fact]
    public async Task Tab_aModuleChosenAKindHidden_listsOnlyTheShownLinksAndSaysHowManyAreHidden()
    {
        var view = MapView.From(Full, Full.Run());
        var (all, hiddenOnes) = (view.LinksOf(RiverModule.Car).ToList(), view.LinksOf(RiverModule.Car).Where(link => link.Kind == MapLinkKind.TaxBase).ToList());

        var html = await RenderTabAsync(Full, RiverModule.Car, Hiding(MapLinkKind.TaxBase));

        html.ShouldSatisfyAllConditions(
            () => hiddenOnes.Count.ShouldBeGreaterThan(1),
            () => ListedKinds(html).ShouldBe(all.Where(link => link.Kind != MapLinkKind.TaxBase).Select(link => link.Kind.ToString())),
            () => HiddenNote(html).ShouldBe(($"{hiddenOnes.Count} links hidden by the kinds you turned off", hiddenOnes.Count)));
    }

    [Fact]
    public async Task Tab_aModuleChosenWithOneLinkHidden_saysSoInTheSingular()
    {
        var view = MapView.From(Full, Full.Run());
        var (module, kind) = (from candidate in MapView.ModulesOf(Full) from drawn in Enum.GetValues<MapLinkKind>() where view.LinksOf(candidate).Count(link => link.Kind == drawn) == 1 select (candidate, drawn)).First();

        var html = await RenderTabAsync(Full, module, Hiding(kind));

        HiddenNote(html).ShouldBe(("1 link hidden by the kinds you turned off", 1));
    }

    // Every kind hidden: the list is empty but for the line, which never says the module moves no money.
    [Fact]
    public async Task Tab_aModuleChosenEveryKindHidden_saysHowManyAreHiddenAndNotThatNoMoneyMoves()
    {
        var view = MapView.From(Full, Full.Run());

        var html = await RenderTabAsync(Full, RiverModule.Car, Hiding([.. Enum.GetValues<MapLinkKind>()]));

        html.ShouldSatisfyAllConditions(
            () => ListedKinds(html).ShouldBeEmpty(),
            () => HiddenNote(html).ShouldBe(($"{view.LinksOf(RiverModule.Car).Count()} links hidden by the kinds you turned off", view.LinksOf(RiverModule.Car).Count())),
            () => html.ShouldNotContain("No money moves through it in this plan."),
            () => Regex.Count(html, "<polyline").ShouldBe(0));
    }

    // A module none of whose links the filter hides says nothing of hidden links, whatever else is hidden.
    [Fact]
    public async Task Tab_aModuleChosenWhoseLinksAreAllShown_saysNothingOfHiddenLinks()
    {
        var view = MapView.From(Full, Full.Run());
        var (module, other) = (from candidate in MapView.ModulesOf(Full) from kind in Enum.GetValues<MapLinkKind>()
            where view.LinksOf(candidate).Any() && view.LinksOf(candidate).All(link => link.Kind != kind) && view.Links.Any(link => link.Kind == kind)
            select (candidate, kind)).First();

        var html = await RenderTabAsync(Full, module, Hiding(other));

        (html.Contains("data-hidden-links"), ListedKinds(html).Count).ShouldBe((false, view.LinksOf(module).Count()));
    }

    // While the engine refuses an amount there is no link to hide: the list says there is no figure, and
    // nothing of hidden links.
    [Fact]
    public async Task Tab_aRefusedAmountAKindHidden_saysNoFigureAndNothingOfHiddenLinks()
    {
        var html = await RenderTabAsync(Full.WithReserve(-1m), RiverModule.Car, Hiding(MapLinkKind.TaxBase));

        (html.Contains("No figures while an amount is refused."), html.Contains("data-hidden-links")).ShouldBe((true, false));
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

    // The kind of each link listed in words, in the order listed.
    private static List<string> ListedKinds(string html) =>
        [.. Regex.Matches(html, "<li class=\"map-links__link\" data-link-kind=\"(\\w+)\"").Select(match => match.Groups[1].Value)];

    // The line under the list of the links hidden: its words and the number it counts.
    private static (string Text, int Count)? HiddenNote(string html) =>
        Regex.Match(html, "<p class=\"map-links__note\" data-hidden-links=\"(\\d+)\"[^>]*>(.*?)</p>", RegexOptions.Singleline) is { Success: true } note
            ? (note.Groups[2].Value.Trim(), int.Parse(note.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            : null;

    // The boxes drawn of a kind of element, as their opening tags: where the map puts them.
    private static List<string> Boxes(string html, string opening) =>
        [.. Regex.Matches(html, Regex.Escape(opening) + "[^>]*style=\"([^\"]*)\"").Select(match => match.Groups[1].Value)];

    private static List<(string Text, bool Ticked)> Legend(string html) =>
    [
        .. Regex.Matches(html, "<li class=\"map-filter__kind\"[^>]*>(.*?)</li>", RegexOptions.Singleline).Select(match => match.Groups[1].Value)
            .Select(box => (Regex.Replace(Regex.Replace(Regex.Match(box, "<span class=\"mud-typography[^>]*>(.*)</span>", RegexOptions.Singleline).Groups[1].Value, "<[^>]*>", " "), "\\s+", " ").Trim(),
                Regex.IsMatch(box, "<input [^>]*type=\"checkbox\"[^>]*\\schecked"))),
    ];
}

using System.Text.RegularExpressions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab's key among the map's tools, and the lines it draws: every line of money, always; a tax base's
// and a condition's only for the module chosen, those with it at an end, each with its arrowhead and its words
// where they were placed from every link of the run, nothing chosen none, the boxes saying them in their badges
// whatever is chosen. The modules, the buckets, the lines and their amounts are where they were whatever is
// chosen.
public class MapTabRulesTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private const string ModuleButton = "<button type=\"button\" class=\"map-module";

    private const string BucketBox = "<button type=\"button\" class=\"map-bucket";

    [Fact]
    public async Task Tab_overTheMap_holdsTheKeyToTheLines()
    {
        var html = await RenderTabAsync(Full);

        var (slot, key, map) = (html.IndexOf("class=\"map-tab__key\"", StringComparison.Ordinal),
            html.IndexOf("<ul class=\"map-key\"", StringComparison.Ordinal), html.IndexOf("class=\"map\"", StringComparison.Ordinal));
        (slot < key, key < map, Regex.Count(html, "<ul class=\"map-key\"")).ShouldBe((true, true, 1));
    }

    // Nothing chosen: every line of money and its amount, no line, arrowhead or word of a tax base or a
    // condition, the boxes of the modules they reach saying them in their badges.
    [Fact]
    public async Task Tab_nothingChosen_drawsTheMoneyAndNoRulesLineButEveryBadge()
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English);
        var money = view.Bundles().Where(bundle => MapLinkStyle.IsMoney(bundle.Kind)).ToList();

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => view.Bundles().Select(bundle => bundle.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => view.Bundles().Select(bundle => bundle.Kind).ShouldContain(MapLinkKind.Condition),
            () => LineKinds(html).ShouldBe(money.Where(bundle => !bundle.StretchOfThePocketLine).Select(bundle => bundle.Kind.ToString())),
            () => Regex.Count(html, "<span class=\"map-arrow ").ShouldBe(money.Count(bundle => bundle.Arrowhead)),
            () => MapTabTests.Amounts(MapTabTests.Map(html)).ShouldBe(money.Where(bundle => bundle.SaysAmount).Select(bundle => Formats.Euros(bundle.Amount!.Value)), ignoreOrder: true),
            () => Regex.Count(html, "<span class=\"map-badge ").ShouldBe(view.Modules.Sum(node => node.Badges.Count)),
            () => view.Modules.Sum(node => node.Badges.Count).ShouldBeGreaterThan(0));
    }

    // A module chosen: its tax bases' and conditions' lines are drawn with the money, each its arrowhead, and
    // their amounts and words where they were placed; the other modules' are not.
    [Theory]
    [InlineData(RiverModule.Car)]
    [InlineData(RiverModule.CorporateTax)]
    [InlineData(RiverModule.Household)]
    public async Task Tab_aModuleChosen_drawsItsRulesLinesAndTheirWordsWhereTheyWerePlaced(RiverModule chosen)
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English);
        var placed = MapAmounts.Of(view, MapAmountsTests.Say);
        var rules = view.Bundles().Where(bundle => !MapLinkStyle.IsMoney(bundle.Kind) && bundle.Touches(chosen)).ToList();
        var drawn = view.Bundles().Where(bundle => MapLinkStyle.IsMoney(bundle.Kind) || bundle.Touches(chosen)).ToList();

        var html = await RenderTabAsync(Full, Chosen(chosen));

        html.ShouldSatisfyAllConditions(
            () => rules.ShouldNotBeEmpty(),
            () => LineKinds(html).Order().ShouldBe(drawn.Where(bundle => !bundle.StretchOfThePocketLine).Select(bundle => bundle.Kind.ToString()).Order()),
            () => Regex.Count(html, "<span class=\"map-arrow ").ShouldBe(drawn.Count(bundle => bundle.Arrowhead)),
            () => MapTabTests.Placed(MapTabTests.Map(html)).ShouldBe(placed.Where(amount => amount.Bundle.Touches(chosen))
                .Select(amount => (amount.Bundle.Kind.ToString(), MapTabTests.Style(amount), "true", amount.Text)), ignoreOrder: true));
    }

    // The pay's pill is drawn with the pay's line, a line of money, whatever is chosen.
    [Theory]
    [InlineData(null)]
    [InlineData(RiverModule.Car)]
    public async Task Tab_whateverIsChosen_drawsThePaysPill(RiverModule? chosen)
    {
        var html = await RenderTabAsync(Full, Chosen(chosen));

        Regex.Count(html, "<div class=\"map-pill\"").ShouldBe(1);
    }

    // Choosing moves nothing: the modules and the buckets are all there, where the map puts them.
    [Fact]
    public async Task Tab_aModuleChosen_leavesTheModulesAndTheBucketsWhereTheyAre()
    {
        var none = await RenderTabAsync(Full);

        var html = await RenderTabAsync(Full, MapChoice.Of(RiverModule.Car));

        html.ShouldSatisfyAllConditions(
            () => Boxes(html, ModuleButton).ShouldBe(Boxes(none, ModuleButton)),
            () => Boxes(html, BucketBox).ShouldBe(Boxes(none, BucketBox)),
            () => Boxes(html, ModuleButton).ShouldNotBeEmpty(),
            () => Boxes(html, BucketBox).Count.ShouldBe(BucketsView.Keys.Count));
    }

    // While the engine refuses an amount there is no link, and nothing is said received: the refusal is said
    // once, on the card or above the map.
    [Fact]
    public async Task Tab_aRefusedAmount_drawsNoLineAndSaysNothingReceived()
    {
        var html = await RenderTabAsync(Full.WithReserve(-1m), MapChoice.Of(RiverModule.Car));

        (Regex.Count(html, "<path class=\"map-"), MapReceivedHtml.Count(html), html.Contains("refused-message", StringComparison.Ordinal)).ShouldBe((0, 0, true));
    }

    // The key and the choice change no live region: the tab has none.
    [Fact]
    public async Task Tab_withTheKey_isSaidInNoLiveRegion()
    {
        var html = await RenderTabAsync(Full, MapChoice.Of(RiverModule.Car));

        Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"").ShouldBeFalse();
    }

    // A module chosen: under the line of what it receives, Corporate tax's here, the car receiving nothing it says,
    // a list of what it does to another's tax base or under
    // another's condition, and what is done to it so, an item for each line of a tax base or a condition drawn for
    // it, each a sentence ending on the other module, a button to go to it; named for a screen reader.
    [Fact]
    public async Task Tab_aModuleChosen_listsItsEffectsUnderWhatItReceives()
    {
        var view = MapView.From(Full, Full.Run(), MapNames.English);
        var effects = MapEffectsView.Of(view, RiverModule.Car).Items;
        var benefit = MapViewTests.Line(view, MapEnd.Of(RiverModule.Car), MapEnd.Of(RiverModule.DirectorPay));

        var html = await RenderTabAsync(Full, MapChoice.Of(RiverModule.Car));
        var corporate = await RenderTabAsync(Full, MapChoice.Of(RiverModule.CorporateTax));

        var (receives, list, panel) = (corporate.IndexOf("class=\"map-tab__receives\"", StringComparison.Ordinal),
            corporate.IndexOf("<ul class=\"map-tab__effects\" aria-label=\"Effects\"", StringComparison.Ordinal), corporate.IndexOf("class=\"map-tab__panel\"", StringComparison.Ordinal));
        html.ShouldSatisfyAllConditions(
            () => (receives > 0, receives < list, list < panel).ShouldBe((true, true, true)),
            () => Effects(html).Count.ShouldBe(effects.Count),
            () => Effects(html).Select(effect => effect.Module).ShouldBe(effects.Select(effect => MapModules.Name(effect.Other))),
            () => Effects(html).ShouldContain(effect => effect.Text == $"Car's benefit in kind after your contribution: +{Formats.Euros(benefit.Amount!.Value)} to the tax base of Director pay"),
            () => Effects(html).ShouldContain(effect => effect.Text.StartsWith("Benefits within their share of the pay: ", StringComparison.Ordinal) && effect.Text.EndsWith(", on Corporate tax", StringComparison.Ordinal)));
    }

    // Nothing chosen, or a module with no tax base's or condition's line chosen, there is no list.
    [Theory]
    [InlineData(null)]
    [InlineData(RiverModule.CompanyYear)]
    public async Task Tab_nothingOrAModuleWithNoEffectChosen_listsNone(RiverModule? chosen)
    {
        var html = await RenderTabAsync(Full, Chosen(chosen));

        html.ShouldNotContain("map-tab__effects");
    }

    // Each line of the list is drawn in a group of its own on the map, saying its sentence as its title, so the
    // pointer over the line shows it; the group and the list's item share their number.
    [Fact]
    public async Task Tab_anEffectsLine_saysItsSentenceAsItsTitle()
    {
        var html = await RenderTabAsync(Full, MapChoice.Of(RiverModule.CorporateTax));

        var titles = Regex.Matches(html, "<g class=\"map-effect\" data-effect=\"(\\d+)\"[^>]*><title[^>]*>([^<]*)</title><path [^>]*>(?:</path>)?<path [^>]*data-link-kind=\"(TaxBase|Condition)\"")
            .Select(match => (int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), match.Groups[2].Value)).ToList();
        titles.ShouldSatisfyAllConditions(
            () => titles.ShouldNotBeEmpty(),
            () => titles.ShouldBe(Effects(html).Select((effect, index) => (index, effect.Text))));
    }

    // Each item of the list a rendered tab draws, by its number: its text as a viewer reads it and the module its
    // button goes to.
    private static List<(string Text, string Module)> Effects(string html) =>
    [
        .. Regex.Matches(html, "<li class=\"map-tab__effect[^\"]*\" data-effect=\"\\d+\"[^>]*>(.*?)</li>", RegexOptions.Singleline).Select(match =>
            (MapReceivedHtml.Text(match.Groups[1].Value), Regex.Match(match.Groups[1].Value, "data-effect-on=\"([a-z-]+)\"").Groups[1].Value)),
    ];

    internal static List<string> LineKinds(string html) =>
        [.. Regex.Matches(html, "<path [^>]*data-link-kind=\"(\\w+)\"[^>]*>").Select(match => match.Groups[1].Value)];

    // The boxes drawn of a kind of element, as their opening tags: where the map puts them.
    private static List<string> Boxes(string html, string opening) =>
        [.. Regex.Matches(html, Regex.Escape(opening) + "[^>]*style=\"([^\"]*)\"").Select(match => match.Groups[1].Value)];
}

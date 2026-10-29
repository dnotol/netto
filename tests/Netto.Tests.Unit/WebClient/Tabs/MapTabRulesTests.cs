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

    private const string BucketBox = "<div class=\"map-bucket";

    [Fact]
    public async Task Tab_underTheHead_holdsTheKeyToTheLines()
    {
        var html = await RenderTabAsync(Full);

        var (head, slot, key, map) = (html.IndexOf("</header>", StringComparison.Ordinal), html.IndexOf("class=\"map-tab__key\"", StringComparison.Ordinal),
            html.IndexOf("<ul class=\"map-key\"", StringComparison.Ordinal), html.IndexOf("class=\"map\"", StringComparison.Ordinal));
        (head < slot, slot < key, key < map, Regex.Count(html, "<ul class=\"map-key\"")).ShouldBe((true, true, true, 1));
    }

    // Nothing chosen: every line of money and its amount, no line, arrowhead or word of a tax base or a
    // condition, the boxes of the modules they reach saying them in their badges.
    [Fact]
    public async Task Tab_nothingChosen_drawsTheMoneyAndNoRulesLineButEveryBadge()
    {
        var view = MapView.From(Full, Full.Run());
        var money = view.Bundles().Where(bundle => MapLinkStyle.IsMoney(bundle.Kind)).ToList();

        var html = await RenderTabAsync(Full);

        html.ShouldSatisfyAllConditions(
            () => view.Bundles().Select(bundle => bundle.Kind).ShouldContain(MapLinkKind.TaxBase),
            () => view.Bundles().Select(bundle => bundle.Kind).ShouldContain(MapLinkKind.Condition),
            () => LineKinds(html).ShouldBe(money.Select(bundle => bundle.Kind.ToString())),
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
        var view = MapView.From(Full, Full.Run());
        var placed = MapAmounts.Of(view, MapAmountsTests.Say);
        var rules = view.Bundles().Where(bundle => !MapLinkStyle.IsMoney(bundle.Kind) && bundle.Touches(chosen)).ToList();
        var drawn = view.Bundles().Where(bundle => MapLinkStyle.IsMoney(bundle.Kind) || bundle.Touches(chosen)).ToList();

        var html = await RenderTabAsync(Full, chosen);

        html.ShouldSatisfyAllConditions(
            () => rules.ShouldNotBeEmpty(),
            () => LineKinds(html).Order().ShouldBe(drawn.Select(bundle => bundle.Kind.ToString()).Order()),
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
        var html = await RenderTabAsync(Full, chosen);

        Regex.Count(html, "<div class=\"map-pill\"").ShouldBe(1);
    }

    // Choosing moves nothing: the modules and the buckets are all there, where the map puts them.
    [Fact]
    public async Task Tab_aModuleChosen_leavesTheModulesAndTheBucketsWhereTheyAre()
    {
        var none = await RenderTabAsync(Full);

        var html = await RenderTabAsync(Full, RiverModule.Car);

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
        var html = await RenderTabAsync(Full.WithReserve(-1m), RiverModule.Car);

        (Regex.Count(html, "<polyline"), MapReceivedHtml.Count(html), html.Contains("refused-message", StringComparison.Ordinal)).ShouldBe((0, 0, true));
    }

    // The key and the choice change no live region: the tab has none.
    [Fact]
    public async Task Tab_withTheKey_isSaidInNoLiveRegion()
    {
        var html = await RenderTabAsync(Full, RiverModule.Car);

        Regex.IsMatch(html, "aria-live|role=\"(status|alert)\"").ShouldBeFalse();
    }

    internal static List<string> LineKinds(string html) =>
        [.. Regex.Matches(html, "<polyline [^>]*data-link-kind=\"(\\w+)\"[^>]*>").Select(match => match.Groups[1].Value)];

    // The boxes drawn of a kind of element, as their opening tags: where the map puts them.
    private static List<string> Boxes(string html, string opening) =>
        [.. Regex.Matches(html, Regex.Escape(opening) + "[^>]*style=\"([^\"]*)\"").Select(match => match.Groups[1].Value)];
}

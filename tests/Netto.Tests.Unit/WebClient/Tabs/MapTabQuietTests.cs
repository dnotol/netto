using System.Text.RegularExpressions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;
using static Netto.Tests.WebClient.Tabs.MapTabTests;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab as a viewer meets the levers that move no money of their own in the plan: their names in one
// dashed row where they follow one another down the chain, each its own button in the river's order, named
// with the row's word, which the row shows once; no amount on the row, the chain's line through it saying
// its amount once; a quiet lever chosen pressed, with its card beside the map and the line running past it
// listed. The royalty that comes to nothing is quiet in its own place.
public class MapTabQuietTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    // The expense allowances, the rent and the car left empty, the vouchers kept.
    private static readonly RiverInputs ChainQuiet = MapQuietTests.Emptied(Full, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car);

    private static readonly string Word = ResxText.For<MapTab>().Get("Quiet")!;

    [Fact]
    public async Task Tab_quietLevers_areButtonsInTheRiversOrderNamedWithTheirWord()
    {
        var badges = MapBadgesTests.Of(ChainQuiet);

        var html = await RenderTabAsync(ChainQuiet);

        Buttons(html).Select(button => (button.Module, button.Name, button.Pressed)).ShouldBe(MapView.ModulesOf(ChainQuiet).Select(module => (
            MapModules.Name(module),
            module is RiverModule.Allowances or RiverModule.Rent or RiverModule.Car
                ? $"{CardTitle(module)} {Word}"
                : string.Join(' ', [CardTitle(module), .. badges.GetValueOrDefault(module, []).Select(BadgeWords)]),
            "false")));
    }

    // Quiet levers following one another down the chain share one row, which shows their names and nothing
    // else: each name says out of sight that it holds nothing in this plan, so a row that wraps on a narrow
    // map takes a line per name, never one more.
    [Fact]
    public async Task Tab_quietLeversFollowingOneAnother_shareOneRowShowingTheirNamesAlone()
    {
        var rows = QuietRows(await RenderTabAsync(ChainQuiet));

        var row = rows.ShouldHaveSingleItem();
        row.ShouldSatisfyAllConditions(
            () => Modules(row).ShouldBe(["allowances", "rent", "car"]),
            () => Shown(row).ShouldBe("Expense allowances Home office rent Company car"));
    }

    // The row draws no amount: the chain's line through it says what the vouchers leave once, not once per
    // lever it runs past.
    [Fact]
    public async Task Tab_aQuietRow_drawsNoAmountAndTheLineThroughItSaysItsAmountOnce()
    {
        var left = Formats.Euros(VoucherView.From(ChainQuiet.Run().Year!, ChainQuiet.Composition).Left);

        var map = Map(await RenderTabAsync(ChainQuiet));

        map.ShouldSatisfyAllConditions(
            () => QuietRows(map).ShouldAllBe(row => !row.Contains('€')),
            () => Amounts(map).Count(amount => amount == left).ShouldBe(1));
    }

    // A quiet lever chosen: its name pressed in its row, its card beside the map, so that its amounts can be
    // entered, and the line running past it listed.
    [Fact]
    public async Task Tab_aQuietLeverChosen_isPressedWithItsCardBesideTheMapAndTheLinePastIt()
    {
        var html = await RenderTabAsync(ChainQuiet, RiverModule.Rent);

        html.ShouldSatisfyAllConditions(
            () => Buttons(html).Where(button => button.Pressed == "true").Select(button => button.Module).ShouldBe(["rent"]),
            () => (html.IndexOf("class=\"map-tab__panel\"", StringComparison.Ordinal) < html.IndexOf("id=\"card-rent\"", StringComparison.Ordinal)).ShouldBeTrue(),
            () => Words(html).ShouldHaveSingleItem().Text.ShouldEndWith(", from Vouchers to The split"));
    }

    // A royalty that comes to nothing is quiet in a row of its own, in its own place.
    [Fact]
    public async Task Tab_aRoyaltyComingToNothing_isQuietInARowOfItsOwn()
    {
        var html = await RenderTabAsync(MapQuietTests.Emptied(Full, OptionalModule.Copyright));

        QuietRows(html).Select(Modules).ShouldBe([["copyright"]]);
    }

    [Fact]
    public async Task Tab_everyLeverMovingMoney_drawsNoQuietRow()
    {
        var html = await RenderTabAsync(Full);

        (QuietRows(html).Count, Buttons(html).Count).ShouldBe((0, MapView.ModulesOf(Full).Count));
    }

    // Each quiet row of the map, as drawn.
    private static List<string> QuietRows(string html) =>
        [.. Regex.Matches(html, "<div class=\"map-quiet\"[^>]*>(.*?)</div>", RegexOptions.Singleline).Select(match => match.Groups[1].Value)];

    private static List<string> Modules(string row) => [.. Regex.Matches(row, "data-module=\"([a-z-]+)\"").Select(match => match.Groups[1].Value)];

    // What a row shows in sight: its text, what its names say out of sight left out.
    private static string Shown(string row) =>
        Regex.Replace(Regex.Replace(Regex.Replace(row, "<span class=\"map-module__quiet\"[^>]*>.*?</span>", ""), "<[^>]*>", " "), "\\s+", " ").Trim();
}

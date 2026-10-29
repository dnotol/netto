using Netto.Web.Client;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The words of the gates' boxes and of the pay's pill, held to MapGateWords.resx: a misspelt key would show on
// screen as the key itself. Each line of a gate fits inside it at the narrowest map, its amount up to the
// limit it is held to.
public class MapGateTextTests
{
    // The name IStringLocalizer<MapGateWords> reads Tabs/MapGateWords.resx under.
    private static readonly ResxText Resources = ResxText.For<MapGateWords>();

    public static TheoryData<string, string> Words => new()
    {
        { MapGateText.Across, "{0} across" },
        { MapGateText.ToYou, "{0} to you" },
        { MapGateText.Untaxed, "untaxed" },
        { MapGateText.Benefit, "{0} benefit" },
        { MapGateText.YouPay, "you pay {0}" },
        { MapGateText.Pay(withContributions: false), "Pay · {0}" },
        { MapGateText.Pay(withContributions: true), "Pay and contributions · {0}" },
    };

    [Theory]
    [MemberData(nameof(Words))]
    public void MapGateWordsResx_eachKey_saysItsWords(string key, string words)
    {
        Resources.Get(key).ShouldBe(words);
    }

    // A sentence is cut where its amount goes, so the amount alone can be drawn apart.
    [Theory]
    [InlineData("{0} to you", "", " to you", true)]
    [InlineData("you pay {0}", "you pay ", "", true)]
    [InlineData("Pay · {0}", "Pay · ", "", true)]
    [InlineData("untaxed", "untaxed", "", false)]
    public void Parts_aSentence_isCutWhereItsAmountGoes(string sentence, string before, string after, bool holdsAmount)
    {
        MapGateText.Parts(sentence).ShouldBe((before, after, holdsAmount));
    }

    [Fact]
    public void Parts_aSentenceWithTwoAmounts_throws()
    {
        Should.Throw<ArgumentException>(() => MapGateText.Parts("{0} and {0}"));
    }

    // The room inside a gate at the narrowest map (34rem, 544 px): the approved prototype's gate, 124 px wide
    // (local/mockups/map-gates-544), less its padding of 0.35rem each side and its frame of 1px each side. The
    // layout that draws the gates (slice B of « I read on the map whose money each module moves ») takes this
    // width or holds its own to this test.
    private const decimal GateRoom = 124m - 2m * 0.35m * 16m - 2m + 0.5m;

    // A gate's line at its own size: what its characters take as MapAmounts.Width reckons them at 0.72rem and
    // weight 400, less the ground it adds, scaled to the line's size, and 2% more for weight 500.
    private static decimal LineWidth(string text) =>
        (MapAmounts.Width(text) - 2m * MapAmounts.GroundRem * MapLayout.PixelsPerRem) * MapGateText.FontRem / MapAmounts.FontRem * 1.02m;

    // Each line with the largest amount it is held to: what crosses and what reaches the viewer up to
    // €999,999.99, the car's benefit and what the viewer pays for it up to €99,999.99, far beyond any car's.
    public static TheoryData<string, decimal> GateLines => new()
    {
        { MapGateText.Across, 999_999.99m },
        { MapGateText.ToYou, 999_999.99m },
        { MapGateText.Untaxed, 0m },
        { MapGateText.Benefit, 99_999.99m },
        { MapGateText.YouPay, 99_999.99m },
    };

    [Theory]
    [MemberData(nameof(GateLines))]
    public void GateLine_withAnAmountUpToItsLimit_fitsInsideTheGate(string key, decimal limit)
    {
        var text = Resources.Format(key, Formats.Euros(limit));

        LineWidth(text).ShouldBeLessThanOrEqualTo(GateRoom);
    }

    // The stylesheet draws a gate's line at the size it is held to here, on one line.
    [Fact]
    public void MapGateWordsCss_drawsALineAtItsSize()
    {
        var line = Stylesheet.Block(Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapGateWords.razor.css"), ".map-gate-words");

        line.ShouldSatisfyAllConditions(
            () => line.ShouldContain($"font-size: {Stylesheet.Rem(MapGateText.FontRem)};"),
            () => line.ShouldContain("font-weight: 500;"),
            () => line.ShouldContain("font-variant-numeric: tabular-nums;"),
            () => line.ShouldContain("white-space: nowrap;"));
    }
}

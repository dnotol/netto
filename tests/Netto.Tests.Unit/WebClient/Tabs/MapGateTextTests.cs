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
        { MapGateText.Quotient, "quotient" },
        { MapGateText.QuotientAmount, "{0}" },
        { MapGateText.Pay(withContributions: false), "Pay · {0}" },
        { MapGateText.Pay(withContributions: true), "Pay + contributions {0}" },
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

    // The room inside a gate at the narrowest map, 34rem or 544 px, as the layout keeps it: a gate's width there,
    // 124 px, less its padding of 0.35rem each side and its frame of 1px each side (MapLayout.GateRoomPixels).
    private const decimal GateRoom = MapLayout.GateRoomPixels;

    // A gate's line at its own size: what its characters take as MapAmounts.Width reckons them at 0.72rem and
    // weight 400, less the ground it adds, scaled to the line's size, and 2% more for weight 500. FrenchTabsTests
    // holds the French lines with it.
    internal static decimal LineWidth(string text) =>
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

    // The room inside the partner's box at the narrowest map, as the layout keeps it: its width there, 78 px,
    // less its padding and frame (MapLayout.HouseholdRoomPixels); each line of the marital quotient fits it, its
    // amount up to €99,999.99, far beyond the most a quotient attributes.
    [Theory]
    [InlineData(MapGateText.Quotient)]
    [InlineData(MapGateText.QuotientAmount)]
    public void HouseholdLine_withTheQuotientUpToItsLimit_fitsInsideThePartnersBox(string key)
    {
        var text = Resources.Format(key, Formats.Euros(99_999.99m));

        (MapLayout.HouseholdRoomPixels, LineWidth(text) <= MapLayout.HouseholdRoomPixels).ShouldBe((78m - 2m * 0.35m * 16m - 2m, true));
    }

    // The pay's pill says the pay alone on one line; with the contributions, its words over its amount, on two.
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void PillLines_thePaysPill_isOneLineWithThePayAloneAndTwoWithTheContributions(bool withContributions, int lines)
    {
        MapGateText.PillLines(MapGateText.Pay(withContributions)).ShouldBe(lines);
    }

    // The room inside the pay's pill at the narrowest map: a gate's width, 124 px, less its padding of 0.35rem each
    // side and its frame (MapLayout.PillRoomPixels).
    [Fact]
    public void PillRoomPixels_isAGatesWidthLessThePillsPaddingAndFrame()
    {
        MapLayout.PillRoomPixels.ShouldBe(124m - 2m * 0.35m * 16m - 2m);
    }

    // Each line of the pay's pill fits inside it at the narrowest map, the pay up to €999,999.99, with the
    // contributions or without.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PayPill_eachLineUpToItsLimit_fitsInsideThePill(bool withContributions)
    {
        var lines = PillLinesOf(Resources, withContributions, Formats.Euros(999_999.99m));

        lines.Where(line => LineWidth(line) > MapLayout.PillRoomPixels).ShouldBeEmpty();
    }

    // The lines the pay's pill draws, its words in the texts given and its amount: its sentence on one line, or
    // its words up to the amount over the amount and what follows it.
    internal static IReadOnlyList<string> PillLinesOf(ResxText texts, bool withContributions, string amount)
    {
        var key = MapGateText.Pay(withContributions);
        var sentence = texts.Get(key).ShouldNotBeNull();
        var (before, after, _) = MapGateText.Parts(sentence);
        return MapGateText.PillLines(key) == 1 ? [string.Format(System.Globalization.CultureInfo.InvariantCulture, sentence, amount)] : [before.TrimEnd(), amount + after];
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

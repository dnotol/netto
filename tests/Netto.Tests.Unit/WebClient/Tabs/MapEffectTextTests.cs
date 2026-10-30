using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The words of the list of what the module chosen does to another's tax base or under another's condition, held
// to MapTab.resx, each sentence ending where the other module's name is said, a button to go to it.
public class MapEffectTextTests
{
    // The name IStringLocalizer<MapTab> reads Tabs/MapTab.resx under.
    private static readonly ResxText Resources = ResxText.For<MapTab>();

    public static TheoryData<string, string> Words => new()
    {
        { MapEffectText.Effects, "Effects" },
        { MapEffectText.TaxBase(sets: true), "{0}: {1} to the tax base of" },
        { MapEffectText.TaxBase(sets: false), "{0}: {1} to its tax base, from" },
        { MapEffectText.Condition(sets: true), "{0}: {1}, on" },
        { MapEffectText.Condition(sets: false), "{0}: {1}, set by" },
        { MapEffectText.Rule(RiverModule.Allowances, RiverModule.Rent), "No office allowance beside a rent" },
        { MapEffectText.Rule(RiverModule.Split, RiverModule.CorporateTax), "Pay keeps the reduced rate" },
        { MapEffectText.Rule(RiverModule.Car, RiverModule.CorporateTax), "Benefits within their share of the pay" },
        { MapEffectText.Rule(RiverModule.Copyright, RiverModule.Split), "Royalty lowers the pay keeping the rate" },
        { MapEffectText.Rule(RiverModule.Dividend, RiverModule.Split), "Pay-out covers the reserve" },
    };

    [Theory]
    [MemberData(nameof(Words))]
    public void MapTabResx_eachKey_saysItsWords(string key, string words)
    {
        Resources.Get(key).ShouldBe(words);
    }

    // Every condition the map draws has its rule's words, by the modules it runs between.
    [Fact]
    public void Rule_everyConditionTheMapDraws_hasItsWords()
    {
        var view = MapView.From(MapLinksTests.Full, MapLinksTests.Full.Run(), MapNames.English);

        view.Bundles().Where(line => line.Kind == MapLinkKind.Condition).Select(line => Resources.Get(MapEffectText.Rule(line.From.Module!.Value, line.To.Module!.Value)))
            .ShouldAllBe(words => !string.IsNullOrEmpty(words));
    }

    [Fact]
    public void Rule_aPairNoConditionRunsBetween_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapEffectText.Rule(RiverModule.Rent, RiverModule.Allowances));
    }

    // An amount added to a tax base says it is added; one below nothing keeps its own sign.
    [Theory]
    [InlineData(3_450, "+€3,450.00")]
    [InlineData(-120.5, "−€120.50")]
    [InlineData(0, "€0.00")]
    public void Amount_aTaxBasesAmount_isSigned(decimal amount, string said)
    {
        MapEffectText.Amount(amount).ShouldBe(said);
    }
}

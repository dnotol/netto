using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The words of the key to the map's lines, held to MapKey.resx: a misspelt key would show on screen as the key
// itself, and a text no line uses would be left behind.
public class MapKeyTextTests
{
    // The name IStringLocalizer<MapKey> reads Tabs/MapKey.resx under.
    private static readonly ResxText Resources = ResxText.For<MapKey>();

    [Fact]
    public void MapKeyResx_everyLine_hasANameAndWhatItIs()
    {
        var keys = MapKeyView.Lines.SelectMany(line => new[] { MapKeyText.Name(line), MapKeyText.Gloss(line) }).Append(MapKeyText.Key).Append(MapKeyText.GlossLine);

        Resources.Missing(keys).ShouldBeEmpty();
    }

    [Fact]
    public void Name_eachLine_isWhatTheMapCallsIt()
    {
        MapKeyView.Lines.Select(line => Resources.Get(MapKeyText.Name(line))).ShouldBe(["Passed on", "Into a bucket", "Into your pocket", "Tax base", "Conditions"]);
    }

    // The key is named for a screen reader, which hears it before its lines.
    [Fact]
    public void Key_isTheListsName()
    {
        Resources.Get(MapKeyText.Key).ShouldBe("Key to the lines");
    }

    // In the key's bubble, each line is its name, then what it is.
    [Fact]
    public void GlossLine_isTheLinesNameThenWhatItIs()
    {
        Resources.Format(MapKeyText.GlossLine, "Tax base", "what changes a tax base").ShouldBe("Tax base: what changes a tax base");
    }

    // What each line is, the rules' saying they are drawn for the module chosen, their boxes saying them
    // otherwise.
    [Fact]
    public void Gloss_eachLine_saysWhatItIs()
    {
        MapKeyView.Lines.Select(line => Resources.Get(MapKeyText.Gloss(line))).ShouldBe([
            "money handed from one module to the next. The thicker the line, the more money.",
            "money that ends in one of the bar's buckets, its line running into it, or that comes back out of a bucket, its line then ending in an arrowhead. The thicker the line, the more money.",
            "what reaches you, in gold: one line down your column into your pocket, saying after each line joining it what has reached you so far.",
            "what changes the amount a tax is worked out on without moving money, a benefit taxed with the pay, what Corporate tax adds back, the marital quotient. Drawn for the module you choose, each line saying its amount.",
            "a condition one module sets on another. Drawn for the module you choose, its rule and whether it holds listed beside the card; a box says it only where it is lost.",
        ]);
    }

    // Every text of the key is one it uses.
    [Fact]
    public void MapKeyResx_noText_isLeftUnused()
    {
        var used = MapKeyView.Lines.SelectMany(line => new[] { MapKeyText.Name(line), MapKeyText.Gloss(line) }).Concat([MapKeyText.Key, MapKeyText.GlossLine]);

        Resources.Keys.Except(used).ShouldBeEmpty();
    }

    [Fact]
    public void Name_aLineItHasNoTextFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapKeyText.Name((MapKeyLine)99));
    }
}

using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The words of the legend of the kinds of link in MapFilter.resx.
public class MapFilterTextTests
{
    // The name IStringLocalizer<MapFilter> reads Tabs/MapFilter.resx under.
    private static readonly ResxText Resources = ResxText.For<MapFilter>();

    private static readonly int[] Totals = [0, 1, 2];

    [Fact]
    public void MapFilterResx_everyKind_hasANameAndWhatItIs()
    {
        var keys = Enum.GetValues<MapLinkKind>().SelectMany(kind => new[] { MapFilterText.Name(kind), MapFilterText.Gloss(kind) }).Append(MapFilterText.Legend).Append(MapFilterText.GlossLine);

        Resources.Missing(keys).ShouldBeEmpty();
    }

    [Fact]
    public void MapFilterResx_everyCountAndTheLineOfNoneTicked_hasAText()
    {
        Resources.Missing([.. Totals.Select(MapFilterText.Count), MapFilterText.AllHidden]).ShouldBeEmpty();
    }

    // A kind's name is the kind's, short, and the count after it says what hiding it would take off the map.
    [Fact]
    public void Name_eachKind_isWhatTheMapCallsIt()
    {
        Enum.GetValues<MapLinkKind>().Select(kind => Resources.Get(MapFilterText.Name(kind))).ShouldBe(["Passed on", "Into a bucket", "Tax base", "Conditions"]);
    }

    // In the legend's bubble, each kind is its name, then what it is.
    [Fact]
    public void GlossLine_isTheKindsNameThenWhatItIs()
    {
        Resources.Format(MapFilterText.GlossLine, "Tax base", "what changes a tax base").ShouldBe("Tax base: what changes a tax base");
    }

    // Money into a bucket says what runs into it, what comes back out of it, and what the gold line down the
    // director's column says, which nothing else on the map explains.
    [Fact]
    public void Gloss_moneyIntoABucket_saysWhatThePocketLineSays()
    {
        Resources.Get(MapFilterText.Gloss(MapLinkKind.IntoBucket)).ShouldBe(
            "money that ends in one of the bar's buckets, its line running into it, or that comes back out of a bucket, its line then ending in an arrowhead. "
            + "The thicker the line, the more money. The gold line down your column says, after each line joining it, what has reached you so far.");
    }

    [Fact]
    public void Count_noneOneAndMany_saysHowManyLinksThePlanHasOfAKind()
    {
        (Resources.Format(MapFilterText.Count(0)), Resources.Format(MapFilterText.Count(1)), Resources.Format(MapFilterText.Count(14), 14))
            .ShouldBe(("(none in this plan)", "(1 link)", "(14 links)"));
    }

    [Fact]
    public void Count_negative_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapFilterText.Count(-1));
    }

    // The line under the boxes, shown only while none is ticked.
    [Fact]
    public void AllHidden_saysToTickOne()
    {
        Resources.Get(MapFilterText.AllHidden).ShouldBe("Every kind of link is hidden: tick one to see its links.");
    }

    // The summary of how many links are shown is gone with the second line of each box: no text of it is
    // left to go unused.
    [Fact]
    public void MapFilterResx_noText_isLeftUnused()
    {
        var used = Enum.GetValues<MapLinkKind>().SelectMany(kind => new[] { MapFilterText.Name(kind), MapFilterText.Gloss(kind) })
            .Concat(Totals.Select(MapFilterText.Count)).Concat([MapFilterText.Legend, MapFilterText.GlossLine, MapFilterText.AllHidden]);

        Resources.Keys.Except(used).ShouldBeEmpty();
    }

    [Fact]
    public void Name_aKindItHasNoTextFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapFilterText.Name((MapLinkKind)99));
    }
}

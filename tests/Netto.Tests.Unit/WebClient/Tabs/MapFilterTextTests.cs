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
        var keys = Enum.GetValues<MapLinkKind>().SelectMany(kind => new[] { MapFilterText.Name(kind), MapFilterText.Gloss(kind) }).Append(MapFilterText.Legend);

        Resources.Missing(keys).ShouldBeEmpty();
    }

    [Fact]
    public void MapFilterResx_everyCountAndTheLineOfNoneTicked_hasAText()
    {
        Resources.Missing([.. Totals.Select(MapFilterText.Count), MapFilterText.AllHidden]).ShouldBeEmpty();
    }

    // A kind's name is the kind's, and the count after it says what hiding it would take off the map.
    [Fact]
    public void Name_eachKind_isWhatTheMapCallsIt()
    {
        Enum.GetValues<MapLinkKind>().Select(kind => Resources.Get(MapFilterText.Name(kind))).ShouldBe(["Money passed on", "Money into a bucket", "Tax base", "Conditions"]);
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
            .Concat(Totals.Select(MapFilterText.Count)).Concat([MapFilterText.Legend, MapFilterText.AllHidden]);

        Resources.Keys.Except(used).ShouldBeEmpty();
    }

    [Fact]
    public void Name_aKindItHasNoTextFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapFilterText.Name((MapLinkKind)99));
    }
}

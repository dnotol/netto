using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The words of the legend of the kinds of link, and of the line under a chosen module's links that counts
// the ones the filter hides, in MapFilter.resx.
public class MapFilterTextTests
{
    // The name IStringLocalizer<MapFilter> reads Tabs/MapFilter.resx under.
    private static readonly ResxText Resources = ResxText.For<MapFilter>();

    private static readonly int[] Totals = [0, 1, 2];

    private static readonly int[] HiddenCounts = [1, 2, 9];

    private static MapFilterView View(bool allHidden, int total)
    {
        var filter = new MapLinkFilter(new BrowserStorage());
        if (allHidden)
        {
            foreach (var kind in Enum.GetValues<MapLinkKind>())
                filter.Set(kind, shown: false);
        }

        return MapFilterView.Of(filter, new Dictionary<MapLinkKind, int> { [MapLinkKind.PassedOn] = total });
    }

    [Fact]
    public void MapFilterResx_everyKind_hasANameAndWhatItIs()
    {
        var keys = Enum.GetValues<MapLinkKind>().SelectMany(kind => new[] { MapFilterText.Name(kind), MapFilterText.Gloss(kind) }).Append(MapFilterText.Legend);

        Resources.Missing(keys).ShouldBeEmpty();
    }

    [Fact]
    public void MapFilterResx_everyCountSummaryAndHiddenLine_hasAText()
    {
        var keys = Totals.Select(MapFilterText.Count)
            .Concat(Totals.Select(total => MapFilterText.Summary(View(allHidden: false, total))).OfType<string>())
            .Append(MapFilterText.Summary(View(allHidden: true, 0))!)
            .Concat(HiddenCounts.Select(MapFilterText.Hidden));

        Resources.Missing(keys).ShouldBeEmpty();
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

    [Fact]
    public void Summary_linksShown_saysHowManyOfThePlansLinks()
    {
        (Resources.Format(MapFilterText.Summary(View(allHidden: false, 1))!, 1, 1), Resources.Format(MapFilterText.Summary(View(allHidden: false, 38))!, 38, 38))
            .ShouldBe(("Showing 1 of 1 link", "Showing 38 of 38 links"));
    }

    // A plan with no link has nothing to count: the line is left out. Every kind hidden says so instead,
    // whatever the plan holds.
    [Fact]
    public void Summary_noLinkAtAll_isNone()
    {
        MapFilterText.Summary(View(allHidden: false, 0)).ShouldBeNull();
    }

    [Fact]
    public void Summary_everyKindHidden_saysToTickOne()
    {
        Resources.Get(MapFilterText.Summary(View(allHidden: true, 12))!).ShouldBe("Every kind of link is hidden: tick one to see its links.");
    }

    // The chosen module's list says how many of its links the filter hides, so an empty list is never read
    // as a module with no conditions.
    [Fact]
    public void Hidden_oneAndMany_saysHowManyLinksTheKindsTurnedOffHide()
    {
        (Resources.Format(MapFilterText.Hidden(1)), Resources.Format(MapFilterText.Hidden(3), 3))
            .ShouldBe(("1 link hidden by the kinds you turned off", "3 links hidden by the kinds you turned off"));
    }

    [Fact]
    public void Hidden_none_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapFilterText.Hidden(0));
    }

    [Fact]
    public void Name_aKindItHasNoTextFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapFilterText.Name((MapLinkKind)99));
    }
}

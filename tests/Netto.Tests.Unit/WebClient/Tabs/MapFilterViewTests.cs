using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// What the legend of the kinds of link draws: a row for every kind, in the order of the kinds, whatever the
// plan holds, with how many links the plan has of it and whether it is shown; and how many of the plan's
// links are shown.
public class MapFilterViewTests
{
    private static Dictionary<MapLinkKind, int> Counts(int passedOn, int intoBucket, int taxBase, int condition) =>
        new Dictionary<MapLinkKind, int>
        {
            [MapLinkKind.PassedOn] = passedOn,
            [MapLinkKind.IntoBucket] = intoBucket,
            [MapLinkKind.TaxBase] = taxBase,
            [MapLinkKind.Condition] = condition,
        };

    [Fact]
    public void Of_everyKind_isARowInTheOrderOfTheKindsWithItsLinksAndWhetherItIsShown()
    {
        var filter = new MapLinkFilter(new BrowserStorage());
        filter.Set(MapLinkKind.TaxBase, shown: false);

        var view = MapFilterView.Of(filter, Counts(10, 14, 6, 4));

        view.Rows.Select(row => (row.Kind, row.Count, row.Shown)).ShouldBe(
            [(MapLinkKind.PassedOn, 10, true), (MapLinkKind.IntoBucket, 14, true), (MapLinkKind.TaxBase, 6, false), (MapLinkKind.Condition, 4, true)]);
    }

    // A kind the plan has no link of is still a row, counted as none.
    [Fact]
    public void Of_aKindTheCountsDoNotName_isARowOfNone()
    {
        var view = MapFilterView.Of(new MapLinkFilter(new BrowserStorage()), new Dictionary<MapLinkKind, int> { [MapLinkKind.PassedOn] = 3 });

        view.Rows.Select(row => (row.Kind, row.Count)).ShouldBe(
            [(MapLinkKind.PassedOn, 3), (MapLinkKind.IntoBucket, 0), (MapLinkKind.TaxBase, 0), (MapLinkKind.Condition, 0)]);
    }

    [Fact]
    public void Of_nothingHidden_showsEveryLink()
    {
        var view = MapFilterView.Of(new MapLinkFilter(new BrowserStorage()), Counts(10, 14, 6, 4));

        (view.Shown, view.Total, view.AllHidden).ShouldBe((34, 34, false));
    }

    [Fact]
    public void Of_kindsHidden_showsTheLinksOfTheOthers()
    {
        var filter = new MapLinkFilter(new BrowserStorage());
        filter.Set(MapLinkKind.IntoBucket, shown: false);
        filter.Set(MapLinkKind.Condition, shown: false);

        var view = MapFilterView.Of(filter, Counts(10, 14, 6, 4));

        (view.Shown, view.Total, view.AllHidden).ShouldBe((16, 34, false));
    }

    // Every kind hidden, even those the plan has no link of: the viewer is told to tick one.
    [Fact]
    public void Of_everyKindHidden_isAllHiddenWhateverThePlanHolds()
    {
        var filter = new MapLinkFilter(new BrowserStorage());
        foreach (var kind in Enum.GetValues<MapLinkKind>())
            filter.Set(kind, shown: false);

        var view = MapFilterView.Of(filter, Counts(0, 0, 0, 0));

        (view.Shown, view.Total, view.AllHidden).ShouldBe((0, 0, true));
    }
}

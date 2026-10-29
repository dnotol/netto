using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// What the legend of the kinds of link draws: a row for every kind, in the order of the kinds, whatever the
// plan holds, with how many links the plan has of it and whether it is shown; and whether the viewer hid
// them all.
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
        var filter = new MapLinkFilter(EveryKindShown());
        filter.Set(MapLinkKind.TaxBase, shown: false);

        var view = MapFilterView.Of(filter, Counts(10, 14, 6, 4));

        view.Rows.Select(row => (row.Kind, row.Count, row.Shown)).ShouldBe(
            [(MapLinkKind.PassedOn, 10, true), (MapLinkKind.IntoBucket, 14, true), (MapLinkKind.TaxBase, 6, false), (MapLinkKind.Condition, 4, true)]);
    }

    // The first visit, nothing stored: the tax bases and the conditions are rows unticked, the money ticked.
    [Fact]
    public void Of_nothingStored_isTheMoneyShownAndTheRulesNot()
    {
        var view = MapFilterView.Of(new MapLinkFilter(new BrowserStorage()), Counts(10, 14, 6, 4));

        view.Rows.Select(row => (row.Kind, row.Shown)).ShouldBe(
            [(MapLinkKind.PassedOn, true), (MapLinkKind.IntoBucket, true), (MapLinkKind.TaxBase, false), (MapLinkKind.Condition, false)]);

    }

    // A kind the plan has no link of is still a row, counted as none.
    [Fact]
    public void Of_aKindTheCountsDoNotName_isARowOfNone()
    {
        var view = MapFilterView.Of(new MapLinkFilter(EveryKindShown()), new Dictionary<MapLinkKind, int> { [MapLinkKind.PassedOn] = 3 });

        view.Rows.Select(row => (row.Kind, row.Count)).ShouldBe(
            [(MapLinkKind.PassedOn, 3), (MapLinkKind.IntoBucket, 0), (MapLinkKind.TaxBase, 0), (MapLinkKind.Condition, 0)]);
    }

    // Every kind hidden, even those the plan has no link of: the viewer is told to tick one.
    [Fact]
    public void Of_everyKindHidden_isAllHiddenWhateverThePlanHolds()
    {
        var filter = new MapLinkFilter(EveryKindShown());
        foreach (var kind in Enum.GetValues<MapLinkKind>())
            filter.Set(kind, shown: false);

        MapFilterView.Of(filter, Counts(0, 0, 0, 0)).AllHidden.ShouldBeTrue();
    }

    [Fact]
    public void Of_aKindShown_isNotAllHidden()
    {
        var filter = new MapLinkFilter(EveryKindShown());
        foreach (var kind in Enum.GetValues<MapLinkKind>().Where(kind => kind != MapLinkKind.IntoBucket))
            filter.Set(kind, shown: false);

        MapFilterView.Of(filter, Counts(10, 14, 6, 4)).AllHidden.ShouldBeFalse();
    }
}

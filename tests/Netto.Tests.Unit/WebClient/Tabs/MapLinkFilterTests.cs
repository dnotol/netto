using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Which kinds of link the Map tab shows is a viewer's preference, kept in the browser's storage as the
// kinds they hid: a new MapLinkFilter over the same storage is the page after a reload. Every kind shows
// until the viewer hides it, so a kind the map gains later shows too, and what Netto cannot read in the
// storage is no choice. What it shows of a map is the links of the kinds shown, the others kept apart as
// hidden, the modules and the lines that stay where they were.
public class MapLinkFilterTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    public static TheoryData<MapLinkKind> Kinds
    {
        get
        {
            var kinds = new TheoryData<MapLinkKind>();
            foreach (var kind in Enum.GetValues<MapLinkKind>())
                kinds.Add(kind);
            return kinds;
        }
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Shows_nothingStored_everyKindShows(MapLinkKind kind)
    {
        new MapLinkFilter(new BrowserStorage()).Shows(kind).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Set_aKindHidden_hidesOnlyIt(MapLinkKind hidden)
    {
        var filter = new MapLinkFilter(new BrowserStorage());

        filter.Set(hidden, shown: false);

        Enum.GetValues<MapLinkKind>().Where(filter.Shows).ShouldBe(Enum.GetValues<MapLinkKind>().Where(kind => kind != hidden));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Set_aKindHidden_staysHiddenAfterAReload(MapLinkKind hidden)
    {
        var storage = new BrowserStorage();
        new MapLinkFilter(storage).Set(hidden, shown: false);

        var reloaded = new MapLinkFilter(storage);

        Enum.GetValues<MapLinkKind>().Where(reloaded.Shows).ShouldBe(Enum.GetValues<MapLinkKind>().Where(kind => kind != hidden));
    }

    [Fact]
    public void Set_hiddenThenShownAgain_showsEveryKindAfterAReload()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);
        filter.Set(MapLinkKind.TaxBase, shown: false);
        filter.Set(MapLinkKind.Condition, shown: false);
        filter.Set(MapLinkKind.TaxBase, shown: true);
        filter.Set(MapLinkKind.Condition, shown: true);

        var reloaded = new MapLinkFilter(storage);

        Enum.GetValues<MapLinkKind>().Select(reloaded.Shows).ShouldAllBe(shown => shown);
    }

    [Fact]
    public void Set_severalKinds_keepsTheOthersHidden()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);
        filter.Set(MapLinkKind.PassedOn, shown: false);
        filter.Set(MapLinkKind.Condition, shown: false);
        filter.Set(MapLinkKind.PassedOn, shown: true);

        var reloaded = new MapLinkFilter(storage);

        Enum.GetValues<MapLinkKind>().Select(kind => (kind, reloaded.Shows(kind))).ShouldBe(
            [(MapLinkKind.PassedOn, true), (MapLinkKind.IntoBucket, true), (MapLinkKind.TaxBase, true), (MapLinkKind.Condition, false)]);
    }

    // The format is the words of the hidden kinds in the order of the kinds, none when none is hidden.
    [Fact]
    public void Set_hidingKinds_storesTheirWordsInTheOrderOfTheKinds()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);

        filter.Set(MapLinkKind.Condition, shown: false);
        filter.Set(MapLinkKind.PassedOn, shown: false);

        storage.Get(MapLinkFilter.StorageKey).ShouldBe("passed_on;condition");
    }

    [Fact]
    public void Set_everyKindShownAgain_storesNoWords()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);
        filter.Set(MapLinkKind.TaxBase, shown: false);

        filter.Set(MapLinkKind.TaxBase, shown: true);

        storage.Get(MapLinkFilter.StorageKey).ShouldBe("");
    }

    // The words are what is stored, so they are not renamed with the kinds: a stored choice would be lost.
    [Theory]
    [InlineData(MapLinkKind.PassedOn, "passed_on")]
    [InlineData(MapLinkKind.IntoBucket, "into_bucket")]
    [InlineData(MapLinkKind.TaxBase, "tax_base")]
    [InlineData(MapLinkKind.Condition, "condition")]
    public void Word_eachKind_isItsStoredWord(MapLinkKind kind, string expected)
    {
        MapLinkFilter.Word(kind).ShouldBe(expected);
    }

    [Fact]
    public void Word_everyKind_hasOneOfItsOwn()
    {
        Enum.GetValues<MapLinkKind>().Select(MapLinkFilter.Word).Distinct().Count().ShouldBe(Enum.GetValues<MapLinkKind>().Length);
    }

    [Fact]
    public void Word_aKindItHasNoWordFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapLinkFilter.Word((MapLinkKind)99));
    }

    // A choice already made is kept as it is, not written again.
    [Fact]
    public void Set_aChoiceAlreadyMade_writesNothing()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);
        filter.Set(MapLinkKind.TaxBase, shown: false);
        var writes = storage.Writes;

        filter.Set(MapLinkKind.TaxBase, shown: false);
        filter.Set(MapLinkKind.Condition, shown: true);

        storage.Writes.ShouldBe(writes);
    }

    // Whatever else sits under Netto's key, from an older version or a hand edit, is no choice: every kind
    // shows.
    [Theory]
    [InlineData("")]
    [InlineData("junk")]
    [InlineData("{\"hidden\":[\"tax_base\"]}")]
    [InlineData("Tax_Base")]
    [InlineData("TaxBase")]
    [InlineData(" tax_base")]
    [InlineData("tax_base=1")]
    [InlineData(";;;")]
    [InlineData("42")]
    public void Shows_storedValueNotAChoice_everyKindShows(string stored)
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, stored);

        var filter = new MapLinkFilter(storage);

        Enum.GetValues<MapLinkKind>().Select(filter.Shows).ShouldAllBe(shown => shown);
    }

    // A word this version does not know, a kind written by another version, is ignored, the others kept.
    [Fact]
    public void Shows_storedValueHoldsAnUnknownKind_ignoresItAndKeepsTheOthers()
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, "tax_base;a_kind_to_come;;condition;tax_base");

        var filter = new MapLinkFilter(storage);

        Enum.GetValues<MapLinkKind>().Select(kind => (kind, filter.Shows(kind))).ShouldBe(
            [(MapLinkKind.PassedOn, true), (MapLinkKind.IntoBucket, true), (MapLinkKind.TaxBase, false), (MapLinkKind.Condition, false)]);
    }

    // A private window or blocked site data: nothing is kept across a reload, but the choice holds while the
    // page is open.
    [Fact]
    public void Set_storageRefused_choiceHoldsUntilTheReload()
    {
        var storage = new RefusingStorage();
        var filter = new MapLinkFilter(storage);

        filter.Set(MapLinkKind.TaxBase, shown: false);

        (filter.Shows(MapLinkKind.TaxBase), new MapLinkFilter(storage).Shows(MapLinkKind.TaxBase)).ShouldBe((false, true));
    }

    // The plan holds money passed on, money into a bucket and a tax base's links, so hiding any of those
    // kinds hides something below.
    [Fact]
    public void Full_theMapOfEveryLever_holdsLinksOfTheKindsTheFilterHidesBelow()
    {
        var kinds = MapView.From(Full, Full.Run()).Links.Select(link => link.Kind).Distinct().ToList();

        kinds.ShouldSatisfyAllConditions(
            () => kinds.ShouldContain(MapLinkKind.PassedOn),
            () => kinds.ShouldContain(MapLinkKind.IntoBucket),
            () => kinds.ShouldContain(MapLinkKind.TaxBase));
    }

    [Fact]
    public void Showing_nothingHidden_isTheMapAsItIs()
    {
        var view = MapView.From(Full, Full.Run());

        var shown = new MapLinkFilter(new BrowserStorage()).Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Links.ShouldBe(view.Links),
            () => shown.Hidden.ShouldBeEmpty());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Showing_aKindHidden_keepsItsLinksApartAndTheOthersInOrder(MapLinkKind hidden)
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(new BrowserStorage());
        filter.Set(hidden, shown: false);

        var shown = filter.Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Links.ShouldBe(view.Links.Where(link => link.Kind != hidden)),
            () => shown.Hidden.ShouldBe(view.Links.Where(link => link.Kind == hidden)));
    }

    [Fact]
    public void Showing_everyKindHidden_showsNoLinkAndKeepsThemAll()
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(new BrowserStorage());
        foreach (var kind in Enum.GetValues<MapLinkKind>())
            filter.Set(kind, shown: false);

        var shown = filter.Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Links.ShouldBeEmpty(),
            () => shown.Hidden.ShouldBe(view.Links));
    }

    // Hiding moves nothing: the modules, the buckets and the routes of the lines left are the map's.
    [Fact]
    public void Showing_aKindHidden_movesNothing()
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(new BrowserStorage());
        filter.Set(MapLinkKind.IntoBucket, shown: false);

        var shown = filter.Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Modules.ShouldBe(view.Modules),
            () => shown.Buckets.ShouldBe(view.Buckets),
            () => shown.Units.ShouldBe(view.Units),
            () => shown.Links.Select(link => link.Route).ShouldBe(view.Links.Where(link => link.Kind != MapLinkKind.IntoBucket).Select(link => link.Route)));
    }

    // What the map draws of the lines, and the words under a card's lines, are the shown links': a hidden
    // kind is in no line and no word.
    [Fact]
    public void Showing_aKindHidden_drawsNoLineAndNoWordOfIt()
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(new BrowserStorage());
        filter.Set(MapLinkKind.TaxBase, shown: false);

        var shown = filter.Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Bundles().Select(bundle => bundle.Kind).Distinct().ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Blocks().SelectMany(block => block.Bundles).Select(bundle => bundle.Kind).Distinct().ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Drawn(RiverModule.Car).Select(line => line.Bundle.Kind).Distinct().ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Bundles().ShouldNotBeEmpty());
    }

    // A module chosen keeps its links subject to the filter: what it lists is the shown ones, and what is
    // hidden is still its own, to be counted.
    [Fact]
    public void Showing_aModuleChosen_listsItsShownLinksAndCountsItsHiddenOnes()
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(new BrowserStorage());
        filter.Set(MapLinkKind.TaxBase, shown: false);

        var shown = filter.Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => view.LinksOf(RiverModule.Car).Any(link => link.Kind == MapLinkKind.TaxBase).ShouldBeTrue(),
            () => shown.LinksOf(RiverModule.Car).ShouldBe(view.LinksOf(RiverModule.Car).Where(link => link.Kind != MapLinkKind.TaxBase)),
            () => shown.HiddenOf(RiverModule.Car).ShouldBe(view.LinksOf(RiverModule.Car).Where(link => link.Kind == MapLinkKind.TaxBase)),
            () => shown.HiddenOf(RiverModule.Car).Any().ShouldBeTrue());
    }

    // An engine's refusal leaves the map without a link, so there is nothing to hide or show.
    [Fact]
    public void Showing_aRefusedAmount_hasNoLinkToShowOrHide()
    {
        var refused = Full.WithReserve(-1m);
        var filter = new MapLinkFilter(new BrowserStorage());
        filter.Set(MapLinkKind.TaxBase, shown: false);

        var shown = filter.Showing(MapView.From(refused, refused.Run()));

        shown.ShouldSatisfyAllConditions(
            () => shown.Links.ShouldBeEmpty(),
            () => shown.Hidden.ShouldBeEmpty());
    }

    // What the legend counts is the plan's links of each kind, whatever is hidden, and a kind the plan has
    // none of is counted as none.
    [Fact]
    public void Counts_theMapOfEveryLever_isTheLinksOfEachKind()
    {
        var view = MapView.From(Full, Full.Run());

        var counts = MapLinkFilter.Counts(view);

        Enum.GetValues<MapLinkKind>().Select(kind => (kind, counts[kind])).ShouldBe(
            Enum.GetValues<MapLinkKind>().Select(kind => (kind, view.Links.Count(link => link.Kind == kind))));
    }

    // A refused amount leaves no link, so every kind is counted as none.
    [Fact]
    public void Counts_aRefusedAmount_isNoneOfEachKind()
    {
        var refused = Full.WithReserve(-1m);

        var counts = MapLinkFilter.Counts(MapView.From(refused, refused.Run()));

        Enum.GetValues<MapLinkKind>().Select(kind => counts[kind]).ShouldAllBe(count => count == 0);
    }

    [Fact]
    public void Counts_aViewAFilterHasShown_stillCountsTheHiddenLinks()
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(new BrowserStorage());
        filter.Set(MapLinkKind.IntoBucket, shown: false);

        var (shown, all) = (MapLinkFilter.Counts(filter.Showing(view)), MapLinkFilter.Counts(view));

        Enum.GetValues<MapLinkKind>().Select(kind => shown[kind]).ShouldBe(Enum.GetValues<MapLinkKind>().Select(kind => all[kind]));
    }
}

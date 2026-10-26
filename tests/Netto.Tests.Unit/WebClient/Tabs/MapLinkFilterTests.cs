using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// Which kinds of link the Map tab shows is a viewer's preference, kept in the browser's storage as the
// kinds they hid: a new MapLinkFilter over the same storage is the page after a reload. The map opens quiet,
// its rules marked on their modules and their lines left out, so with nothing stored the kinds that are tax
// bases and conditions are hidden and the others shown; what is stored, even nothing, is the choice, whole.
// A key of an earlier version is no choice. What the filter shows of a map is the links of the kinds shown,
// the others kept apart as hidden, the modules, their badges and the lines that stay where they were.
public class MapLinkFilterTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private static readonly MapLinkKind[] QuietKinds = [MapLinkKind.TaxBase, MapLinkKind.Condition];

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

    // The kinds a filter shows, in the order of the kinds, and the same as a line of words for a test that
    // sets them beside other figures.
    private static List<MapLinkKind> Shown(MapLinkFilter filter) => [.. Enum.GetValues<MapLinkKind>().Where(filter.Shows)];

    private static string ShownWords(MapLinkFilter filter) => string.Join(' ', Shown(filter));

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Shows_nothingStored_theMoneyShowsAndTheRulesAreQuiet(MapLinkKind kind)
    {
        new MapLinkFilter(new BrowserStorage()).Shows(kind).ShouldBe(!QuietKinds.Contains(kind));
    }

    // The key names the lines, not the links they once were, and the format is the same.
    [Fact]
    public void StorageKey_isTheLinesKey()
    {
        MapLinkFilter.StorageKey.ShouldBe("netto.map.lines");
    }

    // The key of the first version is read by no one, written by no one, and left where it was: a viewer who
    // chose under it meets the quiet map once.
    [Theory]
    [InlineData("")]
    [InlineData("passed_on")]
    [InlineData("tax_base;condition")]
    public void Shows_theKeyOfTheEarlierVersion_isNoChoice(string stored)
    {
        var storage = new BrowserStorage();
        storage.Set("netto.map.links", stored);
        var writes = storage.Writes;

        var filter = new MapLinkFilter(storage);

        (ShownWords(filter), storage.Writes, storage.Get("netto.map.links")).ShouldBe(("PassedOn IntoBucket", writes, stored));
    }

    [Fact]
    public void Set_withTheEarlierKeyStored_leavesItAsItWas()
    {
        var storage = new BrowserStorage();
        storage.Set("netto.map.links", "passed_on");

        new MapLinkFilter(storage).Set(MapLinkKind.TaxBase, shown: true);

        (storage.Get("netto.map.links"), storage.Get(MapLinkFilter.StorageKey)).ShouldBe(("passed_on", "condition"));
    }

    // Stored, even nothing, is the choice: the viewer ticked every box.
    [Fact]
    public void Shows_nothingHiddenStored_everyKindShows()
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, "");

        Shown(new MapLinkFilter(storage)).ShouldBe(Enum.GetValues<MapLinkKind>());
    }

    // The first tick is the whole of the viewer's choice with it: the other quiet kind stays hidden.
    [Fact]
    public void Set_aQuietKindShown_storesTheOtherAsHiddenStill()
    {
        var storage = new BrowserStorage();

        new MapLinkFilter(storage).Set(MapLinkKind.TaxBase, shown: true);

        (storage.Get(MapLinkFilter.StorageKey), ShownWords(new MapLinkFilter(storage))).ShouldBe(("condition", "PassedOn IntoBucket TaxBase"));
    }

    // Hiding money stores the quiet kinds with it, in the order of the kinds, since what is stored is whole.
    [Fact]
    public void Set_aKindHidden_storesItWithTheQuietKinds()
    {
        var storage = new BrowserStorage();

        new MapLinkFilter(storage).Set(MapLinkKind.PassedOn, shown: false);

        (storage.Get(MapLinkFilter.StorageKey), ShownWords(new MapLinkFilter(storage))).ShouldBe(("passed_on;tax_base;condition", "IntoBucket"));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Set_aKindShown_staysShownAfterAReload(MapLinkKind shown)
    {
        var storage = new BrowserStorage();
        new MapLinkFilter(storage).Set(shown, shown: true);

        Shown(new MapLinkFilter(storage)).ShouldBe(Enum.GetValues<MapLinkKind>().Where(kind => kind == shown || !QuietKinds.Contains(kind)));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Set_aKindHidden_staysHiddenAfterAReload(MapLinkKind hidden)
    {
        var storage = new BrowserStorage();
        new MapLinkFilter(storage).Set(hidden, shown: false);

        Shown(new MapLinkFilter(storage)).ShouldBe(Enum.GetValues<MapLinkKind>().Where(kind => kind != hidden && !QuietKinds.Contains(kind)));
    }

    [Fact]
    public void Set_everyKindShown_storesNoWordsAndStaysSo()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);

        foreach (var kind in QuietKinds)
            filter.Set(kind, shown: true);

        (storage.Get(MapLinkFilter.StorageKey), ShownWords(new MapLinkFilter(storage))).ShouldBe(("", "PassedOn IntoBucket TaxBase Condition"));
    }

    // The format is the words of the hidden kinds in the order of the kinds.
    [Fact]
    public void Set_hidingKinds_storesTheirWordsInTheOrderOfTheKinds()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);

        filter.Set(MapLinkKind.Condition, shown: true);
        filter.Set(MapLinkKind.IntoBucket, shown: false);
        filter.Set(MapLinkKind.PassedOn, shown: false);

        storage.Get(MapLinkFilter.StorageKey).ShouldBe("passed_on;into_bucket;tax_base");
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

    // A choice already made is kept as it is, not written again: a rule's kind hidden is the choice that
    // nothing stored makes.
    [Fact]
    public void Set_aChoiceAlreadyMade_writesNothing()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);

        filter.Set(MapLinkKind.TaxBase, shown: false);
        filter.Set(MapLinkKind.PassedOn, shown: true);

        storage.Writes.ShouldBe(0);
    }

    [Fact]
    public void Set_aChoiceMadeTwice_writesItOnce()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);
        filter.Set(MapLinkKind.TaxBase, shown: true);

        filter.Set(MapLinkKind.TaxBase, shown: true);

        storage.Writes.ShouldBe(1);
    }

    // Something under Netto's key that names no kind, from another version or a hand edit, is no choice
    // either: the map opens quiet.
    [Theory]
    [InlineData("junk")]
    [InlineData("{\"hidden\":[\"tax_base\"]}")]
    [InlineData("Tax_Base")]
    [InlineData("TaxBase")]
    [InlineData(" tax_base")]
    [InlineData("tax_base=1")]
    [InlineData("42")]
    public void Shows_storedValueNamingNoKind_theMapOpensQuiet(string stored)
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, stored);

        Shown(new MapLinkFilter(storage)).ShouldBe([MapLinkKind.PassedOn, MapLinkKind.IntoBucket]);
    }

    // Only separators stored says as little as an empty value does, and is read as it: every kind shows.
    [Fact]
    public void Shows_storedValueOfSeparatorsOnly_isTheChoiceOfNoneHidden()
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, ";;;");

        Shown(new MapLinkFilter(storage)).ShouldBe(Enum.GetValues<MapLinkKind>());
    }

    // A word this version does not know, a kind written by another version, is ignored, the others kept.
    [Fact]
    public void Shows_storedValueHoldsAnUnknownKind_ignoresItAndKeepsTheOthers()
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, "into_bucket;a_kind_to_come;;condition;into_bucket");

        Shown(new MapLinkFilter(storage)).ShouldBe([MapLinkKind.PassedOn, MapLinkKind.TaxBase]);
    }

    // A private window or blocked site data: nothing is kept across a reload, but the choice holds while the
    // page is open.
    [Fact]
    public void Set_storageRefused_choiceHoldsUntilTheReload()
    {
        var storage = new RefusingStorage();
        var filter = new MapLinkFilter(storage);

        filter.Set(MapLinkKind.TaxBase, shown: true);

        (filter.Shows(MapLinkKind.TaxBase), new MapLinkFilter(storage).Shows(MapLinkKind.TaxBase)).ShouldBe((true, false));
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
    public void Showing_everyKindShown_isTheMapAsItIs()
    {
        var view = MapView.From(Full, Full.Run());

        var shown = new MapLinkFilter(EveryKindShown()).Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Links.ShouldBe(view.Links),
            () => shown.Hidden.ShouldBeEmpty());
    }

    // The first visit: the lines of the rules are kept apart, so no line, arrowhead or word is drawn for
    // them, and the badges on the modules they reach are the map's, untouched.
    [Fact]
    public void Showing_nothingStored_keepsTheRulesLinesApartAndTheirBadges()
    {
        var view = MapView.From(Full, Full.Run());

        var shown = new MapLinkFilter(new BrowserStorage()).Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Links.ShouldBe(view.Links.Where(link => !QuietKinds.Contains(link.Kind))),
            () => shown.Hidden.ShouldBe(view.Links.Where(link => QuietKinds.Contains(link.Kind))),
            () => shown.Hidden.Select(link => link.Kind).Distinct().Order().ShouldBe(QuietKinds.Order()),
            () => shown.Modules.SelectMany(node => node.Badges).ShouldBe(view.Modules.SelectMany(node => node.Badges)),
            () => shown.Modules.SelectMany(node => node.Badges).ShouldNotBeEmpty());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Showing_aKindHidden_keepsItsLinksApartAndTheOthersInOrder(MapLinkKind hidden)
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(EveryKindShown());
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
        var filter = new MapLinkFilter(EveryKindShown());
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
        var filter = new MapLinkFilter(EveryKindShown());
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
        var filter = new MapLinkFilter(EveryKindShown());
        filter.Set(MapLinkKind.TaxBase, shown: false);

        var shown = filter.Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Bundles().Select(bundle => bundle.Kind).Distinct().ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Blocks(RiverModule.Car).SelectMany(block => block.Bundles).Select(bundle => bundle.Kind).Distinct().ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Drawn(RiverModule.Car).Select(line => line.Bundle.Kind).Distinct().ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Bundles().ShouldNotBeEmpty());
    }

    // A module chosen keeps its links subject to the filter: what it lists is the shown ones, and what is
    // hidden is still its own, to be counted.
    [Fact]
    public void Showing_aModuleChosen_listsItsShownLinksAndCountsItsHiddenOnes()
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(EveryKindShown());
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

        var shown = new MapLinkFilter(new BrowserStorage()).Showing(MapView.From(refused, refused.Run()));

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

    // The links the first visit keeps apart are still counted: the legend says what ticking a kind brings.
    [Fact]
    public void Counts_aViewAFilterHasShown_stillCountsTheHiddenLinks()
    {
        var view = MapView.From(Full, Full.Run());

        var (shown, all) = (MapLinkFilter.Counts(new MapLinkFilter(new BrowserStorage()).Showing(view)), MapLinkFilter.Counts(view));

        Enum.GetValues<MapLinkKind>().Select(kind => shown[kind]).ShouldBe(Enum.GetValues<MapLinkKind>().Select(kind => all[kind]));
    }
}

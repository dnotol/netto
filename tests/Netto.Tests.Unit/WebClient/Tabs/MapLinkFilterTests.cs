using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.Tabs.MapRendering;

namespace Netto.Tests.WebClient.Tabs;

// Which kinds of link the Map tab shows is a viewer's preference, kept in the browser's storage as the
// kinds they hid: a new MapLinkFilter over the same storage is the page after a reload. The rules are
// marked on their modules, so with nothing stored the conditions and the tax bases are hidden by default,
// their totals said by their badges, the partner's quotient on its line once ticked (operator, 29.10.2026),
// and the money shown; what is stored, even nothing, is the choice, whole, a choice saved before the tax
// bases were hidden by default read as saved. A key of an earlier
// version is no choice. What the filter shows of a map is the links of the kinds shown, the others left
// out, the modules, their badges and the lines that stay where they were.
public class MapLinkFilterTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private static readonly MapLinkKind[] HiddenByDefault = [MapLinkKind.TaxBase, MapLinkKind.Condition];

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
    public void Shows_nothingStored_theMoneyShowsAndTheRulesAreHidden(MapLinkKind kind)
    {
        new MapLinkFilter(new BrowserStorage()).Shows(kind).ShouldBe(!HiddenByDefault.Contains(kind));
    }

    // The key names the lines, not the links they once were, and the format is the same.
    [Fact]
    public void StorageKey_isTheLinesKey()
    {
        MapLinkFilter.StorageKey.ShouldBe("netto.map.lines");
    }

    // The key of the first version is read by no one, written by no one, and left where it was: a viewer who
    // chose under it meets the kinds hidden by default once.
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

    // A choice saved while a tax base's lines showed by default is the viewer's still: read as saved.
    [Theory]
    [InlineData("condition", "PassedOn IntoBucket TaxBase")]
    [InlineData("", "PassedOn IntoBucket TaxBase Condition")]
    [InlineData("into_bucket;condition", "PassedOn TaxBase")]
    public void Shows_aChoiceSavedWhileTheTaxBasesShowedByDefault_isReadAsSaved(string stored, string shown)
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, stored);

        ShownWords(new MapLinkFilter(storage)).ShouldBe(shown);
    }

    [Fact]
    public void Set_withTheEarlierKeyStored_leavesItAsItWas()
    {
        var storage = new BrowserStorage();
        storage.Set("netto.map.links", "passed_on");

        new MapLinkFilter(storage).Set(MapLinkKind.Condition, shown: true);

        (storage.Get("netto.map.links"), storage.Get(MapLinkFilter.StorageKey)).ShouldBe(("passed_on", "tax_base"));

    }

    // Stored, even nothing, is the choice: the viewer ticked every box.
    [Fact]
    public void Shows_nothingHiddenStored_everyKindShows()
    {
        var storage = new BrowserStorage();
        storage.Set(MapLinkFilter.StorageKey, "");

        Shown(new MapLinkFilter(storage)).ShouldBe(Enum.GetValues<MapLinkKind>());
    }

    // The first tick is the whole of the viewer's choice with it: showing a tax base keeps the conditions hidden.
    [Fact]
    public void Set_aTaxBaseShown_storesTheConditionsAsHiddenStill()
    {
        var storage = new BrowserStorage();

        new MapLinkFilter(storage).Set(MapLinkKind.TaxBase, shown: true);

        (storage.Get(MapLinkFilter.StorageKey), ShownWords(new MapLinkFilter(storage))).ShouldBe(("condition", "PassedOn IntoBucket TaxBase"));
    }

    // Hiding money stores the kinds hidden by default with it, in the order of the kinds, since what is stored is whole.
    [Fact]
    public void Set_aKindHidden_storesItWithTheKindsHiddenByDefault()
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

        Shown(new MapLinkFilter(storage)).ShouldBe(Enum.GetValues<MapLinkKind>().Where(kind => kind == shown || !HiddenByDefault.Contains(kind)));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Set_aKindHidden_staysHiddenAfterAReload(MapLinkKind hidden)
    {
        var storage = new BrowserStorage();
        new MapLinkFilter(storage).Set(hidden, shown: false);

        Shown(new MapLinkFilter(storage)).ShouldBe(Enum.GetValues<MapLinkKind>().Where(kind => kind != hidden && !HiddenByDefault.Contains(kind)));
    }

    [Fact]
    public void Set_everyKindShown_storesNoWordsAndStaysSo()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);

        foreach (var kind in HiddenByDefault)
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

        filter.Set(MapLinkKind.Condition, shown: false);
        filter.Set(MapLinkKind.PassedOn, shown: true);

        storage.Writes.ShouldBe(0);
    }

    [Fact]
    public void Set_aChoiceMadeTwice_writesItOnce()
    {
        var storage = new BrowserStorage();
        var filter = new MapLinkFilter(storage);
        filter.Set(MapLinkKind.Condition, shown: true);

        filter.Set(MapLinkKind.Condition, shown: true);

        storage.Writes.ShouldBe(1);
    }

    // Something under Netto's key that names no kind, from another version or a hand edit, is no choice
    // either: the kinds hidden by default are hidden.
    [Theory]
    [InlineData("junk")]
    [InlineData("{\"hidden\":[\"tax_base\"]}")]
    [InlineData("Tax_Base")]
    [InlineData("TaxBase")]
    [InlineData(" tax_base")]
    [InlineData("tax_base=1")]
    [InlineData("42")]
    public void Shows_storedValueNamingNoKind_theKindsHiddenByDefaultAreHidden(string stored)
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

        filter.Set(MapLinkKind.Condition, shown: true);

        (filter.Shows(MapLinkKind.Condition), new MapLinkFilter(storage).Shows(MapLinkKind.Condition)).ShouldBe((true, false));
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

        shown.Links.ShouldBe(view.Links);
    }

    // The first visit: the lines of the rules are left out, so no line, arrowhead or word is drawn for
    // them, and the badges on the modules they reach are the map's, untouched.
    [Fact]
    public void Showing_nothingStored_leavesTheRulesLinesOutAndKeepsTheirBadges()
    {
        var view = MapView.From(Full, Full.Run());

        var shown = new MapLinkFilter(new BrowserStorage()).Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Links.ShouldBe(view.Links.Where(link => !HiddenByDefault.Contains(link.Kind))),
            () => view.Links.Select(link => link.Kind).Distinct().Except(shown.Links.Select(link => link.Kind)).Order().ShouldBe(HiddenByDefault.Order()),
            () => shown.Modules.SelectMany(node => node.Badges).ShouldBe(view.Modules.SelectMany(node => node.Badges)),
            () => shown.Modules.SelectMany(node => node.Badges).ShouldNotBeEmpty());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Showing_aKindHidden_leavesItsLinksOutAndTheOthersInOrder(MapLinkKind hidden)
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(EveryKindShown());
        filter.Set(hidden, shown: false);

        var shown = filter.Showing(view);

        shown.Links.ShouldBe(view.Links.Where(link => link.Kind != hidden));
    }

    [Fact]
    public void Showing_everyKindHidden_showsNoLink()
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(EveryKindShown());
        foreach (var kind in Enum.GetValues<MapLinkKind>())
            filter.Set(kind, shown: false);

        var shown = filter.Showing(view);

        shown.Links.ShouldBeEmpty();
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

    // What the map draws of the lines, and the amounts on them, are the shown links': a hidden kind is in no
    // line and no amount.
    [Fact]
    public void Showing_aKindHidden_drawsNoLineAndNoAmountOfIt()
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(EveryKindShown());
        filter.Set(MapLinkKind.TaxBase, shown: false);

        var shown = filter.Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => shown.Bundles().Select(bundle => bundle.Kind).Distinct().ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Amounts(MapAmounts.Of(view, MapAmountsTests.Say)).Select(amount => amount.Bundle.Kind).Distinct().ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Drawn(RiverModule.Car).Select(line => line.Bundle.Kind).Distinct().ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Bundles().ShouldNotBeEmpty());
    }

    // The amounts are placed from every link of the run, so the amounts of a map a filter shows are those
    // placed, the hidden kind's left out, each where it was.
    [Fact]
    public void Showing_aKindHidden_movesNoAmountOfTheOthers()
    {
        var view = MapView.From(Full, Full.Run());
        var placed = MapAmounts.Of(view, MapAmountsTests.Say);
        var filter = new MapLinkFilter(EveryKindShown());
        filter.Set(MapLinkKind.IntoBucket, shown: false);

        var amounts = filter.Showing(view).Amounts(placed);

        amounts.ShouldBe(placed.Where(amount => amount.Bundle.Kind != MapLinkKind.IntoBucket));
    }

    // A module's links under the filter are the shown ones: a hidden kind is in none of them, though the
    // run's map had it among the module's own.
    [Fact]
    public void Showing_aKindHidden_leavesItOutOfAModulesLinks()
    {
        var view = MapView.From(Full, Full.Run());
        var filter = new MapLinkFilter(EveryKindShown());
        filter.Set(MapLinkKind.TaxBase, shown: false);

        var shown = filter.Showing(view);

        shown.ShouldSatisfyAllConditions(
            () => view.LinksOf(RiverModule.Car).Any(link => link.Kind == MapLinkKind.TaxBase).ShouldBeTrue(),
            () => shown.LinksOf(RiverModule.Car).ShouldBe(view.LinksOf(RiverModule.Car).Where(link => link.Kind != MapLinkKind.TaxBase)),
            () => shown.LinksOf(RiverModule.Car).Select(link => link.Kind).ShouldNotContain(MapLinkKind.TaxBase),
            () => shown.Links.Select(link => link.Kind).ShouldNotContain(MapLinkKind.TaxBase));
    }

    // An engine's refusal leaves the map without a link, so there is nothing to hide or show.
    [Fact]
    public void Showing_aRefusedAmount_hasNoLinkToShowOrHide()
    {
        var refused = Full.WithReserve(-1m);

        var shown = new MapLinkFilter(new BrowserStorage()).Showing(MapView.From(refused, refused.Run()));

        shown.Links.ShouldBeEmpty();
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

    // The legend is fed the run's own map, with every link, so that it says what ticking a kind brings: the
    // map a filter has shown holds only the links it shows, and counts only those.
    [Fact]
    public void Counts_aViewAFilterHasShown_countsOnlyTheLinksItKeeps()
    {
        var view = MapView.From(Full, Full.Run());
        var shown = new MapLinkFilter(new BrowserStorage()).Showing(view);

        var (ofShown, ofAll) = (MapLinkFilter.Counts(shown), MapLinkFilter.Counts(view));

        ofShown.ShouldSatisfyAllConditions(
            () => HiddenByDefault.Select(kind => ofAll[kind]).ShouldAllBe(count => count > 0),
            () => HiddenByDefault.Select(kind => ofShown[kind]).ShouldAllBe(count => count == 0),
            () => Enum.GetValues<MapLinkKind>().Except(HiddenByDefault).Select(kind => ofShown[kind]).ShouldBe(
                Enum.GetValues<MapLinkKind>().Except(HiddenByDefault).Select(kind => ofAll[kind])));
    }
}

using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The badges in the modules' boxes (MapBadges), read from every link of the run before a filter hides
// any: each condition one module sets on another that is lost marks the module it acts on; a rule that holds,
// the office rule and a tax base, which the dotted line and the Effects list say, mark none. Nothing here tests
// a rule or computes a tax figure: a badge says what the engine found.
public class MapBadgesTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private static readonly ResxText Resources = ResxText.For<MapTab>();

    // A module a tax base reaches has no badge for it, whatever the base gains: its dotted line says the amount when
    // the module is chosen and the Effects list beside the card says it in words. Every badge is a condition's.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Of_aModuleATaxBaseReaches_hasNoBadgeForIt(bool costlyCar)
    {
        var river = costlyCar ? MapViewConditionsTests.CostlyCar : Full;
        var links = Links(river);

        links.Where(link => link is { Kind: MapLinkKind.TaxBase } && link.To.Module != link.Card).Select(link => link.To.Module!.Value).Distinct()
            .ShouldBe([RiverModule.DirectorPay, RiverModule.CorporateTax], ignoreOrder: true);
        MapBadges.Of(links).Values.SelectMany(list => list).ShouldAllBe(badge => badge.Kind == MapLinkKind.Condition);
        MapView.From(river, river.Run(), MapNames.English).Modules.SelectMany(node => node.Badges).ShouldAllBe(badge => badge.Kind == MapLinkKind.Condition);
    }

    // Each condition lost marks the module it acts on with its own badge, in the engine's order: the car's benefits over 20% of the pay lose Corporate tax its rate.
    [Fact]
    public void Of_theConditionsLost_markTheModulesTheyActOn()
    {
        var links = Links(MapViewConditionsTests.CostlyCar);

        var badges = MapBadges.Of(links);

        badges.ShouldSatisfyAllConditions(
            () => badges.SelectMany(module => module.Value.Where(badge => badge.Kind == MapLinkKind.Condition).Select(badge => (module.Key, badge)))
                .ShouldBe(links.Where(link => link.Badge is not null).Select(link => (link.To.Module!.Value, link.Badge!)).Distinct(), ignoreOrder: true),
            () => badges[RiverModule.CorporateTax].Select(badge => (Resources.Format(badge.Words.Key, [.. badge.Words.Args]), badge.Tone))
                .ShouldContain(("rate lost", MapBadgeTone.Lost)));
    }

    // The office rule always holds: the rent it acts on has no badge.
    [Fact]
    public void Of_theOfficeRule_marksNoModule()
    {
        Links(Full).ShouldContain(link => link.Kind == MapLinkKind.Condition && link.To.Module == RiverModule.Rent);

        Of(Full).ShouldNotContainKey(RiverModule.Rent);
    }

    // The map draws each module's badges in its box, as MapBadges finds them.
    [Fact]
    public void From_aRun_givesEachModuleItsBadges()
    {
        var badges = Of(Full);

        var view = MapView.From(Full, Full.Run(), MapNames.English);

        view.Modules.Select(node => (node.End.Module!.Value, string.Join(" | ", node.Badges.Select(Shape))))
            .ShouldBe(MapView.ModulesOf(Full).Select(module => (module, string.Join(" | ", badges.GetValueOrDefault(module, []).Select(Shape)))));
    }

    // A badge by value, its words' arguments among them.
    private static string Shape(MapBadge badge) => $"{badge.Kind} {badge.Words.Text} {badge.Words.Key} {string.Join(',', badge.Words.Args)} {badge.Tone}";

    // A refused amount leaves the map without a link and so without a badge; the company skipped leaves no
    // year to read, and the partner's marital quotient, the one tax base of the pay alone, reaches the box of
    // the card that draws it, which marks nothing.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void From_aRunWithoutAYear_marksNoModule(bool refused)
    {
        var river = refused ? Full.WithReserve(-1m) : Full.SkipCompany();

        MapView.From(river, river.Run(), MapNames.English).Modules.ShouldAllBe(node => node.Badges.Count == 0);
    }

    // Whatever the plan holds and leaves empty, a quiet lever has no badge, nothing reaching it, and no
    // module more than the layout's tests lay the map out with (MapLayoutTests.MostBadges).
    [Theory]
    [MemberData(nameof(MapLayoutTests.Rivers), MemberType = typeof(MapLayoutTests))]
    public void From_anyPlan_marksNoQuietLeverAndNoModuleBeyondTheMostTheLayoutIsHeldTo(int bits, int quiet, bool companySkipped)
    {
        var river = MapLayoutTests.River(bits, quiet, companySkipped);

        MapView.From(river, river.Run(), MapNames.English).Modules.Where(node => node.Badges.Count > (node.Quiet ? 0 : MapLayoutTests.MostBadges.GetValueOrDefault(node.End.Module!.Value)))
            .Select(node => $"{node.End.Id} {node.Badges.Count}").ShouldBeEmpty();
    }

    [Fact]
    public void From_theCostlyCar_marksNoModuleBeyondTheMostTheLayoutIsHeldTo()
    {
        var river = MapViewConditionsTests.CostlyCar;

        MapView.From(river, river.Run(), MapNames.English).Modules.Where(node => node.Badges.Count > MapLayoutTests.MostBadges.GetValueOrDefault(node.End.Module!.Value))
            .Select(node => $"{node.End.Id} {node.Badges.Count}").ShouldBeEmpty();
    }

    // Every tone of badge has its class and an icon, a rule lost told by its icon as by its words, never by the
    // colour alone.
    [Fact]
    public void Style_everyTone_hasItsClassAndAnIconOfItsOwn()
    {
        var tones = Enum.GetValues<MapBadgeTone>();

        tones.ShouldSatisfyAllConditions(
            () => tones.Select(MapBadgeStyle.Class).Distinct().Count().ShouldBe(tones.Length),
            () => tones.Select(MapBadgeStyle.Icon).ShouldAllBe(icon => icon != null),
            () => tones.Select(MapBadgeStyle.Icon).OfType<string>().Distinct().Count().ShouldBe(tones.Length));
    }

    [Fact]
    public void Style_aToneItHasNoStyleFor_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapBadgeStyle.Class((MapBadgeTone)99));
        Should.Throw<ArgumentOutOfRangeException>(() => MapBadgeStyle.Icon((MapBadgeTone)99));
    }

    // What the stylesheet draws a badge with is what the layout keeps room for, a line of a box at the map's
    // smallest scale: a badge's height, its border in, and the room above it.
    [Fact]
    public void MapTabCss_drawsABadgeWithTheRoomTheLayoutKeepsForIt()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css");

        css.ShouldSatisfyAllConditions(
            () => Stylesheet.Block(css, ".map-badge").ShouldContain($"height: {Stylesheet.Rem(MapLayout.BadgeRem)};"),
            () => Stylesheet.Block(css, ".map-badge").ShouldContain("box-sizing: border-box;"),
            () => Stylesheet.Block(css, ".map-module__badges").ShouldContain($"gap: {Stylesheet.Rem(MapLayout.BadgeGapRem)};"),
            () => Stylesheet.Block(css, ".map-module__badges").ShouldContain($"margin-top: {Stylesheet.Rem(MapLayout.BadgeGapRem)};"),
            () => ((MapLayout.BadgeRem + MapLayout.BadgeGapRem) / MapLayout.SmallestRowRem).ShouldBe(0.3m));
    }

    // What the stylesheet draws a badge with is what the layout reckons its words' room by: the badges reaching
    // into the box's padding, the badge's own padding, and its icon and the gap after it.
    [Fact]
    public void MapTabCss_drawsABadgeWithThePaddingAndIconItsRoomIsReckonedBy()
    {
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css");

        css.ShouldSatisfyAllConditions(
            () => Declares(Stylesheet.Block(css, ".map-module__badges"), $"margin-right: -{Stylesheet.Rem(MapLayout.BadgeInsetRem)};").ShouldBeTrue(),
            () => Declares(Stylesheet.Block(css, ".map-module__badges"), $"margin-left: -{Stylesheet.Rem(MapLayout.BadgeInsetRem)};").ShouldBeTrue(),
            () => Declares(Stylesheet.Block(css, ".map-badge"), $"padding: 0 {Stylesheet.Rem(MapLayout.BadgePadRem)};").ShouldBeTrue(),
            () => Declares(Stylesheet.Block(css, ".map-badge"), $"gap: {Stylesheet.Rem(MapLayout.BadgeIconGapRem)};").ShouldBeTrue(),
            () => Declares(Stylesheet.Block(css, ".map-badge ::deep .mud-icon-root"), $"width: {Stylesheet.Rem(MapLayout.BadgeIconRem)};").ShouldBeTrue());
    }

    // Whether a block declares the declaration given on a line of its own, as a browser reads it: a declaration
    // after another on its line, behind an escape, is no declaration.
    private static bool Declares(string block, string declaration) =>
        System.Text.RegularExpressions.Regex.IsMatch(block, $@"(?m)^\s*{System.Text.RegularExpressions.Regex.Escape(declaration)}\s*$");

    // The room a badge's words have on the narrowest map: its box's column less the box's frame and what its
    // padding keeps of it each side, 0.1rem, the badge's frame and padding, and its icon and the gap after it
    // where it has one: Director pay's 90 px, the company's 108.
    [Theory]
    [InlineData(RiverModule.DirectorPay, false, 90 - 2 - 3.2 - 2 - 8)]
    [InlineData(RiverModule.CorporateTax, false, 108 - 2 - 3.2 - 2 - 8)]
    [InlineData(RiverModule.CorporateTax, true, 108 - 2 - 3.2 - 2 - 8 - 12 - 2.4)]
    [InlineData(RiverModule.Split, true, 108 - 2 - 3.2 - 2 - 8 - 12 - 2.4)]
    public void BadgeRoom_aModule_isItsColumnLessItsFramesPaddingsAndIcon(RiverModule module, bool icon, double room)
    {
        MapLayout.BadgeRoom(module, icon).ShouldBe((decimal)room);
    }

    // Every condition's badge reads whole in the box it marks on the narrowest map, its icon beside it, in English
    // and in French: no ellipsis.
    [Theory]
    [MemberData(nameof(MapConditionsTests.Badged), MemberType = typeof(MapConditionsTests))]
    public void Badge_aConditionsInEitherLanguage_readsWholeInTheBoxItMarks(YearCondition condition)
    {
        var link = MapConditions.Link(condition);
        var (badge, room) = (link.Badge.ShouldNotBeNull(), MapLayout.BadgeRoom(link.To.Module!.Value, icon: true));

        (MapGateTextTests.LineWidth(Resources.Get(badge.Words.Key).ShouldNotBeNull()) <= room, MapGateTextTests.LineWidth(ResxText.French<MapTab>().Get(badge.Words.Key).ShouldNotBeNull()) <= room)
            .ShouldBe((true, true));
    }

    // The links of the run, the conditions of its year between modules on the map among them, before a
    // filter hides any, and the badges they leave: what the tab's tests hold the buttons to.
    internal static IReadOnlyList<MapLink> Links(RiverInputs river)
    {
        var run = river.Run();
        return [.. MapLinks.Of(river, run), .. run.Year is { } year ? MapConditions.From(YearConditions.Of(year), MapView.ModulesOf(river)) : []];
    }

    internal static IReadOnlyDictionary<RiverModule, IReadOnlyList<MapBadge>> Of(RiverInputs river) => MapBadges.Of(Links(river));
}

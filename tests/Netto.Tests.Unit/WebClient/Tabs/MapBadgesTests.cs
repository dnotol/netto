using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The badges in the modules' boxes (MapBadges), read from every link of the run before a filter hides
// any: each condition one module sets on another marks the module it acts on, but the office rule, which
// always holds; each module a tax base reaches is marked once with what that base gains, the sum of the
// card lines reaching it, Corporate tax's the year's disallowed expenses. Nothing here tests a rule or
// computes a tax figure: a badge says what the engine found, a sum is the one a viewer makes of the lines.
public class MapBadgesTests
{
    private static readonly RiverInputs Full = MapLinksTests.Full;

    private static readonly ResxText Resources = ResxText.For<MapTab>();

    // The vouchers' and the car's lines Corporate tax adds back are one badge, the year's disallowed
    // expenses, with the costly car too, whose benefit is beyond 20% of the pay.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Of_everyLever_marksCorporateTaxOnceWithWhatItAddsBack(bool costlyCar)
    {
        var river = costlyCar ? MapViewConditionsTests.CostlyCar : Full;
        var (links, year) = (Links(river), river.Run().Year!);

        var badge = Of(river).GetValueOrDefault(RiverModule.CorporateTax, []).Where(badge => badge.Kind == MapLinkKind.TaxBase).ShouldHaveSingleItem();

        badge.ShouldSatisfyAllConditions(
            () => links.Count(link => link is { Kind: MapLinkKind.TaxBase, To.Module: RiverModule.CorporateTax }).ShouldBeGreaterThan(1),
            () => (badge.Words.Text, badge.Words.Key, string.Join('|', badge.Words.Args), badge.Tone)
                .ShouldBe((MapText.Map, MapTabText.TaxBaseBadge, Formats.Euros(year.DisallowedExpenses), MapBadgeTone.Amount)));
    }

    // The car's benefit, taxed with the pay, marks Director pay with its amount.
    [Fact]
    public void Of_aCarsBenefit_marksDirectorPayWithItsAmount()
    {
        var link = Links(Full).Single(link => link is { Kind: MapLinkKind.TaxBase, To.Module: RiverModule.DirectorPay });

        var badge = Of(Full).GetValueOrDefault(RiverModule.DirectorPay, []).ShouldHaveSingleItem();

        (badge.Kind, Resources.Format(badge.Words.Key, [.. badge.Words.Args]))
            .ShouldBe((MapLinkKind.TaxBase, $"+{Formats.Euros(link.Amount!.Value)} to tax base"));
    }

    // A module a tax base reaches with nothing is not marked.
    [Fact]
    public void Of_aTaxBaseOfNothing_marksNoModule()
    {
        MapLink[] links =
        [
            new(MapLinkKind.TaxBase, MapEnd.Of(RiverModule.Car), MapEnd.Of(RiverModule.DirectorPay), RiverModule.Car, new MapLabel(MapText.CarCard, "Figure_BenefitTaxed", []), 0m),
            new(MapLinkKind.TaxBase, MapEnd.Of(RiverModule.Vouchers), MapEnd.Of(RiverModule.CorporateTax), RiverModule.Vouchers, new MapLabel(MapText.VoucherCard, "Figure_AddedBack", []), 0.004m),
        ];

        MapBadges.Of(links).ShouldBeEmpty();
    }

    // Each condition marks the module it acts on with its own badge, in the engine's order, before what
    // that module's tax base gains: the car's benefits over 20% of the pay lose Corporate tax its rate.
    [Fact]
    public void Of_theConditions_markTheModulesTheyActOnBeforeWhatTheirTaxBaseGains()
    {
        var links = Links(MapViewConditionsTests.CostlyCar);

        var badges = MapBadges.Of(links);

        badges.ShouldSatisfyAllConditions(
            () => badges.SelectMany(module => module.Value.Where(badge => badge.Kind == MapLinkKind.Condition).Select(badge => (module.Key, badge)))
                .ShouldBe(links.Where(link => link.Badge is not null).Select(link => (link.To.Module!.Value, link.Badge!)), ignoreOrder: true),
            () => badges.Values.ShouldAllBe(list => list.SkipWhile(badge => badge.Kind == MapLinkKind.Condition).All(badge => badge.Kind == MapLinkKind.TaxBase)),
            () => badges[RiverModule.CorporateTax].Select(badge => (Resources.Format(badge.Words.Key, [.. badge.Words.Args]), badge.Tone))
                .ShouldContain(("benefits lost", MapBadgeTone.Lost)));
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

    // A badge tells a box what another module's card does to its tax base. The marital quotient to the partner
    // is the Household card's own line, its amount said on it: the partner's narrow box takes no badge, which
    // could not read whole there. From the partner, it reaches Director pay, whose box says it.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Of_theMaritalQuotient_marksDirectorPayWhenItReachesItAndNeverThePartner(bool toThePartner)
    {
        var river = toThePartner
            ? RiverInputs.Example.AddToPlan(OptionalModule.Household).WithPartner(true)
            : RiverInputs.Example.AddToPlan(OptionalModule.Household).WithPartner(true).WithDirectorPay(12_000m).WithPartnerIncome(60_000m);

        var marked = MapView.From(river, river.Run(), MapNames.English).Modules.Where(node => node.Badges.Any(badge => badge.Kind == MapLinkKind.TaxBase)).Select(node => node.End.Id);

        marked.ShouldBe(toThePartner ? [] : ["director-pay"]);
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

    // Every tone of badge has its class, and an icon of its own but an amount, so a rule lost is told from
    // one that holds by its icon as by its words, never by the colour alone.
    [Fact]
    public void Style_everyTone_hasItsClassAndAnIconOfItsOwnButAnAmount()
    {
        var tones = Enum.GetValues<MapBadgeTone>();

        tones.ShouldSatisfyAllConditions(
            () => tones.Select(MapBadgeStyle.Class).Distinct().Count().ShouldBe(tones.Length),
            () => tones.Where(tone => tone != MapBadgeTone.Amount).Select(MapBadgeStyle.Icon).ShouldAllBe(icon => icon != null),
            () => tones.Select(MapBadgeStyle.Icon).OfType<string>().Distinct().Count().ShouldBe(tones.Length - 1),
            () => MapBadgeStyle.Icon(MapBadgeTone.Amount).ShouldBeNull());
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

    // A tax base's badge shows its amount alone, its sign before it; the words after it are heard, part of the
    // button's name, the dotted outline its line has saying what it is, as the key does. A condition's badge is
    // seen whole.
    [Theory]
    [InlineData("+{0} to tax base", "€3,450.00", "+€3,450.00", " to tax base")]
    [InlineData("+{0} à la base", "3.450,00 €", "+3.450,00 €", " à la base")]
    public void Words_aTaxBasesBadge_showsItsAmountAndLeavesItsWordsHeard(string template, string amount, string seen, string heard)
    {
        var badge = new MapBadge(MapLinkKind.TaxBase, new MapLabel(MapText.Map, MapTabText.TaxBaseBadge, [amount]), MapBadgeTone.Amount);

        MapBadgeWords.Of(badge, template).ShouldBe((seen, heard));
    }

    [Fact]
    public void Words_aConditionsBadge_isSeenWhole()
    {
        var badge = new MapBadge(MapLinkKind.Condition, new MapLabel(MapText.Map, MapTabText.ReserveBadge(holds: true), []), MapBadgeTone.Holds);

        MapBadgeWords.Of(badge, "reserve met").ShouldBe(("reserve met", ""));
    }

    // What a tax base gains reads whole in the box it marks on the narrowest map, in English and in French, up to
    // the most it is held to: Director pay's, a car's benefit and the marital quotient, up to €99,999.99, far
    // beyond either; Corporate tax's, the year's disallowed expenses, up to €9,999,999.99.
    public static TheoryData<RiverModule, string, string> TaxBaseLimits => new()
    {
        { RiverModule.DirectorPay, Formats.Euros(99_999.99m), $"99.999,99{NoBreak}€" },
        { RiverModule.CorporateTax, Formats.Euros(9_999_999.99m), $"9.999.999,99{NoBreak}€" },
    };

    private const char NoBreak = (char)0x00A0;

    [Theory]
    [MemberData(nameof(TaxBaseLimits))]
    public void Badge_aTaxBasesAmountUpToItsLimitInEitherLanguage_readsWholeInTheBoxItMarks(RiverModule module, string english, string french)
    {
        var (en, fr) = (Seen(Resources, english), Seen(ResxText.French<MapTab>(), french));

        (en, MapGateTextTests.LineWidth(en) <= MapLayout.BadgeRoom(module, icon: false), fr, MapGateTextTests.LineWidth(fr) <= MapLayout.BadgeRoom(module, icon: false))
            .ShouldBe(($"+{english}", true, $"+{french}", true));

        static string Seen(ResxText texts, string amount) =>
            MapBadgeWords.Of(new MapBadge(MapLinkKind.TaxBase, new MapLabel(MapText.Map, MapTabText.TaxBaseBadge, [amount]), MapBadgeTone.Amount), texts.Get(MapTabText.TaxBaseBadge).ShouldNotBeNull()).Seen;
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

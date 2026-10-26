using System.Globalization;
using System.Text.RegularExpressions;
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
                .ShouldContain(("rate lost: benefits", MapBadgeTone.Lost)));
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

        var view = MapView.From(Full, Full.Run());

        view.Modules.Select(node => (node.End.Module!.Value, string.Join(" | ", node.Badges.Select(Shape))))
            .ShouldBe(MapView.ModulesOf(Full).Select(module => (module, string.Join(" | ", badges.GetValueOrDefault(module, []).Select(Shape)))));
    }

    // A badge by value, its words' arguments among them.
    private static string Shape(MapBadge badge) => $"{badge.Kind} {badge.Words.Text} {badge.Words.Key} {string.Join(',', badge.Words.Args)} {badge.Tone}";

    // A refused amount leaves the map without a link and so without a badge; the company skipped leaves no
    // year to read.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void From_aRunWithoutAYear_marksNoModule(bool refused)
    {
        var river = refused ? Full.WithReserve(-1m) : Full.SkipCompany();

        MapView.From(river, river.Run()).Modules.ShouldAllBe(node => node.Badges.Count == 0);
    }

    // Whatever the plan holds and leaves empty, a quiet lever has no badge, nothing reaching it, and no
    // module more than the layout's tests lay the map out with (MapLayoutTests.MostBadges).
    [Theory]
    [MemberData(nameof(MapLayoutTests.Rivers), MemberType = typeof(MapLayoutTests))]
    public void From_anyPlan_marksNoQuietLeverAndNoModuleBeyondTheMostTheLayoutIsHeldTo(int bits, int quiet, bool companySkipped)
    {
        var river = MapLayoutTests.River(bits, quiet, companySkipped);

        MapView.From(river, river.Run()).Modules.Where(node => node.Badges.Count > (node.Quiet ? 0 : MapLayoutTests.MostBadges.GetValueOrDefault(node.End.Module!.Value)))
            .Select(node => $"{node.End.Id} {node.Badges.Count}").ShouldBeEmpty();
    }

    [Fact]
    public void From_theCostlyCar_marksNoModuleBeyondTheMostTheLayoutIsHeldTo()
    {
        var river = MapViewConditionsTests.CostlyCar;

        MapView.From(river, river.Run()).Modules.Where(node => node.Badges.Count > MapLayoutTests.MostBadges.GetValueOrDefault(node.End.Module!.Value))
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
        var css = File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web.Client", "Tabs", "MapTab.razor.css"));

        css.ShouldSatisfyAllConditions(
            () => Block(css, ".map-badge").ShouldContain($"height: {Rem(MapLayout.BadgeRem)};"),
            () => Block(css, ".map-badge").ShouldContain("box-sizing: border-box;"),
            () => Block(css, ".map-module__badges").ShouldContain($"gap: {Rem(MapLayout.BadgeGapRem)};"),
            () => Block(css, ".map-module__badges").ShouldContain($"margin-top: {Rem(MapLayout.BadgeGapRem)};"),
            () => ((MapLayout.BadgeRem + MapLayout.BadgeGapRem) / MapLayout.SmallestRowRem).ShouldBe(0.3m));
    }

    // The links of the run, the conditions of its year between modules on the map among them, before a
    // filter hides any.
    private static IReadOnlyList<MapLink> Links(RiverInputs river)
    {
        var run = river.Run();
        return [.. MapLinks.Of(river, run), .. run.Year is { } year ? MapConditions.From(YearConditions.Of(year), MapView.ModulesOf(river)) : []];
    }

    private static IReadOnlyDictionary<RiverModule, IReadOnlyList<MapBadge>> Of(RiverInputs river) => MapBadges.Of(Links(river));

    // The declarations of a stylesheet's rules for a selector alone, in a media query or not.
    private static string Block(string css, string selector) =>
        string.Concat(Regex.Matches(css, $@"(?m)^\s*{Regex.Escape(selector)}\s*\{{([^}}]*)\}}").Select(match => match.Groups[1].Value));

    private static string Rem(decimal value) => value.ToString(CultureInfo.InvariantCulture) + "rem";
}

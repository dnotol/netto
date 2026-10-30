using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The conditions one module sets on another, the engine's (YearConditions), as links of the map: each
// dashed, from the module that sets it to the one it acts on, saying in a word or two whether it holds,
// only while both its modules are on the map, and none on a run that has no year.
public class MapConditionsTests
{
    private static readonly ResxText Resources = ResxText.For<MapTab>();

    private static readonly IReadOnlyList<RiverModule> EveryModule = MapModules.Order;

    // One condition of each kind Core declares, with the modules its link runs between, as approved: the
    // office allowance acts on the rent, the pay and the benefits on corporate tax, the royalty and the
    // reserve on the split.
    public static TheoryData<YearCondition, RiverModule, RiverModule> Kinds => new()
    {
        { new NoOfficeAllowanceBesideRent(1_380m, 0m), RiverModule.Allowances, RiverModule.Rent },
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayNotMet, 51_000m, 40_000m, 60_000m), RiverModule.Split, RiverModule.CorporateTax },
        { new BenefitsWithinShareOfPay(false, 1_714.29m, 6_714.29m, 0.20m), RiverModule.Car, RiverModule.CorporateTax },
        { new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false), RiverModule.Copyright, RiverModule.Split },
        { new PayOutCoversReserve(40_000m, 40_000m), RiverModule.Dividend, RiverModule.Split },
    };

    // Every kind of condition the engine declares, read off it by reflection, has its case here, so a new
    // kind fails until it has its link and its words.
    [Fact]
    public void Kinds_everyConditionTheEngineDeclares_isAmongThem()
    {
        var declared = typeof(YearCondition).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(YearCondition)) && !type.IsAbstract);

        declared.ShouldBe(Kinds.Select(row => row.Data.Item1.GetType()), ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Link_aCondition_runsDashedFromTheModuleSettingItToTheOneItActsOn(YearCondition condition, RiverModule from, RiverModule to)
    {
        var link = MapConditions.Link(condition);

        (link.Kind, link.From, link.To, link.Card, link.Carries.Text, link.Amount).ShouldBe((MapLinkKind.Condition, MapEnd.Of(from), MapEnd.Of(to), from, MapText.Map, (decimal?)null));
    }

    // Whether it holds is the engine's, carried as it is.
    public static TheoryData<YearCondition> HoldingAndNot => new()
    {
        new PayKeepsReducedRate(ReducedRateCondition.MinimumPayMet, 51_000m, 60_000m, 40_000m),
        new PayKeepsReducedRate(ReducedRateCondition.MinimumPayNotMet, 51_000m, 40_000m, 60_000m),
        new PayOutCoversReserve(40_000.01m, 40_000m),
        new NoOfficeAllowanceBesideRent(0m, 1_938.44m),
    };

    [Theory]
    [MemberData(nameof(HoldingAndNot))]
    public void Link_aCondition_holdsWhereTheEngineSaysItHolds(YearCondition condition)
    {
        MapConditions.Link(condition).Holds.ShouldBe(condition.Holds);
    }

    // Whether it holds in a word or two, what the map draws on its line: a test of the rate met or lost, the
    // office kept, what a royalty does to the pay keeping the rate, the reserve covered or not.
    public static TheoryData<YearCondition, string> States => new()
    {
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayMet, 51_000m, 60_000m, 40_000m), "met" },
        { new PayKeepsReducedRate(ReducedRateCondition.TaxableIncomeNotAbovePay, 51_000m, 50_000m, 50_000m), "met" },
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayNotMet, 51_000m, 40_000m, 60_000m), "lost" },
        { new BenefitsWithinShareOfPay(true, 1_714.29m, 51_714.29m, 0.20m), "met" },
        { new BenefitsWithinShareOfPay(false, 1_714.29m, 6_714.29m, 0.20m), "lost" },
        { new NoOfficeAllowanceBesideRent(1_380m, 0m), "kept" },
        { new NoOfficeAllowanceBesideRent(0m, 1_938.44m), "kept" },
        { new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false), "lowers" },
        { new RoyaltyLowersPayKeepingReducedRate(40_000m, null, true), "lowers" },
        { new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false), "no effect" },
        { new RoyaltyLowersPayKeepingReducedRate(null, null, true), "no pay keeps" },
        { new PayOutCoversReserve(40_000m, 40_000m), "covered" },
        { new PayOutCoversReserve(40_000.01m, 40_000m), "not covered" },
    };

    [Theory]
    [MemberData(nameof(States))]
    public void Link_aCondition_saysWhetherItHoldsInAWordOrTwo(YearCondition condition, string state)
    {
        var label = MapConditions.Link(condition).State.ShouldNotBeNull();

        (label.Text, Resources.Format(label.Key, [.. label.Args])).ShouldBe((MapText.Map, state));
    }

    // What a condition's link carries is the words its line draws: its sentence is no link's, the cards
    // saying it with their figures.
    [Theory]
    [MemberData(nameof(States))]
    public void Link_aCondition_carriesTheWordsItsLineDraws(YearCondition condition, string _)
    {
        var link = MapConditions.Link(condition);

        link.Carries.ShouldBe(link.State);
    }

    // The mark a condition leaves on the module it acts on, a badge in its box: which rule and whether it
    // holds, in a few words with the state its line says (State), and its tone. One that does not hold and
    // costs the plan something is lost; the royalty that lowers no pay has no effect, nothing being lost by it.
    public static TheoryData<YearCondition, string, string> Badges => new()
    {
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayMet, 51_000m, 60_000m, 40_000m), "pay met", nameof(MapBadgeTone.Holds) },
        { new PayKeepsReducedRate(ReducedRateCondition.TaxableIncomeNotAbovePay, 51_000m, 50_000m, 50_000m), "pay met", nameof(MapBadgeTone.Holds) },
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayNotMet, 51_000m, 40_000m, 60_000m), "rate lost: pay", nameof(MapBadgeTone.Lost) },
        { new BenefitsWithinShareOfPay(true, 1_714.29m, 51_714.29m, 0.20m), "benefits met", nameof(MapBadgeTone.Holds) },
        { new BenefitsWithinShareOfPay(false, 1_714.29m, 6_714.29m, 0.20m), "benefits lost", nameof(MapBadgeTone.Lost) },
        { new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false), "royalty lowers", nameof(MapBadgeTone.Holds) },
        { new RoyaltyLowersPayKeepingReducedRate(40_000m, null, true), "royalty lowers", nameof(MapBadgeTone.Holds) },
        { new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false), "royalty: none", nameof(MapBadgeTone.NoEffect) },
        { new RoyaltyLowersPayKeepingReducedRate(null, null, true), "no pay keeps", nameof(MapBadgeTone.Lost) },
        { new PayOutCoversReserve(40_000m, 40_000m), "reserve met", nameof(MapBadgeTone.Holds) },
        { new PayOutCoversReserve(40_000.01m, 40_000m), "reserve short", nameof(MapBadgeTone.Lost) },
    };

    [Theory]
    [MemberData(nameof(Badges))]
    public void Link_aCondition_marksTheModuleItActsOnWithWhichRuleAndWhetherItHolds(YearCondition condition, string words, string tone)
    {
        var badge = MapConditions.Link(condition).Badge.ShouldNotBeNull();

        (badge.Kind, badge.Words.Text, Resources.Format(badge.Words.Key, [.. badge.Words.Args]), badge.Tone.ToString()).ShouldBe((MapLinkKind.Condition, MapText.Map, words, tone));
    }

    public static TheoryData<YearCondition> Badged => new(Badges.Select(row => row.Data.Item1));

    // A badge names its rule in words of its own, in English and in French, so no two badges say the same, and
    // its icon says whether the rule holds (MapBadgeStyle), its words short enough to read whole in its box
    // (MapBadgesTests), never the line's whole state.
    [Fact]
    public void Link_everyConditionsBadge_hasWordsOfItsOwnAndAnIcon()
    {
        var badges = Badged.Select(row => MapConditions.Link(row.Data).Badge!).DistinctBy(badge => badge.Words.Key).ToList();

        badges.ShouldSatisfyAllConditions(
            () => badges.Select(badge => Resources.Get(badge.Words.Key)).Distinct().Count().ShouldBe(badges.Count),
            () => badges.Select(badge => ResxText.French<MapTab>().Get(badge.Words.Key)).Distinct().Count().ShouldBe(badges.Count),
            () => badges.ShouldAllBe(badge => MapBadgeStyle.Icon(badge.Tone) != null));
    }

    public static TheoryData<YearCondition> OfficeRules => new(new YearCondition[] { new NoOfficeAllowanceBesideRent(1_380m, 0m), new NoOfficeAllowanceBesideRent(0m, 1_938.44m) });

    // The office rule always holds: it leaves no badge, its line saying it is kept.
    [Theory]
    [MemberData(nameof(OfficeRules))]
    public void Link_theOfficeRule_leavesNoBadge(YearCondition condition)
    {
        MapConditions.Link(condition).Badge.ShouldBeNull();
    }

    // Every kind the engine declares (Kinds, held to it by reflection) has its badge or, the office rule,
    // none; a badge marks the module the condition acts on, so it needs no module of its own.
    [Fact]
    public void Badges_everyKindButTheOffice_isAmongThem()
    {
        Kinds.Select(row => row.Data.Item1.GetType()).Where(kind => kind != typeof(NoOfficeAllowanceBesideRent))
            .ShouldBe(Badges.Select(row => row.Data.Item1.GetType()).Distinct(), ignoreOrder: true);
    }

    // A condition shows only while both its modules are on the map: no office allowance beside a rent
    // while the allowances are out of the plan.
    [Fact]
    public void From_aConditionWithAModuleOffTheMap_isNoLink()
    {
        var conditions = new YearCondition[] { new NoOfficeAllowanceBesideRent(1_380m, 0m), new PayOutCoversReserve(40_000m, 40_000m) };

        MapConditions.From(conditions, [.. EveryModule.Where(module => module != RiverModule.Allowances)])
            .Select(link => (link.From.Module, link.To.Module)).ShouldBe([(RiverModule.Dividend, RiverModule.Split)]);
    }

    [Fact]
    public void From_everyModuleOnTheMap_linksEveryCondition()
    {
        var conditions = Kinds.Select(row => row.Data.Item1).ToList();

        MapConditions.From(conditions, EveryModule).Count.ShouldBe(conditions.Count);
    }

}

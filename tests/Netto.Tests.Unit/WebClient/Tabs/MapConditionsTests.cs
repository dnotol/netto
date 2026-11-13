using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The conditions one module sets on another, the engine's (YearConditions), as links of the map: each
// dashed, from the module that sets it to the one it acts on, with no amount and no word on its line, carrying
// whether it holds in a word or two for the Effects list, only while both its modules are on the map, and none on a run that has no year.
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
        { new NoAllowanceBesideEquipment(1, true), RiverModule.Allowances, RiverModule.Equipment },
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

    // Whether it holds in a word or two, what the Effects list says after its rule: a test of the rate met or lost, the
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
        { new NoAllowanceBesideEquipment(1, true), "kept" },
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

    // The mark a condition leaves on the module it acts on, a badge in its box, only for what is lost: the rate the
    // pay, the benefits or a royalty cost the plan, the reserve the pay-out leaves short. Its words read alone in
    // the narrow box, the Effects list giving the rule. A rule that holds, or a royalty that changes nothing,
    // leaves none.
    public static TheoryData<YearCondition, string, string> Badges => new()
    {
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayNotMet, 51_000m, 40_000m, 60_000m), "rate lost", nameof(MapBadgeTone.Lost) },
        { new BenefitsWithinShareOfPay(false, 1_714.29m, 6_714.29m, 0.20m), "rate lost", nameof(MapBadgeTone.Lost) },
        { new RoyaltyLowersPayKeepingReducedRate(null, null, true), "rate lost", nameof(MapBadgeTone.Lost) },
        { new PayOutCoversReserve(40_000.01m, 40_000m), "too much kept", nameof(MapBadgeTone.Lost) },
    };

    public static TheoryData<YearCondition> NoBadge => new()
    {
        new PayKeepsReducedRate(ReducedRateCondition.MinimumPayMet, 51_000m, 60_000m, 40_000m),
        new PayKeepsReducedRate(ReducedRateCondition.TaxableIncomeNotAbovePay, 51_000m, 50_000m, 50_000m),
        new BenefitsWithinShareOfPay(true, 1_714.29m, 51_714.29m, 0.20m),
        new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false),
        new RoyaltyLowersPayKeepingReducedRate(40_000m, null, true),
        new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false),
        new PayOutCoversReserve(40_000m, 40_000m),
    };

    [Theory]
    [MemberData(nameof(NoBadge))]
    public void Link_aRuleThatHoldsOrChangesNothing_leavesNoBadge(YearCondition condition)
    {
        MapConditions.Link(condition).Badge.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(Badges))]
    public void Link_aCondition_marksTheModuleItActsOnWithWhichRuleAndWhetherItHolds(YearCondition condition, string words, string tone)
    {
        var badge = MapConditions.Link(condition).Badge.ShouldNotBeNull();

        (badge.Kind, badge.Words.Text, Resources.Format(badge.Words.Key, [.. badge.Words.Args]), badge.Tone.ToString()).ShouldBe((MapLinkKind.Condition, MapText.Map, words, tone));
    }

    public static TheoryData<YearCondition> Badged => new(Badges.Select(row => row.Data.Item1));

    // A badge says what is lost in words of its own, in English and in French, none of them a bare « yes » or
    // « no », its icon beside it (MapBadgeStyle), its words short enough to read whole in its box
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

    public static TheoryData<YearCondition> OfficeRules => new(new YearCondition[]
    {
        new NoOfficeAllowanceBesideRent(1_380m, 0m), new NoOfficeAllowanceBesideRent(0m, 1_938.44m), new NoAllowanceBesideEquipment(1, true),
    });

    // The office rule always holds: it leaves no badge.
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
        Kinds.Select(row => row.Data.Item1.GetType()).Where(kind => kind != typeof(NoOfficeAllowanceBesideRent) && kind != typeof(NoAllowanceBesideEquipment))
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

    // The benefits' share of the pay (art. 215) is drawn from the module whose benefit the map shows: the car while
    // it carries one, else the IT and phone while it carries one, else the car.
    public static TheoryData<string, RiverModule> BenefitCarriers => new()
    {
        { "every lever", RiverModule.Car },
        { "the car out of the plan", RiverModule.Equipment },
        { "the car without a benefit", RiverModule.Equipment },
        { "neither out of the plan", RiverModule.Car },
    };

    [Theory]
    [MemberData(nameof(BenefitCarriers))]
    public void BenefitsFrom_aPlan_isTheModuleCarryingTheBenefit(string plan, RiverModule expected)
    {
        var river = Carrying(plan);
        var year = river.Run().Year.ShouldNotBeNull();

        MapConditions.BenefitsFrom(year, MapView.ModulesOf(river)).ShouldBe(expected);
    }

    // With the IT and phone alone carrying a benefit, art. 215's 20% condition is drawn from it to Corporate tax.
    [Fact]
    public void Of_theITAndPhoneAlone_drawsTheBenefitsShareFromItToCorporateTax()
    {
        var river = Carrying("the car out of the plan");
        var year = river.Run().Year.ShouldNotBeNull();

        var links = MapConditions.Of(year, MapView.ModulesOf(river));

        links.Where(link => link.To.Module == RiverModule.CorporateTax && link.From.Module != RiverModule.Split).Select(link => (link.From.Module, MapEffectText.Rule(link.From.Module!.Value, link.To.Module!.Value)))
            .ShouldBe([(RiverModule.Equipment, "Rule_BenefitsWithinShareOfPay")]);
    }

    // A PC the company provides beside Expense allowances paying no flat computer allowance: the map draws the rule
    // that keeps that allowance away, between Allowances and the IT and phone, worded in both languages.
    [Fact]
    public void Of_allowancesBesideAPcTheCompanyProvides_drawsTheRuleBetweenThem()
    {
        var river = MapLinksTests.Full.WithComputerAllowance(0m).WithPcs(1);
        var year = river.Run().Year.ShouldNotBeNull();

        var link = MapConditions.Of(year, MapView.ModulesOf(river)).Single(link => link.To.Module == RiverModule.Equipment);
        var rule = MapEffectText.Rule(link.From.Module!.Value, link.To.Module!.Value);

        (link.From.Module, link.Kind, rule, Resources.Get(rule) is not null, ResxText.French<MapTab>().Get(rule) is not null)
            .ShouldBe((RiverModule.Allowances, MapLinkKind.Condition, "Rule_NoAllowanceBesideEquipment", true, true));
    }

    private static RiverInputs Carrying(string plan) => plan switch
    {
        "every lever" => MapLinksTests.Full,
        "the car out of the plan" => MapLinksTests.Full.TakeOutOfPlan(OptionalModule.Car),
        "the car without a benefit" => MapLinksTests.Full.WithCatalogueValue(0m),
        "neither out of the plan" => MapLinksTests.Full.TakeOutOfPlan(OptionalModule.Car).TakeOutOfPlan(OptionalModule.Equipment),
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, null),
    };
}

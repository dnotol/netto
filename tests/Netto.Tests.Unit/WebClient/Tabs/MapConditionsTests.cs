using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The conditions one module sets on another, the engine's (YearConditions), as links of the map: each
// dashed, from the module that sets it to the one it acts on, saying in its words whether it holds and
// the figures it names, only while both its modules are on the map, and none on a run that has no year.
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

    // Each kind in words, with a variant for where it holds and one where it does not, its figures as the
    // cards format them. A test of the rate that holds says its own condition is met, never that the rate
    // is kept, which the other test may still lose; one that fails loses the rate whatever the other does.
    public static TheoryData<YearCondition, string> Words => new()
    {
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayMet, 51_000m, 60_000m, 40_000m), "Pay €60,000.00 reaches the €51,000.00 minimum: pay condition met" },
        { new PayKeepsReducedRate(ReducedRateCondition.TaxableIncomeNotAbovePay, 51_000m, 50_000m, 50_000m), "Pay €50,000.00 not below the €50,000.00 taxable profit: pay condition met" },
        { new PayKeepsReducedRate(ReducedRateCondition.MinimumPayNotMet, 51_000m, 40_000m, 60_000m), "Pay €40,000.00 below the €51,000.00 minimum and the €60,000.00 taxable profit: reduced rate lost" },
        { new BenefitsWithinShareOfPay(true, 1_714.29m, 51_714.29m, 0.20m), "Benefits in kind €1,714.29 within 20% of the €51,714.29 pay: benefits condition met" },
        { new BenefitsWithinShareOfPay(false, 1_714.29m, 6_714.29m, 0.20m), "Benefits in kind €1,714.29 over 20% of the €6,714.29 pay: reduced rate lost" },
        { new NoOfficeAllowanceBesideRent(1_380m, 0m), "No office allowance beside the rent: kept" },
        { new NoOfficeAllowanceBesideRent(0m, 1_938.44m), "No rent beside the office allowance: kept" },
        { new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false), "Lowers the pay keeping the reduced rate to €40,000.00 (€50,000.00 without)" },
        { new RoyaltyLowersPayKeepingReducedRate(40_000m, null, true), "Lets a pay of €40,000.00 keep the reduced rate (none without)" },
        { new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false), "Does not lower the pay keeping the reduced rate, €51,000.00" },
        { new RoyaltyLowersPayKeepingReducedRate(null, null, true), "No pay keeps the reduced rate beside it" },
        { new PayOutCoversReserve(40_000m, 40_000m), "Reserve €40,000.00 covered: up to €40,000.00 to pay out" },
        { new PayOutCoversReserve(40_000.01m, 40_000m), "Reserve €40,000.01 not covered: only €40,000.00 to pay out" },
    };

    [Theory]
    [MemberData(nameof(Words))]
    public void Link_aCondition_saysInWordsWhetherItHolds(YearCondition condition, string words)
    {
        Text(MapConditions.Link(condition)).ShouldBe(words);
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

    private static string Text(MapLink link) => Resources.Format(link.Carries.Key, [.. link.Carries.Args]);
}

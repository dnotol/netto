using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What each lever before the split hands on down the river, on the river's example (RiverInputsTests):
// 100,000 earned before the director is paid anything. The vouchers take 2,310.20 (ModuleViewsTests:
// 220 days of meal vouchers, 250 of eco and 100 of sport/culture), the internet and computer allowances
// 480, the office allowance excluding a rent (ModuleViewsTests), the rent at the cap 1,380.00, the car
// 8,000 of costs less the director's 600 (CarViewTests: 92,600 after it alone), and the split pays a
// fixed royalty of 10,000 beside the pay. Each lever passes on what it receives less what it takes; the
// last one before the split passes on what the pay and the royalty share there, worded as the profit
// before pay, with « and royalty » while Copyright royalty is in the plan; the others say what is left
// after them, to the next lever in the plan.
public class HandOffTests
{
    private static readonly OptionalModule[] Levers = [OptionalModule.Vouchers, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Car];

    private static readonly RiverInputs Filled = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module))
        .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
        .WithInternetAllowance(240m).WithComputerAllowance(240m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m).WithCarContribution(600m)
        .WithRoyalty(10_000m);

    [Fact]
    public void Cost_everyModule_isWhatItTakesOffWhatTheCompanyEarns()
    {
        var year = Year(Filled);

        PlanComposition.Modules.Select(module => (module, HandOff.Cost(year, module))).ShouldBe(new[]
        {
            (OptionalModule.Vouchers, 2_310.20m),
            (OptionalModule.Allowances, 480m),
            (OptionalModule.Rent, 1_380m),
            (OptionalModule.Car, 7_400m),
            (OptionalModule.Copyright, 10_000m),
        });
    }

    // Each receives what the levers above leave of the 100,000, and the split shares what the car leaves.
    [Fact]
    public void BeforeAndAfter_everyLever_areWhatTheCompanyEarnsLessTheLeversAbove()
    {
        var year = Year(Filled);

        year.ShouldSatisfyAllConditions(
            () => Levers.Select(lever => (lever, HandOff.Before(year, lever), HandOff.After(year, lever))).ShouldBe(new[]
            {
                (OptionalModule.Vouchers, 100_000m, 97_689.80m),
                (OptionalModule.Allowances, 97_689.80m, 97_209.80m),
                (OptionalModule.Rent, 97_209.80m, 95_829.80m),
                (OptionalModule.Car, 95_829.80m, 88_429.80m),
            }),
            () => HandOff.Shared(year).ShouldBe(88_429.80m));
    }

    // The first lever receives all the company earns before it pays its director anything, once INASTI's
    // company contribution is paid, and the split shares the profit before pay with the royalty it pays
    // beside the pay.
    [Fact]
    public void BeforeAndShared_everyLever_areTheEnginesFigures()
    {
        var year = Year(Filled);

        (HandOff.Before(year, OptionalModule.Vouchers), HandOff.Shared(year))
            .ShouldBe((year.Scenario.ProfitBeforeDirector - year.CompanyContribution, year.ProfitBeforePay + year.Copyright.Royalty));
    }

    // A module out of the plan takes nothing: the scenario hands the engine none of it.
    [Fact]
    public void Cost_moduleOutOfThePlan_isNothing()
    {
        var year = Year(Filled with { Composition = PlanComposition.None });

        PlanComposition.Modules.Select(module => HandOff.Cost(year, module)).ShouldAllBe(cost => cost == 0m);
    }

    [Fact]
    public void After_royalty_isRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => HandOff.After(Year(Filled), OptionalModule.Copyright));
    }

    // Every composition of the four levers, with and without the royalty.
    public static TheoryData<bool, bool, bool, bool, bool> Compositions()
    {
        var data = new TheoryData<bool, bool, bool, bool, bool>();
        for (var mask = 0; mask < 32; mask++)
            data.Add((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0, (mask & 16) != 0);
        return data;
    }

    // Without the royalty, the last lever in the plan hands the split the profit before pay, each other
    // what is left after it, to the next lever in the plan: each line its key and where it goes.
    [Theory]
    [InlineData("", "")]
    [InlineData("vouchers", "profit_before_pay>split")]
    [InlineData("allowances", "profit_before_pay>split")]
    [InlineData("rent", "profit_before_pay>split")]
    [InlineData("car", "profit_before_pay>split")]
    [InlineData("vouchers allowances", "left_after_vouchers>allowances profit_before_pay>split")]
    [InlineData("vouchers rent", "left_after_vouchers>rent profit_before_pay>split")]
    [InlineData("vouchers car", "left_after_vouchers>car profit_before_pay>split")]
    [InlineData("allowances rent", "left_after_allowances>rent profit_before_pay>split")]
    [InlineData("allowances car", "left_after_allowances>car profit_before_pay>split")]
    [InlineData("rent car", "left_after_rent>car profit_before_pay>split")]
    [InlineData("vouchers allowances rent", "left_after_vouchers>allowances left_after_allowances>rent profit_before_pay>split")]
    [InlineData("vouchers allowances car", "left_after_vouchers>allowances left_after_allowances>car profit_before_pay>split")]
    [InlineData("vouchers rent car", "left_after_vouchers>rent left_after_rent>car profit_before_pay>split")]
    [InlineData("allowances rent car", "left_after_allowances>rent left_after_rent>car profit_before_pay>split")]
    [InlineData("vouchers allowances rent car", "left_after_vouchers>allowances left_after_allowances>rent left_after_rent>car profit_before_pay>split")]
    public void Drain_compositionWithoutTheRoyalty_lastLeverHandsTheProfitBeforePayToTheSplit(string levers, string handedOn)
    {
        HandedOn(levers, royalty: false).ShouldBe(handedOn);
    }

    // With the royalty in the plan, the last lever hands the split the profit before pay and royalty.
    [Theory]
    [InlineData("", "")]
    [InlineData("vouchers", "profit_before_pay_and_royalty>split")]
    [InlineData("allowances", "profit_before_pay_and_royalty>split")]
    [InlineData("rent", "profit_before_pay_and_royalty>split")]
    [InlineData("car", "profit_before_pay_and_royalty>split")]
    [InlineData("vouchers allowances", "left_after_vouchers>allowances profit_before_pay_and_royalty>split")]
    [InlineData("vouchers rent", "left_after_vouchers>rent profit_before_pay_and_royalty>split")]
    [InlineData("vouchers car", "left_after_vouchers>car profit_before_pay_and_royalty>split")]
    [InlineData("allowances rent", "left_after_allowances>rent profit_before_pay_and_royalty>split")]
    [InlineData("allowances car", "left_after_allowances>car profit_before_pay_and_royalty>split")]
    [InlineData("rent car", "left_after_rent>car profit_before_pay_and_royalty>split")]
    [InlineData("vouchers allowances rent", "left_after_vouchers>allowances left_after_allowances>rent profit_before_pay_and_royalty>split")]
    [InlineData("vouchers allowances car", "left_after_vouchers>allowances left_after_allowances>car profit_before_pay_and_royalty>split")]
    [InlineData("vouchers rent car", "left_after_vouchers>rent left_after_rent>car profit_before_pay_and_royalty>split")]
    [InlineData("allowances rent car", "left_after_allowances>rent left_after_rent>car profit_before_pay_and_royalty>split")]
    [InlineData("vouchers allowances rent car", "left_after_vouchers>allowances left_after_allowances>rent left_after_rent>car profit_before_pay_and_royalty>split")]
    public void Drain_compositionWithTheRoyalty_lastLeverHandsTheProfitBeforePayAndRoyaltyToTheSplit(string levers, string handedOn)
    {
        HandedOn(levers, royalty: true).ShouldBe(handedOn);
    }

    // Each lever passes on what it leaves of the 100,000, the car what the split shares, all passed on.
    [Fact]
    public void Drain_everyLever_passesOnWhatItLeaves()
    {
        var year = Year(Filled);

        Levers.Select(lever => HandOff.Drain(year, lever, Filled.Composition)).Select(drain => (drain.Kind, drain.Amount)).ShouldBe(new[]
        {
            (DrainKind.PassedOn, 97_689.80m),
            (DrainKind.PassedOn, 97_209.80m),
            (DrainKind.PassedOn, 95_829.80m),
            (DrainKind.PassedOn, 88_429.80m),
        });
    }

    // Each lever's card ends on what it hands on.
    [Theory]
    [MemberData(nameof(Compositions))]
    public void Views_anyComposition_endOnWhatTheirLeverHandsOn(bool vouchers, bool allowances, bool rent, bool car, bool royalty)
    {
        var composition = new PlanComposition(vouchers, allowances, rent, car, royalty);
        var year = Year(Filled with { Composition = composition });
        var inPlan = Levers.Where(composition.Holds).ToList();

        var last = inPlan.Select(lever => lever switch
        {
            OptionalModule.Vouchers => VoucherView.From(year, composition).Drains[^1],
            OptionalModule.Allowances => AllowanceView.From(year, composition).Drains[^1],
            OptionalModule.Rent => RentView.From(year, composition).Drains[^1],
            _ => CarView.From(year, composition).Drains[^1],
        });

        last.ShouldBe(inPlan.Select(lever => HandOff.Drain(year, lever, composition)));
    }

    private static YearResult Year(RiverInputs inputs) => inputs.Run().Year.ShouldNotBeNull();

    // The line each lever in the plan ends on, by their folds' names (« vouchers rent »), with the royalty
    // in the plan or not: its key and where it goes, « key>destination », one after the other.
    private static string HandedOn(string levers, bool royalty)
    {
        var composition = levers.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => Levers.Single(lever => ModuleFolds.Name(lever) == name))
            .Aggregate(PlanComposition.None.With(OptionalModule.Copyright, royalty), (plan, lever) => plan.With(lever, true));
        var year = Year(Filled with { Composition = composition });

        return string.Join(" ", Levers.Where(composition.Holds).Select(lever => HandOff.Drain(year, lever, composition)).Select(drain => $"{drain.Key}>{drain.To}"));
    }
}

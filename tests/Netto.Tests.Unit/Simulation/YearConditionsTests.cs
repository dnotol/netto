using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// The conditions one part of the year sets on another, as the year meets them, on the made-up company
// (Fixtures): 100,000 of profit before the director, a small company past its first four tax periods,
// 2026's minimum director pay of 51,000 (CorporateTaxCalculatorTests), the director paying their own
// contributions, so the company's taxable income is what the pay leaves of the 100,000. Each condition is
// listed only where it applies, and says whether it holds.
public class YearConditionsTests
{
    private static readonly CompanyStatus NotSmall = new(SmallCompany: false, FirstFourTaxPeriods: false);
    private static readonly CompanyStatus FirstFourTaxPeriods = new(SmallCompany: true, FirstFourTaxPeriods: true);

    // YearSimulationCarTests' electric car at a catalogue value of 1,000,000: a benefit far beyond 20% of
    // any pay these tests reach.
    private static readonly CarFacts CostlyCar = ElectricCar with { CatalogueValue = 1_000_000m };

    // SplitSearchCopyrightTests' royalties: 20,000 fixed, or following the pay for 75% of the working time
    // spent creating.
    private static readonly CopyrightRoyalty FixedRoyalty = new(20_000m);
    private static readonly CopyrightRoyalty FollowingRoyalty = new(0m) { CreativeShare = 0.75m, FollowsPay = true };

    // Art. 215 al. 3, 4° on the pay: under the minimum with 60,000 of taxable income, under it with the
    // taxable income no higher than the pay, and at it.
    public static TheoryData<decimal, ReducedRateCondition, decimal, bool> PayTests => new()
    {
        { 40_000m, ReducedRateCondition.MinimumPayNotMet, 60_000m, false },
        { 50_000m, ReducedRateCondition.TaxableIncomeNotAbovePay, 50_000m, true },
        { 51_000m, ReducedRateCondition.MinimumPayMet, 49_000m, true },
    };

    [Theory]
    [MemberData(nameof(PayTests))]
    public void Of_smallCompanyPastItsStartUp_saysWhetherThePayKeepsTheReducedRate(decimal pay, ReducedRateCondition test, decimal taxableIncome, bool holds)
    {
        var condition = Single<PayKeepsReducedRate>(Scenario(directorPay: pay));

        (condition, condition.Holds).ShouldBe((new PayKeepsReducedRate(test, 51_000m, pay, taxableIncome), holds));
    }

    [Fact]
    public void Of_firstFourTaxPeriods_setsNoConditionOnThePay()
    {
        Conditions(Scenario(company: FirstFourTaxPeriods)).OfType<PayKeepsReducedRate>().ShouldBeEmpty();
    }

    [Fact]
    public void Of_companyTheRateIsNotOpenTo_setsNoConditionOnTheRate()
    {
        var conditions = Conditions(Scenario(company: NotSmall, car: ElectricCar));

        (conditions.OfType<PayKeepsReducedRate>().Count(), conditions.OfType<BenefitsWithinShareOfPay>().Count()).ShouldBe((0, 0));
    }

    // At 50,000 of pay the car's 1,714.29 is well within 20% of the 51,714.29 art. 215 reads, the benefit
    // counted in the pay.
    [Fact]
    public void Of_carWithinItsShare_holds()
    {
        var condition = Single<BenefitsWithinShareOfPay>(Scenario(car: ElectricCar));

        condition.ShouldBe(new BenefitsWithinShareOfPay(true, 1_714.29m, 51_714.29m, 0.20m));
    }

    // In the first four tax periods the pay is not tested, the benefit is: 1,714.29 is more than 20% of
    // the 6,714.29 a pay of 5,000 makes with it.
    [Fact]
    public void Of_firstFourTaxPeriodsWithACarBeyondItsShare_failsTheBenefitsAlone()
    {
        var conditions = Conditions(Scenario(directorPay: 5_000m, company: FirstFourTaxPeriods, car: ElectricCar));

        (conditions.OfType<PayKeepsReducedRate>().Count(), conditions.OfType<BenefitsWithinShareOfPay>().Single())
            .ShouldBe((0, new BenefitsWithinShareOfPay(false, 1_714.29m, 6_714.29m, 0.20m)));
    }

    // A pay at the minimum meets its own test while the costly car's benefit loses the rate: each
    // condition says its own.
    [Fact]
    public void Of_costlyCarBesideAPayAtTheMinimum_failsTheBenefitsAndKeepsThePay()
    {
        var conditions = Conditions(Scenario(directorPay: 51_000m, car: CostlyCar));

        (conditions.OfType<PayKeepsReducedRate>().Single().Holds, conditions.OfType<BenefitsWithinShareOfPay>().Single().Holds).ShouldBe((true, false));
    }

    [Fact]
    public void Of_noCar_setsNoConditionOnBenefits()
    {
        Conditions(Scenario()).OfType<BenefitsWithinShareOfPay>().ShouldBeEmpty();
    }

    // The conditions on the rate say together what the engine decided: a small company keeps it exactly
    // where every one of them listed holds.
    public static TheoryData<decimal, bool, decimal> SmallCompanyYears => new()
    {
        { 0m, false, 0m },
        { 40_000m, false, 0m },
        { 50_000m, false, 0m },
        { 51_000m, false, 0m },
        { 5_000m, false, 50_000m },
        { 50_000m, false, 50_000m },
        { 51_000m, false, 1_000_000m },
        { 0m, true, 0m },
        { 5_000m, true, 50_000m },
        { 10_000m, true, 50_000m },
        { 51_000m, true, 1_000_000m },
    };

    [Theory]
    [MemberData(nameof(SmallCompanyYears))]
    public void Of_smallCompany_theRatesConditionsHoldExactlyWhereTheRateIsKept(decimal pay, bool firstFourTaxPeriods, decimal catalogueValue)
    {
        var scenario = Scenario(
            directorPay: pay, company: new CompanyStatus(SmallCompany: true, firstFourTaxPeriods),
            car: catalogueValue == 0m ? null : ElectricCar with { CatalogueValue = catalogueValue });
        var year = Run(scenario);

        var onTheRate = YearConditions.Of(year).Where(condition => condition is PayKeepsReducedRate or BenefitsWithinShareOfPay);

        onTheRate.All(condition => condition.Holds).ShouldBe(year.Company.Condition.AppliesReducedRate());
    }

    // The office allowance covers the room, its rent included (circular 2021/C/20, §31), so the engine
    // refuses a rent beside one: a year that runs with either keeps the condition.
    [Fact]
    public void Of_rentWithoutAnOfficeAllowance_holds()
    {
        var condition = Single<NoOfficeAllowanceBesideRent>(Scenario(allowances: FlatRateAllowances with { Office = 0m }, rent: RentAtTheCap));

        (condition, condition.Holds).ShouldBe((new NoOfficeAllowanceBesideRent(1_380m, 0m), true));
    }

    [Fact]
    public void Of_officeAllowanceWithoutARent_holds()
    {
        var condition = Single<NoOfficeAllowanceBesideRent>(Scenario(allowances: FlatRateAllowances));

        (condition, condition.Holds).ShouldBe((new NoOfficeAllowanceBesideRent(0m, 1_938.44m), true));
    }

    [Fact]
    public void Of_neitherARentNorAnOfficeAllowance_setsNoConditionBetweenThem()
    {
        Conditions(Scenario(allowances: FlatRateAllowances with { Office = 0m })).OfType<NoOfficeAllowanceBesideRent>().ShouldBeEmpty();
    }

    // No flat computer allowance beside a PC or a tablet the company provides, no flat internet allowance beside
    // an internet connection it pays (circular 2021/C/20, §52 and §48): the engine refuses the plan otherwise, so a
    // year that runs with such pieces keeps the condition, with the PCs and tablets and the connection it holds.
    [Fact]
    public void Of_equipmentHoldingAPcATabletOrInternet_keepsNoAllowanceBesideIt()
    {
        var condition = Single<NoAllowanceBesideEquipment>(Scenario(
            allowances: FlatRateAllowances with { Computer = 0m, Internet = 0m }, equipment: LaptopPhoneAndInternet with { Tablets = 1 }));

        (condition, condition.Holds).ShouldBe((new NoAllowanceBesideEquipment(2, true), true));
    }

    // Phones and subscriptions exclude neither allowance: they set no condition on them.
    [Fact]
    public void Of_equipmentOfPhonesAlone_setsNoConditionOnTheAllowances()
    {
        Conditions(Scenario(equipment: LaptopPhoneAndInternet with { Pcs = 0, Internet = false })).OfType<NoAllowanceBesideEquipment>().ShouldBeEmpty();
    }

    [Fact]
    public void Of_noEquipment_setsNoConditionOnTheAllowances()
    {
        Conditions(Scenario(allowances: FlatRateAllowances)).OfType<NoAllowanceBesideEquipment>().ShouldBeEmpty();
    }

    // The royalty lowers the taxable income art. 215 compares the pay with: the rate is kept from half of
    // the 80,000 a fixed 20,000 leaves, where the same year without it keeps it from half of 100,000.
    [Fact]
    public void Of_fixedRoyalty_lowersThePayKeepingTheReducedRate()
    {
        var condition = Single<RoyaltyLowersPayKeepingReducedRate>(Scenario(royalty: FixedRoyalty));

        (condition, condition.Holds).ShouldBe((new RoyaltyLowersPayKeepingReducedRate(40_000m, 50_000m, false), true));
    }

    // Following the pay, the royalty rises with it: kept from 44,827.59 (SplitSearchCopyrightTests).
    [Fact]
    public void Of_royaltyFollowingThePay_lowersThePayKeepingTheReducedRate()
    {
        var condition = Single<RoyaltyLowersPayKeepingReducedRate>(Scenario(royalty: FollowingRoyalty));

        (condition, condition.Holds).ShouldBe((new RoyaltyLowersPayKeepingReducedRate(44_827.59m, 50_000m, true), true));
    }

    public static TheoryData<CopyrightRoyalty> Royalties => [FixedRoyalty, FollowingRoyalty];

    // Its two pays are the ones the split's search finds with the royalty and for the same year without it.
    [Theory]
    [MemberData(nameof(Royalties))]
    public void Of_royalty_carriesTheSplitsSmallestPaysWithAndWithoutIt(CopyrightRoyalty royalty)
    {
        var scenario = Scenario(royalty: royalty);

        var condition = Single<RoyaltyLowersPayKeepingReducedRate>(scenario);

        (condition.With, condition.Without).ShouldBe(
            (Search(scenario).SmallestPayKeepingReducedRate, Search(scenario with { Copyright = CopyrightRoyalty.None }).SmallestPayKeepingReducedRate));
    }

    // 200,000 of profit before the director: half of it, with or without the royalty, is above the
    // minimum, which keeps the rate either way.
    [Fact]
    public void Of_royaltyWhereTheMinimumPayDecides_doesNotLowerIt()
    {
        var condition = Single<RoyaltyLowersPayKeepingReducedRate>(Scenario(revenue: 220_000m, royalty: FixedRoyalty));

        (condition, condition.Holds).ShouldBe((new RoyaltyLowersPayKeepingReducedRate(51_000m, 51_000m, false), false));
    }

    // No royalty, one that follows the pay with no creative share given, so none at any pay, and a year in
    // the first four tax periods, whose pay is not tested.
    public static TheoryData<CopyrightRoyalty?, bool> NoRoyaltyOnThePayTest => new()
    {
        { null, false },
        { FollowingRoyalty with { CreativeShare = null }, false },
        { FixedRoyalty, true },
    };

    [Theory]
    [MemberData(nameof(NoRoyaltyOnThePayTest))]
    public void Of_noRoyaltyOrNoPayTest_setsNoConditionOnThePayKeepingTheRate(CopyrightRoyalty? royalty, bool firstFourTaxPeriods)
    {
        var scenario = Scenario(company: new CompanyStatus(SmallCompany: true, firstFourTaxPeriods), royalty: royalty);

        Conditions(scenario).OfType<RoyaltyLowersPayKeepingReducedRate>().ShouldBeEmpty();
    }

    // The costly car's benefit needs a pay of four times it to stay within 20%, more than the company
    // pays with or without the royalty: no pay keeps the rate either way, so the royalty sets nothing on it.
    [Fact]
    public void Of_royaltyWhereNoPayKeepsTheRateEitherWay_setsNoConditionOnIt()
    {
        Conditions(Scenario(car: CostlyCar, royalty: FixedRoyalty)).OfType<RoyaltyLowersPayKeepingReducedRate>().ShouldBeEmpty();
    }

    // The company paying the contributions, no payslip gross up to what the company pays has a taxable
    // pay of four times the costly car's benefit, with the royalty fixed or following the pay: the royalty
    // sets nothing on the rate, and the pay test is still said.
    [Theory]
    [MemberData(nameof(Royalties))]
    public void Of_royaltyWhereNoGrossKeepsTheRateCompanyPays_setsNoConditionOnIt(CopyrightRoyalty royalty)
    {
        var conditions = Conditions(Scenario(paidBy: ContributionsPaidBy.Company, car: CostlyCar, royalty: royalty));

        (conditions.OfType<RoyaltyLowersPayKeepingReducedRate>().Count(), conditions.OfType<PayKeepsReducedRate>().Count()).ShouldBe((0, 1));
    }

    // It holds where the royalty leaves a pay that keeps the rate below the one without it, or one where
    // the year without it has none.
    public static TheoryData<decimal?, decimal?, bool> Lowerings => new()
    {
        { 40_000m, 50_000m, true },
        { 50_000m, 50_000m, false },
        { 40_000m, null, true },
        { null, 50_000m, false },
        { null, null, false },
    };

    [Theory]
    [MemberData(nameof(Lowerings))]
    public void Holds_royalty_isWhetherItLowersThePayKeepingTheRate(decimal? with, decimal? without, bool holds)
    {
        new RoyaltyLowersPayKeepingReducedRate(with, without, false).Holds.ShouldBe(holds);
    }

    // At 50,000 of pay the most the year pays out is 40,000 (YearResultTests).
    [Theory]
    [InlineData(40_000, true)]
    [InlineData(40_000.01, false)]
    public void Of_reserve_saysWhetherWhatTheYearPaysOutCoversIt(decimal reserve, bool holds)
    {
        var condition = Single<PayOutCoversReserve>(Scenario(reserve: reserve));

        (condition, condition.Holds).ShouldBe((new PayOutCoversReserve(reserve, 40_000m), holds));
    }

    [Fact]
    public void Of_noReserve_setsNoConditionOnIt()
    {
        Conditions(Scenario()).OfType<PayOutCoversReserve>().ShouldBeEmpty();
    }

    private static IReadOnlyList<YearCondition> Conditions(Scenario scenario) => YearConditions.Of(Run(scenario));

    private static T Single<T>(Scenario scenario)
        where T : YearCondition =>
        Conditions(scenario).OfType<T>().ShouldHaveSingleItem();
}

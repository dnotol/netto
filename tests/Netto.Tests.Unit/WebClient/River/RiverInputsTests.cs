using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The river opens on the example year of YearSimulationTests: 120,000 revenue, 20,000 running costs,
// 50,000 pay at 7%, nothing kept, VVPRbis from the third distribution, which leaves 62,529.24 in
// the pocket; the pay alone leaves 29,579.30.
public class RiverInputsTests
{
    [Fact]
    public void Example_runsTheWholeYear()
    {
        var run = RiverInputs.Example.Run();

        (run.Error, run.Year?.NetInPocket).ShouldBe((null, 62_529.24m));
    }

    [Fact]
    public void Example_amountsOfMoneyAreTheExamples()
    {
        var inputs = RiverInputs.Example;

        new[] { inputs.Company!.Revenue.Origin, inputs.Company.RunningCosts.Origin, inputs.DirectorPay.Origin, inputs.Reserve.Origin, inputs.EarlierProfits.Origin }
            .ShouldAllBe(origin => origin == InputOrigin.Example);
    }

    [Fact]
    public void Example_hasNoEarlierProfits()
    {
        RiverInputs.Example.EarlierProfits.Value.ShouldBe(0m);
    }

    [Fact]
    public void WithEarlierProfits_makesThemTheUsers()
    {
        RiverInputs.Example.WithEarlierProfits(60_000m).EarlierProfits.ShouldBe(new Input<decimal>(60_000m, InputOrigin.Yours));
    }

    // YearSimulationTests: 60,000 kept from earlier years, 30,000 kept at year end.
    [Fact]
    public void Run_earlierProfitsReachTheEngine()
    {
        RiverInputs.Example.WithEarlierProfits(60_000m).WithReserve(30_000m).Run().Year.ShouldNotBeNull().NetInPocket.ShouldBe(87_129.24m);
    }

    [Fact]
    public void Run_negativeEarlierProfits_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.WithEarlierProfits(-0.01m).Run();

        (run.Year, run.Error).ShouldBe((null, YearSimulation.EarlierProfitsNegative));
    }

    [Fact]
    public void Example_ratesAndStatusesAreDefaults()
    {
        var inputs = RiverInputs.Example;

        new[] { inputs.SmallCompany.Origin, inputs.FirstFourTaxPeriods.Origin, inputs.DividendCategory.Origin, inputs.Municipality.Origin }
            .ShouldAllBe(origin => origin == InputOrigin.Default);
    }

    [Fact]
    public void WithRevenue_makesTheRevenueTheUsers()
    {
        var inputs = RiverInputs.Example.WithRevenue(150_000m);

        inputs.Company!.Revenue.ShouldBe(new Input<decimal>(150_000m, InputOrigin.Yours));
    }

    [Fact]
    public void WithRevenue_leavesTheRunningCostsAnExample()
    {
        var inputs = RiverInputs.Example.WithRevenue(150_000m);

        inputs.Company!.RunningCosts.Origin.ShouldBe(InputOrigin.Example);
    }

    [Fact]
    public void WithEveryOtherInput_makesItTheUsers()
    {
        var inputs = RiverInputs.Example
            .WithRunningCosts(25_000m)
            .WithSmallCompany(false)
            .WithFirstFourTaxPeriods(true)
            .WithDirectorPay(40_000m)
            .WithReserve(5_000m)
            .WithDividendCategory(WithholdingCategory.Standard);

        (inputs.Company!.RunningCosts, inputs.SmallCompany, inputs.FirstFourTaxPeriods, inputs.DirectorPay, inputs.Reserve, inputs.DividendCategory)
            .ShouldBe((new Input<decimal>(25_000m, InputOrigin.Yours), new Input<bool>(false, InputOrigin.Yours),
                new Input<bool>(true, InputOrigin.Yours), new Input<decimal>(40_000m, InputOrigin.Yours),
                new Input<decimal>(5_000m, InputOrigin.Yours), new Input<WithholdingCategory>(WithholdingCategory.Standard, InputOrigin.Yours)));
    }

    [Fact]
    public void WithMunicipality_takesTheChoiceWithItsOwnOrigin()
    {
        var namur = MunicipalRates.IncomeYear2026.Municipalities.Single(municipality => municipality.Name == "Namur");

        var inputs = RiverInputs.Example.WithMunicipality(RiverInputs.Example.Municipality.Pick(namur));

        (inputs.Municipality.Municipality, inputs.Municipality.Origin).ShouldBe((namur, InputOrigin.Derived));
    }

    [Fact]
    public void Run_everyInputReachesTheEngine()
    {
        var run = RiverInputs.Example.WithReserve(15_000m).WithDividendCategory(WithholdingCategory.Standard).Run();

        (run.Year!.Dividend.Dividend, run.Year.Dividend.Rate).ShouldBe((25_000.00m, 0.30m));
    }

    [Fact]
    public void Run_yearsDirectorPayIsThePayTheCardsRead()
    {
        var run = RiverInputs.Example.Run();

        run.Pay.ShouldBeSameAs(run.Year!.DirectorPay);
    }

    [Fact]
    public void SkipCompany_runsThePayAlone()
    {
        var run = RiverInputs.Example.SkipCompany().Run();

        (run.Year, run.Pay?.GrossPay, run.Pay?.NetInPocket).ShouldBe((null, 50_000m, 29_579.30m));
    }

    [Fact]
    public void SkipCompany_keepsThePayAndItsOrigin()
    {
        var inputs = RiverInputs.Example.WithDirectorPay(42_000m).SkipCompany();

        (inputs.Company, inputs.DirectorPay).ShouldBe((null, new Input<decimal>(42_000m, InputOrigin.Yours)));
    }

    [Fact]
    public void EnterCompany_afterSkipping_startsTheUsersYearFromNothing()
    {
        var inputs = RiverInputs.Example.SkipCompany().EnterCompany();

        inputs.Company.ShouldBe(new CompanyFigures(new Input<decimal>(0m, InputOrigin.Yours), new Input<decimal>(0m, InputOrigin.Yours)));
    }

    [Fact]
    public void EnterCompany_whenTheCompanyIsThere_changesNothing()
    {
        var inputs = RiverInputs.Example.EnterCompany();

        inputs.ShouldBe(RiverInputs.Example);
    }

    [Fact]
    public void PayCeiling_isTheProfitBeforePay()
    {
        RiverInputs.Example.PayCeiling.ShouldBe(100_000m);
    }

    [Fact]
    public void PayCeiling_lossBeforePay_isZero()
    {
        RiverInputs.Example.WithRevenue(10_000m).PayCeiling.ShouldBe(0m);
    }

    [Fact]
    public void PayCeiling_companySkipped_isNone()
    {
        RiverInputs.Example.SkipCompany().PayCeiling.ShouldBeNull();
    }

    // The pay set stays the user's: it comes back once the profit rises again.
    [Fact]
    public void Run_payAboveTheProfitBeforePay_paysTheProfit()
    {
        var inputs = RiverInputs.Example.WithRevenue(60_000m);

        (inputs.Run().Pay?.GrossPay, inputs.DirectorPay.Value).ShouldBe((40_000m, 50_000m));
    }

    [Fact]
    public void Run_lossBeforePay_paysNothing()
    {
        var run = RiverInputs.Example.WithRevenue(10_000m).Run();

        (run.Pay?.GrossPay, run.Year?.KeptInCompany).ShouldBe((0m, -10_000m));
    }

    [Fact]
    public void Run_negativePay_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.WithDirectorPay(-0.01m).Run();

        (run.Pay, run.Year, run.Error).ShouldBe((null, null, Netto.Core.DirectorPay.GrossToNet.GrossPayNegative));
    }

    [Fact]
    public void Run_payAloneWithANegativePay_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.SkipCompany().WithDirectorPay(-0.01m).Run();

        (run.Pay, run.Error).ShouldBe((null, Netto.Core.DirectorPay.GrossToNet.GrossPayNegative));
    }

    [Fact]
    public void Run_negativeReserve_isRefusedWithTheEnginesKey()
    {
        var run = RiverInputs.Example.WithReserve(-0.01m).Run();

        (run.Year, run.Error).ShouldBe((null, YearSimulation.ReserveNegative));
    }

    // SplitSearchTests: on the example, 20,799 leaves the most.
    [Fact]
    public void SearchSplit_example_findsTheBestPay()
    {
        RiverInputs.Example.SearchSplit().ShouldNotBeNull().Best.ShouldNotBeNull().Pay.ShouldBe(20_799m);
    }

    [Fact]
    public void SearchSplit_holdsTheReserveSet()
    {
        RiverInputs.Example.WithReserve(30_000m).SearchSplit().ShouldNotBeNull().Best.ShouldNotBeNull().KeptInCompany.ShouldBe(30_000m);
    }

    [Fact]
    public void SearchSplit_companySkipped_isNone()
    {
        RiverInputs.Example.SkipCompany().SearchSplit().ShouldBeNull();
    }

    [Fact]
    public void SearchSplit_inputRefused_isNone()
    {
        RiverInputs.Example.WithReserve(-0.01m).SearchSplit().ShouldBeNull();
    }

    // The search does not depend on the pay, so moving it alone needs no new search.
    [Fact]
    public void SameBesidesPay_onlyThePayMoved_isTrue()
    {
        RiverInputs.Example.WithDirectorPay(20_799m).SameBesidesPay(RiverInputs.Example).ShouldBeTrue();
    }

    [Fact]
    public void SameBesidesPay_anotherInputChanged_isFalse()
    {
        var changed = new[]
        {
            RiverInputs.Example.WithReserve(10_000m),
            RiverInputs.Example.WithRevenue(130_000m),
            RiverInputs.Example.WithSmallCompany(false),
            RiverInputs.Example.WithMunicipality(RiverInputs.Example.Municipality.Enter(0.08m)),
            RiverInputs.Example.WithDividendCategory(WithholdingCategory.Standard),
            RiverInputs.Example.WithEarlierProfits(60_000m),
            RiverInputs.Example.SkipCompany(),
        };

        changed.ShouldAllBe(inputs => !inputs.SameBesidesPay(RiverInputs.Example));
    }
}

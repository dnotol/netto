using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Support;

// The made-up company the engine's tests share: 120,000 of revenue and 20,000 of running costs, a small
// company past its first four tax periods, 50,000 of pay at a 7% municipal rate, a VVPRbis dividend from the
// third distribution, nothing kept, enough prepaid and withheld to leave no surcharge, and no lever.
public class FixturesTests
{
    [Fact]
    public void Scenario_noArguments_isTheMadeUpCompany()
    {
        var scenario = Scenario();

        (scenario.IncomeYear, scenario.Revenue, scenario.RunningCosts, scenario.Company, scenario.DirectorPay, scenario.MunicipalRate.Value, scenario.Reserve,
            scenario.DividendCategory, scenario.EarlierProfits, scenario.ContributionsPaidBy)
            .ShouldBe((2026, 120_000m, 20_000m, SmallPastStartUp, 50_000m, 0.07m, 0m, WithholdingCategory.VvprbisThirdYearOnwards, 0m, ContributionsPaidBy.Director));
    }

    [Fact]
    public void Scenario_noArguments_prepaysAndWithholdsEnoughToLeaveNoSurchargeAndHasNoLever()
    {
        var scenario = Scenario();

        (scenario.AdvancePayments, scenario.TaxPrepayments, scenario.Vouchers, scenario.Allowances, scenario.Rent, scenario.Car, scenario.Copyright)
            .ShouldBe((Prepaid, Withheld, VoucherGrants.None, ExpenseAllowances.None, HomeOfficeRent.None, CarFacts.None, CopyrightRoyalty.None));
    }

    [Fact]
    public void Scenario_namedArguments_replaceTheirDefaults()
    {
        var company = new CompanyStatus(SmallCompany: false, FirstFourTaxPeriods: true);
        var royalty = new CopyrightRoyalty(5_000m);

        var scenario = Scenario(
            directorPay: 30_000m, reserve: 4_000m, revenue: 90_000m, company: company, category: WithholdingCategory.Standard, earlierProfits: 7_000m,
            advancePayments: AdvancePayments.None, taxPrepayments: TaxPrepayments.AsWithheld, paidBy: ContributionsPaidBy.Company,
            vouchers: AllVouchers, allowances: FlatRateAllowances, rent: RentAtTheCap, car: ElectricCar, royalty: royalty);

        (scenario.DirectorPay, scenario.Reserve, scenario.Revenue, scenario.Company, scenario.DividendCategory, scenario.EarlierProfits, scenario.AdvancePayments,
            scenario.TaxPrepayments, scenario.ContributionsPaidBy, scenario.Vouchers, scenario.Allowances, scenario.Rent, scenario.Car, scenario.Copyright)
            .ShouldBe((30_000m, 4_000m, 90_000m, company, WithholdingCategory.Standard, 7_000m, AdvancePayments.None, TaxPrepayments.AsWithheld,
                ContributionsPaidBy.Company, AllVouchers, FlatRateAllowances, RentAtTheCap, ElectricCar, royalty));
    }

    // The engine tests take the river's example as its scenario rather than through the client, so this is
    // where they would drift from it: the example moves, this fails, and the numbers they hold are decided again.
    [Fact]
    public void RiverExample_isTheScenarioTheRiversExampleRuns()
    {
        RiverExample.ShouldBe(RiverInputs.Example.MoveScenario());
    }

    [Fact]
    public void Run_madeUpCompany_givesItsYear()
    {
        Run(Scenario()).NetInPocket.ShouldBe(62_529.24m);
    }

    [Fact]
    public void Run_scenarioTheEngineRefuses_failsTheTest()
    {
        Should.Throw<ShouldAssertException>(() => Run(Scenario(reserve: -1m)));
    }

    [Fact]
    public void Search_madeUpCompany_givesTheBestPay()
    {
        Search(Scenario()).Best.ShouldNotBeNull().Pay.ShouldBe(20_799m);
    }

    [Fact]
    public void Search_scenarioTheEngineRefuses_failsTheTest()
    {
        Should.Throw<ShouldAssertException>(() => Search(Scenario(reserve: -1m)));
    }
}

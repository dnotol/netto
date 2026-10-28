using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// The year of a director taxed jointly with a spouse or legal cohabitant: the pay is taxed in the
// household's joint assessment, and the split's search and the moves, which only run the year, follow
// it. What the river counts is what the director's income adds to the household's tax, so the year
// still adds up to what the company earns, the partner's own income and tax staying outside it.
public class YearSimulationHouseholdTests
{
    private static readonly Household NonEarningPartner = new(HouseholdStatus.MarriedOrLegalCohabitant);

    private static readonly Household EarningPartner = new(HouseholdStatus.MarriedOrLegalCohabitant, 40_000m);

    [Fact]
    public void Run_partnerWithoutIncome_taxesThePayInTheJointAssessment()
    {
        var year = Run(Scenario(household: NonEarningPartner));
        var (pay, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, household: NonEarningPartner);

        (year.DirectorPay.IncomeTax, year.DirectorPay.Household).ShouldBe((pay.ShouldNotBeNull().IncomeTax, pay.Household));
        year.PersonalTax.ShouldBeLessThan(Run(Scenario()).PersonalTax);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(40_000)]
    public void Run_household_layersPocketAndCompanyAddUpToTheProfitBeforePay(int partnerIncome)
    {
        var year = Run(Scenario(household: new Household(HouseholdStatus.MarriedOrLegalCohabitant, partnerIncome)));

        (year.CorporateTax + year.SocialContributions + year.PersonalTax + year.NetInPocket + year.KeptInCompany).ShouldBe(year.ProfitBeforePay);
    }

    [Fact]
    public void Run_yearTaxedSeparately_isTheDirectorTaxedAlone()
    {
        var year = Run(Scenario(household: NonEarningPartner with { TaxedSeparately = true }));

        (year.PersonalTax, year.NetInPocket, year.DirectorPay.Household).ShouldBe((Run(Scenario()).PersonalTax, Run(Scenario()).NetInPocket, null));
    }

    [Fact]
    public void Run_jointWithNegativePartnerIncome_isRefused()
    {
        var (value, error) = YearSimulation.Run(Scenario(household: new Household(HouseholdStatus.MarriedOrLegalCohabitant, -1m)));

        (value, error?.Input).ShouldBe((null, RefusedInput.PartnerIncome));
    }

    // The search runs the year at each pay with the household, so its best pay is the year's best: it
    // leaves what the year run at that pay leaves, and no pay a thousand either side leaves more.
    [Theory]
    [InlineData(0)]
    [InlineData(40_000)]
    public void Search_household_findsTheBestPayOfTheJointYear(int partnerIncome)
    {
        var scenario = RiverExample with { Household = new Household(HouseholdStatus.MarriedOrLegalCohabitant, partnerIncome) };

        var best = Search(scenario).Best.ShouldNotBeNull();

        best.NetInPocket.ShouldBe(Run(scenario with { DirectorPay = best.Pay }).NetInPocket);
        Run(scenario with { DirectorPay = best.Pay + 1_000m }).NetInPocket.ShouldBeLessThanOrEqualTo(best.NetInPocket);
        Run(scenario with { DirectorPay = Math.Max(0m, best.Pay - 1_000m) }).NetInPocket.ShouldBeLessThanOrEqualTo(best.NetInPocket);
    }

    // A partner earning nothing makes the pay cheaper in tax, which moves the split's best pay.
    [Fact]
    public void Search_partnerWithoutIncome_movesTheBestPay()
    {
        var joint = Search(RiverExample with { Household = NonEarningPartner }).Best.ShouldNotBeNull();
        var alone = Search(RiverExample).Best.ShouldNotBeNull();

        joint.Pay.ShouldNotBe(alone.Pay);
        joint.NetInPocket.ShouldBeGreaterThan(alone.NetInPocket);
    }

    // The moves measure each gain as the difference of two years run with the household: the split's
    // move alone leaves what the search's best pay does.
    [Fact]
    public void MoveSearch_partnerWithoutIncome_measuresTheGainOnTheJointYear()
    {
        var today = RiverExample with { Household = NonEarningPartner };

        var plan = MoveSearch.Run(today, [new SplitMoveFinder()]).Value.ShouldNotBeNull();

        (plan.TodayInPocket, plan.PlanInPocket).ShouldBe((Run(today).NetInPocket, Search(today).Best.ShouldNotBeNull().NetInPocket));
    }
}

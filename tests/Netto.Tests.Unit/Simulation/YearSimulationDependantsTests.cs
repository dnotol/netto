using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Shouldly;
using static Netto.Tests.Support.Fixtures;

namespace Netto.Tests.Simulation;

// The year of a director with dependants, alone or with a partner: the pay is taxed with their supplements
// and credit, and the split's search and the moves, which only run the year, follow it. The credit is
// refunded, so the year still adds up to what the company earns.
public class YearSimulationDependantsTests
{
    private static readonly Dependants TwoChildren = new(Children: 2, ChildrenUnder3: 1);

    public static TheoryData<string, Household> Households => new()
    {
        { "a single parent", Household.None with { Dependants = TwoChildren with { SingleParent = true } } },
        { "a partner earning nothing, two children", NonEarningPartner with { Dependants = TwoChildren } },
        { "a partner earning 40,000, two children", EarningPartner with { Dependants = TwoChildren } },
    };

    [Theory]
    [MemberData(nameof(Households))]
    public void Run_dependants_taxesThePayWithThem(string household, Household facts)
    {
        _ = household;

        var year = Run(Scenario(household: facts));

        var (pay, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), Withheld, household: facts);
        (year.DirectorPay.IncomeTax, year.DirectorPay.ChildTaxCredit, year.DirectorPay.Assessment).ShouldBe((pay.ShouldNotBeNull().IncomeTax, pay.ChildTaxCredit, pay.Assessment));
        year.NetInPocket.ShouldBeGreaterThan(Run(Scenario(household: facts with { Dependants = Dependants.None })).NetInPocket);
    }

    // The pocket, the company and every layer still add up to the made-up company's 100,000 of profit before
    // the director, the credit refunded, at a pay low enough for a credit with a rent taxing part of it
    // away too, the rent the company pays reaching the pocket.
    [Theory]
    [MemberData(nameof(Households))]
    public void Run_dependants_layersPocketAndCompanyAddUpToTheProfitBeforePay(string household, Household facts)
    {
        _ = household;
        foreach (var scenario in new[] { Scenario(household: facts), Scenario(directorPay: 20_000m, household: facts, rent: RentAtTheCap) })
        {
            var year = Run(scenario);

            (year.CorporateTax + year.SocialContributions + year.PersonalTax + year.NetInPocket + year.KeptInCompany).ShouldBe(100_000m);
        }
    }

    // A single parent on a low pay: the credit reaches the pocket and the assessment's balance, refunded.
    [Fact]
    public void Run_singleParentOnALowPay_refundsTheCreditOnTheBalance()
    {
        var facts = Household.None with { Dependants = TwoChildren with { SingleParent = true } };

        var year = Run(Scenario(directorPay: 20_000m, household: facts, taxPrepayments: new TaxPrepayments(0m, AdvancePayments.None)));

        var pay = year.DirectorPay;
        (pay.ChildTaxCredit, pay.Assessment.Balance).ShouldBe((1_100m, -pay.LowEarnedIncomeCredit - 1_100m));
    }

    [Fact]
    public void Run_dependantsOutOfRange_isRefusedOnTheirField()
    {
        var (value, error) = YearSimulation.Run(Scenario(household: Household.None with { Dependants = new Dependants(Children: 1, ChildrenUnder3: 2) }));

        (value, error?.Input).ShouldBe((null, RefusedInput.ChildrenUnder3));
    }

    // The search runs the year at each pay with the dependants, so its best pay is the year's best.
    [Theory]
    [MemberData(nameof(Households))]
    public void Search_dependants_findsTheBestPayOfTheYear(string household, Household facts)
    {
        _ = household;
        var scenario = RiverExample with { Household = facts };

        var best = Search(scenario).Best.ShouldNotBeNull();

        best.NetInPocket.ShouldBe(Run(scenario with { DirectorPay = best.Pay }).NetInPocket);
        Run(scenario with { DirectorPay = best.Pay + 1_000m }).NetInPocket.ShouldBeLessThanOrEqualTo(best.NetInPocket);
        Run(scenario with { DirectorPay = Math.Max(0m, best.Pay - 1_000m) }).NetInPocket.ShouldBeLessThanOrEqualTo(best.NetInPocket);
    }

    // The moves measure each gain on the year with the dependants.
    [Fact]
    public void MoveSearch_singleParent_measuresTheGainOnTheYearWithTheChildren()
    {
        var today = RiverExample with { Household = Household.None with { Dependants = TwoChildren with { SingleParent = true } } };

        var plan = MoveSearch.Run(today, [new SplitMoveFinder()]).Value.ShouldNotBeNull();

        (plan.TodayInPocket, plan.PlanInPocket).ShouldBe((Run(today).NetInPocket, Search(today).Best.ShouldNotBeNull().NetInPocket));
    }
}

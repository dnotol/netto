using Netto.Core.DirectorPay;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The partner is an optional module of the plan: while in it, the river hands the engine the household,
// a spouse or legal cohabitant with the facts the card holds, and the year is taxed jointly, the company's
// year or the pay alone; out of it, the director is taxed alone and the facts wait, kept. A year taxed
// separately is taxed alone too. The engine refuses a partner's income below nothing, on the card that holds it.
public class HouseholdModuleTests
{
    private static readonly RiverInputs WithPartner = RiverInputs.Example.AddToPlan(OptionalModule.Household);

    [Fact]
    public void Run_partnerInThePlan_taxesTheYearJointly()
    {
        var run = WithPartner.Run();

        run.Pay.ShouldNotBeNull().Household.ShouldNotBeNull();
    }

    // The marital quotient takes part of the pay off the scale's higher bands: more reaches the pocket.
    [Fact]
    public void Run_partnerWithNoIncome_leavesMoreInThePocketThanAlone()
    {
        var (alone, jointly) = (RiverInputs.Example.Run().Year.ShouldNotBeNull(), WithPartner.Run().Year.ShouldNotBeNull());

        jointly.NetInPocket.ShouldBeGreaterThan(alone.NetInPocket);
    }

    [Fact]
    public void Run_partnerTakenOut_taxesTheDirectorAloneAndKeepsTheFacts()
    {
        var inputs = WithPartner.WithPartnerIncome(9_000m).TakeOutOfPlan(OptionalModule.Household);

        (inputs.Run().Pay.ShouldNotBeNull().Household, inputs.Household.PartnerIncome.Value).ShouldBe((null, 9_000m));
    }

    [Fact]
    public void Run_companySkippedWithAPartner_taxesThePayJointly()
    {
        var run = WithPartner.SkipCompany().Run();

        (run.Year, run.Pay.ShouldNotBeNull().Household is not null).ShouldBe((null, true));
    }

    [Fact]
    public void Run_yearTaxedSeparately_taxesTheDirectorAlone()
    {
        WithPartner.WithTaxedSeparately(true).Run().Pay.ShouldNotBeNull().Household.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Run_partnersIncomeBelowNothing_isRefusedOnTheHouseholdCard(bool companySkipped)
    {
        var inputs = WithPartner.WithPartnerIncome(-1m);

        var error = (companySkipped ? inputs.SkipCompany() : inputs).Run().Error.ShouldNotBeNull();

        (error, RiverErrors.Owner(error)).ShouldBe((GrossToNet.PartnerIncomeNegative, RiverModule.Household));
    }

    // Out of the plan the engine never sees the partner, so never refuses what they earn.
    [Fact]
    public void Run_partnersIncomeBelowNothingOutOfThePlan_isNotRefused()
    {
        WithPartner.WithPartnerIncome(-1m).TakeOutOfPlan(OptionalModule.Household).Run().Error.ShouldBeNull();
    }
}

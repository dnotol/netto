using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The dependants are the household's, with or without a partner: while the household is in the plan the
// river hands them to the engine, the company's year or the pay alone, and the engine raises the tax-free
// amount by their supplements; out of it, the director has none and the facts wait, kept. The engine
// refuses a count below nothing, and more children under 3 than children, on the card that holds them.
// The supplements' amounts are 2026's, from the indexation notice for tax year 2027.
public class DependantsModuleTests
{
    private static readonly RiverInputs Household = RiverInputs.Example.AddToPlan(OptionalModule.Household);

    private static readonly RiverInputs SingleParent = Household.WithPartner(false).WithChildren(2).WithSingleParent(true);

    [Fact]
    public void Run_singleParentWithTwoChildren_raisesTheirTaxFreeAmountAlone()
    {
        var pay = SingleParent.Run().Pay.ShouldNotBeNull();

        (pay.Household, pay.Supplements, pay.SupplementsTo).ShouldBe((null, new TaxFreeAmountSupplements(5_130m, 0m, 1_980m, 0m, 0m), Taxpayer.Director));
    }

    [Fact]
    public void Run_singleParent_savesThePayTax()
    {
        SingleParent.Run().Pay.ShouldNotBeNull().DependantsSave.ShouldBeGreaterThan(0m);
    }

    [Fact]
    public void Run_companySkipped_countsTheDependantsOnThePay()
    {
        var run = SingleParent.SkipCompany().Run();

        (run.Year, run.Pay.ShouldNotBeNull().Supplements.Total).ShouldBe((null, 7_110m));
    }

    [Fact]
    public void Run_householdTakenOut_countsNoDependantAndKeepsThem()
    {
        var inputs = SingleParent.TakeOutOfPlan(OptionalModule.Household);

        (inputs.Run().Pay.ShouldNotBeNull().Supplements, inputs.Household.Dependants.Children.Value).ShouldBe((TaxFreeAmountSupplements.None, 2));
    }

    // In a joint return the supplements go to the partner with the higher taxable income.
    [Fact]
    public void Run_partnerEarningMore_givesThemTheSupplements()
    {
        var pay = Household.WithPartnerIncome(150_000m).WithChildren(2).Run().Pay.ShouldNotBeNull();

        (pay.Household is not null, pay.SupplementsTo).ShouldBe((true, Taxpayer.Partner));
    }

    public static TheoryData<string, RiverInputs, Refusal> Refused => new()
    {
        { "children below nothing", Household.WithChildren(-1), GrossToNet.ChildrenNegative },
        { "more children under 3 than children", Household.WithChildren(1).WithChildrenUnder3(2), GrossToNet.ChildrenUnder3OutOfRange },
        { "dependants in care below nothing", Household.WithDependantsInCare(-1), GrossToNet.DependantsInCareNegative },
        { "other dependants below nothing", Household.WithOtherDependants(-1), GrossToNet.OtherDependantsNegative },
    };

    [Theory]
    [MemberData(nameof(Refused))]
    public void Run_countRefused_isRefusedOnTheHouseholdCard(string _, RiverInputs inputs, Refusal refusal)
    {
        var error = inputs.Run().Error.ShouldNotBeNull();

        (error, RiverErrors.Owner(error)).ShouldBe((refusal, RiverModule.Household));
    }

    [Theory]
    [MemberData(nameof(Refused))]
    public void Run_countRefusedWithTheCompanySkipped_isRefusedOnTheHouseholdCard(string _, RiverInputs inputs, Refusal refusal)
    {
        var error = inputs.SkipCompany().Run().Error.ShouldNotBeNull();

        (error, RiverErrors.Owner(error)).ShouldBe((refusal, RiverModule.Household));
    }

    // Out of the plan the engine never sees the dependants, so never refuses them.
    [Fact]
    public void Run_countBelowNothingOutOfThePlan_isNotRefused()
    {
        Household.WithChildren(-1).TakeOutOfPlan(OptionalModule.Household).Run().Error.ShouldBeNull();
    }
}

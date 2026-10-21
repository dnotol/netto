using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Which optional modules a plan holds, in the river's order: the vouchers, the expense allowances,
// then the home office rent.
public class PlanCompositionTests
{
    private static readonly PlanComposition Everything = new(true, true, true);

    [Fact]
    public void None_holdsNoModule()
    {
        (PlanComposition.Modules.Any(PlanComposition.None.Holds), PlanComposition.None.Count).ShouldBe((false, 0));
    }

    [Fact]
    public void Modules_areInTheRiversOrder()
    {
        PlanComposition.Modules.ShouldBe([OptionalModule.Vouchers, OptionalModule.Allowances, OptionalModule.Rent]);
    }

    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    public void With_moduleAdded_holdsItAndNoOther(OptionalModule module)
    {
        var composition = PlanComposition.None.With(module, true);

        PlanComposition.Modules.Where(composition.Holds).ShouldBe([module]);
    }

    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    public void With_moduleTakenOut_keepsTheOthers(OptionalModule module)
    {
        var composition = Everything.With(module, false);

        (composition.Holds(module), composition.Count).ShouldBe((false, 2));
    }

    [Fact]
    public void Count_isTheModulesInThePlan()
    {
        PlanComposition.None.With(OptionalModule.Vouchers, true).With(OptionalModule.Rent, true).Count.ShouldBe(2);
    }

    // What a module leaves goes on to the next module in the plan, the split after the last one.
    [Theory]
    [InlineData(true, true, true, OptionalModule.Vouchers, OptionalModule.Allowances)]
    [InlineData(true, false, true, OptionalModule.Vouchers, OptionalModule.Rent)]
    [InlineData(true, false, false, OptionalModule.Vouchers, null)]
    [InlineData(true, true, true, OptionalModule.Allowances, OptionalModule.Rent)]
    [InlineData(false, true, false, OptionalModule.Allowances, null)]
    [InlineData(true, true, true, OptionalModule.Rent, null)]
    public void NextAfter_isTheNextModuleInThePlan(bool vouchers, bool allowances, bool rent, OptionalModule module, OptionalModule? next)
    {
        new PlanComposition(vouchers, allowances, rent).NextAfter(module).ShouldBe(next);
    }

    [Fact]
    public void TakenOutSince_moduleTakenOut_isThatModule()
    {
        Everything.With(OptionalModule.Allowances, false).TakenOutSince(Everything).ShouldBe(OptionalModule.Allowances);
    }

    [Fact]
    public void TakenOutSince_moduleAddedOrNothingChanged_isNone()
    {
        (Everything.TakenOutSince(PlanComposition.None), Everything.TakenOutSince(Everything)).ShouldBe(((OptionalModule?)null, (OptionalModule?)null));
    }
}

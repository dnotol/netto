using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Which optional modules a plan holds, in the river's order: the vouchers, the expense allowances,
// the home office rent, the company car, then the copyright royalty, and the partner.
public class PlanCompositionTests
{
    private static readonly PlanComposition Everything = new(true, true, true, true, true, true, true);

    [Fact]
    public void None_holdsNoModule()
    {
        (PlanComposition.Modules.Any(PlanComposition.None.Holds), PlanComposition.None.Count).ShouldBe((false, 0));
    }

    [Fact]
    public void Modules_areInTheRiversOrder()
    {
        PlanComposition.Modules.ShouldBe([OptionalModule.Vouchers, OptionalModule.Allowances, OptionalModule.Rent, OptionalModule.Equipment, OptionalModule.Car, OptionalModule.Copyright, OptionalModule.Household]);
    }

    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    [InlineData(OptionalModule.Car)]
    [InlineData(OptionalModule.Copyright)]
    [InlineData(OptionalModule.Household)]
    public void With_moduleAdded_holdsItAndNoOther(OptionalModule module)
    {
        var composition = PlanComposition.None.With(module, true);

        PlanComposition.Modules.Where(composition.Holds).ShouldBe([module]);
    }

    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    [InlineData(OptionalModule.Car)]
    [InlineData(OptionalModule.Copyright)]
    [InlineData(OptionalModule.Household)]
    public void With_moduleTakenOut_keepsTheOthers(OptionalModule module)
    {
        var composition = Everything.With(module, false);

        (composition.Holds(module), composition.Count).ShouldBe((false, 6));
    }

    [Fact]
    public void Count_isTheModulesInThePlan()
    {
        PlanComposition.None.With(OptionalModule.Vouchers, true).With(OptionalModule.Rent, true).Count.ShouldBe(2);
    }

    // What a module leaves goes on to the next module in the plan, the split after the last one.
    [Theory]
    [InlineData(true, true, true, false, OptionalModule.Vouchers, OptionalModule.Allowances)]
    [InlineData(true, false, true, false, OptionalModule.Vouchers, OptionalModule.Rent)]
    [InlineData(true, false, false, false, OptionalModule.Vouchers, null)]
    [InlineData(true, false, false, true, OptionalModule.Vouchers, OptionalModule.Car)]
    [InlineData(true, true, true, false, OptionalModule.Allowances, OptionalModule.Rent)]
    [InlineData(false, true, false, false, OptionalModule.Allowances, null)]
    [InlineData(true, true, true, false, OptionalModule.Rent, null)]
    [InlineData(true, true, true, true, OptionalModule.Rent, OptionalModule.Car)]
    [InlineData(true, true, true, true, OptionalModule.Car, null)]
    public void NextAfter_isTheNextModuleInThePlan(bool vouchers, bool allowances, bool rent, bool car, OptionalModule module, OptionalModule? next)
    {
        new PlanComposition(vouchers, allowances, rent, false, car, false, false).NextAfter(module).ShouldBe(next);
    }

    // The royalty comes after the split, which pays it beside the pay; the Copyright royalty card decides it.
    // What the levers leave goes on to the split, never to the royalty, and nothing goes on from the royalty.
    [Theory]
    [InlineData(true, false, OptionalModule.Car, null)]
    [InlineData(false, true, OptionalModule.Rent, null)]
    [InlineData(true, true, OptionalModule.Copyright, null)]
    public void NextAfter_withTheRoyalty_isNeverTheRoyalty(bool car, bool rent, OptionalModule module, OptionalModule? next)
    {
        new PlanComposition(false, false, rent, false, car, true, false).NextAfter(module).ShouldBe(next);
    }

    // The partner is beside the pay: what the levers leave goes on past it to the split.
    [Theory]
    [InlineData(OptionalModule.Car)]
    [InlineData(OptionalModule.Copyright)]
    [InlineData(OptionalModule.Household)]
    public void NextAfter_withThePartner_isNeverThePartner(OptionalModule module)
    {
        new PlanComposition(false, false, false, false, true, true, true).NextAfter(module).ShouldBeNull();
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

using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The Composition menu on the river's example (RiverInputsTests), which holds no lever in its plan.
// Each lever's values are those of its own tests: 220 days of meal vouchers bring 1,960.20 to the
// pocket, the most the circular allows for the office, internet and a computer 2,418.44, and the rent
// at the 2026 cap 981.32 once its tax is paid.
public class CompositionViewTests
{
    [Fact]
    public void From_example_listsEveryModuleOutOfThePlanWithNothingKept()
    {
        var view = Menu(RiverInputs.Example);

        view.ShouldSatisfyAllConditions(
            () => view.Count.ShouldBe(0),
            () => view.Modules.ShouldBe(new[]
            {
                new ModuleChoice(OptionalModule.Vouchers, false, null, false),
                new ModuleChoice(OptionalModule.Allowances, false, null, false),
                new ModuleChoice(OptionalModule.Rent, false, null, false),
            }));
    }

    [Theory]
    [InlineData(OptionalModule.Vouchers, 1_960.20)]
    [InlineData(OptionalModule.Allowances, 2_418.44)]
    [InlineData(OptionalModule.Rent, 981.32)]
    public void From_moduleInThePlan_saysWhatItBringsToThePocket(OptionalModule module, decimal toPocket)
    {
        var view = Menu(WithValues(module));

        (view.Count, view.Modules.Single(choice => choice.InPlan)).ShouldBe((1, new ModuleChoice(module, true, toPocket, false)));
    }

    // Added but with nothing in it yet, it brings nothing to speak of.
    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    public void From_moduleInThePlanWithNothing_bringsNothingYet(OptionalModule module)
    {
        Menu(RiverInputs.Example.AddToPlan(module)).Modules.Single(choice => choice.InPlan).ShouldBe(new ModuleChoice(module, true, null, false));
    }

    // While an amount is refused there is no year to read what it brings from.
    [Fact]
    public void From_moduleInThePlanWhileAnAmountIsRefused_bringsNothingToShow()
    {
        Menu(WithValues(OptionalModule.Vouchers).WithReserve(-1m)).Modules[0].ToPocket.ShouldBeNull();
    }

    [Theory]
    [InlineData(OptionalModule.Vouchers)]
    [InlineData(OptionalModule.Allowances)]
    [InlineData(OptionalModule.Rent)]
    public void From_moduleTakenOutWithValues_saysTheyAreKept(OptionalModule module)
    {
        var view = Menu(WithValues(module).TakeOutOfPlan(module));

        (view.Count, view.Modules.Single(choice => choice.Module == module)).ShouldBe((0, new ModuleChoice(module, false, null, true)));
    }

    // Nothing but zeros typed: no values of the user's wait for it.
    [Fact]
    public void From_moduleTakenOutWithNothingTyped_keepsNothingToSpeakOf()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers).WithMealVoucherDays(0).TakeOutOfPlan(OptionalModule.Vouchers);

        Menu(inputs).Modules[0].ValuesKept.ShouldBeFalse();
    }

    [Fact]
    public void From_everyModuleInThePlan_countsThem()
    {
        Menu(RiverInputs.Example with { Composition = new PlanComposition(true, true, true) }).Count.ShouldBe(3);
    }

    [Fact]
    public void RemovalNotice_moduleTakenOut_speaksOfIt()
    {
        var before = new PlanComposition(true, false, true);

        RemovalNotice.About(null, before, before.With(OptionalModule.Rent, false)).ShouldBe(OptionalModule.Rent);
    }

    // Another module taken out: the notice speaks of the latest.
    [Fact]
    public void RemovalNotice_anotherModuleTakenOut_speaksOfTheLatest()
    {
        var before = new PlanComposition(true, false, true);

        RemovalNotice.About(OptionalModule.Allowances, before, before.With(OptionalModule.Vouchers, false)).ShouldBe(OptionalModule.Vouchers);
    }

    // Another input changed: the notice stays while its module is out.
    [Fact]
    public void RemovalNotice_compositionUnchanged_staysWhileTheModuleIsOut()
    {
        RemovalNotice.About(OptionalModule.Rent, PlanComposition.None, PlanComposition.None).ShouldBe(OptionalModule.Rent);
    }

    // Back in the plan, from the menu or the notice's undo: nothing left to undo.
    [Fact]
    public void RemovalNotice_moduleBackInThePlan_isGone()
    {
        var after = PlanComposition.None.With(OptionalModule.Rent, true);

        RemovalNotice.About(OptionalModule.Rent, PlanComposition.None, after).ShouldBeNull();
    }

    private static CompositionView Menu(RiverInputs inputs) => CompositionView.From(inputs, inputs.Run());

    private static RiverInputs WithValues(OptionalModule module) => module switch
    {
        OptionalModule.Vouchers => RiverInputs.Example.AddToPlan(module).WithMealVoucherDays(220),
        OptionalModule.Allowances => RiverInputs.Example.AddToPlan(module).WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m),
        _ => RiverInputs.Example.AddToPlan(module).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m),
    };
}

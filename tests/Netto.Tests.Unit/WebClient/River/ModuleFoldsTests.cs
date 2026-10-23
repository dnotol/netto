using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The folds of the optional modules' cards: the name each card is remembered under, which a viewer's
// folds depend on from one version to the next, and the modules a change just added to the plan, which
// open so they can be filled.
public class ModuleFoldsTests
{
    private static readonly PlanComposition Everything = new(true, true, true, true, true);

    // Kept from one version to the next, or a viewer's folds are forgotten.
    [Theory]
    [InlineData(OptionalModule.Vouchers, "vouchers")]
    [InlineData(OptionalModule.Allowances, "allowances")]
    [InlineData(OptionalModule.Rent, "rent")]
    [InlineData(OptionalModule.Car, "car")]
    [InlineData(OptionalModule.Copyright, "copyright")]
    public void Name_eachModule_isTheNameItsCardWasAlwaysRememberedUnder(OptionalModule module, string name)
    {
        ModuleFolds.Name(module).ShouldBe(name);
    }

    [Fact]
    public void Name_everyModule_hasItsOwn()
    {
        PlanComposition.Modules.Select(ModuleFolds.Name).Distinct().Count().ShouldBe(PlanComposition.Modules.Count);
    }

    [Fact]
    public void Added_moduleAdded_isThatModule()
    {
        ModuleFolds.Added(PlanComposition.None, PlanComposition.None.With(OptionalModule.Rent, true)).ShouldBe([OptionalModule.Rent]);
    }

    [Fact]
    public void Added_severalModules_areInTheRiversOrder()
    {
        ModuleFolds.Added(Everything.With(OptionalModule.Car, false).With(OptionalModule.Vouchers, false), Everything)
            .ShouldBe([OptionalModule.Vouchers, OptionalModule.Car]);
    }

    [Fact]
    public void Added_nothingChanged_isNone()
    {
        ModuleFolds.Added(Everything, Everything).ShouldBeEmpty();
    }

    [Fact]
    public void Added_moduleTakenOut_isNone()
    {
        ModuleFolds.Added(Everything, Everything.With(OptionalModule.Allowances, false)).ShouldBeEmpty();
    }

    [Fact]
    public void RevealAdded_moduleAdded_opensItsCardAndNoOther()
    {
        var folds = new Folds(new BrowserStorage());

        ModuleFolds.RevealAdded(folds, PlanComposition.None, PlanComposition.None.With(OptionalModule.Rent, true));

        PlanComposition.Modules.Select(module => folds.IsOpen(ModuleFolds.Name(module), false))
            .ShouldBe([false, false, true, false, false]);
    }

    [Fact]
    public void RevealAdded_moduleTakenOutOrNothingChanged_opensNothing()
    {
        var folds = new Folds(new BrowserStorage());

        ModuleFolds.RevealAdded(folds, Everything, Everything.With(OptionalModule.Car, false));
        ModuleFolds.RevealAdded(folds, Everything, Everything);

        PlanComposition.Modules.Any(module => folds.IsOpen(ModuleFolds.Name(module), false)).ShouldBeFalse();
    }

    // The viewer folded the card before: the lever just added opens anyway, for the session, and the
    // viewer's word stays as it was in the browser.
    [Fact]
    public void RevealAdded_cardTheViewerFolded_opensForTheSessionOnly()
    {
        var storage = new BrowserStorage();
        var folds = new Folds(storage);
        folds.Set("car", false);

        ModuleFolds.RevealAdded(folds, PlanComposition.None, PlanComposition.None.With(OptionalModule.Car, true));

        (folds.IsOpen("car", false), new Folds(storage).IsOpen("car", true)).ShouldBe((true, false));
    }

    // A move that grants a lever the plan lacks adds its module: its card opens. One that only moves a
    // value of the plan adds none.
    [Fact]
    public void Apply_moveGrantingAModule_addsItToThePlan()
    {
        var view = MovesView.Of(RiverInputs.Example, new DateOnly(2026, 10, 22));
        var eco = view.Moves.First(move => move.Move is EcoVouchersMove);

        ModuleFolds.Added(RiverInputs.Example.Composition, view.Apply(eco, RiverInputs.Example).Composition).ShouldBe([OptionalModule.Vouchers]);
    }

    [Fact]
    public void Apply_moveOfThePay_addsNoModule()
    {
        var view = MovesView.Of(RiverInputs.Example, new DateOnly(2026, 10, 22));
        var pay = view.Moves.First(move => move.Move is PayMove);

        ModuleFolds.Added(RiverInputs.Example.Composition, view.Apply(pay, RiverInputs.Example).Composition).ShouldBeEmpty();
    }

    [Fact]
    public void Apply_moveOfAModuleAlreadyInThePlan_addsNoModule()
    {
        var today = RiverInputs.Example;
        var view = MovesView.Of(today, new DateOnly(2026, 10, 22));
        var eco = view.Moves.First(move => move.Move is EcoVouchersMove);
        var inPlan = today.AddToPlan(OptionalModule.Vouchers);

        ModuleFolds.Added(inPlan.Composition, view.Apply(eco, inPlan).Composition).ShouldBeEmpty();
    }
}

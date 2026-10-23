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

    private static readonly DateOnly On22October = new(2026, 10, 22);

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

    // The Composition menu's change, as the card of the menu calls it: the plan with the module in or
    // out, and the card of a module just added open so it can be filled.
    [Fact]
    public void Toggled_moduleAdded_opensItsCardAndReturnsThePlanWithIt()
    {
        var folds = new Folds(new BrowserStorage());

        var changed = ModuleFolds.Toggled(folds, RiverInputs.Example, OptionalModule.Rent, inPlan: true);

        changed.ShouldBe(RiverInputs.Example.AddToPlan(OptionalModule.Rent));
        OpenCards(folds).ShouldBe([OptionalModule.Rent]);
    }

    [Fact]
    public void Toggled_moduleTakenOut_opensNothingAndReturnsThePlanWithoutIt()
    {
        var folds = new Folds(new BrowserStorage());
        var inPlan = RiverInputs.Example.AddToPlan(OptionalModule.Car);

        var changed = ModuleFolds.Toggled(folds, inPlan, OptionalModule.Car, inPlan: false);

        changed.ShouldBe(inPlan.TakeOutOfPlan(OptionalModule.Car));
        OpenCards(folds).ShouldBeEmpty();
    }

    // A module taken out keeps its fold, so Undo brings it back as it was.
    [Fact]
    public void Toggled_moduleTakenOutWhileOpen_staysOpenForUndo()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("car", true);

        ModuleFolds.Toggled(folds, RiverInputs.Example.AddToPlan(OptionalModule.Car), OptionalModule.Car, inPlan: false);

        folds.IsOpen("car", false).ShouldBeTrue();
    }

    [Fact]
    public void Toggled_moduleAlreadyInThePlan_opensNothing()
    {
        var folds = new Folds(new BrowserStorage());
        var inPlan = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);

        var changed = ModuleFolds.Toggled(folds, inPlan, OptionalModule.Vouchers, inPlan: true);

        changed.ShouldBe(inPlan);
        OpenCards(folds).ShouldBeEmpty();
    }

    // A move applied from the moves card: the river with the move's values, and the card of a module the
    // move grants open. A move that only moves a value of the plan opens nothing.
    [Fact]
    public void Applied_moveGrantingAModule_opensItsCardAndReturnsTheAppliedRiver()
    {
        var folds = new Folds(new BrowserStorage());
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var eco = view.Moves.First(move => move.Move is EcoVouchersMove);

        var applied = ModuleFolds.Applied(folds, view, eco, RiverInputs.Example);

        applied.ShouldBe(view.Apply(eco, RiverInputs.Example));
        OpenCards(folds).ShouldBe([OptionalModule.Vouchers]);
    }

    [Fact]
    public void Applied_moveOfThePay_opensNothingAndReturnsTheAppliedRiver()
    {
        var folds = new Folds(new BrowserStorage());
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var pay = view.Moves.First(move => move.Move is PayMove);

        var applied = ModuleFolds.Applied(folds, view, pay, RiverInputs.Example);

        applied.ShouldBe(view.Apply(pay, RiverInputs.Example));
        OpenCards(folds).ShouldBeEmpty();
    }

    [Fact]
    public void Applied_moveOfAModuleAlreadyInThePlan_opensNothing()
    {
        var folds = new Folds(new BrowserStorage());
        var view = MovesView.Of(RiverInputs.Example, On22October);
        var eco = view.Moves.First(move => move.Move is EcoVouchersMove);
        var inPlan = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);

        var applied = ModuleFolds.Applied(folds, view, eco, inPlan);

        applied.ShouldBe(view.Apply(eco, inPlan));
        OpenCards(folds).ShouldBeEmpty();
    }

    // A royalty move grants Copyright royalty; the card of the lever opens, no other.
    [Fact]
    public void Applied_royaltyMove_opensTheRoyaltyCard()
    {
        var folds = new Folds(new BrowserStorage());
        var today = RiverInputs.Example.WithCreativeShare(0.75m).WithAttestation(true);
        var royalty = new PlannedMove(new CopyrightFollowsPayMove(), 1m);

        ModuleFolds.Applied(folds, MovesView.Of(today, On22October), royalty, RiverInputs.Example);

        OpenCards(folds).ShouldBe([OptionalModule.Copyright]);
    }

    // The modules whose card is open, in the river's order, over the default of a card: folded.
    private static IReadOnlyList<OptionalModule> OpenCards(Folds folds) =>
        [.. PlanComposition.Modules.Where(module => folds.IsOpen(ModuleFolds.Name(module), false))];
}

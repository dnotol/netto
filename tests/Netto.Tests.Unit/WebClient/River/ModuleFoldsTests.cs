using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The names of the optional modules' cards: the name each card is remembered under, which a viewer's
// folds depend on from one version to the next, and the modules a change just added to the plan, which the
// plan chooses on the map so they can be filled (PlanMapChoiceTests).
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
}

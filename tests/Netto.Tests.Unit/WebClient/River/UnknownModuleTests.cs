using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The switches over the optional modules name every lever they know and refuse the rest: a lever added
// to OptionalModule without its arm throws where it is first met, never acts as the car or the split.
// The module no switch knows is (OptionalModule)99, which stands for the lever not written yet.
public class UnknownModuleTests
{
    private static readonly OptionalModule NoSuchModule = (OptionalModule)99;

    [Fact]
    public void ToPocket_aModuleItHasNoArmFor_isRefusedNotTakenForTheCar()
    {
        var river = RiverInputs.Example;
        var year = river.Run().Year.ShouldNotBeNull();

        Should.Throw<ArgumentOutOfRangeException>(() => CompositionView.ToPocket(year, river.Composition, NoSuchModule));
    }

    [Fact]
    public void HasValues_aModuleItHasNoArmFor_isRefusedNotComparedWithTheCar()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CompositionView.HasValues(RiverInputs.Example, NoSuchModule));
    }

    // Nothing comes before the first module, so a module before it makes Vouchers the next one in the plan,
    // which no lever passes on to: the stand-in for a new lever the destinations do not name.
    [Fact]
    public void After_aModuleNoLeverPassesOnTo_isRefusedNotTakenForTheSplit()
    {
        var everyLever = new PlanComposition(true, true, true, true, true, false, false);

        Should.Throw<ArgumentOutOfRangeException>(() => NextModule.After((OptionalModule)(-1), everyLever));
    }

    [Theory]
    [InlineData(OptionalModule.Car)]
    [InlineData(OptionalModule.Copyright)]
    public void After_aModuleWithNoneAfterIt_passesOnToTheSplit(OptionalModule module)
    {
        NextModule.After(module, new PlanComposition(true, true, true, true, true, true, false)).ShouldBe("split");
    }
}

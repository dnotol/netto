using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The river's inputs while the tab is open. A switch of language reloads the page, which would lose the edits
// no plan holds: the river is carried across in the tab's own storage, written as a saved plan is (PlanStorage),
// and the page that loads next opens on it, once, before the plan it was opened from.
public class RiverStateTests
{
    private static readonly RiverInputs Edited = RiverInputs.Example.WithDirectorPay(31_000m).WithRevenue(140_000m);

    private static readonly RiverInputs Opened = RiverInputs.Example.WithDirectorPay(20_799m);

    [Fact]
    public void New_riverCarried_startsOnIt()
    {
        var session = new BrowserSession();
        new RiverState(Plans(), session) { Inputs = Edited }.Carry();

        new RiverState(Plans(opened: Opened), session).Inputs.ShouldBe(Edited);
    }

    // Taken once: a later reload opens on the plan, as one always has.
    [Fact]
    public void New_afterTheCarriedRiverWasTaken_startsOnThePlanOpened()
    {
        var session = new BrowserSession();
        new RiverState(Plans(), session) { Inputs = Edited }.Carry();
        _ = new RiverState(Plans(), session);

        new RiverState(Plans(opened: Opened), session).Inputs.ShouldBe(Opened);
    }

    [Fact]
    public void New_nothingCarried_startsOnThePlanOpened()
    {
        new RiverState(Plans(opened: Opened), new BrowserSession()).Inputs.ShouldBe(Opened);
    }

    [Fact]
    public void New_nothingCarriedNorOpened_startsOnTheExample()
    {
        new RiverState(Plans(), new BrowserSession()).Inputs.ShouldBe(RiverInputs.Example);
    }

    // What cannot be read as this version reads a plan is dropped, as an unreadable plan is never opened.
    [Theory]
    [InlineData("{ broken")]
    [InlineData("""{"version":99,"today":null,"opened":null,"plans":[]}""")]
    [InlineData("""{"version":9,"today":null,"opened":null,"plans":[]}""")]
    public void New_carriedRiverUnreadable_startsOnThePlanOpened(string carried)
    {
        var session = new BrowserSession();
        session.Set(RiverState.CarriedKey, carried);

        new RiverState(Plans(opened: Opened), session).Inputs.ShouldBe(Opened);
    }

    // Carried as one plan of the saved plans' own document, so it is read back exactly as a saved plan is.
    [Fact]
    public void Carry_river_writesItAsADocumentOfOnePlan()
    {
        var session = new BrowserSession();

        new RiverState(Plans(), session) { Inputs = Edited }.Carry();

        PlanStorage.Read(session.Peek(RiverState.CarriedKey)).ShouldSatisfyAllConditions(
            read => read.Unreadable.ShouldBeFalse(),
            read => read.Book.Plans.Select(plan => plan.Inputs).ShouldBe([Edited]));
    }

    // The river a carried plan opens on still differs from the plan it was opened from: Save shows it unsaved.
    [Fact]
    public void New_riverCarried_hasTheUnsavedChangesItHadBefore()
    {
        var storage = new BrowserStorage();
        var plans = new SavedPlans(storage);
        plans.Save(Opened, number => $"Plan {number}");
        var session = new BrowserSession();
        new RiverState(plans, session) { Inputs = Edited }.Carry();

        var reloaded = new SavedPlans(storage);

        reloaded.HasUnsavedChanges(new RiverState(reloaded, session).Inputs).ShouldBeTrue();
    }

    private static SavedPlans Plans(RiverInputs? opened = null)
    {
        var plans = new SavedPlans(new BrowserStorage());
        if (opened is not null)
            plans.Save(opened, number => $"Plan {number}");
        return plans;
    }
}

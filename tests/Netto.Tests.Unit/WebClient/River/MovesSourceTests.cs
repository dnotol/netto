using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Where the Optimisation tab's moves and its badge come from: the plan starred Today while a readable one
// is, or else the plan in the river as it was when the page opened, when a plan was opened from Plans or
// when the river was saved as one, named while it is that saved plan as saved; and the search, run once
// per plan and date, never at a change of the river. A search that notes what it is asked stands in for
// the engine's, whose moves MovesViewTests holds.
public class MovesSourceTests
{
    private static readonly DateOnly On23October = new(2026, 10, 23);

    private static readonly DateOnly On24October = new(2026, 10, 24);

    // The river as a plan was opened from Plans, as the viewer has changed it since, and other plans.
    private static readonly RiverInputs Opened = RiverInputs.Example.WithDirectorPay(40_000m);

    private static readonly RiverInputs Edited = RiverInputs.Example.WithDirectorPay(30_000m);

    private static readonly RiverInputs Starred = RiverInputs.Example.WithDirectorPay(20_000m);

    private static readonly RiverInputs Other = RiverInputs.Example.WithDirectorPay(10_000m);

    [Fact]
    public void Baseline_pageOpened_isTheRiver()
    {
        var source = new MovesSource(PlanFixtures.Saved(), Edited);

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    [Fact]
    public void Baseline_riverIsTheOpenedPlan_namesIt()
    {
        var source = new MovesSource(WithoutToday(Opened), Opened);

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Plan 2"));
    }

    // The river changed since its plan was opened, and kept for the browser tab, when the page opened: it
    // is no saved plan.
    [Fact]
    public void Baseline_riverEditedBeforeThePageOpened_isNotNamed()
    {
        var source = new MovesSource(WithoutToday(Opened), Edited);

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    [Fact]
    public void Baseline_todayStarred_isToday()
    {
        var source = new MovesSource(PlanFixtures.Saved(Starred), Edited);

        source.Baseline.ShouldBe(MovesBaseline.Today(Starred));
    }

    // A plan starred Today that this version cannot read is no Today.
    [Fact]
    public void Baseline_todayUnreadable_isThePlanInTheRiver()
    {
        var source = new MovesSource(Unreadable(PlanFixtures.Saved(Starred)), Edited);

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    // Today wins while starred, and the plan opened meanwhile is the baseline again once it is deleted.
    [Fact]
    public void Baseline_todayDeleted_isThePlanOpenedBefore()
    {
        var plans = PlanFixtures.Saved(Starred, Opened);
        var source = new MovesSource(plans, Opened);
        var today = source.Baseline;

        plans.Delete(plans.Plans[0].Id);
        source.Kept();

        (today, source.Baseline).ShouldBe((MovesBaseline.Today(Starred), new MovesBaseline(Opened, IsToday: false, "Plan 2")));
    }

    [Fact]
    public void Opened_aPlan_becomesTheBaseline()
    {
        var plans = WithoutToday(Opened, Other);
        var source = new MovesSource(plans, Other);

        source.Opened(plans.Open(plans.Plans[0].Id).ShouldNotBeNull());

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Plan 2"));
    }

    // Opening the plan the river was opened from takes it as it was saved, the changes made since gone.
    [Fact]
    public void Opened_thePlanAlreadyOpened_becomesItsInputsAgain()
    {
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Edited);

        source.Opened(plans.Open(plans.Plans[0].Id).ShouldNotBeNull());

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Plan 2"));
    }

    // The river, changed since the page opened, is saved as a new plan, which it is then opened from: once
    // Today is gone, the moves are from that plan, not from the river as the page opened.
    [Fact]
    public void Kept_aSave_takesTheRiverAsSaved()
    {
        var plans = PlanFixtures.Saved(Starred);
        var source = new MovesSource(plans, Edited);

        plans.Save(Opened, number => $"Plan {number}");
        source.Kept();
        plans.Delete(plans.Plans[0].Id);
        source.Kept();

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Plan 2"));
    }

    // A star, a rename or an import opens no plan: the moves stay measured from the river as the page
    // opened, once a plan starred meanwhile is gone.
    [Theory]
    [InlineData("star")]
    [InlineData("rename")]
    [InlineData("import")]
    public void Kept_starRenameOrImport_keepsTheBaseline(string change)
    {
        var plans = WithoutToday(Other, Opened);
        var source = new MovesSource(plans, Edited);

        Change(plans, source, change);

        source.Baseline.ShouldBe(new MovesBaseline(Edited, IsToday: false));
    }

    [Fact]
    public void Kept_theOpenedPlanDeleted_keepsItUnnamed()
    {
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Opened);

        plans.Delete(plans.Plans[0].Id);
        source.Kept();

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false));
    }

    [Fact]
    public void Kept_theOpenedPlanRenamed_takesItsNewName()
    {
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Opened);

        plans.Rename(plans.Plans[0].Id, "Mine");
        source.Kept();

        source.Baseline.ShouldBe(new MovesBaseline(Opened, IsToday: false, "Mine"));
    }

    [Fact]
    public void Found_theSamePlanAndDate_searchesOnce()
    {
        var search = new Search();
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);

        source.Found(On23October);
        source.Found(On23October);

        search.Asked.ShouldBe([(Edited, On23October)]);
    }

    // The page draws the tab and its badge from the source at each change of the river, here the pay
    // dragged across sixty values: nothing of the river reaches it, and the moves are found once.
    [Fact]
    public void Found_theRiverDraggedThroughSixtyPays_searchesOnce()
    {
        var search = new Search();
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);

        var drawn = Enumerable.Range(1, 60).Select(step => Edited.WithDirectorPay(step * 1_000m))
            .Select(_ => (source.Baseline, source.Found(On23October), source.View(On23October)))
            .ToList();

        (drawn.Count, search.Asked.Count).ShouldBe((60, 1));
    }

    [Fact]
    public void Found_aPlanOpened_searchesAgainForIt()
    {
        var search = new Search();
        var plans = WithoutToday(Opened);
        var source = new MovesSource(plans, Edited, search.Run);

        source.Found(On23October);
        source.Opened(plans.Open(plans.Plans[0].Id).ShouldNotBeNull());
        source.Found(On23October);

        search.Asked.ShouldBe([(Edited, On23October), (Opened, On23October)]);
    }

    // Starring a plan Today, then another, searches from each.
    [Fact]
    public void Found_todayStarredOrMoved_searchesForIt()
    {
        var search = new Search();
        var plans = WithoutToday(Starred, Opened);
        var source = new MovesSource(plans, Edited, search.Run);

        source.Found(On23October);
        plans.Star(plans.Plans[0].Id);
        source.Kept();
        source.Found(On23October);
        plans.Star(plans.Plans[1].Id);
        source.Kept();
        source.Found(On23October);

        search.Asked.ShouldBe([(Edited, On23October), (Starred, On23October), (Opened, On23October)]);
    }

    // The date decides which deadlines are past.
    [Fact]
    public void Found_aNewDate_searchesAgain()
    {
        var search = new Search();
        var source = new MovesSource(PlanFixtures.Saved(), Edited, search.Run);

        source.Found(On23October);
        source.Found(On24October);

        search.Asked.ShouldBe([(Edited, On23October), (Edited, On24October)]);
    }

    // The river saved as it was when the page opened is starred Today, the first plan saved: the same
    // plan, whose moves are already found.
    [Fact]
    public void Found_theRiverSavedUnchanged_searchesNoMore()
    {
        var search = new Search();
        var plans = PlanFixtures.Saved();
        var source = new MovesSource(plans, Edited, search.Run);

        source.Found(On23October);
        plans.Save(Edited, number => $"Plan {number}");
        source.Kept();
        source.Found(On23October);

        (source.Baseline, search.Asked.Count).ShouldBe((MovesBaseline.Today(Edited), 1));
    }

    // What the Plans panel did, then told the page (PlansPanel.OnChanged): starring a plan, which is then
    // deleted, renaming the plan opened, or importing a file without a Today.
    private static void Change(SavedPlans plans, MovesSource source, string change)
    {
        switch (change)
        {
            case "star":
                plans.Star(plans.Plans[0].Id);
                source.Kept();
                plans.Delete(plans.Plans[0].Id);
                break;
            case "rename":
                plans.Rename(plans.Plans[1].Id, "Mine");
                break;
            case "import":
                plans.Import(WithoutToday(Starred).Export());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change), change, null);
        }

        source.Kept();
    }

    // These plans saved in this order, named from Plan 2, the river opened from the last, and none starred
    // Today: the plan saved first, which Plans stars, is deleted.
    private static SavedPlans WithoutToday(params RiverInputs[] inputs)
    {
        var plans = PlanFixtures.Saved([RiverInputs.Example, .. inputs]);
        plans.Delete(plans.Plans[0].Id);
        return plans;
    }

    // The plans as a later version of Netto would find them: each stored with a field this version does not
    // know in place of one it needs, so none can be read. They are read again from the storage.
    private static SavedPlans Unreadable(SavedPlans plans)
    {
        var storage = new RiverRendering.BrowserStorage();
        storage.Set(PlanStorage.Key, plans.Export().Replace("\"reserve\"", "\"keptAside\"", StringComparison.Ordinal));
        return new SavedPlans(storage);
    }

    // The engine's search stood in for: each plan and date it is asked for, in order, and nothing found.
    private sealed class Search
    {
        public List<(RiverInputs Plan, DateOnly Date)> Asked { get; } = [];

        public (MovePlan? Value, string? Error)? Run(RiverInputs plan, DateOnly date)
        {
            Asked.Add((plan, date));
            return null;
        }
    }
}

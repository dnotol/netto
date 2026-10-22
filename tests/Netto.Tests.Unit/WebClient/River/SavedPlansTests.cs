using Netto.Web.Client;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

public class SavedPlansTests
{
    private static readonly RiverInputs BestPay = RiverInputs.Example.WithDirectorPay(20_799m);

    private static string Name(int number) => $"Plan {number}";

    [Fact]
    public void Plans_nothingStored_isNone()
    {
        var plans = new SavedPlans(new MemoryStorage());

        (plans.Plans.Count, plans.Today, plans.Opened, plans.CouldNotRead).ShouldBe((0, null, null, false));
    }

    [Fact]
    public void Save_first_isStarredAndOpened()
    {
        var plans = new SavedPlans(new MemoryStorage());

        var saved = plans.Save(RiverInputs.Example, Name);

        (saved.Name, saved.Inputs, plans.Today, plans.Opened).ShouldBe(("Plan 1", RiverInputs.Example, saved.Id, saved.Id));
    }

    [Fact]
    public void Save_again_addsAPlanAndLeavesTheStar()
    {
        var plans = new SavedPlans(new MemoryStorage());
        var first = plans.Save(RiverInputs.Example, Name);

        var second = plans.Save(BestPay, Name);

        plans.ShouldSatisfyAllConditions(
            () => plans.Plans.Select(plan => (plan.Name, plan.Inputs)).ShouldBe([("Plan 1", RiverInputs.Example), ("Plan 2", BestPay)]),
            () => (plans.Today, plans.Opened).ShouldBe((first.Id, second.Id)));
    }

    // Plan 1 deleted, the next is not a second Plan 2.
    [Fact]
    public void Save_afterADeletion_takesTheNextFreeName()
    {
        var plans = new SavedPlans(new MemoryStorage());
        var first = plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);
        plans.Delete(first.Id);

        plans.Save(RiverInputs.Example, Name);

        plans.Plans.Select(plan => plan.Name).ShouldBe(["Plan 2", "Plan 3"]);
    }

    [Fact]
    public void Rename_name_isTrimmed()
    {
        var plans = new SavedPlans(new MemoryStorage());
        var saved = plans.Save(RiverInputs.Example, Name);

        plans.Rename(saved.Id, "  Today's setup ");

        plans.Plans.Single().Name.ShouldBe("Today's setup");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_empty_keepsTheOldName(string name)
    {
        var plans = new SavedPlans(new MemoryStorage());
        var saved = plans.Save(RiverInputs.Example, Name);

        plans.Rename(saved.Id, name);

        plans.Plans.Single().Name.ShouldBe("Plan 1");
    }

    [Fact]
    public void Star_another_movesTheStar()
    {
        var plans = new SavedPlans(new MemoryStorage());
        plans.Save(RiverInputs.Example, Name);
        var second = plans.Save(BestPay, Name);

        plans.Star(second.Id);

        plans.TodayPlan.ShouldBe(plans.Plans[1]);
    }

    [Fact]
    public void Delete_today_leavesNoStar()
    {
        var plans = new SavedPlans(new MemoryStorage());
        var first = plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);

        plans.Delete(first.Id);

        (plans.Plans.Count, plans.Today).ShouldBe((1, null));
    }

    [Fact]
    public void Delete_opened_leavesNoneOpened()
    {
        var plans = new SavedPlans(new MemoryStorage());
        plans.Save(RiverInputs.Example, Name);
        var second = plans.Save(BestPay, Name);

        plans.Delete(second.Id);

        plans.Opened.ShouldBeNull();
    }

    [Fact]
    public void Open_plan_givesItsInputsAndRemembersIt()
    {
        var plans = new SavedPlans(new MemoryStorage());
        var first = plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);

        var inputs = plans.Open(first.Id);

        (inputs, plans.Opened).ShouldBe((RiverInputs.Example, first.Id));
    }

    [Fact]
    public void Open_unreadablePlan_isNothingAndOpensNothing()
    {
        var storage = new MemoryStorage();
        var plans = new SavedPlans(storage);
        var saved = plans.Save(RiverInputs.Example, Name);
        storage.Set(PlanStorage.Key, storage.Get(PlanStorage.Key)!.Replace("\"reserve\"", "\"keptAside\""));
        var reloaded = new SavedPlans(storage);

        var inputs = reloaded.Open(saved.Id);

        (inputs, reloaded.Plans.Single().Readable).ShouldBe((null, false));
    }

    [Fact]
    public void HasUnsavedChanges_riverAsOpened_isFalse()
    {
        var plans = new SavedPlans(new MemoryStorage());
        plans.Save(RiverInputs.Example, Name);

        plans.HasUnsavedChanges(RiverInputs.Example).ShouldBeFalse();
    }

    [Fact]
    public void HasUnsavedChanges_riverMoved_isTrue()
    {
        var plans = new SavedPlans(new MemoryStorage());
        plans.Save(RiverInputs.Example, Name);

        plans.HasUnsavedChanges(BestPay).ShouldBeTrue();
    }

    // With no plan opened the river is the example until the user changes it, and those changes are
    // saved nowhere.
    [Fact]
    public void HasUnsavedChanges_noneOpenedRiverIsTheExample_isFalse()
    {
        new SavedPlans(new MemoryStorage()).HasUnsavedChanges(RiverInputs.Example).ShouldBeFalse();
    }

    [Fact]
    public void HasUnsavedChanges_noneOpenedRiverMoved_isTrue()
    {
        new SavedPlans(new MemoryStorage()).HasUnsavedChanges(BestPay).ShouldBeTrue();
    }

    // Closing the tab and opening it again reads the browser's storage again.
    [Fact]
    public void SavedPlans_reloaded_keepsThePlansTheStarAndTheOpenedOne()
    {
        var storage = new MemoryStorage();
        var plans = new SavedPlans(storage);
        plans.Save(RiverInputs.Example, Name);
        var second = plans.Save(BestPay, Name);
        plans.Rename(second.Id, "Best pay");
        plans.Star(second.Id);

        var reloaded = new SavedPlans(storage);

        reloaded.ShouldSatisfyAllConditions(
            () => reloaded.Plans.Select(plan => (plan.Name, plan.Inputs)).ShouldBe([("Plan 1", RiverInputs.Example), ("Best pay", BestPay)]),
            () => (reloaded.Today, reloaded.Opened).ShouldBe((second.Id, second.Id)));
    }

    // What cannot be read is kept aside before anything is written over it.
    [Fact]
    public void SavedPlans_unreadableStorage_keepsItAsideAndSaysSo()
    {
        var storage = new MemoryStorage();
        storage.Set(PlanStorage.Key, "{ broken");

        var plans = new SavedPlans(storage);
        plans.Save(RiverInputs.Example, Name);

        (plans.CouldNotRead, storage.Get(PlanStorage.UnreadableKey), plans.Plans.Count).ShouldBe((true, "{ broken", 1));
    }

    // The river opens on the plan it was last opened from, or on the example while none is saved.
    [Fact]
    public void RiverState_planOpened_startsOnIt()
    {
        var storage = new MemoryStorage();
        var plans = new SavedPlans(storage);
        plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);

        new RiverState(new SavedPlans(storage)).Inputs.ShouldBe(BestPay);
    }

    [Fact]
    public void RiverState_nothingSaved_startsOnTheExample()
    {
        new RiverState(new SavedPlans(new MemoryStorage())).Inputs.ShouldBe(RiverInputs.Example);
    }

    private sealed class MemoryStorage : IBrowserStorage
    {
        private readonly Dictionary<string, string> _stored = [];

        public string? Get(string key) => _stored.GetValueOrDefault(key);

        public void Set(string key, string value) => _stored[key] = value;
    }
}

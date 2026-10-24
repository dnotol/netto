using System.Text.Json.Nodes;
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
        var plans = new SavedPlans(new BrowserStorage());

        (plans.Plans.Count, plans.Today, plans.Opened, plans.CouldNotRead).ShouldBe((0, null, null, false));
    }

    [Fact]
    public void Save_first_isStarredAndOpened()
    {
        var plans = new SavedPlans(new BrowserStorage());

        var saved = plans.Save(RiverInputs.Example, Name);

        (saved.Name, saved.Inputs, plans.Today, plans.Opened).ShouldBe(("Plan 1", RiverInputs.Example, saved.Id, saved.Id));
    }

    [Fact]
    public void Save_again_addsAPlanAndLeavesTheStar()
    {
        var plans = new SavedPlans(new BrowserStorage());
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
        var plans = new SavedPlans(new BrowserStorage());
        var first = plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);
        plans.Delete(first.Id);

        plans.Save(RiverInputs.Example, Name);

        plans.Plans.Select(plan => plan.Name).ShouldBe(["Plan 2", "Plan 3"]);
    }

    [Fact]
    public void Rename_name_isTrimmed()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var saved = plans.Save(RiverInputs.Example, Name);

        plans.Rename(saved.Id, "  Today's setup ");

        plans.Plans.Single().Name.ShouldBe("Today's setup");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_empty_keepsTheOldName(string name)
    {
        var plans = new SavedPlans(new BrowserStorage());
        var saved = plans.Save(RiverInputs.Example, Name);

        plans.Rename(saved.Id, name);

        plans.Plans.Single().Name.ShouldBe("Plan 1");
    }

    [Fact]
    public void Star_another_movesTheStar()
    {
        var plans = new SavedPlans(new BrowserStorage());
        plans.Save(RiverInputs.Example, Name);
        var second = plans.Save(BestPay, Name);

        plans.Star(second.Id);

        plans.TodayPlan.ShouldBe(plans.Plans[1]);
    }

    [Fact]
    public void Delete_today_leavesNoStar()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var first = plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);

        plans.Delete(first.Id);

        (plans.Plans.Count, plans.Today).ShouldBe((1, null));
    }

    [Fact]
    public void Delete_opened_leavesNoneOpened()
    {
        var plans = new SavedPlans(new BrowserStorage());
        plans.Save(RiverInputs.Example, Name);
        var second = plans.Save(BestPay, Name);

        plans.Delete(second.Id);

        plans.Opened.ShouldBeNull();
    }

    [Fact]
    public void Open_plan_givesItsInputsAndRemembersIt()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var first = plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);

        var inputs = plans.Open(first.Id);

        (inputs, plans.Opened).ShouldBe((RiverInputs.Example, first.Id));
    }

    [Fact]
    public void Open_unreadablePlan_isNothingAndOpensNothing()
    {
        var storage = new BrowserStorage();
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
        var plans = new SavedPlans(new BrowserStorage());
        plans.Save(RiverInputs.Example, Name);

        plans.HasUnsavedChanges(RiverInputs.Example).ShouldBeFalse();
    }

    [Fact]
    public void HasUnsavedChanges_riverMoved_isTrue()
    {
        var plans = new SavedPlans(new BrowserStorage());
        plans.Save(RiverInputs.Example, Name);

        plans.HasUnsavedChanges(BestPay).ShouldBeTrue();
    }

    // With no plan opened the river is the example until the user changes it, and those changes are
    // saved nowhere.
    [Fact]
    public void HasUnsavedChanges_noneOpenedRiverIsTheExample_isFalse()
    {
        new SavedPlans(new BrowserStorage()).HasUnsavedChanges(RiverInputs.Example).ShouldBeFalse();
    }

    [Fact]
    public void HasUnsavedChanges_noneOpenedRiverMoved_isTrue()
    {
        new SavedPlans(new BrowserStorage()).HasUnsavedChanges(BestPay).ShouldBeTrue();
    }

    // Closing the tab and opening it again reads the browser's storage again.
    [Fact]
    public void SavedPlans_reloaded_keepsThePlansTheStarAndTheOpenedOne()
    {
        var storage = new BrowserStorage();
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
        var storage = new BrowserStorage();
        storage.Set(PlanStorage.Key, "{ broken");

        var plans = new SavedPlans(storage);
        plans.Save(RiverInputs.Example, Name);

        (plans.CouldNotRead, storage.Get(PlanStorage.UnreadableKey), plans.Plans.Count).ShouldBe((true, "{ broken", 1));
    }

    // The river opens on the plan it was last opened from, or on the example while none is saved.
    [Fact]
    public void RiverState_planOpened_startsOnIt()
    {
        var storage = new BrowserStorage();
        var plans = new SavedPlans(storage);
        plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);

        new RiverState(new SavedPlans(storage)).Inputs.ShouldBe(BestPay);
    }

    [Fact]
    public void RiverState_nothingSaved_startsOnTheExample()
    {
        new RiverState(new SavedPlans(new BrowserStorage())).Inputs.ShouldBe(RiverInputs.Example);
    }

    // The file is the document the browser keeps: it reads back as a saved plan does.
    [Fact]
    public void Export_plans_isTheDocumentTheBrowserKeeps()
    {
        var storage = new BrowserStorage();
        var plans = new SavedPlans(storage);
        plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);

        plans.Export().ShouldBe(storage.Get(PlanStorage.Key));
    }

    [Fact]
    public void Import_file_addsItsPlansAfterTheOnesHereWithTheirNames()
    {
        var plans = new SavedPlans(new BrowserStorage());
        plans.Save(RiverInputs.Example, Name);

        var imported = plans.Import(FileOf(("Elsewhere", BestPay)));

        plans.ShouldSatisfyAllConditions(
            () => imported.ShouldBe(new PlanImport(1, 0, false)),
            () => plans.Plans.Select(plan => (plan.Name, plan.Inputs)).ShouldBe([("Plan 1", RiverInputs.Example), ("Elsewhere", BestPay)]));
    }

    // The same file twice: two more plans, each under an id of its own, none replaced.
    [Fact]
    public void Import_sameFileTwice_addsItsPlansAgain()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var file = FileOf(("Elsewhere", BestPay));

        plans.Import(file);
        plans.Import(file);

        (plans.Plans.Count, plans.Plans.Select(plan => plan.Id).Distinct().Count()).ShouldBe((2, 2));
    }

    [Fact]
    public void Import_withNoTodayHere_starsTheFilesToday()
    {
        var plans = new SavedPlans(new BrowserStorage());

        plans.Import(FileOf(("Elsewhere", BestPay)));

        plans.TodayPlan.ShouldNotBeNull().Name.ShouldBe("Elsewhere");
    }

    [Fact]
    public void Import_withTodayHere_keepsIt()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var today = plans.Save(RiverInputs.Example, Name);

        plans.Import(FileOf(("Elsewhere", BestPay)));

        plans.Today.ShouldBe(today.Id);
    }

    // The river stays on the plan it was opened from.
    [Fact]
    public void Import_file_opensNothing()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var opened = plans.Save(RiverInputs.Example, Name);

        plans.Import(FileOf(("Elsewhere", BestPay)));

        plans.Opened.ShouldBe(opened.Id);
    }

    [Fact]
    public void Import_planThisVersionCannotRead_leavesItOutAndCountsIt()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var file = JsonNode.Parse(FileOf(("Elsewhere", BestPay), ("Broken", RiverInputs.Example)))!;
        file["plans"]![1]!["inputs"]!["carLease"] = 850;

        var imported = plans.Import(file.ToJsonString());

        (imported, plans.Plans.Single().Name).ShouldBe((new PlanImport(1, 1, false), "Elsewhere"));
    }

    // A file edited by hand can hold one id twice: each plan still joins under an id of its own.
    [Fact]
    public void Import_fileHoldingOneIdTwice_addsBothPlans()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var file = JsonNode.Parse(FileOf(("First", RiverInputs.Example), ("Second", BestPay)))!;
        file["plans"]![1]!["id"] = file["plans"]![0]!["id"]!.GetValue<string>();

        var imported = plans.Import(file.ToJsonString());

        (imported.Added, plans.Plans.Select(plan => plan.Id).Distinct().Count(), plans.TodayPlan?.Name).ShouldBe((2, 2, "First"));
    }

    // A file any version exported reads as storage does (StoredPlanDocuments): each plan as it was saved, an
    // example's nothing as none, a royalty given before one could follow the pay fixed at it.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Import_fileOfEachVersion_readsItsPlansAsStorageDoes(int version)
    {
        var plans = new SavedPlans(new BrowserStorage());

        plans.Import(StoredPlanDocuments.Of(version));

        plans.Plans.Select(plan => plan.Inputs).ShouldBe(StoredPlanDocuments.Plans(version));
    }

    [Theory]
    [InlineData("{ broken")]
    [InlineData("")]
    [InlineData("""{"version":99,"plans":[]}""")]
    public void Import_fileThisVersionCannotRead_addsNothing(string file)
    {
        var plans = new SavedPlans(new BrowserStorage());
        plans.Save(RiverInputs.Example, Name);

        var imported = plans.Import(file);

        (imported, plans.Plans.Count).ShouldBe((new PlanImport(0, 0, true), 1));
    }

    // What reads the plans from outside them (the Compare plans tab) hears of each change, once.
    [Fact]
    public void Changed_eachChangeOfThePlans_isRaisedOnce()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var raised = 0;
        plans.Changed += () => raised++;

        var first = plans.Save(RiverInputs.Example, Name);
        var second = plans.Save(BestPay, Name);
        plans.Rename(second.Id, "Best pay");
        plans.Star(second.Id);
        plans.Open(first.Id);
        plans.Import(FileOf(("Other", BestPay)));
        plans.Delete(second.Id);

        raised.ShouldBe(7);
    }

    // Nothing changed, nothing to hear and nothing written: an empty name, a plan that is not there to
    // rename, star, open or delete, a file that adds nothing.
    [Fact]
    public void Changed_nothingChanges_isNotRaisedNorWritten()
    {
        var storage = new BrowserStorage();
        var plans = new SavedPlans(storage);
        var first = plans.Save(RiverInputs.Example, Name);
        var kept = storage.Get(PlanStorage.Key);
        var raised = 0;
        plans.Changed += () => raised++;

        plans.Rename(first.Id, "  ");
        plans.Rename("nobody", "Elsewhere");
        plans.Star("nobody");
        plans.Open("nobody");
        plans.Delete("nobody");
        plans.Import("not a file");

        (raised, storage.Get(PlanStorage.Key)).ShouldBe((0, kept));
    }

    // The event is raised once the plans are kept, so a listener reads the plans as they now are.
    [Fact]
    public void Changed_listener_readsThePlansAsTheyNowAre()
    {
        var plans = new SavedPlans(new BrowserStorage());
        var seen = new List<int>();
        plans.Changed += () => seen.Add(plans.Plans.Count);

        plans.Save(RiverInputs.Example, Name);
        plans.Save(BestPay, Name);

        seen.ShouldBe([1, 2]);
    }

    // A file of every plan, as another browser would export it.
    private static string FileOf(params (string Name, RiverInputs Inputs)[] saved)
    {
        var elsewhere = new SavedPlans(new BrowserStorage());
        foreach (var (name, inputs) in saved)
        {
            var plan = elsewhere.Save(inputs, Name);
            elsewhere.Rename(plan.Id, name);
        }

        return elsewhere.Export();
    }
}

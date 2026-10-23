using System.Text.Json.Nodes;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Which two saved plans the Compare plans tab sets side by side, and what it can say of them: a plan
// to compare needs a second one; the plan starred « Today » is the one every other is measured against,
// so it is the first column unless the viewer picks otherwise; a plan Netto cannot read is left out, one
// the engine refuses has no figures, and a river that starts at the pay is not compared with one that
// starts at the company's year. The rows are CompareRowsTests'.
public class CompareViewTests
{
    private static readonly RiverInputs BestPay = RiverInputs.Example.WithDirectorPay(20_799m);

    [Fact]
    public void Of_noPlanSaved_isNoPlan()
    {
        var view = CompareView.Of(new SavedPlans(new MemoryStorage()));

        view.ShouldSatisfyAllConditions(
            () => (view.State, view.Plan, view.Against, view.CouldNotRead).ShouldBe((CompareState.NoPlan, null, null, false)),
            () => view.Choices.ShouldBeEmpty());
    }

    // What the browser keeps cannot be read: there is no plan, and the tab says why.
    [Fact]
    public void Of_storageCouldNotBeRead_isNoPlanAndSaysSo()
    {
        var storage = new MemoryStorage();
        storage.Set(PlanStorage.Key, "{ broken");

        var view = CompareView.Of(new SavedPlans(storage));

        (view.State, view.CouldNotRead).ShouldBe((CompareState.NoPlan, true));
    }

    // One plan has nothing to be compared with: the tab names it and asks for a second.
    [Fact]
    public void Of_onePlan_isOnePlanAndNamesIt()
    {
        var view = CompareView.Of(Saved(RiverInputs.Example));

        view.ShouldSatisfyAllConditions(
            () => (view.State, view.Plan, view.Against).ShouldBe((CompareState.OnePlan, null, null)),
            () => view.Choices.Select(choice => choice.Name).ShouldBe(["Plan 1"]));
    }

    // Today is the first column, and the plan the river was opened from, the one saved last, the second.
    [Fact]
    public void Of_twoPlans_comparesTodayAgainstThePlanTheRiverWasOpenedFrom()
    {
        var view = CompareView.Of(Saved(RiverInputs.Example, BestPay));

        (view.State, view.Plan!.Name, view.Plan.IsToday, view.Against!.Name, view.Against.IsToday).ShouldBe((CompareState.Compared, "Plan 1", true, "Plan 2", false));
    }

    // The river opened from Today itself: the second column is the plan saved last.
    [Fact]
    public void Of_riverOpenedFromToday_comparesItAgainstThePlanSavedLast()
    {
        var plans = Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m));
        plans.Open(plans.Plans[0].Id);

        var view = CompareView.Of(plans);

        (view.Plan!.Name, view.Against!.Name).ShouldBe(("Plan 1", "Plan 3"));
    }

    [Fact]
    public void Of_riverOpenedFromAnotherPlan_comparesTodayAgainstIt()
    {
        var plans = Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m));
        plans.Open(plans.Plans[1].Id);

        var view = CompareView.Of(plans);

        (view.Plan!.Name, view.Against!.Name).ShouldBe(("Plan 1", "Plan 2"));
    }

    [Fact]
    public void Of_todayIsNotTheFirstPlanSaved_isStillTheFirstColumn()
    {
        var plans = Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m));
        plans.Star(plans.Plans[1].Id);
        plans.Open(plans.Plans[2].Id);

        var view = CompareView.Of(plans);

        (view.Plan!.Name, view.Plan.IsToday, view.Against!.Name).ShouldBe(("Plan 2", true, "Plan 3"));
    }

    // No star: the first plan is the first column, and the tab says nothing is starred.
    [Fact]
    public void Of_noToday_comparesTheTwoPlansWithEachOther()
    {
        var plans = Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m));
        plans.Delete(plans.Plans[0].Id);

        var view = CompareView.Of(plans);

        (view.State, view.HasToday, view.Plan!.Name, view.Plan.IsToday, view.Against!.Name).ShouldBe((CompareState.Compared, false, "Plan 2", false, "Plan 3"));
    }

    // The plan starred Today cannot be read: it is no first column, and nothing is starred as far as the
    // tab can tell.
    [Fact]
    public void Of_todayUnreadable_takesTheFirstPlanItCanRead()
    {
        var plans = Break(Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m)), 0);

        var view = CompareView.Of(plans);

        (view.HasToday, view.Plan!.Name, view.Against!.Name, view.Unreadable).ShouldBe((false, "Plan 2", "Plan 3", 1));
    }

    [Fact]
    public void Of_unreadablePlans_areLeftOutOfTheChoicesAndCounted()
    {
        var plans = Break(Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m)), 1);

        var view = CompareView.Of(plans);

        view.ShouldSatisfyAllConditions(
            () => view.Choices.Select(choice => choice.Name).ShouldBe(["Plan 1", "Plan 3"]),
            () => view.Unreadable.ShouldBe(1));
    }

    // One readable plan and one that cannot be read is still a single plan to compare.
    [Fact]
    public void Of_oneReadablePlanBesideAnUnreadableOne_isOnePlan()
    {
        var view = CompareView.Of(Break(Saved(RiverInputs.Example, BestPay), 1));

        (view.State, view.Unreadable).ShouldBe((CompareState.OnePlan, 1));
    }

    [Fact]
    public void Of_choices_markTheOneStarredAsToday()
    {
        var view = CompareView.Of(Saved(RiverInputs.Example, BestPay));

        view.Choices.Select(choice => (choice.Name, choice.IsToday)).ShouldBe([("Plan 1", true), ("Plan 2", false)]);
    }

    [Fact]
    public void Of_plansPicked_areTheOnesCompared()
    {
        var plans = Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m));

        var view = CompareView.Of(plans, plans.Plans[2].Id, plans.Plans[1].Id);

        (view.Plan!.Name, view.Against!.Name).ShouldBe(("Plan 3", "Plan 2"));
    }

    // A plan deleted elsewhere while the tab was open: its pick gives way to the default.
    [Fact]
    public void Of_pickedPlanDeleted_fallsBackToTheDefault()
    {
        var plans = Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m));
        var picked = plans.Plans[2].Id;
        plans.Delete(picked);

        var view = CompareView.Of(plans, picked, picked);

        (view.Plan!.Name, view.Against!.Name).ShouldBe(("Plan 1", "Plan 2"));
    }

    // The same plan twice would compare it with itself: the second pick gives way.
    [Fact]
    public void Of_samePlanPickedTwice_keepsTheFirstAndTakesTheDefaultForTheSecond()
    {
        var plans = Saved(RiverInputs.Example, BestPay);

        var view = CompareView.Of(plans, plans.Plans[1].Id, plans.Plans[1].Id);

        (view.Plan!.Name, view.Against!.Name).ShouldBe(("Plan 2", "Plan 1"));
    }

    // A pick of a plan that cannot be read is no pick.
    [Fact]
    public void Of_unreadablePlanPicked_fallsBackToTheDefault()
    {
        var plans = Break(Saved(RiverInputs.Example, BestPay, RiverInputs.Example.WithDirectorPay(30_000m)), 2);

        var view = CompareView.Of(plans, plans.Plans[2].Id, null);

        (view.Plan!.Name, view.Against!.Name).ShouldBe(("Plan 1", "Plan 2"));
    }

    // The plan the engine refuses has no figures: nothing is compared, and the tab says which plan.
    [Fact]
    public void Of_planTheEngineRefuses_isRefusedWithItsError()
    {
        var plans = Saved(RiverInputs.Example, RiverInputs.Example.WithReserve(-1m));

        var view = CompareView.Of(plans);

        view.ShouldSatisfyAllConditions(
            () => (view.State, view.Plan!.Run.Error, view.Against!.Run.Error).ShouldBe((CompareState.Refused, null, "reserve_negative")),
            () => view.Groups.ShouldBeEmpty());
    }

    // One river starts at the pay and the other at the company's year: the first leaves out the dividend,
    // so their pockets are not comparable (BaselineGain).
    [Fact]
    public void Of_oneStartsAtThePayTheOtherAtTheCompanysYear_isNotComparable()
    {
        var view = CompareView.Of(Saved(RiverInputs.Example, RiverInputs.Example.SkipCompany()));

        view.ShouldSatisfyAllConditions(
            () => view.State.ShouldBe(CompareState.NotComparable),
            () => view.Groups.ShouldBeEmpty(),
            () => view.Pocket.ShouldBeNull());
    }

    [Fact]
    public void Of_bothStartAtThePay_areCompared()
    {
        var view = CompareView.Of(Saved(RiverInputs.Example.SkipCompany(), RiverInputs.Example.SkipCompany().WithDirectorPay(40_000m)));

        view.State.ShouldBe(CompareState.Compared);
    }

    private static SavedPlans Saved(params RiverInputs[] inputs)
    {
        var plans = new SavedPlans(new MemoryStorage());
        foreach (var plan in inputs)
            plans.Save(plan, number => $"Plan {number}");
        return plans;
    }

    // The plan at that place, as a later version of Netto would find it: stored with a field this version
    // does not know, so it is kept as it was and cannot be read. The plans are read again from the storage.
    private static SavedPlans Break(SavedPlans plans, int index)
    {
        var storage = new MemoryStorage();
        var book = JsonNode.Parse(plans.Export())!;
        book["plans"]![index]!["inputs"]!["carLease"] = 850;
        storage.Set(PlanStorage.Key, book.ToJsonString());
        return new SavedPlans(storage);
    }

    private sealed class MemoryStorage : IBrowserStorage
    {
        private readonly Dictionary<string, string> _stored = [];

        public string? Get(string key) => _stored.GetValueOrDefault(key);

        public void Set(string key, string value) => _stored[key] = value;
    }
}

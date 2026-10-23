using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// Saved plans for a test to start from, beside the fakes the renders use (RiverRendering): plans kept in
// memory and named Plan 1, Plan 2…, and two of them compared.
internal static class PlanFixtures
{
    // These plans saved in this order: the first is starred as Today, the last is the one the river was
    // opened from.
    public static SavedPlans Saved(params RiverInputs[] inputs)
    {
        var plans = new SavedPlans(new RiverRendering.BrowserStorage());
        foreach (var plan in inputs)
            plans.Save(plan, number => $"Plan {number}");
        return plans;
    }

    // The comparison of two plans the engine runs, which must be one the tab draws a table for.
    public static CompareView Compare(RiverInputs plan, RiverInputs against)
    {
        var view = CompareView.Of(Saved(plan, against));
        view.State.ShouldBe(CompareState.Compared);
        return view;
    }
}

namespace Netto.Web.Client.River;

// What the panel of saved plans draws for each plan, in the order they were saved: whether it is
// « Today » or the plan the river was opened from, what reaches the pocket and its share as the bar
// says them (BucketsView), and its difference from « Today » (BaselineGain), none for « Today »
// itself, while there is no « Today », or for a plan that cannot be read. Each plan runs once.
internal sealed record PlanRow(SavedPlan Plan, bool IsToday, bool IsOpened, decimal? Pocket, decimal? Reach, BaselineGain? Gain)
{
    public static IReadOnlyList<PlanRow> From(SavedPlans plans)
    {
        var today = plans.TodayPlan?.Inputs?.Run();
        return [.. plans.Plans.Select(plan =>
        {
            var isToday = plan.Id == plans.Today;
            var run = plan.Inputs?.Run();
            var view = run is null ? null : BucketsView.From(run);
            var pocket = view?.Buckets.Single(bucket => bucket.Key == "pocket").Amount;
            var gain = run is null || isToday ? null : BaselineGain.From(run, today);
            return new PlanRow(plan, isToday, plan.Id == plans.Opened, pocket, view?.Reach, gain);
        })];
    }
}

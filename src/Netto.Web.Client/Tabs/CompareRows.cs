using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The rows of the Compare plans table: the figures of two plans' runs the bar of buckets and the cards
// already show, read from the same runs and set side by side, so a column always says what the river
// says for that plan. Every row is the engine's own figure for each plan and what separates them, the
// second plan's less the first's; none is a rule of its own. The year's result first (what the bar
// draws), then the taxes layer by layer in the law's order, read from the ledger's steps so a river that
// starts at the pay has them too. A row neither plan has a figure for is not drawn.
internal static class CompareRows
{
    public static IReadOnlyList<CompareGroup> Of(ComparedPlan plan, ComparedPlan against)
    {
        var gain = BaselineGain.From(against.Run, plan.Run)!;
        return
        [
            new CompareGroup("result", Drawn(
            [
                Row("pocket", PocketOf(plan), PocketOf(against), OfGain(gain)),
                Row("reach", Share(plan), Share(against)),
                MuddyRow(plan, against, gain),
                Row("kept", Company(plan, KeptOf), Company(against, KeptOf)),
            ])),
            new CompareGroup("taxes", Drawn(
            [
                Row("taxes", Money(AllTaxes(plan)), Money(AllTaxes(against))),
                Row("corporate_tax", Company(plan, column => PaidOut(column, Layer.Corporate)), Company(against, column => PaidOut(column, Layer.Corporate)), sub: true),
                Row("social_contributions", Money(PaidOut(plan, Layer.Social)), Money(PaidOut(against, Layer.Social)), sub: true),
                Row("personal_tax", Money(PaidOut(plan, Layer.Personal)), Money(PaidOut(against, Layer.Personal)), sub: true),
            ])),
        ];
    }

    // The percentage points between two shares as the table shows them, a tenth of a percent each, so
    // the column reads as a subtraction of the figures drawn, never of the hundredths beneath them.
    public static decimal Points(decimal plan, decimal against) => Shown(against) - Shown(plan);

    private static decimal Shown(decimal share) => Math.Round(share * 100m, 1, MidpointRounding.AwayFromZero);

    private static IReadOnlyList<CompareRow> Drawn(IEnumerable<CompareRow?> rows) => [.. rows.OfType<CompareRow>()];

    // The row, or none when neither plan has a figure for it.
    private static CompareRow? Row(string key, CompareFigure plan, CompareFigure against, CompareDifference? difference = null, bool sub = false) =>
        plan.Present || against.Present ? new CompareRow(key, plan, against, difference ?? Between(plan, against)) { Sub = sub } : null;

    // What separates two figures of the same kind: money, with a module that is not in a plan as nothing,
    // the way the engine counts it; shares as points; the reduced rate as the same or changed.
    private static CompareDifference Between(CompareFigure plan, CompareFigure against)
    {
        if ((plan.Kind is FigureKind.Money or FigureKind.Absent) && (against.Kind is FigureKind.Money or FigureKind.Absent))
            return Direction(against.Amount - plan.Amount);
        if (plan.Kind == FigureKind.Share && against.Kind == FigureKind.Share)
            return Direction(Points(plan.Amount, against.Amount));
        if (plan.Kind == FigureKind.Rate && against.Kind == FigureKind.Rate)
            return plan.Stance == against.Stance ? CompareDifference.Same : CompareDifference.Changed;

        return CompareDifference.None;
    }

    // A signed difference, the second less the first, as a direction and a magnitude, never a negative
    // zero: nothing between two equal figures is the same.
    private static CompareDifference Direction(decimal difference) =>
        difference > 0m ? new CompareDifference(DifferenceKind.More, difference)
        : difference < 0m ? new CompareDifference(DifferenceKind.Less, -difference)
        : CompareDifference.Same;

    // The pocket's difference is the gain the bar and the Plans panel say, to the cent.
    private static CompareDifference OfGain(BaselineGain gain) => Direction(gain.Kind switch
    {
        GainKind.More => gain.Amount,
        GainKind.Less => -gain.Amount,
        _ => 0m,
    });

    // The muddy pocket, once a car counts in either plan, a plan without one holding nothing.
    private static CompareRow? MuddyRow(ComparedPlan plan, ComparedPlan against, BaselineGain gain)
    {
        if (!HasMuddy(plan) && !HasMuddy(against))
            return null;

        return Row("muddy", Money(MuddyOf(plan)), Money(MuddyOf(against)), Direction(gain.MuddyDifference));
    }

    private static bool HasMuddy(ComparedPlan column) => column.Buckets is { HasMuddyPocket: true };

    private static decimal MuddyOf(ComparedPlan column) => HasMuddy(column) ? Bucket(column, BucketsView.Muddy) : 0m;

    private static CompareFigure PocketOf(ComparedPlan column) => Money(Bucket(column, "pocket"));

    private static decimal KeptOf(ComparedPlan column) => Bucket(column, "kept_in_company");

    private static decimal Bucket(ComparedPlan column, string key) => column.Buckets!.Buckets.Single(bucket => bucket.Key == key).Amount;

    private static CompareFigure Share(ComparedPlan column) => CompareFigure.Share(column.Buckets!.Reach);

    private static CompareFigure Money(decimal amount) => CompareFigure.Money(amount);

    // A figure the company's year gives, none for a river that starts at the pay.
    private static CompareFigure Company(ComparedPlan column, Func<ComparedPlan, decimal> figure) =>
        column.Run.Year is null ? CompareFigure.None : Money(figure(column));

    private static decimal AllTaxes(ComparedPlan column) =>
        PaidOut(column, Layer.Corporate) + PaidOut(column, Layer.Social) + PaidOut(column, Layer.Personal);

    // What a layer took, from the ledger the year or the pay ran: the year's own total for the layer.
    private static decimal PaidOut(ComparedPlan column, Layer layer) =>
        (column.Run.Year?.Steps ?? column.Run.Pay!.Steps).Where(step => step.Layer == layer && step.Track == Track.PaidOut).Sum(step => step.Amount);
}

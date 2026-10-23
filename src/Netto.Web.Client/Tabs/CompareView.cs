using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the Compare plans tab draws: two of the saved plans side by side, the first the baseline (the
// plan starred « Today » unless the viewer picked another) and the second measured against it, the
// difference being the second less the first, as a plan's gain is against Today (BaselineGain). It only
// selects the engine's figures of each plan's run and regroups them (CompareRows); it decides no tax
// outcome. Each plan runs once, here: the tab holds the view and builds it again only when the viewer
// picks or the saved plans change.
internal sealed record CompareView(
    CompareState State,
    IReadOnlyList<PlanChoice> Choices,
    ComparedPlan? Plan,
    ComparedPlan? Against,
    int Unreadable,
    bool CouldNotRead,
    bool HasToday)
{
    // The figures, group by group, once two plans are compared.
    public IReadOnlyList<CompareGroup> Groups { get; init; } = [];

    // The pockets drawn on one scale, once two plans are compared.
    public PocketPanel? Pocket { get; init; }

    public static CompareView Of(SavedPlans plans, string? plan = null, string? against = null) =>
        Of(plans.Plans, plans.Today, plans.Opened, plans.CouldNotRead, plan, against);

    // The plans picked (null for the default) when they are still there and can be read; the default
    // first plan is Today, or the first plan that can be read, and the default second plan the one the
    // river was opened from, or the one saved last, never the first plan itself.
    public static CompareView Of(
        IReadOnlyList<SavedPlan> saved, string? today, string? opened, bool couldNotRead, string? plan = null, string? against = null)
    {
        var readable = saved.Where(candidate => candidate.Readable).ToList();
        var choices = readable.Select(candidate => new PlanChoice(candidate.Id, candidate.Name, candidate.Id == today)).ToList();
        var unreadable = saved.Count - readable.Count;
        var hasToday = readable.Any(candidate => candidate.Id == today);
        var view = new CompareView(readable.Count == 1 ? CompareState.OnePlan : CompareState.NoPlan, choices, null, null, unreadable, couldNotRead, hasToday);
        if (readable.Count < 2)
            return view;

        var first = readable.FirstOrDefault(candidate => candidate.Id == plan) ?? readable.FirstOrDefault(candidate => candidate.Id == today) ?? readable[0];
        var others = readable.Where(candidate => candidate.Id != first.Id).ToList();
        var second = others.FirstOrDefault(candidate => candidate.Id == against) ?? others.FirstOrDefault(candidate => candidate.Id == opened) ?? others[^1];

        var left = Compared(first, today);
        var right = Compared(second, today);
        view = view with { State = CompareState.Compared, Plan = left, Against = right };
        if (left.Run.Error is not null || right.Run.Error is not null)
            return view with { State = CompareState.Refused };
        if (BaselineGain.From(right.Run, left.Run)?.Kind == GainKind.NotComparable)
            return view with { State = CompareState.NotComparable };

        return view with { Groups = CompareRows.Of(left, right) };
    }

    private static ComparedPlan Compared(SavedPlan plan, string? today)
    {
        var inputs = plan.Inputs!;
        var run = inputs.Run();
        return new ComparedPlan(plan.Id, plan.Name, plan.Id == today, inputs, run, BucketsView.From(run));
    }
}

internal enum CompareState
{
    // No plan is saved, or none can be read.
    NoPlan,

    // One plan can be read: there is nothing to compare it with yet.
    OnePlan,

    // The engine refuses an amount of one of the two plans, which therefore has no figures.
    Refused,

    // One plan starts at the pay and the other at the company's year (BaselineGain): their pockets are not
    // comparable, the first leaving out the dividend.
    NotComparable,

    Compared,
}

// A plan the viewer can pick: one that can be read.
internal sealed record PlanChoice(string Id, string Name, bool IsToday);

// One of the two columns: the plan, its inputs, its one run and what the bar of buckets draws from it.
internal sealed record ComparedPlan(string Id, string Name, bool IsToday, RiverInputs Inputs, RiverRun Run, BucketsView? Buckets);

// The figures of one layer of the year, or the year's result, in the order the table draws them.
internal sealed record CompareGroup(string Key, IReadOnlyList<CompareRow> Rows);

// The panel drawing both pockets on one scale.
internal sealed record PocketPanel(PocketBar Plan, PocketBar Against, BaselineGain Gain);

// One plan's pocket in the panel: its width as a fraction of the larger pocket, none when neither pocket
// is above nothing, and whether it is the larger.
internal sealed record PocketBar(string Name, bool IsToday, decimal Pocket, decimal? Width, bool IsBetter);

// A figure of the table for both plans, and what separates them.
internal sealed record CompareRow(string Key, CompareFigure Plan, CompareFigure Against, CompareDifference Difference)
{
    // Whether it is read as part of the row above (a tax layer under all taxes).
    public bool Sub { get; init; }

    public bool Differs => Difference.Kind is DifferenceKind.More or DifferenceKind.Less or DifferenceKind.Changed;
}

// What a plan has at a row: an amount, a share, how the reduced corporate rate stands, nothing because
// the module is not in the plan, or no figure because the plan's river does not reach it.
internal sealed record CompareFigure(FigureKind Kind, decimal Amount = 0m, RateStance Stance = default)
{
    public static CompareFigure Money(decimal amount) => new(FigureKind.Money, amount);

    public static CompareFigure Share(decimal? fraction) => fraction is { } share ? new CompareFigure(FigureKind.Share, share) : None;

    public static CompareFigure Rate(RateStance stance) => new(FigureKind.Rate, Stance: stance);

    public static CompareFigure Absent { get; } = new(FigureKind.Absent);

    public static CompareFigure None { get; } = new(FigureKind.None);

    // Whether the plan has something to show here.
    public bool Present => Kind is FigureKind.Money or FigureKind.Share or FigureKind.Rate;
}

internal enum FigureKind
{
    Money,

    // A fraction.
    Share,

    Rate,

    // The module is not in the plan: « Not in this plan », worth nothing to the engine.
    Absent,

    // The plan's river does not reach this row (a river that starts at the pay has no corporate tax).
    None,
}

// How the reduced corporate rate stands, from the engine's condition (ReducedRateCondition).
internal enum RateStance
{
    Kept,
    GivenUp,
    NotOpen,
}

// What the second plan has more or less than the first, as a magnitude: euros for money, percentage
// points for shares; none when a figure is missing on a side.
internal sealed record CompareDifference(DifferenceKind Kind, decimal Amount = 0m)
{
    public static CompareDifference Same { get; } = new(DifferenceKind.Same);

    public static CompareDifference Changed { get; } = new(DifferenceKind.Changed);

    public static CompareDifference None { get; } = new(DifferenceKind.None);
}

internal enum DifferenceKind
{
    Same,
    More,
    Less,

    // A word that is not the same (the reduced rate kept or given up).
    Changed,

    None,
}

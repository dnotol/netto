using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys CompareTab.razor builds from the comparison's rows, figures and states. The component
// and CompareTabTextTests both go through these, so a row, a group or a state without a text in
// CompareTab.resx fails a test instead of showing on screen.
internal static class CompareTabText
{
    public static string Group(string key) => $"Group_{key}";

    public static string Row(string key) => $"Row_{key}";

    public static string Rate(RateStance stance) => $"Rate_{stance}";

    // What the second plan leaves in the pocket against the first, in a sentence: the bar's and the Plans
    // panel's gain in words.
    public static string Headline(GainKind kind) => kind switch
    {
        GainKind.More => "Headline_More",
        GainKind.Less => "Headline_Less",
        _ => "Headline_Same",
    };

    // The note the tab opens on when there is nothing to put side by side or nothing to compare.
    public static string Note(CompareState state) => state switch
    {
        CompareState.NoPlan => "NoPlan",
        CompareState.OnePlan => "OnePlan",
        CompareState.NotComparable => "NotComparable",
        CompareState.Refused => "RefusedPlan",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "A comparison has no note."),
    };

    public static string Unreadable(int count) => count == 1 ? "Unreadable_One" : "Unreadable_Many";

    // A difference in points, as drawn, signed (+2.5 points). Euros are drawn by Formats.
    public static string Points(DifferenceKind kind) => kind == DifferenceKind.More ? "Points_More" : "Points_Less";

    // The same difference in words for a screen reader, which would read a sign as a dash: 2.5 points more.
    public static string Spoken(DifferenceKind kind, FigureKind figure) => (kind, figure) switch
    {
        (DifferenceKind.More, FigureKind.Share) => "SpokenPoints_More",
        (DifferenceKind.Less, FigureKind.Share) => "SpokenPoints_Less",
        (DifferenceKind.More, _) => "Spoken_More",
        _ => "Spoken_Less",
    };
}

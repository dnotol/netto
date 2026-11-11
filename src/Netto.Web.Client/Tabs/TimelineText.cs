using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys CalendarTimeline.razor builds from the timeline's view. The component and
// TimelineTextTests both go through these, so a state without a text in CalendarTimeline.resx fails a
// test instead of showing on screen.
internal static class TimelineText
{
    // What a payer's lane says of itself when none of its marks is the next payment.
    public static string Note(LaneNote note) => $"Note_{note}";

    // Where Today falls on the year, in the sentence that labels the picture.
    public static string TodayLabel(TodayPlace place) => $"Label_Today_{place}";

    // Under the picture, when Today is outside the income year: none within it.
    public static string? TodayNote(TodayPlace place) => place == TodayPlace.InYear ? null : $"Note_Today_{place}";

    public static string Key(KeyItem item) => $"Key_{item}";

    // The sentence naming whoever pays at the next deadline: the company, the director, both or nobody,
    // chosen by the classifier the Next panel uses (WhenToPaySectionText.Payers), so the two agree.
    public static string Next(TimelineMark? company, TimelineMark? director) =>
        $"Label_Next_{WhenToPaySectionText.Payers(company is not null, director is not null)}";

    // The contributions' lane, and their sentence in the picture's label, name the company when it pays
    // them on the director's behalf; the sentence names the fund's fee too once the quarters hold it.
    public static string ContributionsLane(bool paidByCompany) => paidByCompany ? "Lane_Contributions_Company" : "Lane_Contributions";

    public static string ContributionsLabel(bool paidByCompany, bool withFundFee) => (paidByCompany, withFundFee) switch
    {
        (false, false) => "Label_Contributions",
        (true, false) => "Label_Contributions_Company",
        (false, true) => "Label_Contributions_WithFee",
        (true, true) => "Label_Contributions_Company_WithFee",
    };

    // The pay's sentence in the picture's label for a run of its months, as the table beside it runs
    // them (WhenToPaySectionText.PayLine): every month while the year pays alike, else the run's months, or
    // its month alone.
    public static string Pay(PayRun run, int runs) => runs == 1 ? "Label_Pay" : run.First == run.Last ? "Label_PayRunOne" : "Label_PayRun";
}

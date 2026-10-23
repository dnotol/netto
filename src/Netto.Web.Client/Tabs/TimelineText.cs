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

    // The sentence naming whoever pays at the next deadline: the company, the director, both or nobody.
    public static string Next(TimelineMark? company, TimelineMark? director) => (company, director) switch
    {
        (not null, not null) => "Label_Next_Both",
        (not null, null) => "Label_Next_Company",
        (null, not null) => "Label_Next_Director",
        _ => "Label_Next_None",
    };

    // The contributions' lane, and their sentence in the picture's label, name the company when it pays
    // them on the director's behalf.
    public static string ContributionsLane(bool paidByCompany) => paidByCompany ? "Lane_Contributions_Company" : "Lane_Contributions";

    public static string ContributionsLabel(bool paidByCompany) => paidByCompany ? "Label_Contributions_Company" : "Label_Contributions";
}

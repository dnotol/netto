using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The timeline's words and its colours: every state it can draw has a text in its resx, and its styles
// take their colours from the site's tokens alone, which both themes declare.
public class TimelineTextTests
{
    // The name IStringLocalizer<CalendarTimeline> reads Tabs/CalendarTimeline.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Tabs.CalendarTimeline", typeof(CalendarTimeline).Assembly);

    private static readonly DateOnly On21December = new(2026, 12, 21);

    private static string? Text(string key) => Resources.GetString(key, CultureInfo.InvariantCulture);

    private static TimelineMark Mark(decimal amount) => new(On21December, 0.97m, MarkState.Next, amount);

    [Fact]
    public void CalendarTimelineResx_everyNoteOfALane_hasAText()
    {
        Enum.GetValues<LaneNote>().Where(note => note != LaneNote.None).Select(TimelineText.Note).Where(key => Text(key) is null).ShouldBeEmpty();
    }

    [Fact]
    public void CalendarTimelineResx_everyPlaceOfToday_hasALabelAndOutsideTheYearANote()
    {
        Enum.GetValues<TodayPlace>().Select(TimelineText.TodayLabel).Where(key => Text(key) is null).ShouldBeEmpty();
        Enum.GetValues<TodayPlace>().Select(TimelineText.TodayNote).OfType<string>().Where(key => Text(key) is null).ShouldBeEmpty();
    }

    [Fact]
    public void TodayNote_inTheYear_isNone()
    {
        TimelineText.TodayNote(TodayPlace.InYear).ShouldBeNull();
    }

    [Fact]
    public void CalendarTimelineResx_everyItemOfTheKey_hasAText()
    {
        Enum.GetValues<KeyItem>().Select(TimelineText.Key).Where(key => Text(key) is null).ShouldBeEmpty();
    }

    // Whoever pays next, and nobody, each has a sentence.
    [Theory]
    [InlineData(true, true, "Label_Next_Both")]
    [InlineData(true, false, "Label_Next_Company")]
    [InlineData(false, true, "Label_Next_Director")]
    [InlineData(false, false, "Label_Next_None")]
    public void Next_whoPaysNext_isTheSentenceThatNamesThem(bool company, bool director, string expected)
    {
        var key = TimelineText.Next(company ? Mark(15_000m) : null, director ? Mark(14_317.50m) : null);

        (key, Text(key) is not null).ShouldBe((expected, true));
    }

    [Theory]
    [InlineData(false, "Lane_Contributions")]
    [InlineData(true, "Lane_Contributions_Company")]
    public void Contributions_paidByWhom_isTheLaneThatSaysSo(bool paidByCompany, string expected)
    {
        var key = TimelineText.ContributionsLane(paidByCompany);

        (key, Text(key) is not null).ShouldBe((expected, true));
    }

    // A label that names its payer: "Social contributions: €x a quarter" or, paid by the company, so.
    [Theory]
    [InlineData(false, "Label_Contributions")]
    [InlineData(true, "Label_Contributions_Company")]
    public void ContributionsLabel_paidByWhom_isTheSentenceThatSaysSo(bool paidByCompany, string expected)
    {
        var key = TimelineText.ContributionsLabel(paidByCompany);

        (key, Text(key) is not null).ShouldBe((expected, true));
    }

    // The timeline's styles name no colour of their own: a theme redefines the tokens and the timeline
    // follows (CLAUDE.md, UI).
    [Fact]
    public void CalendarTimelineCss_namesNoColourButTheSitesTokens()
    {
        var css = File.ReadAllText(FindUp("src", "Netto.Web.Client", "Tabs", "CalendarTimeline.razor.css"));

        Regex.Matches(css, @"#[0-9A-Fa-f]{3,8}\b|\b(?:rgb|rgba|hsl|hsla)\(").Select(match => match.Value).ShouldBeEmpty();
    }

    [Fact]
    public void CalendarTimelineCss_everyTokenItUsesIsDeclaredInAppCss()
    {
        var css = File.ReadAllText(FindUp("src", "Netto.Web.Client", "Tabs", "CalendarTimeline.razor.css"));
        var appCss = File.ReadAllText(FindUp("src", "Netto.Web", "wwwroot", "app.css"));

        var declared = Regex.Matches(appCss, @"(--netto-[a-z-]+)\s*:").Select(match => match.Groups[1].Value).ToHashSet();
        var used = Regex.Matches(css, @"var\((--netto-[a-z-]+)").Select(match => match.Groups[1].Value).Distinct();

        used.Where(token => !declared.Contains(token)).ShouldBeEmpty();
        used.ShouldNotBeEmpty();
    }

    private static string FindUp(params string[] path)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var file = Path.Combine([directory.FullName, .. path]);
            if (File.Exists(file))
                return file;
        }

        throw new FileNotFoundException($"No {Path.Combine(path)} above {AppContext.BaseDirectory}.");
    }
}

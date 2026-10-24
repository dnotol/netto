using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Calendar;
using Netto.Core.SocialContributions;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The timeline as a viewer meets it, drawn without a browser (RiverRendering): the year's lanes with their
// marks and blocks, what is past, next and to come told by shape and by words, the picture as one named
// image whose label says what it shows, and the key and notes outside the image, as text. Positions and
// states are TimelineViewTests'; here is what is drawn from them. The layout itself, its stretching and
// its scrolling, is the browser check.
public class CalendarTimelineTests
{
    private static readonly DateOnly On23October = new(2026, 10, 23);

    private static async Task<string> RenderAsync(RiverInputs river, DateOnly today)
    {
        var view = CalendarView.From(river, river.Run(), today).ShouldNotBeNull();
        return WebUtility.HtmlDecode(await RenderAsync<CalendarTimeline>(new Folds(new BrowserStorage()), new()
        {
            ["Calendar"] = view.Calendar, ["NextDate"] = view.Next?.Date,
        }));
    }

    private static int Count(string html, string pattern) => Regex.Count(html, pattern);

    private static List<string> Lanes(string html) =>
        [.. Regex.Matches(html, "<span class=\"timeline__lane\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)];

    private static List<string> Texts(string html, string cssClass) =>
        [.. Regex.Matches(html, $"<span class=\"{cssClass}[^\"]*\"[^>]*>([^<]*)</span>").Select(match => match.Groups[1].Value)];

    // The pay runs as the table beside it runs it: 3,275.73 net a month to October, 3,284.20 from
    // November by the rules from 1 November.
    [Fact]
    public async Task Timeline_example_isOneNamedImageWhoseLabelSaysWhatItShows()
    {
        var river = RiverInputs.Example;
        var view = CalendarView.From(river, river.Run(), On23October).ShouldNotBeNull();

        var html = await RenderAsync(river, On23October);

        var label = Regex.Match(html, "<div class=\"timeline__chart\" role=\"img\" aria-label=\"([^\"]*)\"").Groups[1].Value;
        label.ShouldBe(
            "Timeline of 2026. Today is 23 October 2026. Next payment by 21 December 2026: €14,999.89 from the company. "
            + "Your pay from January to October: €3,275.73 net a month. Your pay from November to December: €3,284.20 net a month. "
            + "October to December still to come. "
            + $"Social contributions: {Formats.Euros(view.Contributions.EachQuarter)} a quarter. 1 of 4 quarters still to come. "
            + "The tables below give every figure.");
    }

    // A pay both periods withhold nothing on pays alike all year: the label says it once.
    [Fact]
    public async Task Timeline_payPaidAlikeAllYear_saysItsNetOnce()
    {
        var html = await RenderAsync(RiverInputs.Example.WithDirectorPay(3_600m), On23October);

        var label = Regex.Match(html, "<div class=\"timeline__chart\" role=\"img\" aria-label=\"([^\"]*)\"").Groups[1].Value;
        label.ShouldContain("Your pay: €300.00 net a month. October to December still to come.");
    }

    // Past the year, the label says there is nothing to come.
    [Fact]
    public async Task Timeline_afterTheYear_labelSaysNothingIsLeft()
    {
        var html = await RenderAsync(RiverInputs.Example, new DateOnly(2027, 2, 3));

        var label = Regex.Match(html, "aria-label=\"([^\"]*)\"").Groups[1].Value;
        label.ShouldStartWith("Timeline of 2026. Today, 3 February 2027, is after the year. No advance payment left to make. Your pay from January to October: ");
        label.ShouldNotContain("still to come");
    }

    [Fact]
    public async Task Timeline_nothingWithheld_labelNamesBothPayers()
    {
        var html = await RenderAsync(RiverInputs.Example.WithTaxWithheld(0m), On23October);

        html.ShouldContain("Next payment by 21 December 2026: €14,999.89 from the company, €14,317.50 from you.");
    }

    // The one picture is a named group a keyboard can scroll, headed by an h2 one level under the tab's h1.
    [Fact]
    public async Task Timeline_example_isAFocusableNamedBoxUnderItsOwnH2()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        Regex.IsMatch(html, "<h2 id=\"timeline-title\" class=\"timeline__title\"[^>]*>The year at a glance</h2>").ShouldBeTrue();
        html.ShouldContain("<div class=\"timeline__scroll\" role=\"group\" aria-labelledby=\"timeline-title\" tabindex=\"0\"");
    }

    [Fact]
    public async Task Timeline_example_hasFourLanesInOrder()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        Lanes(html).ShouldBe(["The company · advance payments", "You · advance payments", "Your pay", "Social contributions"]);
    }

    [Fact]
    public async Task Timeline_riverStartingAtThePay_hasNoCompanyLane()
    {
        var html = await RenderAsync(RiverInputs.Example.SkipCompany(), On23October);

        Lanes(html).ShouldBe(["You · advance payments", "Your pay", "Social contributions"]);
    }

    [Fact]
    public async Task Timeline_companyPaysTheContributions_saysSoOnTheirLane()
    {
        var html = await RenderAsync(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company), On23October);

        (Lanes(html).Last(), html.Contains("Social contributions, paid by the company: ")).ShouldBe(("Social contributions · paid by the company", true));
    }

    // Each deadline is a diamond of its own shape: hollow and muted when past, a larger gold one for the
    // next payment, a small hollow one for a deadline to come with nothing to pay.
    [Fact]
    public async Task Timeline_example_drawsEachDeadlineInTheShapeOfItsState()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        (Count(html, "class=\"timeline__mark timeline__diamond timeline__diamond--past\""),
            Count(html, "class=\"timeline__mark timeline__diamond timeline__diamond--next\""),
            Count(html, "class=\"timeline__mark timeline__diamond timeline__diamond--nothing\""),
            Count(html, "class=\"timeline__mark timeline__diamond timeline__diamond--later\""))
            .ShouldBe((6, 1, 1, 0));
    }

    [Fact]
    public async Task Timeline_paymentPlannedAfterTheOpenDeadline_drawsItFilled()
    {
        var html = await RenderAsync(RiverInputs.Example.WithAdvancePayment(3, 3_000m), new DateOnly(2026, 9, 1));

        Count(html, "class=\"timeline__mark timeline__diamond timeline__diamond--later\"").ShouldBe(1);
    }

    // The next payment's label says its amount and its day; a deadline with nothing says its day alone.
    [Fact]
    public async Task Timeline_example_labelsTheNextPaymentWithItsAmountAndTheOthersWithTheirDay()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        Texts(html, "timeline__label").Take(5).ShouldBe(["10 Apr", "10 Jul", "12 Oct", "€14,999.89 by 21 Dec", "10 Apr"]);
        Regex.IsMatch(html, "timeline__label timeline__label--next[^>]*>€14,999.89 by 21 Dec<").ShouldBeTrue();
    }

    [Fact]
    public async Task Timeline_example_saysNoneDueOnTheDirectorsLane()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        Texts(html, "timeline__note").ShouldBe(["none due"]);
    }

    [Fact]
    public async Task Timeline_exemptDirector_saysExempt()
    {
        var html = await RenderAsync(RiverInputs.Example.WithStarter(true), On23October);

        Texts(html, "timeline__note").ShouldBe(["exempt from the surcharge"]);
    }

    [Fact]
    public async Task Timeline_noPay_saysNothingThisYearOnThePayLane()
    {
        var html = await RenderAsync(RiverInputs.Example.WithDirectorPay(0m), On23October);

        (Texts(html, "timeline__note").Contains("nothing this year"), html.Contains("net in October</span>")).ShouldBe((true, false));
    }

    // The positions are percentages of the year, the middle of Today's day.
    [Fact]
    public async Task Timeline_example_placesTodayAsAPercentageOfTheYear()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        Regex.IsMatch(html, "<span class=\"timeline__today timeline__today--end\" style=\"left: 80.96%\"[^>]*>Today, 23 Oct</span>").ShouldBeTrue();
    }

    [Fact]
    public async Task Timeline_example_labelsTheTwelveMonths()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        Texts(html, "timeline__month").ShouldBe(["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]);
    }

    // The month lines and the Today line are an overlay that stretches with the plot, and no assistive
    // technology meets: the label says what it shows.
    [Fact]
    public async Task Timeline_example_drawsItsLinesInAHiddenStretchingOverlay()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        html.ShouldContain("<svg viewBox=\"0 0 1000 100\" preserveAspectRatio=\"none\" aria-hidden=\"true\" focusable=\"false\"");
        Count(html, "<svg").ShouldBe(1);
        Count(html, "class=\"timeline__grid\"").ShouldBe(11);
        Count(html, "class=\"timeline__today-line\"").ShouldBe(1);
    }

    [Fact]
    public async Task Timeline_afterTheYear_drawsNoLineAndSaysSo()
    {
        var html = await RenderAsync(RiverInputs.Example, new DateOnly(2027, 2, 3));

        Count(html, "timeline__today-line").ShouldBe(0);
        Count(html, "class=\"timeline__today[\" ]").ShouldBe(0);
        html.ShouldContain("Today, 3 February 2027, is after 2026: the whole year is past.");
    }

    [Fact]
    public async Task Timeline_beforeTheYear_drawsNoLineAndSaysSo()
    {
        var html = await RenderAsync(RiverInputs.Example, new DateOnly(2025, 12, 15));

        Count(html, "timeline__today-line").ShouldBe(0);
        html.ShouldContain("Today, 15 December 2025, is before 2026: the whole year is still to come.");
    }

    // A month or a quarter that has ended is an outline, one to come is filled; the quarters carry their
    // number, the months nothing: every month's rules are modelled.
    [Fact]
    public async Task Timeline_example_drawsEndedBlocksAsOutlinesAndComingOnesFilled()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        (Count(html, "timeline__block timeline__block--past"), Count(html, "timeline__block timeline__block--coming")).ShouldBe((12, 4));
        Texts(html, "timeline__block").Where(text => text.Length > 0).ShouldBe(["Q1", "Q2", "Q3", "Q4"]);
    }

    [Fact]
    public async Task Timeline_example_hasNoFootnoteOnRulesNotModelled()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        (html.Contains("not model yet", StringComparison.Ordinal), html.Contains("rules of 1 January", StringComparison.Ordinal)).ShouldBe((false, false));
    }

    // The month under way and its net, the quarter under way and its share.
    [Fact]
    public async Task Timeline_example_captionsTheNextMonthAndTheNextQuarter()
    {
        var river = RiverInputs.Example;
        var view = CalendarView.From(river, river.Run(), On23October).ShouldNotBeNull();

        var html = await RenderAsync(river, On23October);

        Texts(html, "timeline__caption").ShouldBe(["€3,275.73 net in October", $"{Formats.Euros(view.Contributions.EachQuarter)} in Q4"]);
    }

    // From November the caption gives November's own net.
    [Fact]
    public async Task Timeline_exampleInNovember_captionsNovembersNet()
    {
        var html = await RenderAsync(RiverInputs.Example, new DateOnly(2026, 11, 5));

        Texts(html, "timeline__caption")[0].ShouldBe("€3,284.20 net in November");
    }

    [Fact]
    public async Task Timeline_afterTheYear_hasNoCaption()
    {
        var html = await RenderAsync(RiverInputs.Example, new DateOnly(2027, 2, 3));

        Texts(html, "timeline__caption").ShouldBeEmpty();
    }

    // The key names the marks drawn, as words beside their shapes, so no state rests on colour.
    [Fact]
    public async Task Timeline_example_hasAKeyOfTheMarksDrawn()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        Texts(html, "timeline__key-item").ShouldBe(
            ["Deadline passed", "Next payment", "Deadline to come, nothing to pay", "Month or quarter ended", "Month or quarter to come", "Today"]);
        html.ShouldContain("<ul class=\"timeline__key\" aria-label=\"Key to the timeline\"");
    }

    [Fact]
    public async Task Timeline_example_saysTheQuartersAreDrawnWhole()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        html.ShouldContain("The quarters are drawn whole: Netto does not model the day each is paid.");
    }

    // Nothing in the picture is a live region: the bar of buckets announces the river, no one else.
    [Fact]
    public async Task Timeline_example_hasNoLiveRegion()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        html.ShouldNotContain("aria-live");
    }

    // A label stands centred on its mark, whatever its place in the year: none leans, and each is at its
    // mark's position.
    [Fact]
    public async Task Timeline_example_centresEveryLabelOnItsMark()
    {
        var html = await RenderAsync(RiverInputs.Example, On23October);

        Count(html, "timeline__label--start|timeline__label--end").ShouldBe(0);
        var marks = Regex.Matches(html, "timeline__diamond--[a-z]+\" style=\"left: ([0-9.]+)%\"").Select(match => match.Groups[1].Value);
        var labels = Regex.Matches(html, "class=\"timeline__label[^\"]*\" style=\"left: ([0-9.]+)%\"").Select(match => match.Groups[1].Value);
        labels.ShouldBe(marks);
    }

    // A deadline a centred label would push out of the plot is anchored to the side that has room: the
    // label's left edge at a mark early in the year, its right edge at one late in it.
    [Fact]
    public async Task Timeline_deadlinesAtTheEdgesOfTheYear_anchorTheirLabelsInward()
    {
        var river = RiverInputs.Example;
        var calendar = CalendarView.From(river, river.Run(), On23October).ShouldNotBeNull().Calendar;
        var payments = calendar.Director.Payments;
        var early = payments[0] with { Deadline = payments[0].Deadline with { Value = new DateOnly(2026, 1, 2) } };
        var late = payments[3] with { Deadline = payments[3].Deadline with { Value = new DateOnly(2026, 12, 30) } };
        var edges = calendar with { Director = calendar.Director with { Payments = [early, payments[1], payments[2], late] } };

        var html = WebUtility.HtmlDecode(await RenderAsync<CalendarTimeline>(new Folds(new BrowserStorage()), new() { ["Calendar"] = edges, ["NextDate"] = null }));

        Regex.IsMatch(html, "class=\"timeline__label[^\"]*timeline__label--start\" style=\"left: 0.41%\"[^>]*>2 Jan<").ShouldBeTrue();
        Regex.IsMatch(html, "class=\"timeline__label[^\"]*timeline__label--end\" style=\"right: 0.41%\"[^>]*>30 Dec<").ShouldBeTrue();
    }
}

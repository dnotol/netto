using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The When to pay tab as a viewer meets it, drawn without a browser (RiverRendering): what the calendar
// card held at the end of the river, now a tab of its own, open, under its own head: who pays what next,
// the year on a timeline, then the tables. The figures are CalendarViewTests'.
public class WhenToPayTabTests
{
    // The deadline still open on this date is December's.
    private static readonly DateOnly On23October = new(2026, 10, 23);

    private const string CompanySurcharge = "The company's payments leave a surcharge of €675.00 on its corporate tax: €14,999.89 more by 21 December 2026 cancels it.";

    private const string DirectorUnderMinimum = "What is withheld on your pay leaves the surcharge on its income tax under the minimum: none is due.";

    // What is paid next comes first, in a panel of its own: the day, then who pays what.
    [Fact]
    public async Task WhenToPay_example_saysFirstWhatIsPaidNext()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        Regex.IsMatch(html, "^\\s*<div class=\"calendar\"[^>]*>\\s*<section class=\"calendar__panel\"[^>]*>\\s*<h2[^>]*>Next</h2>\\s*<p class=\"calendar__next\"[^>]*>21 December 2026 · €14,999.89 from the company</p>").ShouldBeTrue();
    }

    [Fact]
    public async Task WhenToPay_nothingWithheld_namesWhatBothPay()
    {
        var html = await RenderTabAsync(RiverInputs.Example.WithTaxWithheld(0m));

        html.ShouldContain("21 December 2026 · €14,999.89 from the company, €14,317.50 from you");
    }

    [Fact]
    public async Task WhenToPay_everyDeadlinePast_saysNothingIsLeftAndThatItIsTooLate()
    {
        var html = await RenderTabAsync(RiverInputs.Example, new DateOnly(2026, 12, 22));

        html.ShouldContain("Nothing left to pay in advance in 2026");
        Count(html, "The company's payments leave a surcharge of €675.00 on its corporate tax, and every deadline of 2026 is past.").ShouldBe(1);
    }

    // The sentence on each surcharge is the panel's, once: it is not repeated under the table.
    [Fact]
    public async Task WhenToPay_example_saysEachSurchargeOnceInTheNextPanelBeforeTheTimeline()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        (Count(html, CompanySurcharge), Count(html, DirectorUnderMinimum)).ShouldBe((1, 1));
        var timeline = html.IndexOf("class=\"timeline\"", StringComparison.Ordinal);
        (html.IndexOf(CompanySurcharge, StringComparison.Ordinal) < timeline, html.IndexOf(DirectorUnderMinimum, StringComparison.Ordinal) < timeline).ShouldBe((true, true));
    }

    // The year on a timeline sits between what is next and the tables that say every figure.
    [Fact]
    public async Task WhenToPay_example_drawsTheTimelineAboveTheTables()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        var next = html.IndexOf("class=\"calendar__panel\"", StringComparison.Ordinal);
        var timeline = html.IndexOf("class=\"timeline\"", StringComparison.Ordinal);
        var table = html.IndexOf("class=\"calendar__table\"", StringComparison.Ordinal);

        (next >= 0 && next < timeline && timeline < table).ShouldBeTrue();
    }

    // The timeline is drawn from the same calendar as the tables: its label names the next payment they list.
    [Fact]
    public async Task WhenToPay_example_drawsTheTimelineFromTheSameCalendar()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        html.ShouldContain("Next payment by 21 December 2026: €14,999.89 from the company.");
    }

    // Each part of the calendar under a heading of its own, one level under the tab's title.
    [Fact]
    public async Task WhenToPay_example_showsEachPartUnderItsOwnH2()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        Regex.Matches(html, "<h2[^>]*>([^<]*)</h2>").Select(match => match.Groups[1].Value)
            .ShouldBe(["Next", "The year at a glance", "Advance payments", "Your pay each month", "Social contributions each quarter", "After the year"]);
    }

    // What the fund bills is the tab's one input: refused, the refusal shows beside its field.
    [Fact]
    public async Task WhenToPay_billedAmountRefused_showsTheRefusalBesideItsField()
    {
        var html = await RenderTabAsync(RiverInputs.Example.WithBilledContributions(-1m));

        (html.Contains("id=\"river-billed-contributions\""), html.Contains("refused-message")).ShouldBe((true, true));
    }

    // The refusal is of the fund's bill: the year the timeline draws is not changed by it.
    [Fact]
    public async Task WhenToPay_billedAmountRefused_stillDrawsTheTimeline()
    {
        var html = await RenderTabAsync(RiverInputs.Example.WithBilledContributions(-1m));

        html.ShouldContain("class=\"timeline\"");
    }

    [Fact]
    public async Task WhenToPay_riverRefused_saysThePaymentsWaitAndDrawsNoTimeline()
    {
        var html = await RenderTabAsync(RiverInputs.Example.WithReserve(-1m));

        (html.Contains("The payments wait for the amount the river refuses."), html.Contains("calendar__table"), html.Contains("class=\"timeline\"")).ShouldBe((true, false, false));
    }

    // The fund's bill is an amount: its field sits in a wrapper that keeps it to the width of one, never the
    // tab's, and the wrapper's width is the stylesheet's (the browser's check is how it looks).
    [Fact]
    public async Task WhenToPay_billedField_isInAWrapperSizedToAnAmount()
    {
        var html = await RenderTabAsync(RiverInputs.Example);
        var css = Stylesheet.Read("src", "Netto.Web.Client", "Tabs", "WhenToPayTab.razor.css");

        var width = Regex.Match(Stylesheet.Block(css, ".calendar__billed"), @"max-width:\s*([0-9.]+)rem");
        (Regex.IsMatch(html, "<div class=\"calendar__billed\"[^>]*>\\s*<div class=\"river-field\"[^>]*>.*?id=\"river-billed-contributions\"", RegexOptions.Singleline),
                width.Success && decimal.Parse(width.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) <= 24m)
            .ShouldBe((true, true));
    }

    // Where the tab says which date it reads: with the next deadline it dates, in a note of its own, never in
    // an introduction above the title.
    [Fact]
    public async Task WhenToPay_nextPanel_endsOnTheDateItReads()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        var panel = Regex.Match(html, "<section class=\"calendar__panel\".*?</section>", RegexOptions.Singleline).Value;
        (Regex.IsMatch(panel, "<p class=\"calendar__note\"[^>]*>Today is your device's date; a deadline stays open until the end of its day\\.</p>\\s*</section>$"),
                html.Contains("tab-head__lede"))
            .ShouldBe((true, false));
    }

    // A lens, not a card: no fold to open, and no head of its own: My plan's h1 is the page's, drawn above the lens.
    [Fact]
    public async Task WhenToPay_isNoFoldAndHoldsNoHeadOfItsOwn()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        (html.Contains("fold__toggle"), html.Contains("<h1"), html.Contains("<header")).ShouldBe((false, false, false));
    }

    private static int Count(string html, string text) => html.Split(text).Length - 1;

    private static async Task<string> RenderTabAsync(RiverInputs river, DateOnly? date = null) =>
        WebUtility.HtmlDecode(await RenderAsync<WhenToPayTab>(new Folds(new BrowserStorage()), new()
        {
            ["Inputs"] = river, ["Run"] = river.Run(), ["Date"] = date ?? On23October,
        }));
}

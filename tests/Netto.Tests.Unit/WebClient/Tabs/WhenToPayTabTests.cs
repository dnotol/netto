using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// The When to pay tab as a viewer meets it, drawn without a browser (RiverRendering): what the calendar
// card held at the end of the river, now a tab of its own, open, under its own head. The figures are
// CalendarViewTests'.
public class WhenToPayTabTests
{
    // The deadline still open on this date is December's.
    private static readonly DateOnly On23October = new(2026, 10, 23);

    // What is paid next comes first, as the folded card said it.
    [Fact]
    public async Task WhenToPay_example_saysFirstWhatIsPaidNext()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        Regex.IsMatch(html, "</header>\\s*<div class=\"calendar\"[^>]*>\\s*<p class=\"calendar__next\"[^>]*>Next by 21 December 2026").ShouldBeTrue();
    }

    // Each part of the calendar under a heading of its own, one level under the tab's title.
    [Fact]
    public async Task WhenToPay_example_showsEachPartUnderItsOwnH2()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        Regex.Matches(html, "<h2[^>]*>([^<]*)</h2>").Select(match => match.Groups[1].Value)
            .ShouldBe(["Advance payments", "Your pay each month", "Social contributions each quarter", "After the year"]);
    }

    // What the fund bills is the tab's one input: refused, the refusal shows beside its field.
    [Fact]
    public async Task WhenToPay_billedAmountRefused_showsTheRefusalBesideItsField()
    {
        var html = await RenderTabAsync(RiverInputs.Example.WithBilledContributions(-1m));

        (html.Contains("id=\"river-billed-contributions\""), html.Contains("refused-message")).ShouldBe((true, true));
    }

    [Fact]
    public async Task WhenToPay_riverRefused_saysThePaymentsWait()
    {
        var html = await RenderTabAsync(RiverInputs.Example.WithReserve(-1m));

        (html.Contains("The payments wait for the amount the river refuses."), html.Contains("calendar__table")).ShouldBe((true, false));
    }

    // A tab, not a card: no fold to open, its title the page's h1.
    [Fact]
    public async Task WhenToPay_isNoFoldAndOpensOnItsTitle()
    {
        var html = await RenderTabAsync(RiverInputs.Example);

        (html.Contains("fold__toggle"), Regex.IsMatch(html, "<h1[^>]*>When to pay</h1>")).ShouldBe((false, true));
    }

    private static async Task<string> RenderTabAsync(RiverInputs river) =>
        WebUtility.HtmlDecode(await RenderAsync<WhenToPayTab>(new Folds(new BrowserStorage()), new()
        {
            ["Inputs"] = river, ["Run"] = river.Run(), ["Date"] = On23October,
        }));
}

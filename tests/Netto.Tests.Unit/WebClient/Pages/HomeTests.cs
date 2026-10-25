using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// The front page, at /about, for the portfolio, drawn without a browser (RiverRendering) as a viewer meets
// it on first load. Its scroll to a section when it is opened at one runs after the render, which a
// static render skips: that is the browser check's.
public class HomeTests
{
    // Every link the page draws, in page order: the planner is /plan (the app's own links never go to /,
    // which leaves the plan page), and the page's own sections are its fragments at /about, the header's
    // links being its same-page ones.
    [Fact]
    public async Task Home_links_goToThePlanAndToItsOwnSections()
    {
        var html = await RenderHomeAsync();

        Hrefs(html).ShouldBe(["/plan", "/about#how-it-works", "/plan"]);
    }

    // The sections the header links to and the page scrolls to are on the page.
    [Fact]
    public async Task Home_sections_areEveryOneTheHeaderLinksTo()
    {
        var html = await RenderHomeAsync();

        FrontPage.Sections.Where(section => !html.Contains($"id=\"{section}\"", StringComparison.Ordinal)).ShouldBeEmpty();
    }

    // The portfolio's page makes no claim the product does not keep: the planner is there to use, so the page
    // carries no « In development » label and no line that says it is still being built (the plan's tabs
    // keep their own chip).
    [Fact]
    public async Task Home_firstDraw_carriesNoInDevelopmentLabel()
    {
        var html = await RenderHomeAsync();

        (html.Contains("In development", StringComparison.OrdinalIgnoreCase), html.Contains("being built", StringComparison.OrdinalIgnoreCase)).ShouldBe((false, false));
    }

    // The band that closes the page sends the viewer to the planner with what to do there.
    [Fact]
    public async Task Home_ctaBand_invitesTheViewerToDescribeTheirYear()
    {
        var html = WebUtility.HtmlDecode(await RenderHomeAsync());

        html.ShouldContain("Describe your company's year and see the plan Netto builds from it.");
    }

    // « A simulator, not advice » is one of the page's three principles, with the accountant's check
    // behind it; the footer's disclaimer, on every page, says it again (MainLayout).
    [Fact]
    public async Task Home_principles_sayItIsASimulatorNotAdvice()
    {
        var html = await RenderHomeAsync();

        (Regex.IsMatch(html, "<h3 [^>]*class=\"principle__title\"[^>]*>A simulator, not advice</h3>"),
            Regex.IsMatch(html, "<p [^>]*class=\"principle__text\"[^>]*>Netto shows you the numbers. Check your plan with your accountant before you act on it.</p>"))
            .ShouldBe((true, true));
    }

    private static Task<string> RenderHomeAsync() => RenderAsync<Home>(new Folds(new BrowserStorage()), []);

    private static List<string> Hrefs(string html) =>
        [.. Regex.Matches(html, "href=\"([^\"]*)\"").Select(match => match.Groups[1].Value)];
}

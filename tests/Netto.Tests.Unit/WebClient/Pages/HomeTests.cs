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

    private static Task<string> RenderHomeAsync() => RenderAsync<Home>(new Folds(new BrowserStorage()), []);

    private static List<string> Hrefs(string html) =>
        [.. Regex.Matches(html, "href=\"([^\"]*)\"").Select(match => match.Groups[1].Value)];
}

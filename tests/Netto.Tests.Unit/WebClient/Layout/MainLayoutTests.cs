using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Netto.Web.Client.Layout;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Layout;

// The site's header and footer around every page, drawn without a browser (RiverRendering) at the address a
// viewer is at, with a page's body in between. The app opens on the plan, and the front page, at /about,
// is where the header's links to its sections live.
public class MainLayoutTests
{
    // The router matches the address whatever its case and ignores a trailing slash, and so does the header.
    [Theory]
    [InlineData("about")]
    [InlineData("About")]
    [InlineData("about/")]
    [InlineData("about#levers")]
    public async Task Header_onTheFrontPage_linksToItsSections(string path)
    {
        var html = await RenderLayoutAsync(path);

        Hrefs(Nav(html)).ShouldBe(["/about#levers", "/about#how-it-works", "/about#principles"]);
    }

    // The links are same-page links Blazor scrolls to, which it does not from another page: they exist only
    // on the front page.
    [Theory]
    [InlineData("")]
    [InlineData("plan/map")]
    [InlineData("not-found")]
    public async Task Header_onAnyOtherPage_hasNoSectionLinks(string path)
    {
        var html = await RenderLayoutAsync(path);

        html.ShouldNotContain("site-nav");
    }

    // « Start your plan » is the way into the planner from the front page. On the plan, and on any page of
    // the app, it would link to where the viewer already is, so it is drawn with the section links only.
    [Theory]
    [InlineData("about", true)]
    [InlineData("About/", true)]
    [InlineData("", false)]
    [InlineData("plan/map", false)]
    [InlineData("not-found", false)]
    public async Task Header_startYourPlan_isOnlyOnTheFrontPage(string path, bool shown)
    {
        var html = await RenderLayoutAsync(path);

        html.Contains("Start your plan", StringComparison.Ordinal).ShouldBe(shown);
    }

    // The front page has no link in the header on the app's pages, so the footer, on every page and at every
    // width, is where it is reached from, beside the disclaimer.
    [Theory]
    [InlineData("about")]
    [InlineData("")]
    [InlineData("plan")]
    [InlineData("plan/compare")]
    [InlineData("not-found")]
    public async Task Footer_everyPage_linksToTheFrontPageBesideTheDisclaimer(string path)
    {
        var html = await RenderLayoutAsync(path);

        var footer = Regex.Match(html, "<footer [^>]*>.*?</footer>", RegexOptions.Singleline).Value;
        (Regex.IsMatch(footer, "<a [^>]*href=\"/about\"[^>]*>About Netto</a>"), footer.Contains("Netto is a simulator, not tax advice.", StringComparison.Ordinal))
            .ShouldBe((true, true));
    }

    // On the plan page the band at its top is the header (River/BucketBar): the wordmark, the buckets, the
    // tabs, the plan's actions and the theme switch, in one band, so the layout draws none of its own.
    [Theory]
    [InlineData("plan")]
    [InlineData("Plan/")]
    [InlineData("plan/optimisation")]
    [InlineData("plan/When-To-Pay")]
    [InlineData("plan?card=split")]
    public async Task Header_onThePlan_isLeftToThePlansBand(string path)
    {
        var html = await RenderLayoutAsync(path);

        (html.Contains("site-header", StringComparison.Ordinal), html.Contains("Switch to dark theme", StringComparison.Ordinal)).ShouldBe((false, false));
    }

    // Every other page keeps the site's header, its wordmark and its theme switch: the front page, the root
    // on its way to the plan, and a page not found, at an address under /plan too.
    [Theory]
    [InlineData("about")]
    [InlineData("")]
    [InlineData("not-found")]
    [InlineData("plan/map")]
    public async Task Header_offThePlan_isTheSitesWithItsThemeSwitch(string path)
    {
        var html = await RenderLayoutAsync(path);

        var header = Regex.Match(html, "<header class=\"site-header\"[^>]*>.*?</header>", RegexOptions.Singleline).Value;
        (header.Contains("class=\"wordmark site-header__wordmark\"", StringComparison.Ordinal), header.Contains("aria-label=\"Switch to dark theme\"", StringComparison.Ordinal))
            .ShouldBe((true, true));
    }

    // The wordmark goes to the plan, never to /: a link to / leaves the plan page, which is rebuilt, and
    // forgets what it had found, where /plan from one of its tabs keeps the page. On the plan the band
    // draws its own (BucketBarBandTests).
    [Theory]
    [InlineData("about")]
    [InlineData("not-found")]
    [InlineData("plan/map")]
    public async Task Header_wordmark_goesToThePlan(string path)
    {
        var html = await RenderLayoutAsync(path);

        Regex.Match(html, "<a [^>]*class=\"wordmark site-header__wordmark\"[^>]*>").Value.ShouldContain("href=\"/plan\"");
    }

    // The layout drawn at that address, around a body, over a theme nothing has picked.
    private static Task<string> RenderLayoutAsync(string path) =>
        RenderAsync(typeof(MainLayout), new Folds(new BrowserStorage()), new() { ["Body"] = Text("The page") }, services =>
        {
            services.AddSingleton<NavigationManager>(new NoNavigation($"http://localhost/{path}"));
            services.AddSingleton<IJSRuntime>(new OnlyStopsMudWatching());
            services.AddSingleton(new ThemePreference(new BrowserStorage(), new SystemTheme(prefersLight: true)));
        });

    // The header's nav, from its opening tag to its closing one.
    private static string Nav(string html) =>
        Regex.Match(html, "<nav [^>]*class=\"site-nav\"[^>]*>.*?</nav>", RegexOptions.Singleline).Value;

    private static List<string> Hrefs(string html) =>
        [.. Regex.Matches(html, "href=\"([^\"]*)\"").Select(match => match.Groups[1].Value)];

    // The theme provider tells the script it has stopped watching the system's theme as the render ends, which
    // is the one call a layout's render makes; any other fails the test, as a static render's script does.
    private sealed class OnlyStopsMudWatching : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => Answer<TValue>(identifier);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            Answer<TValue>(identifier);

        private static ValueTask<TValue> Answer<TValue>(string identifier) =>
            identifier == "mudThemeProvider.stopWatchingDarkMode"
                ? default
                : throw new InvalidOperationException($"A static render called JavaScript: {identifier}.");
    }
}

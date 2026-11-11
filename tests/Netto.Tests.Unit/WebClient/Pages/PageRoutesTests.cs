using System.Reflection;
using Microsoft.AspNetCore.Components;
using Netto.Web.Client.Pages;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// Where each page of the client lives. The router matches a URL against these templates and throws on two
// that are ambiguous, but only in the browser, once the page is open: so they are read here, from the
// attributes @page writes. The app opens on the plan: / replaces itself with /plan (PlanEntry), the front
// page being at /about.
public class PageRoutesTests
{
    private static readonly (string Template, string Page)[] Routes =
    [
        .. typeof(Home).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>().Select(route => (route.Template, Page: type.Name)))
            .OrderBy(route => route.Template, StringComparer.Ordinal),
    ];

    [Fact]
    public void Routes_everyPage_isAtItsAddress()
    {
        Routes.ShouldBe(
        [
            ("/", nameof(PlanEntry)),
            ("/about", nameof(Home)),
            ("/not-found", nameof(NotFound)),
            ("/plan", nameof(Plan)),
            ("/plan/compare", nameof(PlanMoved)),
            ("/plan/when-to-pay", nameof(PlanMoved)),
            ("/plan/{Tab}", nameof(Plan)),
        ]);
    }

    // Two pages at one address, the router compares them whatever their case, is how a route goes wrong.
    [Fact]
    public void Routes_noTemplate_isDeclaredTwice()
    {
        Routes.GroupBy(route => route.Template, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).ShouldBeEmpty();
    }

    // What the header reads to know it is on the front page is the page's own address.
    [Fact]
    public void Routes_theFrontPage_isAtFrontPagePath()
    {
        Routes.Single(route => route.Page == nameof(Home)).Template.ShouldBe($"/{FrontPage.Path}");
    }
}

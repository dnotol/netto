using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// The addresses of the two views the plan page folded in: When to pay, now the by-date lens of My plan, and
// Compare plans, now a mode of the Plans panel. A bookmark to either replaces itself with the new place as
// it is first drawn, so Back from there goes to what the viewer came from. A static render runs the
// component's first lifecycle step, which is where it navigates, and goes nowhere, so the navigation is
// read from a manager that records it. That the browser then shows the place is the browser check's.
public class PlanMovedTests
{
    [Theory]
    [InlineData("http://localhost/plan/when-to-pay", "plan?by=date")]
    [InlineData("http://localhost/plan/compare", "plan?plans=compare")]
    [InlineData("http://localhost/Plan/Compare/", "plan?plans=compare")]
    [InlineData("http://localhost/plan/when-to-pay?x=1#section", "plan?by=date")]
    public async Task PlanMoved_firstDraw_replacesItselfWithTheNewPlaceWithoutReloading(string address, string place)
    {
        var navigation = new RecordingNavigation(address);

        await RenderAsync(typeof(PlanMoved), new Folds(new BrowserStorage()), [], services => services.AddSingleton<NavigationManager>(navigation));

        navigation.Navigations.ShouldBe([(place, Replace: true, ForceLoad: false)]);
    }

    // The router, as it runs in the browser: a literal address wins over the plan page's /plan/{Tab}, whose
    // segment the moved addresses would otherwise be taken for, so the moved ones reach their redirect and
    // the other views the plan page, with their segment.
    [Theory]
    [InlineData("/plan/when-to-pay", nameof(PlanMoved), "")]
    [InlineData("/plan/compare", nameof(PlanMoved), "")]
    [InlineData("/plan/Compare", nameof(PlanMoved), "")]
    [InlineData("/plan/compare/", nameof(PlanMoved), "")]
    [InlineData("/plan/pistes", nameof(Plan), "Tab=pistes")]
    [InlineData("/plan/optimisation", nameof(Plan), "Tab=optimisation")]
    [InlineData("/plan", nameof(Plan), "Tab=")]
    [InlineData("/plan?by=date", nameof(Plan), "Tab=")]
    public async Task Router_anAddress_reachesThePageThatAnswersIt(string path, string page, string values)
    {
        var html = await RenderAsync(typeof(Routed), new Folds(new BrowserStorage()), [], services =>
        {
            services.AddSingleton<NavigationManager>(new RecordingNavigation($"http://localhost{path}"));
            services.AddSingleton<INavigationInterception, NoInterception>();
            services.AddSingleton<IScrollToLocationHash, NoScroll>();
        });

        html.ShouldBe($"{page}|{values}");
    }

    // The router as the app runs it, drawing the page it finds as text.
    private sealed class Routed : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Router>(0);
            builder.AddAttribute(1, nameof(Router.AppAssembly), typeof(Plan).Assembly);
            builder.AddAttribute(2, nameof(Router.Found), (RenderFragment<RouteData>)(data => text =>
                text.AddContent(0, $"{data.PageType.Name}|{string.Join(',', data.RouteValues.Select(value => $"{value.Key}={value.Value}"))}")));
            builder.CloseComponent();
        }
    }

    private sealed class NoInterception : INavigationInterception
    {
        public Task EnableNavigationInterceptionAsync() => Task.CompletedTask;
    }

    private sealed class NoScroll : IScrollToLocationHash
    {
        public Task RefreshScrollPositionForHash(string locationAbsolute) => Task.CompletedTask;
    }

    // Records where the page went and how, and goes nowhere, as a render without a browser does.
    private sealed class RecordingNavigation : NavigationManager
    {
        public RecordingNavigation(string address) => Initialize("http://localhost/", address);

        public List<(string Uri, bool Replace, bool ForceLoad)> Navigations { get; } = [];

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            Navigations.Add((uri, options.ReplaceHistoryEntry, options.ForceLoad));
    }
}

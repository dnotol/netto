using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// The app opens on the plan: the address / draws no page of its own, it replaces itself with /plan as it is
// first drawn, so Back from the plan goes to what the viewer came from and not round to / again. A static
// render runs the component's first lifecycle step, which is where it navigates, and goes nowhere, so the
// navigation is read from a manager that records it. That the browser then shows the plan, with Back and
// Forward as they should be, is the browser check's.
public class PlanEntryTests
{
    [Fact]
    public async Task PlanEntry_firstDraw_replacesItselfWithThePlanWithoutReloading()
    {
        var navigation = new RecordingNavigation();

        await RenderAsync(typeof(PlanEntry), new Folds(new BrowserStorage()), [], services => services.AddSingleton<NavigationManager>(navigation));

        navigation.Navigations.ShouldBe([("plan", Replace: true, ForceLoad: false)]);
    }

    // Records where the page went and how, and goes nowhere, as a render without a browser does.
    private sealed class RecordingNavigation : NavigationManager
    {
        public RecordingNavigation() => Initialize("http://localhost/", "http://localhost/");

        public List<(string Uri, bool Replace, bool ForceLoad)> Navigations { get; } = [];

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            Navigations.Add((uri, options.ReplaceHistoryEntry, options.ForceLoad));
    }
}

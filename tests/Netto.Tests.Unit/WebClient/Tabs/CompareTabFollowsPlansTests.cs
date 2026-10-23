using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;
using static Netto.Tests.WebClient.River.PlanFixtures;

namespace Netto.Tests.WebClient.Tabs;

// The tab stays open while the viewer saves, stars, renames or deletes a plan in the Plans panel of the bar,
// so it follows SavedPlans.Changed: it draws the plans as they are then, and lets go of the event when it
// is left. Drawn without a browser, the component kept alive between a change and its second draw.
public class CompareTabFollowsPlansTests
{
    [Fact]
    public async Task Compare_secondPlanSavedWhileOpen_isComparedAtOnce()
    {
        var plans = Saved(RiverInputs.Example);

        var (before, after, _) = await RenderAroundAsync(plans, () => plans.Save(RiverInputs.Example.WithDirectorPay(20_799m), number => $"Plan {number}"));

        (before.Contains("<table"), after.Contains("<table"), after.Contains("« Plan 2 » leaves €2,551.13 more in your pocket than « Plan 1 ».")).ShouldBe((false, true, true));
    }

    [Fact]
    public async Task Compare_planRenamedWhileOpen_isNamedAsItNowIs()
    {
        var plans = Saved(RiverInputs.Example, RiverInputs.Example.WithDirectorPay(20_799m));

        var (_, after, _) = await RenderAroundAsync(plans, () => plans.Rename(plans.Plans[1].Id, "Best pay"));

        (after.Contains("« Best pay » leaves"), after.Contains("Plan 2")).ShouldBe((true, false));
    }

    // The star moves to the second plan: it is the first column now, and the old Today the second.
    [Fact]
    public async Task Compare_todayMovedWhileOpen_isTheFirstColumnNow()
    {
        var plans = Saved(RiverInputs.Example, RiverInputs.Example.WithDirectorPay(20_799m));

        var (_, after, _) = await RenderAroundAsync(plans, () => plans.Star(plans.Plans[1].Id));

        after.Contains("« Plan 1 » leaves €2,551.13 less in your pocket than « Plan 2 ».").ShouldBeTrue();
    }

    [Fact]
    public async Task Compare_planDeletedWhileOpen_leavesOnePlan()
    {
        var plans = Saved(RiverInputs.Example, RiverInputs.Example.WithDirectorPay(20_799m));

        var (_, after, _) = await RenderAroundAsync(plans, () => plans.Delete(plans.Plans[1].Id));

        (after.Contains("<table"), after.Contains("You have one saved plan, « Plan 1 »")).ShouldBe((false, true));
    }

    // Left for another tab, the component lets go of the plans' event, or it would draw into the void and
    // keep the page's plans alive.
    [Fact]
    public async Task Compare_tabLeft_stopsListeningToThePlans()
    {
        var plans = Saved(RiverInputs.Example, RiverInputs.Example.WithDirectorPay(20_799m));

        var (_, _, listening) = await RenderAroundAsync(plans, () => { });

        (listening, Listeners(plans)).ShouldBe((1, 0));
    }

    private static int Listeners(SavedPlans plans) =>
        ((Delegate?)typeof(SavedPlans).GetField("Changed", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(plans))?.GetInvocationList().Length ?? 0;

    // The tab as first drawn, then as drawn once the change has been made and the component has drawn it,
    // and how many listeners the plans had while it was open.
    private static async Task<(string Before, string After, int Listening)> RenderAroundAsync(SavedPlans plans, Action change)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddMudServices();
        services.AddSingleton(new Folds(new BrowserStorage()));
        services.AddSingleton(plans);
        services.AddSingleton<IJSRuntime, NoJavaScript>();
        services.AddSingleton<NavigationManager, NoNavigation>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<CompareTab>(
                ParameterView.FromDictionary(new Dictionary<string, object?> { ["Inputs"] = RiverInputs.Example, ["Run"] = RiverInputs.Example.Run() }));
            var before = WebUtility.HtmlDecode(output.ToHtmlString());
            change();
            await output.QuiescenceTask;
            return (before, WebUtility.HtmlDecode(output.ToHtmlString()), Listeners(plans));
        });
    }
}

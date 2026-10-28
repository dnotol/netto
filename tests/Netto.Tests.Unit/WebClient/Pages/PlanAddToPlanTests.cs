using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// « Add to plan », which sets the plan's optional modules, on the Plan tab among the map's tools, beside the
// legend of its lines, and no longer in the band: the Composition menu, its panel of checkboxes as it was,
// under its new name. The notice of a module taken out outlives the tab it was taken out on, so once Undo or
// Dismiss takes it away the focus goes to « Add to plan » where the tab draws it, and to the start of the
// tab's content where it does not.
public class PlanAddToPlanTests
{
    private static readonly DateTimeOffset On23October = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    // Its button is among the map's tools, after the legend, and the band holds none.
    [Fact]
    public async Task Plan_onThePlanTab_drawsAddToPlanAmongTheMapsToolsAfterTheLegend()
    {
        var html = await RenderPlanAsync();

        var tools = Regex.Match(html, "<div class=\"map-tab__tools\".*?<div class=\"map\" ", RegexOptions.Singleline).Value;
        (Regex.Count(html, "composition__button"), tools.Contains("composition__button", StringComparison.Ordinal),
                tools.IndexOf("map-filter", StringComparison.Ordinal) < tools.IndexOf("composition__button", StringComparison.Ordinal))
            .ShouldBe((1, true, true));
    }

    // The other tabs draw no map, and no « Add to plan ».
    [Theory]
    [InlineData("optimisation")]
    [InlineData("when-to-pay")]
    [InlineData("compare")]
    public async Task Plan_onAnotherTab_drawsNoAddToPlan(string tab)
    {
        var html = await RenderPlanAsync(tab);

        html.ShouldNotContain("composition__button");
    }

    // Its button says « Add to plan », and to a screen reader how many optional modules the plan holds.
    [Fact]
    public async Task Plan_addToPlan_isNamedAddToPlanWithTheModulesThePlanHoldsForAScreenReader()
    {
        var html = await RenderPlanAsync();

        var button = Regex.Match(html, "<button [^>]*composition__button[^>]*>(.*?)</button>", RegexOptions.Singleline);
        (Regex.Match(button.Value, "aria-label=\"([^\"]*)\"").Groups[1].Value, Regex.Replace(Regex.Replace(button.Groups[1].Value, "<[^>]*>", " "), "\\s+", " ").Trim())
            .ShouldBe(("Add to plan: no optional module in your plan", "Add to plan"));
    }

    // On the Plan tab the focus goes back to « Add to plan »; on a tab that does not draw it, to the start of
    // the tab's content, never to a button no longer on the page.
    [Theory]
    [InlineData(PlanTab.Plan, true)]
    [InlineData(PlanTab.Optimisation, false)]
    [InlineData(PlanTab.WhenToPay, false)]
    [InlineData(PlanTab.Compare, false)]
    public void RemovalNoticeFocus_onATab_isAddToPlanWhereItIsDrawnAndTheTabsContentElsewhere(PlanTab tab, bool toAddToPlan)
    {
        RemovalNoticeFocus.After(tab).ShouldBe(toAddToPlan ? RemovalFocus.AddToPlan : RemovalFocus.TabContent);
    }

    // A module taken out on the Plan tab, the notice still showing on Optimisation: Undo puts it back, the
    // focus goes somewhere once, and nothing fails.
    [Fact]
    public async Task Plan_undoneOnAnotherTab_putsTheModuleBackAndMovesTheFocusOnce()
    {
        var storage = new BrowserStorage();
        new SavedPlans(storage).Save(RiverInputs.Example.AddToPlan(OptionalModule.Vouchers), number => $"Plan {number}");
        await using var page = LivePlan.At("http://localhost/plan", storage, holdPaint: true, river: RiverInputs.Example.AddToPlan(OptionalModule.Vouchers));
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Vouchers));
        var card = (await page.FindAllAsync<ModuleCard>()).First(card => card.RemoveLabel is not null);
        await page.InvokeAsync(() => card.OnRemove.InvokeAsync());
        await page.SwitchTabAsync("optimisation");
        var before = page.Focuses;

        await page.ClickAsync(button => button.Class == "taken-out__undo");

        var notice = await page.FindAsync<TakenOutNotice>();
        (notice.Module, (await page.FindAsync<OptimisationTab>()).Inputs.InPlan(OptionalModule.Vouchers), page.Focuses - before, page.Errors.Count)
            .ShouldBe((null, true, 1, 0));
    }

    private static async Task<string> RenderPlanAsync(string? tab = null) =>
        WebUtility.HtmlDecode(await RenderAsync(typeof(Plan), new Folds(new BrowserStorage()), tab is null ? [] : new() { ["Tab"] = tab }, services =>
        {
            var storage = new BrowserStorage();
            var plans = new SavedPlans(storage);
            services.AddSingleton<IBrowserStorage>(storage);
            services.AddSingleton(plans);
            services.AddSingleton(new MapLinkFilter(storage));
            services.AddSingleton(new RiverState(plans) { Inputs = RiverInputs.Example });
            services.AddSingleton<TimeProvider>(new Clock(On23October));
        }));
}

using System.Net;
using System.Text.RegularExpressions;
using MudBlazor;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// « Edit plan », which adds and removes the plan's optional modules, on the Plan tab among the map's tools,
// beside the switch of its picture, and not in the band: the Composition menu, its panel of checkboxes. The
// notice of a module taken out outlives the tab it was taken out on, so once Undo or Dismiss takes it away the
// focus goes to « Edit plan » where the tab draws it, and to the start of the tab's content where it does not.
public class PlanAddToPlanTests
{
    // Its button is among the map's tools, after the switch of the picture, and the band holds none; the key to
    // the lines is no longer among them.
    [Fact]
    public async Task Plan_onThePlanTab_drawsEditPlanAmongTheMapsToolsAfterTheSwitchAndNoKey()
    {
        var html = await RenderPlanAsync();

        var tools = Regex.Match(html, "<div class=\"map-tab__tools\".*?<div class=\"map-tab__frame\"", RegexOptions.Singleline).Value;
        var (picture, button) = (tools.IndexOf("map-picture-switch", StringComparison.Ordinal), tools.IndexOf("composition__button", StringComparison.Ordinal));
        (Regex.Count(html, "composition__button"), picture >= 0, button >= 0, picture < button, tools.Contains("map-key", StringComparison.Ordinal))
            .ShouldBe((1, true, true, true, false));
    }

    // The other views draw no map, and no « Edit plan »; nor does Optimise at the When to pay section.
    [Theory]
    [InlineData("optimisation", null)]
    [InlineData("optimisation", "when")]
    public async Task Plan_onAnotherView_drawsNoEditPlan(string? tab, string? show)
    {
        var html = await PlanPageTests.RenderPlanAsync(new Folds(new BrowserStorage()), tab, show: show);

        html.ShouldNotContain("composition__button");
    }

    // Its button says « Edit plan », and to a screen reader how many optional modules the plan holds.
    [Fact]
    public async Task Plan_editPlan_isNamedEditPlanWithTheModulesThePlanHoldsForAScreenReader()
    {
        var html = await RenderPlanAsync();

        var button = Regex.Match(html, "<button [^>]*composition__button[^>]*>(.*?)</button>", RegexOptions.Singleline);
        (Regex.Match(button.Value, "aria-label=\"([^\"]*)\"").Groups[1].Value, ButtonText(button))
            .ShouldBe(("Edit plan: no optional module in your plan", "Edit plan"));
    }

    // A tune icon, not the plus of an addition: the panel removes as well.
    [Fact]
    public async Task Plan_editPlan_wearsTheTuneIconNotThePlus()
    {
        var html = await RenderPlanAsync();

        var button = Regex.Match(html, "<button [^>]*composition__button[^>]*>.*?</button>", RegexOptions.Singleline).Value;
        (button.Contains(Icons.Material.Rounded.Tune, StringComparison.Ordinal), button.Contains(Icons.Material.Rounded.Add, StringComparison.Ordinal))
            .ShouldBe((true, false));
    }

    [Fact]
    public async Task Plan_editPlan_inFrenchIsModifierLePlan()
    {
        using var french = InLanguage.French();

        var html = WebUtility.HtmlDecode(await RenderPlanAsync());

        var button = Regex.Match(html, "<button [^>]*composition__button[^>]*>(.*?)</button>", RegexOptions.Singleline);
        (Regex.Match(button.Value, "aria-label=\"([^\"]*)\"").Groups[1].Value, ButtonText(button))
            .ShouldBe(("Modifier le plan : aucun module facultatif dans votre plan", "Modifier le plan"));
    }

    // The panel's title names what the panel does, as its button does.
    [Fact]
    public void Panel_title_saysEditYourPlanInBothLanguages()
    {
        (ResxText.For<CompositionMenu>().Get("AddTitle"), ResxText.French<CompositionMenu>().Get("AddTitle")).ShouldBe(("Edit your plan", "Modifier votre plan"));
    }

    // On the map the focus goes back to « Edit plan »; where it is not drawn, on another view, to the start of
    // the view's content, never to a button no longer on the page.
    [Theory]
    [InlineData(PlanTab.Plan, true)]
    [InlineData(PlanTab.Optimisation, false)]
    public void RemovalNoticeFocus_onAView_isEditPlanWhereItIsDrawnAndTheViewsContentElsewhere(PlanTab tab, bool toEditPlan)
    {
        RemovalNoticeFocus.After(tab).ShouldBe(toEditPlan ? RemovalFocus.AddToPlan : RemovalFocus.TabContent);
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
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(MapChoice.Of(RiverModule.Vouchers)));
        var card = (await page.FindAllAsync<ModuleCard>()).First(card => card.RemoveLabel is not null);
        await page.InvokeAsync(() => card.OnRemove.InvokeAsync());
        await page.SwitchTabAsync("optimisation");
        var before = page.Focuses;

        await page.ClickAsync(button => button.Class == "taken-out__undo");

        var notice = await page.FindAsync<TakenOutNotice>();
        (notice.Module, (await page.FindAsync<OptimisationTab>()).Inputs.InPlan(OptionalModule.Vouchers), page.Focuses - before, page.Errors.Count)
            .ShouldBe((null, true, 1, 0));
    }

    private static string ButtonText(Match button) =>
        Regex.Replace(Regex.Replace(button.Groups[1].Value, "<[^>]*>", " "), "\\s+", " ").Trim();

    // The plan page drawn on the example, on the tab given, as PlanPageTests draws it.
    private static Task<string> RenderPlanAsync(string? tab = null) => PlanPageTests.RenderPlanAsync(new Folds(new BrowserStorage()), tab);
}

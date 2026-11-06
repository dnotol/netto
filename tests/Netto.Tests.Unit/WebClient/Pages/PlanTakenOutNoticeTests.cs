using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The notice « … taken out of your plan », with its Undo, on the plan page as the browser runs it (LivePlan,
// the paint held, since none of this is about the moves' search): a module the viewer takes out, from
// « Add to plan » or from its card, is announced; a plan opened from Plans replaces the river and is not an
// edit of it, so it announces nothing, takes the focus to no Undo and leaves the river on the plan as saved.
// The notice is read from the parameter the page hands it, the focus from the calls the page makes of the
// script, and the viewer's clicks are the callbacks the page's components hand each other.
public class PlanTakenOutNoticeTests
{
    private static readonly RiverInputs WithVouchers = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);

    private static readonly RiverInputs WithAllowances = RiverInputs.Example.AddToPlan(OptionalModule.Allowances);

    // The river holds Vouchers, a plan saved without them is opened: the notice would say Vouchers were taken out.
    [Fact]
    public async Task Plan_planWithoutAModuleOpenedOverARiverHoldingIt_showsNoNotice()
    {
        var storage = Saved(RiverInputs.Example, WithVouchers);
        await using var page = At(storage);
        await page.LoadAsync();
        await OpenPanelAsync(page);

        await OpenAsync(page, storage, "Plan 1");

        (await page.FindAsync<TakenOutNotice>()).Module.ShouldBeNull();
        page.Errors.ShouldBeEmpty();
    }

    // Opening gives the focus back to the Plans button, which PlansPanel does as it closes, and moves it
    // nowhere else, Undo least of all.
    [Fact]
    public async Task Plan_planOpened_givesTheFocusBackToThePlansButtonAlone()
    {
        var storage = Saved(RiverInputs.Example, WithVouchers);
        await using var page = At(storage);
        await page.LoadAsync();
        await OpenPanelAsync(page);
        var before = page.Focuses;

        await OpenAsync(page, storage, "Plan 1");

        (page.Focuses - before, page.Errors.Count).ShouldBe((1, 0));
    }

    // The map shows the plan as saved and has nothing unsaved, and the split's search runs again on it.
    [Fact]
    public async Task Plan_planOpened_showsItOnTheMapWithoutUnsavedChanges()
    {
        var storage = Saved(RiverInputs.Example, WithVouchers);
        await using var page = At(storage);
        await page.LoadAsync();
        var searched = (await page.FindAsync<MapTab>()).Search;
        await OpenPanelAsync(page);

        await OpenAsync(page, storage, "Plan 1");

        var plan = Inputs(storage, "Plan 1");
        var plans = await page.FindAsync<PlansPanel>();
        var menu = await page.FindAsync<CompositionMenu>();
        var map = await page.FindAsync<MapTab>();
        page.ShouldSatisfyAllConditions(
            () => plans.Unsaved.ShouldBeFalse(),
            () => menu.Inputs.ShouldBe(plan),
            () => map.Inputs.ShouldBe(plan),
            () => map.Search.ShouldNotBeSameAs(searched),
            () => page.Errors.ShouldBeEmpty());
    }

    // A notice already showing speaks of an edit of the river the opened plan has replaced: its Undo would
    // put the module into the plan just opened, and leave it with unsaved changes.
    [Fact]
    public async Task Plan_planOpenedWhileANoticeShows_clearsIt()
    {
        var storage = Saved(RiverInputs.Example, WithAllowances);
        await using var page = At(storage);
        await page.LoadAsync();
        await page.ClickAsync(Styled("composition__button"));
        await TakeOutOfTheMenuAsync(page, OptionalModule.Allowances);
        await page.ClickAsync(Styled("composition__done"));
        (await page.FindAsync<TakenOutNotice>()).Module.ShouldBe(OptionalModule.Allowances);
        await OpenPanelAsync(page);

        await OpenAsync(page, storage, "Plan 1", confirm: true);

        (await page.FindAsync<TakenOutNotice>()).Module.ShouldBeNull();
        page.Errors.ShouldBeEmpty();
    }

    // With no plan starred Today, the moves are measured from the plan opened, under its name.
    [Fact]
    public async Task Plan_planOpenedWithNoTodayStarred_measuresTheMovesFromIt()
    {
        var storage = SavedWithoutToday(RiverInputs.Example.WithDirectorPay(40_000m), RiverInputs.Example, WithVouchers);
        await using var page = At(storage, "optimisation");
        await page.LoadAsync("optimisation");
        await OpenPanelAsync(page);

        await OpenAsync(page, storage, "Plan 2");

        var tab = await page.FindAsync<OptimisationTab>();
        page.ShouldSatisfyAllConditions(
            () => tab.From.ShouldBe(new MovesBaseline(Inputs(storage, "Plan 2"), IsToday: false, "Plan 2")),
            () => page.Errors.ShouldBeEmpty());
    }

    // Taken out from the open menu, the focus stays on the menu's checkbox, so it moves nowhere.
    [Fact]
    public async Task Plan_moduleTakenOutFromTheMenu_showsTheNoticeAndMovesNoFocus()
    {
        await using var page = At(Saved(WithVouchers));
        await page.LoadAsync();
        await page.ClickAsync(Styled("composition__button"));
        var before = page.Focuses;

        await TakeOutOfTheMenuAsync(page, OptionalModule.Vouchers);

        var notice = await page.FindAsync<TakenOutNotice>();
        (notice.Module, page.Focuses - before, page.Errors.Count).ShouldBe((OptionalModule.Vouchers, 0, 0));
    }

    // Undo puts the module back, the notice goes, and the focus goes to « Add to plan », where it was taken out.
    [Fact]
    public async Task Plan_moduleTakenOutFromTheMenuAndUndone_isBackInThePlan()
    {
        await using var page = At(Saved(WithVouchers));
        await page.LoadAsync();
        await page.ClickAsync(Styled("composition__button"));
        await TakeOutOfTheMenuAsync(page, OptionalModule.Vouchers);
        var before = page.Focuses;

        await page.ClickAsync(Styled("taken-out__undo"));

        var notice = await page.FindAsync<TakenOutNotice>();
        var menu = await page.FindAsync<CompositionMenu>();
        (notice.Module, menu.Inputs.InPlan(OptionalModule.Vouchers), page.Focuses - before, page.Errors.Count).ShouldBe((null, true, 1, 0));
    }

    // Taken out from its card beside the map, the word that did it is gone with the card, so the focus goes
    // to Undo.
    [Fact]
    public async Task Plan_moduleTakenOutFromItsCard_showsTheNoticeAndFocusesUndo()
    {
        await using var page = At(Saved(WithVouchers));
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(MapChoice.Of(RiverModule.Vouchers)));
        var card = (await page.FindAllAsync<ModuleCard>()).First(card => card.RemoveLabel is not null);
        var before = page.Focuses;

        await page.InvokeAsync(() => card.OnRemove.InvokeAsync());

        var notice = await page.FindAsync<TakenOutNotice>();
        (notice.Module, page.Focuses - before, page.Errors.Count).ShouldBe((OptionalModule.Vouchers, 1, 0));
    }

    // These plans saved in this order into the browser's storage, named Plan 1, Plan 2…: the first is starred
    // Today, unless it is deleted again, and the last is the one the river is opened from.
    private static BrowserStorage Saved(params RiverInputs[] inputs)
    {
        var storage = new BrowserStorage();
        var plans = new SavedPlans(storage);
        foreach (var plan in inputs)
            plans.Save(plan, number => $"Plan {number}");

        return storage;
    }

    // The same, with the first plan deleted again, so that none is starred Today.
    private static BrowserStorage SavedWithoutToday(params RiverInputs[] inputs)
    {
        var storage = Saved(inputs);
        var plans = new SavedPlans(storage);
        plans.Delete(plans.Today ?? throw new InvalidOperationException("The first plan saved is starred."));

        return storage;
    }

    // The page at the plan's address, or another tab's, its river opened on the plan the storage opens it
    // from, as the page itself reads it.
    private static LivePlan At(BrowserStorage storage, string? tab = null) =>
        LivePlan.At(tab is null ? "http://localhost/plan" : $"http://localhost/plan/{tab}", storage, holdPaint: true, river: Inputs(storage));

    // The inputs of the plan the storage holds under that name, or those of the plan opened.
    private static RiverInputs Inputs(BrowserStorage storage, string? name = null)
    {
        var plans = new SavedPlans(storage);
        var plan = name is null ? plans.OpenedPlan : plans.Plans.Single(saved => saved.Name == name);

        return plan?.Inputs ?? throw new InvalidOperationException($"The storage holds no readable plan {name ?? "the river was opened from"}.");
    }

    private static Task OpenPanelAsync(LivePlan page) => page.ClickAsync(Styled("plans__button"));

    // The viewer opens that plan from the panel, asked first when the river has changes no plan holds, and the
    // river is then on it.
    private static async Task OpenAsync(LivePlan page, BrowserStorage storage, string name, bool confirm = false)
    {
        await page.ClickAsync(Labelled(ResxText.For<PlansPanel>().Format("OpenLabel", name)));
        if (confirm)
            await page.ClickAsync(Styled("plan__yes"));

        (await page.FindAsync<PlansPanel>()).Inputs.ShouldBe(Inputs(storage, name), $"{name} should be in the river.");
    }

    // The menu's checkbox for the module, « Add to plan » being among the map's tools after the legend's
    // boxes, one for each kind of link.
    private static async Task TakeOutOfTheMenuAsync(LivePlan page, OptionalModule module)
    {
        var box = (await page.FindAllAsync<MudCheckBox<bool>>())[(int)module];
        await page.InvokeAsync(() => box.ValueChanged.InvokeAsync(false));
    }

    private static Func<MudButton, bool> Styled(string @class) => button => button.Class == @class;

    // A label the component drew from its localizer is a LocalizedString, not a string, until it is rendered.
    private static Func<MudButton, bool> Labelled(string label) =>
        button => button.UserAttributes.TryGetValue("aria-label", out var value) && value?.ToString() == label;
}

using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The card of the module chosen on the Map tab, beside the map, on the plan page as the browser runs it
// (LivePlan): the same card as on the Plan tab, whose edits go to the page's one river so the bar, the map
// and the card say the same figures at once, the engine's refusal on the card that holds the field, the split's
// search, its word to take the module out of the plan with the notice and Undo of any tab, and the focus,
// which goes to the card's heading when the viewer chooses a module, never on arriving at the tab. The
// focus is read from the calls the page makes of the script, the viewer's clicks are the callbacks the
// page's components hand each other; where the focus lands and how the layout looks is the browser check's.
public class PlanMapPanelTests
{
    // The plan the Map tab's other page tests take, with a company car.
    private static RiverInputs WithACar => PlanMapChoiceTests.WithACar;

    [Fact]
    public async Task Plan_aModuleChosenOnTheMap_opensItsCardBesideIt()
    {
        await using var page = await MapAsync(WithACar);

        await ChooseAsync(page, RiverModule.Car);

        var (panel, card) = (await page.FindAsync<MapPanel>(), await page.FindAsync<ModuleCard>());
        (panel.Module, card.Name, (await page.FindAllAsync<ModuleCard>()).Count, page.Errors.Count).ShouldBe((RiverModule.Car, "car", 1, 0));
    }

    // Nothing chosen: no card, only its room.
    [Fact]
    public async Task Plan_nothingChosenOnTheMap_drawsNoCard()
    {
        await using var page = await MapAsync(WithACar);

        ((await page.FindAllAsync<ModuleCard>()).Count, (await page.FindAsync<MapPanel>()).Module, page.Errors.Count).ShouldBe((0, (RiverModule?)null, 0));
    }

    // An amount changed on the card changes the river everywhere at once: the bar's run, the map's run and
    // what it draws, and the card's own; the page has changes no plan holds.
    [Fact]
    public async Task Plan_anAmountChangedOnTheCard_changesTheBarTheMapAndTheCardAtOnce()
    {
        await using var page = await MapAsync(WithACar);
        await ChooseAsync(page, RiverModule.CompanyYear);
        var before = await page.FindAsync<MapTab>();
        var wordsBefore = before.DrawnWords;
        var card = await page.FindAsync<CompanyYearCard>();
        var edited = card.Inputs.WithRevenue(card.Inputs.Company!.Revenue.Value + 30_000m);

        await page.InvokeAsync(() => card.InputsChanged.InvokeAsync(edited));

        var (bar, map, changed) = (await page.FindAsync<BucketBar>(), await page.FindAsync<MapTab>(), await page.FindAsync<CompanyYearCard>());
        page.ShouldSatisfyAllConditions(
            () => map.Inputs.ShouldBe(edited),
            () => changed.Inputs.ShouldBe(edited),
            () => map.Run.ShouldBeSameAs(bar.Run),
            () => changed.Run.ShouldBeSameAs(bar.Run),
            () => bar.Run.Year!.Scenario.Revenue.ShouldBe(edited.Company!.Revenue.Value),
            () => map.DrawnWords.ShouldNotBe(wordsBefore),
            () => bar.Unsaved.ShouldBeTrue(),
            () => page.Errors.ShouldBeEmpty());
    }

    // The split's search is the page's one, the curve its card draws; the card has no search of its own.
    [Fact]
    public async Task Plan_theSplitChosenOnTheMap_isHandedThePagesSearch()
    {
        await using var page = await MapAsync(WithACar);

        await ChooseAsync(page, RiverModule.Split);

        var (split, map) = (await page.FindAsync<SplitCard>(), await page.FindAsync<MapTab>());
        map.Search.ShouldNotBeNull();
        split.Search.ShouldBeSameAs(map.Search);
    }

    // The engine's refusal goes to the card holding the refused field, as on the Plan tab, and to no other.
    [Fact]
    public async Task Plan_aRefusedAmount_isOnTheCardHoldingItWhenThatModuleIsChosen()
    {
        await using var page = await MapAsync(RiverInputs.Example.WithReserve(-1m));

        await ChooseAsync(page, RiverModule.Dividend);
        var holding = (await page.FindAsync<DividendCard>()).Refused;
        await ChooseAsync(page, RiverModule.DirectorPay);
        var other = (await page.FindAsync<DirectorPayCard>()).Refused;

        (holding?.Input, other).ShouldBe((Netto.Core.Rules.RefusedInput.Reserve, null));
    }

    // The refusal is said once, where its field is: on the card that holds it when that module is chosen, and
    // above the map otherwise. A screen reader hears each refusal drawn (role="alert"), so the card's and the
    // map's must not both be there.
    [Theory]
    [InlineData(null)]
    [InlineData(RiverModule.DirectorPay)]
    [InlineData(RiverModule.Dividend)]
    public async Task Plan_aRefusedAmount_isSaidOnceWhateverModuleIsChosen(RiverModule? chosen)
    {
        await using var page = await MapAsync(RiverInputs.Example.WithReserve(-1m));
        await ChooseAsync(page, chosen);

        var said = (await page.FindAllAsync<RefusedMessage>()).Count(message => message.Error is not null);

        said.ShouldBe(1);
    }

    // Taken out from the card, the notice and Undo of any tab say so and the focus goes to Undo, as on the
    // Plan tab; the module is off the map, so it is chosen no more and the card goes.
    [Fact]
    public async Task Plan_aLeverTakenOutFromItsCardBesideTheMap_isNoticedAndLeavesTheMapUnchosen()
    {
        await using var page = await MapAsync(WithACar);
        await ChooseAsync(page, RiverModule.Car);
        var card = (await page.FindAllAsync<ModuleCard>()).Single(card => card.RemoveLabel is not null);
        var before = page.Focuses;

        await page.InvokeAsync(() => card.OnRemove.InvokeAsync());

        var (notice, map, panel, cards) = (await page.FindAsync<TakenOutNotice>(), await page.FindAsync<MapTab>(), await page.FindAsync<MapPanel>(), await page.FindAllAsync<ModuleCard>());
        page.ShouldSatisfyAllConditions(
            () => notice.Module.ShouldBe(OptionalModule.Car),
            () => page.Focuses.ShouldBe(before + 1),
            () => map.Chosen.ShouldBeNull(),
            () => panel.Module.ShouldBeNull(),
            () => map.Inputs.InPlan(OptionalModule.Car).ShouldBeFalse(),
            () => cards.ShouldBeEmpty(),
            () => page.Errors.ShouldBeEmpty());
    }

    // Undo puts the module back on the map, unchosen: the viewer chooses it again if they want its card.
    [Fact]
    public async Task Plan_aLeverTakenOutFromItsCardAndUndone_isBackOnTheMapUnchosen()
    {
        await using var page = await MapAsync(WithACar);
        await ChooseAsync(page, RiverModule.Car);
        var card = (await page.FindAllAsync<ModuleCard>()).Single(card => card.RemoveLabel is not null);
        await page.InvokeAsync(() => card.OnRemove.InvokeAsync());

        await page.ClickAsync(button => button.Class == "taken-out__undo");

        var map = await page.FindAsync<MapTab>();
        (map.Inputs.InPlan(OptionalModule.Car), map.Chosen, (await page.FindAllAsync<ModuleCard>()).Count, page.Errors.Count).ShouldBe((true, (RiverModule?)null, 0, 0));
    }

    // The viewer's choice puts the focus on the card's heading, once for each module chosen; letting a module
    // go takes it nowhere.
    [Fact]
    public async Task Plan_eachModuleChosen_takesTheFocusToItsCardOnce()
    {
        await using var page = await MapAsync(WithACar);
        var opened = page.Focuses;

        await ChooseAsync(page, RiverModule.Car);
        var first = page.Focuses;
        await ChooseAsync(page, RiverModule.Dividend);
        var second = page.Focuses;
        await ChooseAsync(page, null);
        var letGo = page.Focuses;

        (first - opened, second - first, letGo - second, page.Errors.Count).ShouldBe((1, 1, 0, 0));
    }

    // A change made on the card does not take the focus again: it is where the viewer left it.
    [Fact]
    public async Task Plan_anAmountChangedOnTheCard_takesNoFocus()
    {
        await using var page = await MapAsync(WithACar);
        await ChooseAsync(page, RiverModule.CompanyYear);
        var before = page.Focuses;
        var card = await page.FindAsync<CompanyYearCard>();

        await page.InvokeAsync(() => card.InputsChanged.InvokeAsync(card.Inputs.WithRevenue(card.Inputs.Company!.Revenue.Value + 1_000m)));

        page.Focuses.ShouldBe(before);
    }

    // Arriving at the tab with a module already chosen (the page keeps the choice from one tab to the next)
    // draws its card, and only the tab's title takes the focus.
    [Fact]
    public async Task Plan_theMapReopenedWithAModuleChosen_drawsItsCardAndTakesNoFocusForIt()
    {
        await using var page = await MapAsync(WithACar);
        await ChooseAsync(page, RiverModule.Car);
        await page.SwitchTabAsync("compare");
        var before = page.Focuses;

        await page.SwitchTabAsync("map");

        var (card, panel) = ((await page.FindAllAsync<ModuleCard>()).Count, await page.FindAsync<MapPanel>());
        (card, panel.Module, page.Focuses - before, page.Errors.Count).ShouldBe((1, RiverModule.Car, 1, 0));
    }

    // Nothing the card does touches the folds the viewer made: nothing is stored by choosing, editing or
    // taking a module out.
    [Fact]
    public async Task Plan_aCardBesideTheMap_storesNoFold()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan/map", storage, river: WithACar);
        await page.LoadAsync("map");
        await ChooseAsync(page, RiverModule.CompanyYear);
        var card = await page.FindAsync<CompanyYearCard>();
        await page.InvokeAsync(() => card.InputsChanged.InvokeAsync(card.Inputs.WithRevenue(card.Inputs.Company!.Revenue.Value + 1_000m)));
        await ChooseAsync(page, RiverModule.Car);
        var car = (await page.FindAllAsync<ModuleCard>()).Single(module => module.RemoveLabel is not null);

        await page.InvokeAsync(() => car.OnRemove.InvokeAsync());

        storage.Get(Folds.StorageKey).ShouldBeNull();
    }

    // A skipped Company year stays on the map and can be chosen: its skipped card is beside the map, and its
    // button to enter the company's year works there, the card staying chosen and taking the focus on its
    // heading, since the button the viewer pressed is gone with the skipped card.
    [Fact]
    public async Task Plan_aSkippedCompanyYearChosenOnTheMap_hasItsSkippedCardWhoseEnterWorks()
    {
        await using var page = await MapAsync(WithACar.SkipCompany());
        await ChooseAsync(page, RiverModule.CompanyYear);
        var skipped = (await page.FindAsync<CompanyYearCard>()).Inputs.Company;
        var before = page.Focuses;

        await page.ClickAsync(button => button.Class == "company-year__enter");

        var (map, card) = (await page.FindAsync<MapTab>(), await page.FindAsync<CompanyYearCard>());
        page.ShouldSatisfyAllConditions(
            () => skipped.ShouldBeNull(),
            () => card.Inputs.Company.ShouldNotBeNull(),
            () => map.Chosen.ShouldBe(RiverModule.CompanyYear),
            () => map.ShownMap.Modules.Select(node => node.End.Module).ShouldContain(RiverModule.Split),
            () => page.Focuses.ShouldBe(before + 1),
            () => page.Errors.ShouldBeEmpty());
    }

    // Cleared from its card beside the map, Company year stays on the map and chosen, its card turns to the
    // skipped one, and the focus goes to its heading rather than falling to the document with the button.
    [Fact]
    public async Task Plan_companyYearClearedFromItsCardBesideTheMap_staysChosenWithItsSkippedCardAndTheFocus()
    {
        await using var page = await MapAsync(WithACar);
        await ChooseAsync(page, RiverModule.CompanyYear);
        var before = page.Focuses;

        await page.ClickAsync(button => button.Class == "company-year__clear");

        var (map, card, panel) = (await page.FindAsync<MapTab>(), await page.FindAsync<CompanyYearCard>(), await page.FindAsync<MapPanel>());
        page.ShouldSatisfyAllConditions(
            () => card.Inputs.Company.ShouldBeNull(),
            () => map.Chosen.ShouldBe(RiverModule.CompanyYear),
            () => panel.Module.ShouldBe(RiverModule.CompanyYear),
            () => map.ShownMap.Modules.Select(node => node.End.Module).ShouldBe([RiverModule.CompanyYear, RiverModule.DirectorPay]),
            () => page.Focuses.ShouldBe(before + 1),
            () => page.Errors.ShouldBeEmpty());
    }

    // Cleared and entered again from the panel, the viewer is where they were, the focus on the heading each time.
    [Fact]
    public async Task Plan_companyYearClearedThenEnteredFromItsCardBesideTheMap_isBackWithItsCompany()
    {
        await using var page = await MapAsync(WithACar);
        await ChooseAsync(page, RiverModule.CompanyYear);
        var before = page.Focuses;
        await page.ClickAsync(button => button.Class == "company-year__clear");

        await page.ClickAsync(button => button.Class == "company-year__enter");

        var (map, card) = (await page.FindAsync<MapTab>(), await page.FindAsync<CompanyYearCard>());
        page.ShouldSatisfyAllConditions(
            () => card.Inputs.Company.ShouldNotBeNull(),
            () => map.Chosen.ShouldBe(RiverModule.CompanyYear),
            () => map.ShownMap.Modules.Select(node => node.End.Module).ShouldContain(RiverModule.Split),
            () => page.Focuses.ShouldBe(before + 2),
            () => page.Errors.ShouldBeEmpty());
    }

    // On the Plan tab the card is the river's, as it was: the page moves no focus for Clear.
    [Fact]
    public async Task Plan_companyYearClearedFromItsCardInTheRiver_movesNoFocus()
    {
        var storage = new BrowserStorage();
        new Folds(storage).Set("company-year", true);
        await using var page = LivePlan.At("http://localhost/plan", storage, river: WithACar);
        await page.LoadAsync();
        var before = page.Focuses;

        await page.ClickAsync(button => button.Class == "company-year__clear");

        ((await page.FindAsync<CompanyYearCard>()).Inputs.Company, page.Focuses - before, page.Errors.Count).ShouldBe((null, 0, 0));
    }

    private static async Task<LivePlan> MapAsync(RiverInputs river)
    {
        var page = LivePlan.At("http://localhost/plan/map", new BrowserStorage(), river: river);
        await page.LoadAsync("map");

        return page;
    }

    // The viewer chooses that module on the map, or lets it go.
    private static async Task ChooseAsync(LivePlan page, RiverModule? module)
    {
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(module));
    }

    private static int Focuses(LivePlan page) => page.Calls.Count(call => call.Contains("focus", StringComparison.OrdinalIgnoreCase));

}

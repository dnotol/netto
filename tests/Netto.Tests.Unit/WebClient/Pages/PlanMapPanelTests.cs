using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The card of the module chosen on the map, beside it, on the plan page as the browser runs it (LivePlan):
// the module's card, whose edits go to the page's one river so the bar, the map and the card say the same
// figures at once, the engine's refusal on the card that holds the field, the split's
// search, its word to take the module out of the plan with the notice and Undo of any tab, and the focus,
// which goes to the card's heading when the viewer chooses a module on the map, never on arriving at the
// tab nor for a module the page chooses itself, a lever just added. The
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
        (panel.Chosen, card.Name, (await page.FindAllAsync<ModuleCard>()).Count, page.Errors.Count).ShouldBe((MapChoice.Of(RiverModule.Car), "car", 1, 0));
    }

    // The viewer clicks the module chosen on the map again, as the browser sends the click to its box: it is
    // let go, its card leaving the room beside the map, and nothing fails in between.
    [Fact]
    public async Task Plan_theModuleChosenClickedAgainOnTheMap_isLetGoAndNothingFails()
    {
        await using var page = await MapAsync(WithACar);
        await page.PressAsync("data-module", "director-pay");

        await page.PressAsync("data-module", "director-pay");

        var (module, cards) = ((await page.FindAsync<MapPanel>()).Chosen, (await page.FindAllAsync<ModuleCard>()).Count);
        page.ShouldSatisfyAllConditions(
            () => page.Errors.Select(error => error.Message).ShouldBeEmpty(),
            () => (module, cards).ShouldBe(((MapChoice?)null, 0)));
    }

    // Company year, chosen on arriving, let go: no card, only its room.
    [Fact]
    public async Task Plan_theModuleLetGo_drawsNoCard()
    {
        await using var page = await MapAsync(WithACar);

        await ChooseAsync(page, null);

        ((await page.FindAllAsync<ModuleCard>()).Count, (await page.FindAsync<MapPanel>()).Chosen, page.Errors.Count).ShouldBe((0, (MapChoice?)null, 0));
    }

    // A lever added from « Edit plan » is chosen, its card beside the map to be filled, and the focus stays
    // in its panel, where the viewer may add another.
    [Fact]
    public async Task Plan_aLeverAddedFromTheMenu_isChosenWithoutTakingTheFocus()
    {
        await using var page = await MapAsync(RiverInputs.Example);
        var menu = await page.FindAsync<CompositionMenu>();
        var before = page.Focuses;

        await page.InvokeAsync(() => menu.InputsChanged.InvokeAsync(RiverInputs.Example.AddToPlan(OptionalModule.Vouchers)));

        ((await page.FindAsync<MapPanel>()).Chosen, page.Focuses - before, page.Errors.Count).ShouldBe((MapChoice.Of(RiverModule.Vouchers), 0, 0));
    }

    // An amount changed on the card changes the river everywhere at once: the bar's run, the map's run and
    // what it draws, and the card's own; the page has changes no plan holds.
    [Fact]
    public async Task Plan_anAmountChangedOnTheCard_changesTheBarTheMapAndTheCardAtOnce()
    {
        await using var page = await MapAsync(WithACar);
        await ChooseAsync(page, RiverModule.CompanyYear);
        var before = await page.FindAsync<MapTab>();
        var amountsBefore = before.DrawnAmounts;
        var card = await page.FindAsync<CompanyYearCard>();
        var edited = card.Inputs.WithRevenue(card.Inputs.Company!.Revenue.Value + 30_000m);

        await page.InvokeAsync(() => card.InputsChanged.InvokeAsync(edited));

        var (bar, map, changed, plans) = (await page.FindAsync<BucketBar>(), await page.FindAsync<MapTab>(), await page.FindAsync<CompanyYearCard>(),
            await page.FindAsync<PlansPanel>());
        page.ShouldSatisfyAllConditions(
            () => map.Inputs.ShouldBe(edited),
            () => changed.Inputs.ShouldBe(edited),
            () => map.Run.ShouldBeSameAs(bar.Run),
            () => changed.Run.ShouldBeSameAs(bar.Run),
            () => bar.Run.Year!.Scenario.Revenue.ShouldBe(edited.Company!.Revenue.Value),
            () => map.DrawnAmounts.ShouldNotBe(amountsBefore),
            () => plans.Unsaved.ShouldBeTrue(),
            () => page.Errors.ShouldBeEmpty());
    }

    // The node « To share » chosen on the map opens its read-only card beside it, which holds no field and no
    // curve: the pay is typed on the Director pay card and the curve is Optimise's.
    [Fact]
    public async Task Plan_theSplitChosenOnTheMap_isItsReadOnlyCard()
    {
        await using var page = await MapAsync(WithACar);

        await ChooseAsync(page, RiverModule.Split);

        var shared = await page.FindAsync<SharedCard>();
        shared.Inputs.ShouldBe((await page.FindAsync<MapTab>()).Inputs);
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

    // A bucket's card holds no field: with a bucket chosen the refusal is said above the map, once.
    [Fact]
    public async Task Plan_aRefusedAmount_isSaidOnceWithABucketChosen()
    {
        await using var page = await MapAsync(RiverInputs.Example.WithReserve(-1m));
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(MapChoice.InBucket("pocket")));

        var said = (await page.FindAllAsync<RefusedMessage>()).Count(message => message.Error is not null);

        (said, page.Errors.Count).ShouldBe((1, 0));
    }

    // The module holding the refusal clicked on the map: its card says the refusal, and nothing fails while the
    // page hands the refusal from the map's note to the card. A rent beside an office allowance is refused.
    [Fact]
    public async Task Plan_theModuleHoldingARefusalChosenOnTheMap_saysItOnItsCardAndNothingFails()
    {
        var refused = RiverInputs.Example.AddToPlan(OptionalModule.Allowances).WithOfficeAllowance(1_500m)
            .AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);
        await using var page = await MapAsync(refused);

        await page.PressAsync("data-module", "rent");

        var said = (await page.FindAllAsync<RefusedMessage>()).Where(message => message.Error is not null).Select(message => message.Error!.Key);
        page.ShouldSatisfyAllConditions(
            () => page.Errors.Select(error => error.Message).ShouldBeEmpty(),
            () => said.ShouldBe(["rent_with_office_allowance"]));
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
            () => panel.Chosen.ShouldBeNull(),
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
        (map.Inputs.InPlan(OptionalModule.Car), map.Chosen, (await page.FindAllAsync<ModuleCard>()).Count, page.Errors.Count).ShouldBe((true, (MapChoice?)null, 0, 0));
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

    // A bucket the viewer chooses on the map takes the focus to its card's heading, once, as a module does: going
    // from a module to a bucket and from one bucket to another, each moves it; letting it go moves it nowhere.
    [Fact]
    public async Task Plan_eachBucketChosen_takesTheFocusToItsCardOnce()
    {
        await using var page = await MapAsync(WithACar);
        var map = await page.FindAsync<MapTab>();
        var opened = page.Focuses;

        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(MapChoice.InBucket("pocket")));
        var first = page.Focuses;
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(MapChoice.InBucket("expenses")));
        var second = page.Focuses;
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(null));
        var letGo = page.Focuses;

        var cards = (await page.FindAllAsync<BucketCard>()).Count;
        (first - opened, second - first, letGo - second, cards, page.Errors.Count).ShouldBe((1, 1, 0, 0, 0));
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
        await page.SwitchTabAsync("optimisation");
        var before = page.Focuses;

        await page.SwitchTabAsync(null);

        var (card, panel) = ((await page.FindAllAsync<ModuleCard>()).Count, await page.FindAsync<MapPanel>());
        (card, panel.Chosen, page.Focuses - before, page.Errors.Count).ShouldBe((1, MapChoice.Of(RiverModule.Car), 1, 0));
    }

    // Nothing the card does touches the folds the viewer made: nothing is stored by choosing, editing or
    // taking a module out.
    [Fact]
    public async Task Plan_aCardBesideTheMap_storesNoFold()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage, river: WithACar);
        await page.LoadAsync();
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
            () => map.Chosen.ShouldBe(MapChoice.Of(RiverModule.CompanyYear)),
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
            () => map.Chosen.ShouldBe(MapChoice.Of(RiverModule.CompanyYear)),
            () => panel.Chosen.ShouldBe(MapChoice.Of(RiverModule.CompanyYear)),
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
            () => map.Chosen.ShouldBe(MapChoice.Of(RiverModule.CompanyYear)),
            () => map.ShownMap.Modules.Select(node => node.End.Module).ShouldContain(RiverModule.Split),
            () => page.Focuses.ShouldBe(before + 2),
            () => page.Errors.ShouldBeEmpty());
    }

    private static async Task<LivePlan> MapAsync(RiverInputs river)
    {
        var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: river);
        await page.LoadAsync();

        return page;
    }

    // The viewer chooses that module on the map, or lets it go.
    private static async Task ChooseAsync(LivePlan page, RiverModule? module)
    {
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(Tabs.MapRendering.Chosen(module)));
    }

    private static int Focuses(LivePlan page) => page.Calls.Count(call => call.Contains("focus", StringComparison.OrdinalIgnoreCase));

}

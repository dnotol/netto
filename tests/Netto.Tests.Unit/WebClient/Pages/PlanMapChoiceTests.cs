using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The plan is the map, with the card of the module chosen beside it. The choice is the page's: Company year,
// the river's start, on arriving, the module a link names, a lever just added to the plan, or the one the
// viewer chose, handed back to the map; let go once the river no longer draws it, a lever taken out.
public class PlanMapChoiceTests
{
    internal static readonly RiverInputs WithACar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    [Fact]
    public async Task Plan_arriving_choosesCompanyYear()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());

        await page.LoadAsync();

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((RiverModule.CompanyYear, 0));
    }

    // A link from another tab names a card, the Optimisation tab's moves by the card they act on.
    [Fact]
    public async Task Plan_arrivingWithACard_choosesItsModule()
    {
        await using var page = LivePlan.At("http://localhost/plan?card=split", new BrowserStorage());

        await page.LoadAsync();

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((RiverModule.Split, 0));
    }

    // From the Composition menu or a move applied: the viewer asked for the lever, so its card opens beside
    // the map to be filled.
    [Fact]
    public async Task Plan_aLeverAddedToThePlan_isChosen()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(RiverInputs.Example.AddToPlan(OptionalModule.Car)));

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((RiverModule.Car, 0));
    }

    [Fact]
    public async Task Plan_aModuleChosenOnTheMap_isTheMapsChoice()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: WithACar);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();

        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Car));

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((RiverModule.Car, 0));
    }

    [Fact]
    public async Task Plan_aLeverChosenOnTheMapTakenOutOfThePlan_isChosenNoMore()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: WithACar);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Car));

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithACar.TakeOutOfPlan(OptionalModule.Car)));

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe(((RiverModule?)null, 0));
    }

    // A module the river still draws stays chosen whatever else changes.
    [Fact]
    public async Task Plan_aModuleChosenOnTheMapStillDrawn_staysChosen()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: WithACar);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.DirectorPay));

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithACar.TakeOutOfPlan(OptionalModule.Car)));

        (await page.FindAsync<MapTab>()).Chosen.ShouldBe(RiverModule.DirectorPay);
    }
}

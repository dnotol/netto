using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The module chosen on the Map tab is the page's: handed back to the map once chosen, and let go once the
// river no longer draws it, a lever taken out of the plan.
public class PlanMapChoiceTests
{
    internal static readonly RiverInputs WithACar = RiverInputs.Example.AddToPlan(OptionalModule.Car)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    [Fact]
    public async Task Plan_aModuleChosenOnTheMap_isTheMapsChoice()
    {
        await using var page = LivePlan.At("http://localhost/plan/map", new BrowserStorage(), river: WithACar);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();

        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Car));

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((RiverModule.Car, 0));
    }

    [Fact]
    public async Task Plan_aLeverChosenOnTheMapTakenOutOfThePlan_isChosenNoMore()
    {
        await using var page = LivePlan.At("http://localhost/plan/map", new BrowserStorage(), river: WithACar);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.Car));

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithACar.TakeOutOfPlan(OptionalModule.Car)));

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe(((RiverModule?)null, 0));
    }

    // A module the river still draws stays chosen whatever else changes.
    [Fact]
    public async Task Plan_aModuleChosenOnTheMapStillDrawn_staysChosen()
    {
        await using var page = LivePlan.At("http://localhost/plan/map", new BrowserStorage(), river: WithACar);
        await page.LoadAsync("map");
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(RiverModule.DirectorPay));

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithACar.TakeOutOfPlan(OptionalModule.Car)));

        (await page.FindAsync<MapTab>()).Chosen.ShouldBe(RiverModule.DirectorPay);
    }
}

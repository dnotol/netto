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

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((MapChoice.Of(RiverModule.CompanyYear), 0));
    }

    // A link from another tab names a card, the Optimisation tab's moves by the card they act on.
    [Fact]
    public async Task Plan_arrivingWithACard_choosesItsModule()
    {
        await using var page = LivePlan.At("http://localhost/plan?card=split", new BrowserStorage());

        await page.LoadAsync();

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((MapChoice.Of(RiverModule.Split), 0));
    }

    // From « Add to plan » or a move applied: the viewer asked for the lever, so its card opens beside
    // the map to be filled.
    [Fact]
    public async Task Plan_aLeverAddedToThePlan_isChosen()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(RiverInputs.Example.AddToPlan(OptionalModule.Car)));

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((MapChoice.Of(RiverModule.Car), 0));
    }

    [Fact]
    public async Task Plan_aModuleChosenOnTheMap_isTheMapsChoice()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: WithACar);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();

        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(MapChoice.Of(RiverModule.Car)));

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((MapChoice.Of(RiverModule.Car), 0));
    }

    [Fact]
    public async Task Plan_aLeverChosenOnTheMapTakenOutOfThePlan_isChosenNoMore()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: WithACar);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(MapChoice.Of(RiverModule.Car)));

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithACar.TakeOutOfPlan(OptionalModule.Car)));

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe(((MapChoice?)null, 0));
    }

    // A module the river still draws stays chosen whatever else changes.
    [Fact]
    public async Task Plan_aModuleChosenOnTheMapStillDrawn_staysChosen()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: WithACar);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(MapChoice.Of(RiverModule.DirectorPay)));

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithACar.TakeOutOfPlan(OptionalModule.Car)));

        (await page.FindAsync<MapTab>()).Chosen.ShouldBe(MapChoice.Of(RiverModule.DirectorPay));
    }

    // A link names a bucket by its key, as it names a module by its fold's name.
    [Fact]
    public async Task Plan_arrivingWithABucket_choosesIt()
    {
        await using var page = LivePlan.At("http://localhost/plan?card=pocket", new BrowserStorage());

        await page.LoadAsync();

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((MapChoice.InBucket("pocket"), 0));
    }

    // The viewer presses a bucket on the map, as the browser sends the click to it: it is chosen, its card beside
    // the map; pressed again, it is let go.
    [Fact]
    public async Task Plan_aBucketPressedOnTheMap_isChosenAndPressedAgainLetGo()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: WithACar);
        await page.LoadAsync();

        await page.PressAsync("data-bucket", "expenses");
        var (chosen, cards) = ((await page.FindAsync<MapTab>()).Chosen, string.Join(' ', (await page.FindAllAsync<BucketCard>()).Select(card => card.Bucket)));
        await page.PressAsync("data-bucket", "expenses");

        var letGo = (await page.FindAsync<MapTab>()).Chosen;
        page.ShouldSatisfyAllConditions(
            () => (chosen, cards).ShouldBe((MapChoice.InBucket("expenses"), "expenses")),
            () => letGo.ShouldBeNull(),
            () => page.Errors.ShouldBeEmpty());
    }

    // The five buckets are on the map whatever the plan holds: a bucket chosen stays chosen through an edit, a
    // lever taken out and the company skipped.
    [Fact]
    public async Task Plan_aBucketChosen_staysChosenWhateverTheRiverBecomes()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage(), river: WithACar);
        await page.LoadAsync();
        var map = await page.FindAsync<MapTab>();
        await page.InvokeAsync(() => map.ChosenChanged.InvokeAsync(MapChoice.InBucket("kept_in_company")));

        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithACar.WithRevenue(WithACar.Company!.Revenue.Value + 1_000m)));
        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithACar.TakeOutOfPlan(OptionalModule.Car)));
        await page.InvokeAsync(() => map.InputsChanged.InvokeAsync(WithACar.TakeOutOfPlan(OptionalModule.Car).SkipCompany()));

        ((await page.FindAsync<MapTab>()).Chosen, page.Errors.Count).ShouldBe((MapChoice.InBucket("kept_in_company"), 0));
    }
}

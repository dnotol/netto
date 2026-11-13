using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// A bucket pressed in the band, on the plan page as the browser runs it (LivePlan): it chooses that bucket on the
// Plan tab, its card beside the map, as the map's bucket does; from another tab it brings the viewer to the plan,
// where the tab's title takes the focus, not the card. It is an action, not a toggle: pressed again it stays
// chosen. The focus is read from the calls the page makes of the script, the address from the navigations it
// asks for; where the focus lands is the browser check's.
public class PlanBucketChoiceTests
{
    [Fact]
    public async Task Plan_aBucketPressedInTheBand_isChosenOnTheMapAndTakesTheFocusToItsCard()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();
        var before = page.Focuses;

        await page.PressAsync("data-bucket-choose", "taxes_on_pay");

        var (map, cards) = (await page.FindAsync<MapTab>(), await page.FindAllAsync<BucketCard>());
        page.ShouldSatisfyAllConditions(
            () => (map.Chosen, map.ViewerChose).ShouldBe((MapChoice.InBucket("taxes_on_pay"), true)),
            () => cards.Select(card => card.Bucket).ShouldBe(["taxes_on_pay"]),
            () => page.Focuses.ShouldBe(before + 1),
            () => page.Navigations.ShouldBeEmpty(),
            () => page.Errors.ShouldBeEmpty());
    }

    // An action, not a toggle: the bucket stays chosen, its card where it is, the focus where the viewer left it.
    [Fact]
    public async Task Plan_theChosenBucketPressedAgainInTheBand_staysChosen()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();
        await page.PressAsync("data-bucket-choose", "pocket");
        var before = page.Focuses;

        await page.PressAsync("data-bucket-choose", "pocket");

        ((await page.FindAsync<MapTab>()).Chosen, page.Focuses - before, page.Errors.Count).ShouldBe((MapChoice.InBucket("pocket"), 0, 0));
    }

    // From another tab the bucket is chosen and the page goes to the plan; arriving there, the router sets the
    // tab, the tab's title takes the focus as on any arrival, and the card beside the map does not.
    [Fact]
    public async Task Plan_aBucketPressedInTheBandOnAnotherTab_goesToThePlanWithItChosenAndTheTitleFocused()
    {
        await using var page = LivePlan.At("http://localhost/plan/optimisation", new BrowserStorage());
        await page.LoadAsync("optimisation");

        await page.PressAsync("data-bucket-choose", "expenses");
        var navigations = page.Navigations.ToList();
        var before = page.Focuses;
        await page.SwitchTabAsync(null);

        var (map, cards) = (await page.FindAsync<MapTab>(), await page.FindAllAsync<BucketCard>());
        page.ShouldSatisfyAllConditions(
            () => navigations.ShouldBe([PlanTabRoutes.Href(PlanTab.Plan)]),
            () => map.Chosen.ShouldBe(MapChoice.InBucket("expenses")),
            () => cards.Select(card => card.Bucket).ShouldBe(["expenses"]),
            () => page.Focuses.ShouldBe(before + 1),
            () => page.Errors.ShouldBeEmpty());
    }
}

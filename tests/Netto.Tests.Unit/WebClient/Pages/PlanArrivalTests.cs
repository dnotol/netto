using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The plan page loaded at an address that names a card (plan?card=<name>), a reload or a new browser tab,
// drawn as the browser runs it (LivePlan): its work after each render included, with a script runtime that
// records what the page asks of it. What the script then does with the card, after the tab's head has taken
// the focus, is the browser check's.
public class PlanArrivalTests
{
    // Opened directly, a reload or a new tab: the card opens, remembered, and the script brings it in.
    [Fact]
    public async Task Plan_loadedNamingACard_opensItAndAsksTheScriptToBringItIn()
    {
        var storage = new BrowserStorage();

        var calls = await LoadAsync("http://localhost/plan?card=split", storage);

        (calls.Count(call => call == "nettoOutline.arrive split"), new Folds(storage).IsOpen("split", false)).ShouldBe((1, true));
    }

    // Vouchers are out of the example's plan: no card to bring in, and no fold written for it.
    [Fact]
    public async Task Plan_loadedNamingACardOutOfThePlan_asksNothing()
    {
        var storage = new BrowserStorage();

        var calls = await LoadAsync("http://localhost/plan?card=vouchers", storage);

        (calls.Any(call => call.StartsWith("nettoOutline.arrive", StringComparison.Ordinal)), storage.Get("netto.folds")).ShouldBe((false, null));
    }

    [Fact]
    public async Task Plan_loadedWithoutACard_asksNothing()
    {
        var calls = await LoadAsync("http://localhost/plan", new BrowserStorage());

        calls.ShouldNotContain(call => call.StartsWith("nettoOutline.arrive", StringComparison.Ordinal));
    }

    // The page at that address, as on a first load, with what it asked the script.
    private static async Task<List<string>> LoadAsync(string uri, BrowserStorage storage)
    {
        await using var page = LivePlan.At(uri, storage);

        await page.LoadAsync();

        page.Errors.ShouldBeEmpty();
        return [.. page.Calls];
    }
}

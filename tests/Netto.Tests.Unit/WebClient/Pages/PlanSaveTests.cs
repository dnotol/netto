using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// « Save this plan » in the plan's band, on the page as the browser runs it (LivePlan): the plan it keeps is the
// river on screen, an amount typed a moment before included. A viewer types in a field and clicks Save at once:
// the browser sends the typing, the field's blur and the click one right after the other, each as the page lets
// go of its thread, while the field still commits what was typed (TypedFields).
public class PlanSaveTests
{
    [Fact]
    public async Task Save_clickedRightAfterTyping_keepsTheAmountTyped()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage);
        await page.LoadAsync();

        await page.SendTogetherAsync(
            ("id", "river-revenue", "oninput", new ChangeEventArgs { Value = "140,000" }),
            ("id", "plans-save", "onclick", new MouseEventArgs()));

        Revenues(storage).ShouldBe([140_000m]);
        page.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Save_clickedAfterTypingAndLeavingTheField_keepsTheAmountTyped()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage);
        await page.LoadAsync();

        await page.SendTogetherAsync(
            ("id", "river-revenue", "oninput", new ChangeEventArgs { Value = "140,000" }),
            ("id", "river-revenue", "onblur", new FocusEventArgs()),
            ("id", "plans-save", "onclick", new MouseEventArgs()));

        Revenues(storage).ShouldBe([140_000m]);
        page.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Save_clickedOnceTheTypedAmountIsTaken_keepsIt()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage);
        await page.LoadAsync();
        await page.SendTogetherAsync(
            ("id", "river-revenue", "oninput", new ChangeEventArgs { Value = "140,000" }),
            ("id", "river-revenue", "onblur", new FocusEventArgs()));

        await page.SendTogetherAsync(("id", "plans-save", "onclick", new MouseEventArgs()));

        Revenues(storage).ShouldBe([140_000m]);
    }

    // A field the viewer has not typed in hands over nothing: no amount is rounded to the text that shows it.
    [Fact]
    public async Task Save_nothingTyped_keepsTheRiverAsItWas()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage);
        await page.LoadAsync();

        await page.SendTogetherAsync(("id", "plans-save", "onclick", new MouseEventArgs()));

        Saved(storage).ShouldBe([RiverInputs.Example]);
    }

    // The engine refuses a negative revenue: the river cannot be kept, as the button disabled says for an amount
    // already taken, and it is not kept either when the click comes before the amount is.
    [Fact]
    public async Task Save_clickedRightAfterTypingAnAmountTheEngineRefuses_keepsNothing()
    {
        var storage = new BrowserStorage();
        await using var page = LivePlan.At("http://localhost/plan", storage);
        await page.LoadAsync();

        await page.SendTogetherAsync(
            ("id", "river-revenue", "oninput", new ChangeEventArgs { Value = "-5" }),
            ("id", "plans-save", "onclick", new MouseEventArgs()));

        Saved(storage).ShouldBeEmpty();
        page.Errors.ShouldBeEmpty();
    }

    // The revenue of each plan the browser's storage holds.
    private static IReadOnlyList<decimal?> Revenues(BrowserStorage storage) =>
        [.. Saved(storage).Select(plan => plan.Company?.Revenue.Value)];

    // The inputs of the plans the browser's storage holds.
    private static IReadOnlyList<RiverInputs> Saved(BrowserStorage storage) =>
        [.. new SavedPlans(storage).Plans.Select(plan => plan.Inputs ?? throw new InvalidOperationException("A plan saved just now is readable."))];
}

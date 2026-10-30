using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The language switch in the plan's band, on the page as the browser runs it (LivePlan): the river it carries
// across the reload is the one on screen, an amount typed a moment before included. A viewer types in a field and
// clicks the switch at once: the browser sends the typing, the field's blur and the click one right after the
// other, each as the page lets go of its thread, while the field still commits what was typed.
public class PlanLanguageSwitchTests
{
    [Fact]
    public async Task Switch_clickedRightAfterTyping_carriesTheAmountTyped()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();

        await page.SendTogetherAsync(
            ("id", "river-revenue", "oninput", new ChangeEventArgs { Value = "140,000" }),
            ("id", "river-revenue", "onblur", new FocusEventArgs()),
            ("lang", "fr", "onclick", new MouseEventArgs()));

        (page.Reloads.Reloads, Carried(page)?.Company?.Revenue.Value, page.Errors.Count).ShouldBe((1, 140_000m, 0));
    }

    // A click that takes no focus, as a button does in Safari or on a touch screen: the field is never left, and
    // its debounced value is still to come when the switch carries the river.
    [Fact]
    public async Task Switch_clickedWithoutLeavingTheField_carriesTheAmountTyped()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();

        await page.SendTogetherAsync(
            ("id", "river-revenue", "oninput", new ChangeEventArgs { Value = "140,000" }),
            ("lang", "fr", "onclick", new MouseEventArgs()));

        (page.Reloads.Reloads, Carried(page)?.Company?.Revenue.Value, page.Errors.Count).ShouldBe((1, 140_000m, 0));
    }

    [Fact]
    public async Task Switch_clickedOnceTheTypedAmountIsTaken_carriesIt()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();
        await page.SendTogetherAsync(
            ("id", "river-revenue", "oninput", new ChangeEventArgs { Value = "140,000" }),
            ("id", "river-revenue", "onblur", new FocusEventArgs()));

        await page.SendTogetherAsync(("lang", "fr", "onclick", new MouseEventArgs()));

        (page.Reloads.Reloads, Carried(page)?.Company?.Revenue.Value).ShouldBe((1, 140_000m));
    }

    // A field the viewer has not typed in hands over nothing: no amount is rounded to the text that shows it.
    [Fact]
    public async Task Switch_nothingTyped_carriesTheRiverAsItWas()
    {
        await using var page = LivePlan.At("http://localhost/plan", new BrowserStorage());
        await page.LoadAsync();

        await page.SendTogetherAsync(("lang", "fr", "onclick", new MouseEventArgs()));

        (page.Reloads.Reloads, Carried(page)).ShouldBe((1, RiverInputs.Example));
    }

    // The river carried across the reload, as the page that loads next reads it; none when nothing was carried.
    private static RiverInputs? Carried(LivePlan page) =>
        page.Session.Peek(RiverState.CarriedKey) is { } carried ? PlanStorage.Read(carried).Book.Plans.Single().Inputs : null;
}

using MudBlazor;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// Comparing two saved plans is a mode of the Plans panel in the band, not a view of the plan: « Compare plans »
// takes the place of the list from two plans on, « Back to plans » returns to it, and the old address of Compare
// plans (/plan?plans=compare) opens the panel on the comparison once. Run as the browser runs the plan page
// (LivePlan); the focus is read from the script calls, where it lands and the panel at its wide width are the
// browser check's.
public class PlansPanelCompareTests
{
    [Fact]
    public async Task Panel_twoPlansSaved_offersToCompareThem()
    {
        await using var page = await OpenPanelAsync(plans: 2);

        (await page.FindAllAsync<MudButton>()).Count(Styled("plans__compare-button")).ShouldBe(1);
    }

    // Two saved plans of which one cannot be read by this version are one to compare: the button counts the
    // readable ones, as the comparison does.
    [Fact]
    public async Task Panel_twoPlansOneUnreadable_offersNothingToCompare()
    {
        var storage = Storage(plans: 2);
        var book = System.Text.Json.Nodes.JsonNode.Parse(new SavedPlans(storage).Export())!;
        book["plans"]![1]!["inputs"]!["carLease"] = 850;
        storage.Set(PlanStorage.Key, book.ToJsonString());
        await using var page = LivePlan.At("http://localhost/plan", storage);
        await page.LoadAsync();
        await page.ClickAsync(Styled("plans__button"));

        (await page.FindAllAsync<MudButton>()).Count(Styled("plans__compare-button")).ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Panel_lessThanTwoPlans_offersNothingToCompare(int plans)
    {
        await using var page = await OpenPanelAsync(plans);

        (await page.FindAllAsync<MudButton>()).Count(Styled("plans__compare-button")).ShouldBe(0);
    }

    // The comparison takes the list's place in the panel, which widens for it; nothing else of the page moves.
    [Fact]
    public async Task Panel_compareChosen_showsTheComparisonInPlaceOfTheListAndFocusesItsTitle()
    {
        await using var page = await OpenPanelAsync(plans: 2);
        var before = page.Focuses;

        await page.ClickAsync(Styled("plans__compare-button"));

        (await page.FindAllAsync<CompareTab>()).Count.ShouldBe(1);
        (await page.FindAllAsync<MudButton>()).Count(Styled("plans__compare-button")).ShouldBe(0);
        (await page.FindAllAsync<MudButton>()).Count(Styled("plans__back-button")).ShouldBe(1);
        (page.Focuses - before, page.Errors.Count).ShouldBe((1, 0));
    }

    [Fact]
    public async Task Panel_backToPlans_showsTheListAgainAndFocusesTheButtonThatOpenedTheComparison()
    {
        await using var page = await OpenPanelAsync(plans: 2);
        await page.ClickAsync(Styled("plans__compare-button"));
        var before = page.Focuses;

        await page.ClickAsync(Styled("plans__back-button"));

        (await page.FindAllAsync<CompareTab>()).Count.ShouldBe(0);
        (await page.FindAllAsync<MudButton>()).Count(Styled("plans__compare-button")).ShouldBe(1);
        (page.Focuses - before, page.Errors.Count).ShouldBe((1, 0));
    }

    // Done closes the panel whichever it shows, and it opens on its list again.
    [Fact]
    public async Task Panel_doneOnTheComparison_closesAndReopensOnTheList()
    {
        await using var page = await OpenPanelAsync(plans: 2);
        await page.ClickAsync(Styled("plans__compare-button"));

        await page.ClickAsync(Styled("plans__done"));
        await page.ClickAsync(Styled("plans__button"));

        (await page.FindAllAsync<CompareTab>()).Count.ShouldBe(0);
        (await page.FindAllAsync<MudButton>()).Count(Styled("plans__compare-button")).ShouldBe(1);
    }

    // The old address of Compare plans: the panel opens on the comparison as the page arrives.
    [Fact]
    public async Task Panel_comparePlansAddress_opensOnTheComparison()
    {
        await using var page = LivePlan.At("http://localhost/plan?plans=compare", Storage(plans: 2));

        await page.LoadAsync();

        ((await page.FindAllAsync<CompareTab>()).Count, page.Errors.Count).ShouldBe((1, 0));
    }

    // A panel that opens takes the focus: the arrival ends on the comparison's title alone, the page's head
    // leaves it (TabHead.Focus): the head awaits the scroll before it focuses, so it would take the focus back
    // from the panel. Exactly one focus is asked for, and the head holds none.
    [Fact]
    public async Task Panel_comparePlansAddress_endsWithTheFocusOnTheComparisonTitleAlone()
    {
        await using var page = LivePlan.At("http://localhost/plan?plans=compare", Storage(plans: 2));

        await page.LoadAsync();

        (page.Focuses, (await page.FindAllAsync<TabHead>()).Single().Focus).ShouldBe((1, false));
    }

    // Once: with the address still asking, going back to the list stays on the list through what the page
    // draws next.
    [Fact]
    public async Task Panel_comparePlansAddress_opensOnlyOnArrival()
    {
        await using var page = LivePlan.At("http://localhost/plan?plans=compare", Storage(plans: 2));
        await page.LoadAsync();
        await page.ClickAsync(Styled("plans__back-button"));

        await page.SwitchTabAsync(null);

        (await page.FindAllAsync<CompareTab>()).Count.ShouldBe(0);
    }

    // An address that asks for the comparison without two plans to compare opens it all the same: it says there is
    // nothing to compare yet, with the way back.
    [Fact]
    public async Task Panel_comparePlansAddressWithoutPlans_saysThereIsNothingToCompare()
    {
        await using var page = LivePlan.At("http://localhost/plan?plans=compare", Storage(plans: 0));

        await page.LoadAsync();

        ((await page.FindAllAsync<CompareTab>()).Count, (await page.FindAllAsync<MudButton>()).Count(Styled("plans__back-button"))).ShouldBe((1, 1));
    }

    private static async Task<LivePlan> OpenPanelAsync(int plans)
    {
        var page = LivePlan.At("http://localhost/plan", Storage(plans));
        await page.LoadAsync();
        await page.ClickAsync(Styled("plans__button"));
        return page;
    }

    private static BrowserStorage Storage(int plans)
    {
        var storage = new BrowserStorage();
        var saved = new SavedPlans(storage);
        for (var plan = 0; plan < plans; plan++)
            saved.Save(RiverInputs.Example.WithDirectorPay(20_000m + plan * 1_000m), number => $"Plan {number}");

        return storage;
    }

    private static Func<MudButton, bool> Styled(string @class) => button => button.Class == @class;
}

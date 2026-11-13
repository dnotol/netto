using Microsoft.AspNetCore.Components.Web;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// The Optimise view's three sub-tabs on the plan page as the browser runs it (LivePlan): each address draws its
// panel alone, the page staying the same one; a sub-tab chosen in the row leaves the focus on its link, any other
// arrival brings the panel's start into focus, once; « Apply all » works from any sub-tab; « Why » opens and
// folds a move's reasons. The focus is read from the calls the page makes of the script; where it lands, and the
// keyboard's order, are the browser check's.
public class PlanOptimiseSectionsTests
{
    private const string Moves = "http://localhost/plan/optimisation";
    private const string Pay = "http://localhost/plan/optimisation?show=pay";
    private const string When = "http://localhost/plan/optimisation?show=when";

    private static readonly DateOnly On23October = new(2026, 10, 23);

    [Fact]
    public async Task Plan_payAddress_drawsTheCurveAlone()
    {
        await using var page = LivePlan.At(Pay, new BrowserStorage());

        await page.LoadAsync("optimisation");

        ((await page.FindAllAsync<PayCurve>()).Count, (await page.FindAllAsync<WhenToPaySection>()).Count, (await page.FindAsync<OptimisationTab>()).Section, page.Errors.Count)
            .ShouldBe((1, 0, OptimiseSection.Pay, 0));
    }

    // A value Netto has no sub-tab for shows the moves.
    [Fact]
    public async Task Plan_unknownSubTab_drawsTheMoves()
    {
        await using var page = LivePlan.At("http://localhost/plan/optimisation?show=bogus", new BrowserStorage());

        await page.LoadAsync("optimisation");

        ((await page.FindAllAsync<PayCurve>()).Count, (await page.FindAllAsync<WhenToPaySection>()).Count, (await page.FindAsync<OptimisationTab>()).Section, page.Errors.Count)
            .ShouldBe((0, 0, OptimiseSection.Moves, 0));
    }

    // Back and Forward walk the sub-tabs: each address draws its own panel, on the same page, which keeps what
    // it holds.
    [Fact]
    public async Task Plan_subTabAddressesOneAfterTheOther_drawEachPanelOnTheSamePage()
    {
        await using var page = LivePlan.At(Moves, new BrowserStorage());
        await page.LoadAsync("optimisation");
        var plan = await page.PageAsync();

        await page.GoToAsync(Pay, "optimisation");
        var onPay = ((await page.FindAllAsync<PayCurve>()).Count, (await page.FindAllAsync<WhenToPaySection>()).Count);
        await page.GoToAsync(When, "optimisation");
        var onWhen = ((await page.FindAllAsync<PayCurve>()).Count, (await page.FindAllAsync<WhenToPaySection>()).Count);
        await page.GoToAsync(Moves, "optimisation");
        var onMoves = ((await page.FindAllAsync<PayCurve>()).Count, (await page.FindAllAsync<WhenToPaySection>()).Count);

        (onPay, onWhen, onMoves, await page.PageAsync() == plan, page.Errors.Count).ShouldBe(((1, 0), (0, 1), (0, 0), true, 0));
    }

    // Opened at the curve, its heading takes the focus, once, and the view's title none.
    [Fact]
    public async Task Plan_openedAtThePay_focusesItOnceAndNeverAgainWhenTheMovesAreFound()
    {
        await using var page = LivePlan.At(Pay, new BrowserStorage(), holdPaint: true);

        await page.LoadAsync("optimisation");
        var onLoad = page.Focuses;
        await page.PaintAsync();

        (onLoad, page.Focuses - onLoad, page.Errors.Count).ShouldBe((1, 0, 0));
    }

    // Back from the curve to the moves, or on to the calendar: the panel's start takes the focus, once each.
    [Theory]
    [InlineData(Moves)]
    [InlineData(When)]
    public async Task Plan_anotherSubTabByItsAddress_movesTheFocusOnce(string address)
    {
        await using var page = LivePlan.At(Pay, new BrowserStorage());
        await page.LoadAsync("optimisation");
        var before = page.Focuses;

        await page.GoToAsync(address, "optimisation");

        (page.Focuses - before, page.Errors.Count).ShouldBe((1, 0));
    }

    // The viewer chooses a sub-tab in the row: Blazor runs the link's click before it follows its address, and
    // the focus stays on the link.
    [Fact]
    public async Task Plan_subTabChosenInTheRow_leavesTheFocusOnTheLink()
    {
        await using var page = LivePlan.At(Moves, new BrowserStorage());
        await page.LoadAsync("optimisation");
        var before = page.Focuses;

        await page.PressAsync("href", OptimiseSections.Href(OptimiseSection.Pay));
        await page.GoToAsync(Pay, "optimisation");

        ((await page.FindAllAsync<PayCurve>()).Count, page.Focuses - before, page.Errors.Count).ShouldBe((1, 0, 0));
    }

    // Having chosen in the row, the next arrival by address, Back, brings the focus again.
    [Fact]
    public async Task Plan_backAfterAChoiceInTheRow_movesTheFocusOnce()
    {
        await using var page = LivePlan.At(Moves, new BrowserStorage());
        await page.LoadAsync("optimisation");
        await page.PressAsync("href", OptimiseSections.Href(OptimiseSection.Pay));
        await page.GoToAsync(Pay, "optimisation");
        var before = page.Focuses;

        await page.GoToAsync(Moves, "optimisation");

        (page.Focuses - before, page.Errors.Count).ShouldBe((1, 0));
    }

    // The sub-tab shown, chosen again in the row, follows no new address: nothing waits to hold the next focus.
    [Fact]
    public async Task Plan_shownSubTabChosenAgain_leavesNoChoiceForTheNextArrival()
    {
        await using var page = LivePlan.At(Pay, new BrowserStorage());
        await page.LoadAsync("optimisation");
        await page.PressAsync("href", OptimiseSections.Href(OptimiseSection.Pay));
        var before = page.Focuses;

        await page.GoToAsync(Moves, "optimisation");

        (page.Focuses - before, page.Errors.Count).ShouldBe((1, 0));
    }

    // A click that opens the sub-tab in another browser tab, a modifier held or another button, follows nothing
    // here: the next arrival, by the badge or Back, still brings the focus.
    public static TheoryData<string, MouseEventArgs> ClicksElsewhere => new()
    {
        { "ctrl", new MouseEventArgs { CtrlKey = true } },
        { "meta", new MouseEventArgs { MetaKey = true } },
        { "shift", new MouseEventArgs { ShiftKey = true } },
        { "alt", new MouseEventArgs { AltKey = true } },
        { "middle button", new MouseEventArgs { Button = 1 } },
    };

    [Theory]
    [MemberData(nameof(ClicksElsewhere))]
    public async Task Plan_subTabOpenedElsewhere_leavesTheNextArrivalItsFocus(string _, MouseEventArgs click)
    {
        await using var page = LivePlan.At(Moves, new BrowserStorage());
        await page.LoadAsync("optimisation");
        await page.SendTogetherAsync(("href", OptimiseSections.Href(OptimiseSection.Pay), "onclick", click));
        var before = page.Focuses;

        await page.GoToAsync(When, "optimisation");

        (page.Focuses - before, page.Errors.Count).ShouldBe((1, 0));
    }

    // « Apply all » from the curve: the head's state line, drawn on every sub-tab, takes the focus.
    [Fact]
    public async Task Plan_applyAllOnThePay_focusesTheStateLineOnce()
    {
        await using var page = LivePlan.At(Pay, new BrowserStorage());
        await page.LoadAsync("optimisation");
        var before = page.Focuses;

        await page.ClickAsync(button => button.Class == "moves__apply" && button.UserAttributes?.ContainsKey("aria-label") != true);

        ((await page.FindAsync<OptimisationTab>()).Inputs == RiverInputs.Example, page.Focuses - before, page.Errors.Count).ShouldBe((false, 1, 0));
    }

    // « Why » on a move opens its reasons, said by the button, and folds them again.
    [Fact]
    public async Task Plan_whyOnAMove_opensItsReasonsAndFoldsThem()
    {
        await using var page = LivePlan.At(Moves, new BrowserStorage());
        await page.LoadAsync("optimisation");

        await page.PressAsync("aria-label", WhyLabelOfFirstMove());
        await page.PressAsync("aria-expanded", "true");

        await Should.ThrowAsync<InvalidOperationException>(() => page.PressAsync("aria-expanded", "true"));
        page.Errors.Count.ShouldBe(0);
    }

    // The name of the first move's Why, as the tab words it.
    private static string WhyLabelOfFirstMove()
    {
        var texts = ResxText.For<OptimisationTab>();
        var view = MovesView.Of(RiverInputs.Example, On23October);
        var first = view.Moves[0].Move;
        var what = texts.Format(OptimisationTabText.Move(first), OptimisationTabText.MoveArguments(first, view.FromPay, view.From.Copyright.CreativeShare.Value ?? 0m));
        return texts.Format(OptimisationTabText.WhyToggleLabel, what);
    }
}

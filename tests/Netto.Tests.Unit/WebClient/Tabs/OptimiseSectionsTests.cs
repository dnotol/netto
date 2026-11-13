using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The Optimise view's three sub-tabs, each named by the address's show query (docs/architecture/decisions/
// 2026-11-13-optimise-in-three-sub-tabs.md): the moves at the view's own address, the pay curve at ?show=pay,
// When to pay at ?show=when, where the deadline badge, a payment move's link and the old addresses lead.
public class OptimiseSectionsTests
{
    [Fact]
    public void Order_isTheMovesThePayThenWhenToPay()
    {
        OptimiseSections.Order.ShouldBe([OptimiseSection.Moves, OptimiseSection.Pay, OptimiseSection.When]);
    }

    // The router matches whatever the case, and so does this; a value Netto has no sub-tab for shows the moves.
    [Theory]
    [InlineData(null, OptimiseSection.Moves)]
    [InlineData("", OptimiseSection.Moves)]
    [InlineData("pay", OptimiseSection.Pay)]
    [InlineData("PAY", OptimiseSection.Pay)]
    [InlineData("when", OptimiseSection.When)]
    [InlineData("When", OptimiseSection.When)]
    [InlineData("moves", OptimiseSection.Moves)]
    [InlineData("bogus", OptimiseSection.Moves)]
    public void From_aQueryValue_isTheSubTabItNames(string? show, OptimiseSection section)
    {
        OptimiseSections.From(show).ShouldBe(section);
    }

    [Theory]
    [InlineData(OptimiseSection.Moves, "plan/optimisation")]
    [InlineData(OptimiseSection.Pay, "plan/optimisation?show=pay")]
    [InlineData(OptimiseSection.When, "plan/optimisation?show=when")]
    public void Href_eachSubTab_isTheViewAskingForIt(OptimiseSection section, string href)
    {
        OptimiseSections.Href(section).ShouldBe(href);
    }

    // When to pay keeps the address the band's badge and the old addresses lead to.
    [Fact]
    public void Href_whenToPay_isTheSectionsKeptAddress()
    {
        OptimiseSections.Href(OptimiseSection.When).ShouldBe(PlanTabRoutes.WhenToPayHref);
    }

    // Each sub-tab's address reads back as that sub-tab.
    [Fact]
    public void From_eachHrefsQuery_isItsSubTab()
    {
        OptimiseSections.Order.Select(section => OptimiseSections.From(QueryOf(OptimiseSections.Href(section))))
            .ShouldBe(OptimiseSections.Order);
    }

    private static string? QueryOf(string href) =>
        href.Split('?') is [_, var query] ? query.Split('=')[1] : null;
}

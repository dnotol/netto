using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The tab bar's texts, held to PlanTabBar.resx: a misspelt key would show on screen as the key itself.
public class PlanTabBarTextTests
{
    // The name IStringLocalizer<PlanTabBar> reads Tabs/PlanTabBar.resx under.
    private static readonly ResxText Resources = ResxText.For<PlanTabBar>();

    [Fact]
    public void PlanTabBarResx_label_namesTheNav()
    {
        Resources.Get(PlanTabBarText.Label).ShouldBe("Your plan");
    }

    [Fact]
    public void PlanTabBarResx_everyTab_hasItsName()
    {
        PlanTabRoutes.Order.Select(tab => Resources.Get(PlanTabBarText.Name(tab))).ShouldBe(["My plan", "Optimise", "Ideas"]);
    }

    // A badge shows short (+€3,054.18, 21 Dec, an icon) and a screen reader hears it in full, after the
    // tab's name.
    [Theory]
    [InlineData(TabBadgeKind.MoreThanPlan, "€3,054.18", "€3,054.18 more than the plan on screen")]
    [InlineData(TabBadgeKind.NextBy, "21 December 2026", "When to pay: next advance payment by 21 December 2026")]
    [InlineData(TabBadgeKind.BilledRefused, "", "When to pay: what the fund bills is refused")]
    [InlineData(TabBadgeKind.Refused, "", "an amount here is refused")]
    [InlineData(TabBadgeKind.Searching, "", "looking for moves")]
    public void PlanTabBarResx_everyBadge_isSpokenInFull(TabBadgeKind kind, string figure, string expected)
    {
        Resources.Format(PlanTabBarText.Spoken(kind), figure).ShouldBe(expected);
    }

    // The ideas' badge is said by its counts alone, after the tab's name: « Ideas 1 priced, 3 questions ».
    [Fact]
    public void PlanTabBarResx_pistes_isSpokenByItsCounts()
    {
        Resources.Format(PlanTabBarText.Spoken(TabBadgeKind.Pistes), "1 priced, 3 questions").ShouldBe("1 priced, 3 questions");
    }

    // While the page looks for the moves, the badge shows an ellipsis, which a screen reader skips for the
    // words above.
    [Fact]
    public void PlanTabBarResx_searching_showsAnEllipsis()
    {
        Resources.Get(PlanTabBarText.Searching).ShouldBe("…");
    }

    [Fact]
    public void PlanTabBarResx_everyBadgeKind_hasItsSpokenText()
    {
        Enum.GetValues<TabBadgeKind>().Where(kind => Resources.Get(PlanTabBarText.Spoken(kind)) is null).ShouldBeEmpty();
    }
}

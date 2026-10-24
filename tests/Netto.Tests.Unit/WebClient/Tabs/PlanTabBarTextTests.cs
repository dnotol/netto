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
        PlanTabRoutes.Order.Select(tab => Resources.Get(PlanTabBarText.Name(tab))).ShouldBe(["Plan", "Optimisation", "When to pay", "Compare plans"]);
    }

    // A badge shows short (+€3,054.18, 21 Dec, an icon) and a screen reader hears it in full, after the
    // tab's name.
    [Theory]
    [InlineData(TabBadgeKind.MoreThanToday, "€3,054.18", "€3,054.18 more than Today")]
    [InlineData(TabBadgeKind.MoreThanPlan, "€3,054.18", "€3,054.18 more than the plan in your river")]
    [InlineData(TabBadgeKind.NextBy, "21 December 2026", "next advance payment by 21 December 2026")]
    [InlineData(TabBadgeKind.Refused, "", "an amount here is refused")]
    [InlineData(TabBadgeKind.SavedPlans, "3", "3 saved plans to compare")]
    public void PlanTabBarResx_everyBadge_isSpokenInFull(TabBadgeKind kind, string figure, string expected)
    {
        Resources.Format(PlanTabBarText.Spoken(kind), figure).ShouldBe(expected);
    }

    [Fact]
    public void PlanTabBarResx_everyBadgeKind_hasItsSpokenText()
    {
        Enum.GetValues<TabBadgeKind>().Where(kind => Resources.Get(PlanTabBarText.Spoken(kind)) is null).ShouldBeEmpty();
    }
}

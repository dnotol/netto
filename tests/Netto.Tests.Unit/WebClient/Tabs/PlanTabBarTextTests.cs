using System.Globalization;
using System.Resources;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The tab bar's texts, held to PlanTabBar.resx: a misspelt key would show on screen as the key itself.
public class PlanTabBarTextTests
{
    // The name IStringLocalizer<PlanTabBar> reads Tabs/PlanTabBar.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Tabs.PlanTabBar", typeof(PlanTabBar).Assembly);

    private static string? Text(string key) => Resources.GetString(key, CultureInfo.InvariantCulture);

    [Fact]
    public void PlanTabBarResx_label_namesTheNav()
    {
        Text(PlanTabBarText.Label).ShouldBe("Your plan");
    }

    [Fact]
    public void PlanTabBarResx_everyTab_hasItsName()
    {
        PlanTabRoutes.Order.Select(tab => Text(PlanTabBarText.Name(tab))).ShouldBe(["Plan", "Optimisation", "When to pay", "Compare plans"]);
    }

    // A badge shows short (+€3,054.18, 21 Dec, an icon) and a screen reader hears it in full, after the
    // tab's name.
    [Theory]
    [InlineData(TabBadgeKind.MoreThanToday, "€3,054.18", "€3,054.18 more than Today")]
    [InlineData(TabBadgeKind.NextBy, "21 December 2026", "next advance payment by 21 December 2026")]
    [InlineData(TabBadgeKind.Refused, "", "an amount here is refused")]
    [InlineData(TabBadgeKind.SavedPlans, "3", "3 saved plans to compare")]
    public void PlanTabBarResx_everyBadge_isSpokenInFull(TabBadgeKind kind, string figure, string expected)
    {
        string.Format(CultureInfo.InvariantCulture, Text(PlanTabBarText.Spoken(kind)) ?? "", figure).ShouldBe(expected);
    }

    [Fact]
    public void PlanTabBarResx_everyBadgeKind_hasItsSpokenText()
    {
        Enum.GetValues<TabBadgeKind>().Where(kind => Text(PlanTabBarText.Spoken(kind)) is null).ShouldBeEmpty();
    }
}

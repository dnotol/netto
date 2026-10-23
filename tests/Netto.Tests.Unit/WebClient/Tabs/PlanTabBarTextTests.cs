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
}

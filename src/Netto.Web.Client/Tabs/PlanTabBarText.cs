namespace Netto.Web.Client.Tabs;

// The resource keys PlanTabBar.razor builds, held to PlanTabBar.resx by PlanTabBarTextTests, so a tab
// added without its name fails a test instead of showing its key on screen.
internal static class PlanTabBarText
{
    // What the nav of tabs is, for a screen reader's list of landmarks.
    public const string Label = "Label";

    public static string Name(PlanTab tab) => $"Tab_{tab}";

    // A badge as a screen reader hears it after the tab's name, in full where the bar shows it short.
    public static string Spoken(TabBadgeKind kind) => $"Spoken_{kind}";

    // What the badge shows while the moves are being looked for: « … ».
    public const string Searching = "Shown_Searching";
}

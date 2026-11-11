namespace Netto.Web.Client.Tabs;

// The resource keys PlanLensBar.razor builds, held to PlanLensBar.resx by PlanLensBarTests, so a lens added
// without its name fails a test instead of showing its key on screen.
internal static class PlanLensBarText
{
    // What the nav of lenses is, for a screen reader's list of landmarks.
    public const string Label = "Label";

    public static string Name(PlanLens lens) => $"Lens_{lens}";
}

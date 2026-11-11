using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// The resource keys PistesTab.razor builds, held to PistesTab.resx by PistesTabTextTests.
internal static class PistesTabText
{
    // How a gain is measured, and that Netto asks rather than decides.
    public const string Intro = "Intro";

    // An idea just kept: « {0} is in your plan now », with the link to Optimise.
    public const string Kept = "Kept";

    public const string GoToOptimise = "GoToOptimise";

    // What follows what Netto does not model: « Not in Netto yet. ».
    public const string NotInNetto = "NotInNetto";

    // Why there is no idea: the plan starts at the pay, or every idea is in it already.
    public static string None(bool startsAtPay) => startsAtPay ? "None_StartsAtPay" : "None_AllInPlan";

    // What Netto does not model, named.
    public static string NotYet(NotInNetto kind) => $"NotYet_{kind}";
}

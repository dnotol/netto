namespace Netto.Web.Client.Pages;

// The resource keys of Plan.razor that PlanTextTests holds to Plan.resx.
internal static class PlanText
{
    // The skip link, the keyboard's first stop on the page.
    public const string SkipToRiver = "SkipToRiver";

    // Under the split: the pay, and the royalty once it is in the plan, on the director's side.
    public static string Fork(bool royalty) => royalty ? "Fork_Royalty" : "Fork";
}

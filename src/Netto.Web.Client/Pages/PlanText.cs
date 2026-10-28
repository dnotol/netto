using Netto.Web.Client.Tabs;

namespace Netto.Web.Client.Pages;

// The resource keys of Plan.razor that PlanTextTests holds to Plan.resx.
internal static class PlanText
{
    // The window's title on each tab, as a page of its own would have.
    public static string PageTitle(PlanTab tab) => tab == PlanTab.Plan ? "PageTitle" : $"PageTitle_{tab}";

    // The skip link, the keyboard's first stop on the page, worded for where it goes on each tab.
    public static string SkipTo(PlanTab tab) => tab switch
    {
        PlanTab.Plan => "SkipToMap",
        PlanTab.Optimisation => "SkipToMoves",
        PlanTab.WhenToPay => "SkipToPayments",
        PlanTab.Compare => "SkipToComparison",
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, null),
    };

    // The two notes under the river, each a fold of its own (River/Fold) that the viewer opens: where it
    // is remembered, kept from one version to the next, or a viewer's folds are forgotten.
    public const string AssumedFold = "assumed";

    public const string LimitsFold = "not-counted";

    public const string AssumedTitle = "AssumedTitle";

    // The note folded to one line that says what it holds: « 8 assumptions Netto makes », its count the
    // list's own.
    public const string AssumedSummary = "AssumedSummary";

    public const string LimitsTitle = "LimitsTitle";

    public const string LimitsSummary = "LimitsSummary";

    // The note under the list of what is not counted, not one of its lines.
    public const string LimitTaxCalcNote = "LimitTaxCalc";

    // What Netto assumes about the director and the company, a line each, in the order the page draws.
    public static IReadOnlyList<string> Assumptions { get; } =
    [
        "AssumedSingle",
        "AssumedPartnerWithholding",
        "AssumedWallonia",
        "AssumedMainOccupation",
        "AssumedNoSpecialContribution",
        "AssumedLowIncomeCredit",
        "AssumedCalendarYear",
        "AssumedSteadyState",
        "AssumedFinalContributions",
    ];

    // What the figures do not count yet, a line each.
    public static IReadOnlyList<string> Limits { get; } =
    [
        "LimitReserves",
        "LimitDisallowed",
        "LimitManagementFee",
        "LimitPartner",
    ];
}

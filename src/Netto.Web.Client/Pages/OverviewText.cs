using Netto.Core.CorporateTax;
using Netto.Core.Dividends;

namespace Netto.Web.Client.Pages;

// The resource keys Overview.razor builds from the engine's keys and enums. The page and
// OverviewTextTests both go through these, so a key without a text in Overview.resx fails a test
// instead of showing on screen.
internal static class OverviewText
{
    public static string Split(string key) => $"Split_{key}";

    public static string Ledger(string key) => $"Ledger_{key}";

    public static string LedgerHint(string key) => $"LedgerHint_{key}";

    public static string Row(string key) => $"Row_{key}";

    public static string Condition(ReducedRateCondition condition) => $"Condition_{condition}";

    public static string Category(WithholdingCategory category) => $"Category_{category}";

    public static string Error(string errorKey) => $"Error_{errorKey}";
}

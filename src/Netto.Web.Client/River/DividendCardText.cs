using Netto.Core.Dividends;

namespace Netto.Web.Client.River;

// The resource keys the Dividend card builds from values, gone through by the card and its tests alike.
internal static class DividendCardText
{
    // What VVPRbis shares are and which of their rates is for which distribution, in the bubble after
    // the rate's label.
    public const string CategoryHelp = "Help_Category";

    public static string Category(WithholdingCategory category) => $"Category_{category}";

    // The summary starts from what comes in, all of which is paid out, or, once the company keeps
    // some, from what is paid out: the base its rate is out of.
    public static string Summary(DividendView view) => view.Kept > 0m ? "Summary_PaidOut" : "Summary";
}

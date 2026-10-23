namespace Netto.Web.Client.River;

// The resource keys the rent card builds from values, gone through by the card and its tests alike; its
// figure lines' keys are RentFigures'.
internal static class RentCardText
{
    // What the cadastral income is, in the bubble after its field's label.
    public const string CadastralIncomeHelp = "Help_CadastralIncome";

    // How the cap under the rent is set, in the bubble after the rent's label.
    public const string CapHelp = "Help_Cap";

    // The summary ends on what the split shares: « before your pay and royalty » while the royalty is
    // in the plan.
    public static string Summary(bool royalty) => royalty ? "Summary_Royalty" : "Summary";
}

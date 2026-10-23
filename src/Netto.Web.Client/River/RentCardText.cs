namespace Netto.Web.Client.River;

// The resource keys the rent card builds from values, gone through by the card and its tests alike.
internal static class RentCardText
{
    // What the cadastral income is, in the bubble the term opens in the cap's sentence.
    public const string CadastralIncomeTerm = "Term_CadastralIncome";

    // What the flat charges are, in the bubble the term opens where the rent's tax is said.
    public const string FlatChargesTerm = "Term_FlatCharges";

    // The summary ends on what the split shares: « before your pay and royalty » while the royalty is
    // in the plan.
    public static string Summary(bool royalty) => royalty ? "Summary_Royalty" : "Summary";
}

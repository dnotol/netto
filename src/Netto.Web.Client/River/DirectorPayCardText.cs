using Netto.Core.SocialContributions;

namespace Netto.Web.Client.River;

// The resource keys the Director pay card builds from values, gone through by the card, the lines of
// its settings (SettingsViews) and its tests alike.
internal static class DirectorPayCardText
{
    public static string Surcharge(TaxSurchargeState state) => $"Surcharge_{state}";

    public static string PaidBy(ContributionsPaidBy paidBy) => $"PaidBy_{paidBy}";

    // What the key formula is, in the bubble the term opens where the withholding is derived from it.
    public const string KeyFormulaTerm = "Term_KeyFormula";

    // Who pays the contributions as the line of « Your situation » states it.
    public static string SituationPaidBy(ContributionsPaidBy paidBy) => $"Situation_PaidBy_{paidBy}";
}

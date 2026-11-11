using Netto.Core.SocialContributions;

namespace Netto.Web.Client.River;

// The resource keys the Director pay card builds from values, gone through by the card, the lines of
// its settings (SettingsViews) and its tests alike.
internal static class DirectorPayCardText
{
    // Why a surcharge is due or why none is, never what to pay to leave none due: the card suggests no payment.
    public static string Note(TaxSurchargeState state) => $"Surcharge_{state}";

    public static string PaidBy(ContributionsPaidBy paidBy) => $"PaidBy_{paidBy}";

    // Who pays the contributions as the line of « Your situation » states it.
    public static string SituationPaidBy(ContributionsPaidBy paidBy) => $"Situation_PaidBy_{paidBy}";
}

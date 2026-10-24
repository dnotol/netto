using Netto.Core.SocialContributions;

namespace Netto.Web.Client.River;

// The resource keys the Director pay card builds from values, gone through by the card, the lines of
// its settings (SettingsViews) and its tests alike.
internal static class DirectorPayCardText
{
    // Why no surcharge is due, or, while one is, the line of what more by the first deadline still open
    // leaves none due.
    public static string Surcharge(TaxSurchargeState state) => $"Surcharge_{state}";

    // The note the card draws where it draws no such line: why no surcharge is due, or, while one is and
    // no deadline is open any more, that no payment can still leave none due.
    public static string Note(TaxSurchargeState state, MoreBy? more) =>
        state == TaxSurchargeState.Due && more is null ? "Surcharge_TooLate" : Surcharge(state);

    public static string PaidBy(ContributionsPaidBy paidBy) => $"PaidBy_{paidBy}";

    // Who pays the contributions as the line of « Your situation » states it.
    public static string SituationPaidBy(ContributionsPaidBy paidBy) => $"Situation_PaidBy_{paidBy}";
}

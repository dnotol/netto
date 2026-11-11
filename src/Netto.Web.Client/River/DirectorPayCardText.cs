using Netto.Core.Copyright;
using Netto.Core.SocialContributions;

namespace Netto.Web.Client.River;

// The resource keys the Director pay card builds from values, gone through by the card, the lines of
// its settings (SettingsViews) and its tests alike.
internal static class DirectorPayCardText
{
    // Why a surcharge is due or why none is, never what to pay to leave none due: the card suggests no payment.
    public static string Note(TaxSurchargeState state) => $"Surcharge_{state}";

    // Why the pay goes no further, in the pay field's bubble, its bound staying in sight under it and out of
    // the bubble: the profit before pay, what a fixed royalty leaves of it, or where the pay and the royalty
    // that follows it take all of it, while the engine set that royalty at what a ruling accepts; one that is
    // none leaves the whole profit before pay.
    public static string CeilingHint(SplitRoyaltyView? royalty) => royalty switch
    {
        null => "CeilingHint",
        { FollowsPay.Value: true, Following: CopyrightFollowing.AtCeiling } => "CeilingHint_Follows",
        { FollowsPay.Value: true } => "CeilingHint",
        _ => "CeilingHint_Fixed",
    };

    // What the slider says of its value while the royalty follows the pay: the pay and the royalty beside it.
    public const string SliderValueWithRoyalty = "SliderValue_Royalty";

    public static string PaidBy(ContributionsPaidBy paidBy) => $"PaidBy_{paidBy}";

    // Who pays the contributions as the line of « Your situation » states it.
    public static string SituationPaidBy(ContributionsPaidBy paidBy) => $"Situation_PaidBy_{paidBy}";
}

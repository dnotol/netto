using Netto.Core.Copyright;

namespace Netto.Web.Client.River;

// The resource keys the split's card builds from values, gone through by the card and its tests alike.
internal static class SplitCardText
{
    // What the reduced corporate rate is, in the bubble the term opens where the rate is weighed.
    public const string ReducedRateTerm = "Term_ReducedRate";

    // What the slider says of its value while the royalty follows the pay: the pay and the royalty
    // beside it.
    public const string SliderValueWithRoyalty = "SliderValue_Royalty";

    // The royalty's two ways, one chip each.
    public static string Policy(bool followsPay) => followsPay ? "Policy_Follows" : "Policy_Fixed";

    // The summary names the royalty once it is in the plan, and the best pay once there is one.
    public static string Summary(SplitRoyaltyView? royalty, bool withBest) => (withBest ? "SummaryWithBest" : "Summary") + (royalty is null ? "" : "_Royalty");

    public static string Hint(SplitRoyaltyView? royalty) => royalty is null ? "Hint" : "Hint_Royalty";

    // Why the pay goes no further, in the pay field's bubble, its bound staying in sight under it: the
    // profit before pay, what a fixed royalty leaves of it, or where the pay and the royalty that
    // follows it take all of it, while the engine set that royalty at what a ruling accepts; one that
    // is none leaves the whole profit before pay.
    public static string CeilingHint(SplitRoyaltyView? royalty) => royalty switch
    {
        null => "CeilingHint",
        { FollowsPay.Value: true, Following: CopyrightFollowing.AtCeiling } => "CeilingHint_Follows",
        { FollowsPay.Value: true } => "CeilingHint",
        _ => "CeilingHint_Fixed",
    };

    // Why a royalty that follows the pay is none; none when the engine set it at what a ruling
    // accepts, its figure being among the split's sides.
    public static string? Following(CopyrightFollowing following) => following == CopyrightFollowing.AtCeiling ? null : $"Following_{following}";

    // A sentence about one pay of the curve names the royalty beside it while the royalty follows the
    // pay; a fixed one is the same at every pay.
    public static string AlongTheCurve(string key, SplitRoyaltyView? royalty) => royalty is { FollowsPay.Value: true } ? $"{key}_Royalty" : key;

    // The sentence weighs the best pay on the other side of the threshold only when one keeps the
    // reserve, and names no amount when that pay is the pay set: what the best pay gains over it, its
    // line says.
    public static string Rate(RateWeighing rate, decimal pay) => rate switch
    {
        { Choice: RateChoice.NotWeighed } => "Rate_NotWeighed",
        { Other: null } => $"Rate_{rate.Choice}Only",
        { Other.Pay: var other } when other == pay => $"Rate_{rate.Choice}Yours",
        _ => $"Rate_{rate.Choice}",
    };
}

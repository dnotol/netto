using Netto.Core.Copyright;

namespace Netto.Web.Client.River;

// The resource keys the split's card builds from values, gone through by the card and its tests alike.
internal static class SplitCardText
{
    // What the reduced corporate rate is, in the bubble the term opens where the rate is weighed.
    public const string ReducedRateTerm = "Term_ReducedRate";

    // What reaches the pocket at the best pay, against the pay set.
    public const string BestReach = "BestReach";

    // What reaches the pocket at the pay set, the best.
    public const string BestIsYoursReach = "BestIsYoursReach";

    // What the slider says of its value while the royalty follows the pay: the pay and the royalty
    // beside it.
    public const string SliderValueWithRoyalty = "SliderValue_Royalty";

    // The royalty's two ways, one chip each.
    public static string Policy(bool followsPay) => followsPay ? "Policy_Follows" : "Policy_Fixed";

    // The royalty's figure among the split's ends says which way it is set.
    public static string RoyaltyEnd(SplitRoyaltyView royalty) => royalty.FollowsPay.Value ? "RoyaltyEnd_Follows" : "RoyaltyEnd_Fixed";

    // The summary names the royalty once it is in the plan, and the best pay once there is one.
    public static string Summary(SplitRoyaltyView? royalty, bool withBest) => (withBest ? "SummaryWithBest" : "Summary") + (royalty is null ? "" : "_Royalty");

    public static string Hint(SplitRoyaltyView? royalty) => royalty is null ? "Hint" : "Hint_Royalty";

    // How far the pay goes: the profit before pay, what a fixed royalty leaves of it, or where the pay
    // and the royalty that follows it take all of it, while the engine set that royalty at what a
    // ruling accepts; one that is none leaves the whole profit before pay.
    public static string Ceiling(SplitRoyaltyView? royalty) => royalty switch
    {
        null => "Ceiling",
        { FollowsPay.Value: true, Following: CopyrightFollowing.AtCeiling } => "Ceiling_Follows",
        { FollowsPay.Value: true } => "Ceiling",
        _ => "Ceiling_Fixed",
    };

    // How the engine set a royalty that follows the pay, or why it is none.
    public static string Following(CopyrightFollowing following) => $"Following_{following}";

    // A sentence about one pay of the curve names the royalty beside it while the royalty follows the
    // pay; a fixed one is the same at every pay.
    public static string AlongTheCurve(string key, SplitRoyaltyView? royalty) => royalty is { FollowsPay.Value: true } ? $"{key}_Royalty" : key;

    // The sentence names the best pay on the other side of the threshold only when one keeps the
    // reserve.
    public static string Rate(RateWeighing rate) => rate switch
    {
        { Choice: RateChoice.NotWeighed } => "Rate_NotWeighed",
        { Other: null } => $"Rate_{rate.Choice}Only",
        _ => $"Rate_{rate.Choice}",
    };
}

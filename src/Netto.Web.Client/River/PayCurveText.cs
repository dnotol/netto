using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// The resource keys the pay curve builds from values, gone through by the component and its tests alike.
internal static class PayCurveText
{
    // A sentence about one pay of the curve names the royalty beside it while the royalty follows the
    // pay; a fixed one is the same at every pay.
    public static string AlongTheCurve(string key, SplitRoyaltyView? royalty) => royalty is { FollowsPay.Value: true } ? $"{key}_Royalty" : key;

    // The sentence weighs the best pay on the other side of the threshold only when one keeps
    // the reserve, and names no amount when that pay is the pay set: what the best pay gains over it, its
    // line says.
    public static string Rate(RateWeighing rate, decimal pay) => rate switch
    {
        { Choice: RateChoice.NotWeighed } => "Rate_NotWeighed",
        { Other: null } => $"Rate_{rate.Choice}Only",
        { Other.Pay: var other } when other == pay => $"Rate_{rate.Choice}Yours",
        _ => $"Rate_{rate.Choice}",
    };
}

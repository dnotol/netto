namespace Netto.Web.Client.River;

// The resource keys the read-only card of the node « To share » builds from values, gone through by the card
// and its tests alike.
internal static class SharedCardText
{
    // The link to the Copyright royalty card, which decides the royalty.
    public const string RoyaltyLink = "RoyaltyLink";

    // How the royalty beside the pay is set, as the Copyright royalty card sets it: it follows the pay, so
    // the slider moves it too, or it is a fixed amount whatever the pay.
    public static string RoyaltyNote(SplitRoyaltyView royalty) => royalty.FollowsPay.Value ? "RoyaltyNote_Follows" : "RoyaltyNote_Fixed";
}

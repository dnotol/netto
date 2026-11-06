namespace Netto.Web.Client.River;

// The resource keys BucketCard.razor reads from BucketCard.resx; BucketCardTextTests holds each to a text in
// both languages. The bucket's name and what it holds come from the bar's texts (BucketBarText), a module's
// name from the map's (MapTabText.Module).
internal static class BucketCardText
{
    // Beside the total, worded with the share of the bar's start: « 12% of the start ».
    public const string Rate = "Rate";

    // Under the heading while nothing is sent into the bucket in this plan.
    public const string Nothing = "Nothing";

    // In place of the figures while the engine refuses an amount.
    public const string Refused = "Refused";

    // The heading of the pocket line's running totals.
    public const string PocketLine = "PocketLine";
}

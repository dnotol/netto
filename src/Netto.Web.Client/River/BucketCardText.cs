namespace Netto.Web.Client.River;

// The resource keys BucketCard.razor reads from BucketCard.resx; BucketCardTextTests holds each to a text in
// both languages. The bucket's name, what it holds and the refusal come from the bar's texts (BucketBarText), a module's
// name from the map's (MapTabText.Module).
internal static class BucketCardText
{
    // Beside the total, worded with the share of the bar's start: « 12% of the start ».
    public const string Rate = "Rate";

    // Under the heading while nothing is sent into the bucket in this plan.
    public const string Nothing = "Nothing";

    // The heading of the pocket line's running totals.
    public const string PocketLine = "PocketLine";
}

namespace Netto.Web.Client.River;

// The resource keys BucketCard.razor reads from BucketCard.resx; BucketCardTextTests holds each to a text in
// both languages. The bucket's name, what it holds and the refusal come from the bar's texts (BucketBarText), a module's
// name from the map's (MapTabText.Module).
internal static class BucketCardText
{
    // Under the heading while nothing is sent into the bucket in this plan.
    public const string Nothing = "Nothing";

    // Under the heading when one module sends all of it, worded with that module's name, as a module (« the Rent module »): its amount is the
    // heading's, so no line repeats it.
    public const string OnlyFrom = "OnlyFrom";
}

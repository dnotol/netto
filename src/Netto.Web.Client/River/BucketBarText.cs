namespace Netto.Web.Client.River;

// The resource keys BucketBar.razor builds from BucketsView's keys. The component and
// BucketBarTextTests both go through these, so a bucket without a text in BucketBar.resx fails a
// test instead of showing on screen.
internal static class BucketBarText
{
    public static string Bucket(string key) => $"Bucket_{key}";
}

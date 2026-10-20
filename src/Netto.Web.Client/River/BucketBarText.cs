namespace Netto.Web.Client.River;

// The resource keys BucketBar.razor builds from BucketsView's keys. The component and
// BucketBarTextTests both go through these, so a bucket without a text in BucketBar.resx fails a
// test instead of showing on screen.
internal static class BucketBarText
{
    // The taxes on pay say they hold the rent's tax too once there is one.
    public static string Bucket(string key, bool taxesOnPayIncludeRent) =>
        taxesOnPayIncludeRent && key == "taxes_on_pay" ? "Bucket_taxes_on_pay_and_rent" : $"Bucket_{key}";
}

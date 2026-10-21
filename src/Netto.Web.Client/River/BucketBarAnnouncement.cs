namespace Netto.Web.Client.River;

// What the bar of buckets' live region says: what reaches the pocket, and that alone, once the river
// has stayed still for a moment (Settle), so that a slider dragged or an amount typed is not read out
// at every step. The other buckets stay readable in the bar itself.
internal static class BucketBarAnnouncement
{
    // How long the river stays still before the pocket is announced.
    public static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(800);

    // The pocket's amount, or nothing while the engine refuses an amount: the card that holds it
    // says why.
    public static string? Pocket(BucketsView? view) =>
        view?.Buckets.Single(bucket => bucket.Key == "pocket").Amount is { } amount ? Formats.Euros(amount) : null;
}

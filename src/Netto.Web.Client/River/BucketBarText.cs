namespace Netto.Web.Client.River;

// The resource keys BucketBar.razor builds from BucketsView's keys. The component and
// BucketBarTextTests both go through these, so a bucket without a text in BucketBar.resx fails a
// test instead of showing on screen.
internal static class BucketBarText
{
    // What reaches the pocket, as the bar's live region says it once the river settles.
    public const string PocketAnnounced = "PocketAnnounced";

    // Why there is no figure while the engine refuses an amount, in the bar and its live region.
    public const string Refused = "Refused";

    // What the reach is out of, in the bubble its share opens.
    public const string ReachMeaning = "ReachMeaning";

    // With the company skipped, after the reach: where the rest of the river is.
    public const string EnterCompany = "EnterCompany";

    // Under the bar, what reaches the pocket and what went to taxes: of what the company does not
    // keep, once it keeps some.
    public static string Reach(BucketsView view) =>
        view.Buckets.Single(bucket => bucket.Key == "kept_in_company").Amount > 0m ? "Reach_Kept" : "Reach";

    // Under the reach, what the river leaves in the pocket against the setup the user marked, or that
    // the two cannot be compared (BaselineGain); the live region says it after the pocket.
    public static string Gain(GainKind kind) => $"Gain_{kind}";

    // The taxes on pay say they hold the rent's tax too once there is one.
    public static string Bucket(string key, bool taxesOnPayIncludeRent) =>
        taxesOnPayIncludeRent && key == "taxes_on_pay" ? "Bucket_taxes_on_pay_and_rent" : $"Bucket_{key}";

    // What the bucket holds, in the bubble its name opens: the rent's tax too, once there is one.
    public static string Holds(string key, bool taxesOnPayIncludeRent) =>
        taxesOnPayIncludeRent && key == "taxes_on_pay" ? "Holds_taxes_on_pay_and_rent" : $"Holds_{key}";
}

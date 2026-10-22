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

    // Under the bar, what reaches the pocket, what landed in the muddy pocket once a car counts, and
    // what went to taxes: of what the company does not keep, once it keeps some.
    public static string Reach(BucketsView view) =>
        (view.HasMuddyPocket ? "Reach_Muddy" : "Reach") + (view.Buckets.Single(bucket => bucket.Key == "kept_in_company").Amount > 0m ? "_Kept" : "");

    // Under the gain, that the river has changes no saved plan holds.
    public const string Unsaved = "Unsaved";

    // Under the reach, what the river leaves in the pocket against the plan starred « Today », or that
    // the two cannot be compared (BaselineGain); the live region says it after the pocket.
    public static string Gain(GainKind kind) => $"Gain_{kind}";

    // Beside the gain, what the muddy pocket holds more or less than Today's, when it differs.
    public static string Muddy(decimal difference) => difference > 0m ? "Muddy_More" : "Muddy_Less";

    // The taxes on pay say they hold the rent's tax too once there is one.
    public static string Bucket(string key, bool taxesOnPayIncludeRent) =>
        taxesOnPayIncludeRent && key == "taxes_on_pay" ? "Bucket_taxes_on_pay_and_rent" : $"Bucket_{key}";

    // What the bucket holds, in the bubble its name opens: the rent's tax too, once there is one.
    public static string Holds(string key, bool taxesOnPayIncludeRent) =>
        taxesOnPayIncludeRent && key == "taxes_on_pay" ? "Holds_taxes_on_pay_and_rent" : $"Holds_{key}";
}

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

    // Under the bar, what reaches the pocket and what went to taxes: of what the company does not keep,
    // once it keeps some.
    public static string Reach(BucketsView view) =>
        "Reach" + (view.Buckets.Single(bucket => bucket.Key == "kept_in_company").Amount > 0m ? "_Kept" : "");

    // What the river starts from, worded with three figures in this order: what the year starts from
    // before the profits kept from earlier years, those profits, and a car's costs. The profit before
    // what the company pays the director, after a car's costs once there are any (the river starts
    // after them), with the earlier profits once there are any; or, with the company skipped, the pay,
    // with the contributions the company pays on it when it does.
    public static string From(BucketsView view) => view.StartsAtPay
        ? view.CompanyPaysContributions ? "FromPayCost" : "FromPay"
        : (view.CarCosts > 0m ? "FromProfitAfterCar" : "FromProfit") + (view.EarlierProfits > 0m ? "AndEarlier" : "");

    // Under the gain, that the river has changes no saved plan holds.
    public const string Unsaved = "Unsaved";

    // Under the reach, what the river leaves in the pocket against the plan starred « Today », or that
    // the two cannot be compared (BaselineGain); the live region says it after the pocket.
    public static string Gain(GainKind kind) => $"Gain_{kind}";

    // The taxes on pay say they hold the rent's tax too once there is one. A copyright royalty's tax is
    // said in what the bucket holds alone: the name keeps « pay » (decided unattended), short enough for
    // the bar.
    public static string Bucket(string key, bool taxesOnPayIncludeRent) =>
        $"Bucket_{key}{TaxesOnPayWith(key, taxesOnPayIncludeRent, false)}";

    // What the bucket holds, in the bubble its name opens: the rent's tax and a royalty's too, once
    // there is one.
    public static string Holds(string key, bool taxesOnPayIncludeRent, bool taxesOnPayIncludeRoyalty = false) =>
        $"Holds_{key}{TaxesOnPayWith(key, taxesOnPayIncludeRent, taxesOnPayIncludeRoyalty)}";

    private static string TaxesOnPayWith(string key, bool rent, bool royalty) => key != "taxes_on_pay"
        ? ""
        : (rent, royalty) switch
        {
            (true, true) => "_rent_and_royalty",
            (true, false) => "_and_rent",
            (false, true) => "_and_royalty",
            _ => "",
        };
}

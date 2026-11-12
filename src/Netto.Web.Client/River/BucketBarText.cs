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

    // Beside the pocket's amount, after the one share the band shows: that it reaches the pocket. Out of
    // sight in a narrow column, for a screen reader.
    public const string ReachWords = "ReachWords";

    // After a bucket's amount, which is a button choosing the bucket on the map, out of sight: the bucket's name
    // and what the button does.
    public const string ChooseWords = "ChooseWords";

    // What the reach's (i) is « More about ».
    public const string ReachLabel = "ReachLabel";

    // What the reach is out of, in its bubble.
    public const string ReachMeaning = "ReachMeaning";

    // With the company skipped, in the reach's bubble: where the rest of the river is.
    public const string EnterCompany = "EnterCompany";

    // What the river starts from, worded with two figures in this order: what the year starts from
    // before the profits kept from earlier years, and those profits. The profit before INASTI's company
    // contribution and what the company pays the director, always, before a car's costs too once there
    // are any, which the expenses hold, with the earlier profits once there are any; or, with the
    // company skipped, the pay, with the contributions the company pays on it when it does.
    public static string From(BucketsView view) => view.StartsAtPay
        ? view.CompanyPaysContributions ? "FromPayCost" : "FromPay"
        : (view.CarCosts > 0m ? "FromProfitWithCar" : "FromProfit") + (view.EarlierProfits > 0m ? "AndEarlier" : "");

    // What the river leaves in the pocket against the plan starred « Today », or that the two cannot be
    // compared (BaselineGain), in full: the live region says it after the pocket, and the bubble of a gain
    // that is not a figure says why.
    public static string Gain(GainKind kind) => $"Gain_{kind}";

    // The same, short, on the pocket's name line: « +€2,551.13 vs Today ».
    public static string GainShown(GainKind kind) => $"GainShown_{kind}";

    // A bucket's name is one text whatever the plan holds: the bucket of what leaves the pay names the social
    // contributions with the income tax, which covers the pay, a rent and a royalty alike; its key keeps its
    // first name, taxes_on_pay. What the plan adds to it is said in what the bucket holds (Holds).
    public static string Bucket(string key) => $"Bucket_{key}";

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

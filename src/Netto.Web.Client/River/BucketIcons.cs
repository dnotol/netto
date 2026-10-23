namespace Netto.Web.Client.River;

// The icon of each bucket of the river, set here and nowhere else: beside the bucket's name in the bar
// of buckets, and on a glance of the river's outline whose money lands in that bucket, so an icon
// always means the bucket the bar names with it. Emojis for now, until a designed set replaces them
// in this file. An icon is decoration, drawn hidden from screen readers: the bucket's name stays the
// text, and no meaning rests on the icon's colours. The muddy pocket's is a key: it holds what the
// company puts at the director's disposal, a car today, other things later.
internal static class BucketIcons
{
    public static string For(string bucket) => bucket switch
    {
        "pocket" => "👛",
        "taxes_on_pay" => "🧾",
        "taxes_on_profit" => "🏛️",
        "kept_in_company" => "🏦",
        BucketsView.Muddy => "🔑",
        _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null),
    };
}

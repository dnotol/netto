using Netto.Core.Copyright;

namespace Netto.Web.Client.River.Copyright;

// The resource keys the copyright card builds from values, gone through by the card and its tests
// alike.
internal static class CopyrightCardText
{
    // Which limit set the part within them, as the engine says it was.
    public static string Limits(CopyrightResult result) => $"Limits_{result.Bound}";

    // The summary names the part taxed as pay once there is one, and says when all of it is.
    public static string Summary(CopyrightView view) => view switch
    {
        { HasRoyalty: false } => "SummaryNoRoyalty",
        { Result.WithinLimits: 0m } => "Summary_AllAsPay",
        { Result.TaxedAsPay: > 0m } => "Summary_AsPay",
        _ => "Summary",
    };

    // The card's row in the river's outline: the summary's first figure, as the summary says it. What
    // reaches the pocket after the tax apart, or, with nothing within the limits, all of it taxed as pay;
    // nothing before there is a royalty.
    public static CardGlance? Glance(CopyrightView view) => Summary(view) switch
    {
        "SummaryNoRoyalty" => null,
        "Summary_AllAsPay" => new CardGlance(GlanceKind.TaxedAsPay, Formats.Euros(view.Result.TaxedAsPay)),
        _ => new CardGlance(GlanceKind.ToYou, Formats.Euros(view.AfterTax)),
    };

    // Whether the flat-rate costs came off the part within the limits, as the engine took them.
    public static string Costs(CopyrightResult result) => result.FlatRateCosts > 0m ? "FlatRateCosts" : "OnTheGross";

    // What the royalty leaves in the pocket against the same plan without it: more, less, or the same.
    public static string Gain(CopyrightComparison comparison) => comparison.Gain switch
    {
        > 0m => "Gain_More",
        < 0m => "Gain_Less",
        _ => "Gain_None",
    };
}

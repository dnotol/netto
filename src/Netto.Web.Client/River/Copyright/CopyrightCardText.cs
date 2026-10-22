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

    // What the royalty leaves in the pocket against the same plan without it: more, less, or the same.
    public static string Gain(CopyrightComparison comparison) => comparison.Gain switch
    {
        > 0m => "Gain_More",
        < 0m => "Gain_Less",
        _ => "Gain_None",
    };
}

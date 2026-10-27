using Netto.Core.Copyright;

namespace Netto.Web.Client.River.Copyright;

// The resource keys the copyright card builds from values, gone through by the card and its tests
// alike.
internal static class CopyrightCardText
{
    // Which limit set the part within them, as the engine says it was, in the note under its lines.
    public static string Limits(CopyrightResult result) => $"Limits_{result.Bound}";

    // The summary names the part taxed as pay once there is one, and says when all of it is.
    public static string Summary(CopyrightView view) => view switch
    {
        { HasRoyalty: false } => "SummaryNoRoyalty",
        { Result.WithinLimits: 0m } => "Summary_AllAsPay",
        { Result.TaxedAsPay: > 0m } => "Summary_AsPay",
        _ => "Summary",
    };

    // The line of what the royalty leaves in the pocket against the same plan without it: more, less, or
    // the same.
    public static string Gain(CopyrightComparison comparison) => comparison.Gain switch
    {
        > 0m => "Figure_Gain_More",
        < 0m => "Figure_Gain_Less",
        _ => "Figure_Gain_None",
    };
}

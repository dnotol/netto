using Netto.Core.Copyright;
using Netto.Core.Rules;

namespace Netto.Web.Client.River.Copyright;

// The resource keys the copyright card builds from values, gone through by the card and its tests
// alike.
internal static class CopyrightCardText
{
    // A rate a text states, the rules' own as the card words it; the year's rate where the year has none (the
    // ruling's envelope in 2025) or has no rules, as the cap is « the year's cap ».
    public static string Rate(RuleAmount? rate) => rate is null ? "the year's rate" : Formats.RuleAmount(rate);

    // Which limit set the part within them, as the engine says it was, in the note under its lines.
    public static string Limits(CopyrightResult result) => $"Limits_{result.Bound}";

    // The royalty's two ways, one chip each.
    public static string Policy(bool followsPay) => followsPay ? "Policy_Follows" : "Policy_Fixed";

    // Why a royalty that follows the pay is none; none when the engine set it at what a ruling accepts,
    // its figure being among the card's lines.
    public static string? Following(CopyrightFollowing following) => following == CopyrightFollowing.AtCeiling ? null : $"Following_{following}";

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

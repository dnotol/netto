using Microsoft.Extensions.Localization;
using Netto.Core.Copyright;
using Netto.Core.Rules;

namespace Netto.Web.Client.River.Copyright;

// The resource keys the copyright card builds from values, gone through by the card and its tests
// alike.
internal static class CopyrightCardText
{
    // A rate a text states, the rules' own as the card words it; the year's rate (YearsRate) where the year
    // has none (the ruling's envelope in 2025) or has no rules, as the cap is « the year's cap ».
    public static string Rate(IStringLocalizer texts, RuleAmount? rate) => rate is null ? texts["YearsRate"].Value : Formats.RuleAmount(rate);

    // What the lines of the smallest pays keeping the reduced rate cannot say: that no pay keeps it beside the
    // royalty, or without it. None where both pays are lines.
    public static string? RoyaltyRateNote(RoyaltyRateView rate) => rate switch
    {
        { With: null } => "RoyaltyRate_NoPayKeeps",
        { Without: null } => "RoyaltyRate_NoPayKeepsWithout",
        _ => null,
    };

    // Which limit set the part within them, as the engine says it was, in the note under its lines.
    public static string Limits(CopyrightResult result) => $"Limits_{result.Bound}";

    // The royalty's two ways, one chip each.
    public static string Policy(bool followsPay) => followsPay ? "Policy_Follows" : "Policy_Fixed";

    // Why a royalty that follows the pay is none; none when the engine set it at what a ruling accepts,
    // its figure being among the card's lines.
    public static string? Following(CopyrightFollowing following) => following == CopyrightFollowing.AtCeiling ? null : $"Following_{following}";

    // The summary says what the royalty adds to the pocket or takes from it, the plan with it less the plan
    // without (LeverGain), or that there is no royalty yet.
    public static string Summary(CopyrightView view, decimal gain) => view.HasRoyalty ? LeverGainText.Summary(gain) : "SummaryNoRoyalty";
}

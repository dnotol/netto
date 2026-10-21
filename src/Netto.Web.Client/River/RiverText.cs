using Netto.Core.CorporateTax;
using Netto.Core.Dividends;
using Netto.Core.SocialContributions;

namespace Netto.Web.Client.River;

// The resource keys the river's components build from values and the engine's keys, one class per
// component. The components and RiverTextTests both go through these, so a key without a text in
// its .resx fails a test instead of showing on screen.
internal static class OriginChipText
{
    public static string Origin(InputOrigin origin) => $"Origin_{origin}";

    public static string Hint(InputOrigin origin) => $"Hint_{origin}";
}

internal static class DrainListText
{
    public static string Drain(string key) => $"Drain_{key}";

    // Where a drain goes; what comes back says where from.
    public static string Destination(Drain drain) => drain.Kind == DrainKind.Back ? $"From_{drain.To}" : $"To_{drain.To}";
}

internal static class RefusedMessageText
{
    public static string Error(string errorKey) => $"Error_{errorKey}";
}

internal static class CorporateTaxCardText
{
    public static string Condition(ReducedRateCondition condition) => $"Condition_{condition}";

    public static string Surcharge(SurchargeState state) => $"Surcharge_{state}";

    public static string FinancialYear(FinancialYear year) => $"FinancialYear_{year}";

    // The financial year as the line of « Your company » states it.
    public static string CompanyFinancialYear(FinancialYear year) => $"Company_FinancialYear_{year}";
}

internal static class DirectorPayCardText
{
    public static string Surcharge(TaxSurchargeState state) => $"Surcharge_{state}";

    public static string PaidBy(ContributionsPaidBy paidBy) => $"PaidBy_{paidBy}";

    // Who pays the contributions as the line of « Your situation » states it.
    public static string SituationPaidBy(ContributionsPaidBy paidBy) => $"Situation_PaidBy_{paidBy}";
}

internal static class DividendCardText
{
    public static string Category(WithholdingCategory category) => $"Category_{category}";
}

internal static class ConditionListText
{
    public static string Condition(string key) => $"Condition_{key}";

    // « 1 condition to meet » or « 5 conditions to meet ».
    public static string Heading(int count) => count == 1 ? "Heading_One" : "Heading_Many";
}

internal static class SplitCardText
{
    // The sentence names the best pay on the other side of the threshold only when one keeps the
    // reserve.
    public static string Rate(RateWeighing rate) => rate switch
    {
        { Choice: RateChoice.NotWeighed } => "Rate_NotWeighed",
        { Other: null } => $"Rate_{rate.Choice}Only",
        _ => $"Rate_{rate.Choice}",
    };
}

internal static class CompositionMenuText
{
    public static string Title(OptionalModule module) => $"Title_{module}";

    // What the module is, said while it is out of the plan.
    public static string Description(OptionalModule module) => $"Description_{module}";

    // What it brings to the pocket, said while it is in the plan.
    public static string Brings(OptionalModule module) => $"Brings_{module}";

    // The button's name, with how many optional modules the plan holds.
    public static string ButtonLabel(int count) => count switch
    {
        0 => "ButtonLabel_None",
        1 => "ButtonLabel_One",
        _ => "ButtonLabel_Many",
    };
}

internal static class TakenOutNoticeText
{
    public static string TakenOut(OptionalModule module) => $"TakenOut_{module}";
}

internal static class InfoBubbleText
{
    // The name of a bubble's (i): « More about » what it stands beside.
    public const string MoreAbout = "MoreAbout";
}

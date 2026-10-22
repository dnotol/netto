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
    // A text the step behind a drain follows, as its bubble cites it.
    public const string Source = "Source";

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
    // With benefits in kind, a condition the pay decides says they are counted in it.
    public static string Condition(ReducedRateCondition condition, bool withBenefits) =>
        withBenefits && condition is ReducedRateCondition.FirstFourTaxPeriods or ReducedRateCondition.MinimumPayMet
            or ReducedRateCondition.TaxableIncomeNotAbovePay or ReducedRateCondition.MinimumPayNotMet
            ? $"Condition_{condition}_WithBenefits"
            : $"Condition_{condition}";

    public static string Surcharge(SurchargeState state) => $"Surcharge_{state}";

    // What the taxable income adds back: the vouchers the company cannot deduct, a company car's share
    // of its benefit, or both.
    public static string Disallowed(CorporateTaxView view) => (view.DisallowedVouchers, view.CarAddBack) switch
    {
        ( > 0m, > 0m) => "Disallowed_VouchersAndCar",
        (_, > 0m) => "Disallowed_Car",
        _ => "Disallowed",
    };

    public static string FinancialYear(FinancialYear year) => $"FinancialYear_{year}";

    // The financial year as the line of « Your company » states it.
    public static string CompanyFinancialYear(FinancialYear year) => $"Company_FinancialYear_{year}";
}

internal static class DirectorPayCardText
{
    public static string Surcharge(TaxSurchargeState state) => $"Surcharge_{state}";

    public static string PaidBy(ContributionsPaidBy paidBy) => $"PaidBy_{paidBy}";

    // What the key formula is, in the bubble the term opens where the withholding is derived from it.
    public const string KeyFormulaTerm = "Term_KeyFormula";

    // Who pays the contributions as the line of « Your situation » states it.
    public static string SituationPaidBy(ContributionsPaidBy paidBy) => $"Situation_PaidBy_{paidBy}";
}

internal static class DividendCardText
{
    // What VVPRbis shares are, in the bubble the term opens where the rates are said.
    public const string VvprbisTerm = "Term_Vvprbis";

    public static string Category(WithholdingCategory category) => $"Category_{category}";

    // The summary starts from what comes in, all of which is paid out, or, once the company keeps
    // some, from what is paid out: the base its rate is out of.
    public static string Summary(DividendView view) => view.Kept > 0m ? "Summary_PaidOut" : "Summary";
}

internal static class RentCardText
{
    // What the cadastral income is, in the bubble the term opens in the cap's sentence.
    public const string CadastralIncomeTerm = "Term_CadastralIncome";

    // What the flat charges are, in the bubble the term opens where the rent's tax is said.
    public const string FlatChargesTerm = "Term_FlatCharges";
}

internal static class CarCardText
{
    // What the car takes in cash against what it puts in the muddy pocket: the muddy pocket holds more
    // than it costs, or less, or it costs no cash at all.
    public static string Gap(CarComparison comparison) => comparison switch
    {
        { CashCost: <= 0m } => "Gap_NoCash",
        { Gap: >= 0m } => "Gap_More",
        _ => "Gap_Less",
    };
}

internal static class ConditionListText
{
    public static string Condition(string key) => $"Condition_{key}";

    // « 1 condition to meet » or « 5 conditions to meet ».
    public static string Heading(int count) => count == 1 ? "Heading_One" : "Heading_Many";
}

// The share a card's summary ends on, worded once in ModuleCard.resx for every card.
internal static class ModuleCardText
{
    public const string ToYou = "ToYou";

    public const string PassesOn = "PassesOn";
}

internal static class SplitCardText
{
    // What the reduced corporate rate is, in the bubble the term opens where the rate is weighed.
    public const string ReducedRateTerm = "Term_ReducedRate";

    // What reaches the pocket at the best pay, against the pay set.
    public const string BestReach = "BestReach";

    // What reaches the pocket at the pay set, the best.
    public const string BestIsYoursReach = "BestIsYoursReach";

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

// A sentence of a card with a term of art in it, marked in its .resx text between square brackets:
// « It keeps the [reduced corporate rate], … ». TermSentence draws the term as the trigger of an info
// bubble that says what it means, and the rest as text.
internal static class TermText
{
    // The text before the term, the term and the text after it; the whole sentence, with no term,
    // when none is marked.
    public static (string Before, string? Term, string After) Split(string sentence)
    {
        var open = sentence.IndexOf('[');
        var close = open < 0 ? -1 : sentence.IndexOf(']', open + 1);
        return close < 0 ? (sentence, null, "") : (sentence[..open], sentence[(open + 1)..close], sentence[(close + 1)..]);
    }
}

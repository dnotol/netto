using Netto.Core.CorporateTax;

namespace Netto.Web.Client.River;

// The resource keys the Corporate tax card builds from values, gone through by the card, the lines of
// its settings (SettingsViews) and its tests alike.
internal static class CorporateTaxCardText
{
    // With benefits in kind, a condition the pay decides says they are counted in it.
    public static string Condition(ReducedRateCondition condition, bool withBenefits) =>
        withBenefits && condition is ReducedRateCondition.FirstFourTaxPeriods or ReducedRateCondition.MinimumPayMet
            or ReducedRateCondition.TaxableIncomeNotAbovePay or ReducedRateCondition.MinimumPayNotMet
            ? $"Condition_{condition}_WithBenefits"
            : $"Condition_{condition}";

    // A year with no taxable profit pays no dividend out of it, but the profits kept from earlier years
    // still pay one, less this year's loss and the reserve: the sentence names it while there is one.
    public static string NoTaxableProfit(decimal dividend) => dividend > 0m ? "NoTaxableProfit_EarlierProfits" : "NoTaxableProfit";

    public static string Surcharge(SurchargeState state) => $"Surcharge_{state}";

    // What the advance payments leave, as the card says it: why no surcharge is due, or, while one is,
    // what more by the first deadline still open cancels it, or that no payment can, once none is open.
    public static string SurchargeSentence(SurchargeState state, MoreBy? more) =>
        state == SurchargeState.Due && more is null ? "Surcharge_TooLate" : Surcharge(state);

    // The sentence's arguments: that amount and that deadline, as the card words them.
    public static object[] SurchargeArguments(MoreBy? more) => more is null ? [] : [Formats.Euros(more.Amount), Formats.Day(more.Deadline)];

    public static string FinancialYear(FinancialYear year) => $"FinancialYear_{year}";

    // The financial year as the line of « Your company » states it.
    public static string CompanyFinancialYear(FinancialYear year) => $"Company_FinancialYear_{year}";
}

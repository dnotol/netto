using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;

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

    // What the pay art. 215 tests holds, in the bubble of its term: Director pay's taxable pay while no
    // copyright royalty's part beyond its limits is taxed as pay; beside one, what that taxable pay counts
    // beyond the pay tested, the difference of the engine's two figures: that part, and, when the company
    // pays the contributions, the contributions and the fund's fee on it, which the test leaves out too.
    public static (string Key, decimal LeftOut) TestedPayTerm(GrossToNetResult pay) => pay switch
    {
        { RequalifiedPay: <= 0m } => ("Term_TestedPay", 0m),
        { ContributionsPaidByCompany: > 0m } => ("Term_TestedPay_RoyaltyCompanyPays", pay.TaxablePay - pay.TaxablePayWithoutRequalified),
        _ => ("Term_TestedPay_Royalty", pay.TaxablePay - pay.TaxablePayWithoutRequalified),
    };

    // A year with no taxable profit pays no dividend out of it, but the profits kept from earlier years
    // still pay one, less this year's loss and the reserve: the sentence names it while there is one.
    public static string NoTaxableProfit(decimal dividend) => dividend > 0m ? "NoTaxableProfit_EarlierProfits" : "NoTaxableProfit";

    // What the advance payments leave: why a surcharge is due or why none is, never what to pay to cancel it.
    public static string Surcharge(SurchargeState state) => $"Surcharge_{state}";

    public static string FinancialYear(FinancialYear year) => $"FinancialYear_{year}";

    // The financial year as the line of « Your company » states it.
    public static string CompanyFinancialYear(FinancialYear year) => $"Company_FinancialYear_{year}";
}

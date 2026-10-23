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

    public static string Surcharge(SurchargeState state) => $"Surcharge_{state}";

    // What the taxable income adds back: the vouchers the company cannot deduct, a company car's share
    // of its benefit with the costs it cannot deduct when there are any, or both.
    public static string Disallowed(CorporateTaxView view) => (view.DisallowedVouchers, view.CarCostsNotDeductible, view.CarAddBack) switch
    {
        ( > 0m, > 0m, _) => "Disallowed_VouchersAndCarCosts",
        ( > 0m, _, > 0m) => "Disallowed_VouchersAndCar",
        (_, > 0m, _) => "Disallowed_CarCosts",
        (_, _, > 0m) => "Disallowed_Car",
        _ => "Disallowed",
    };

    public static string FinancialYear(FinancialYear year) => $"FinancialYear_{year}";

    // The financial year as the line of « Your company » states it.
    public static string CompanyFinancialYear(FinancialYear year) => $"Company_FinancialYear_{year}";
}

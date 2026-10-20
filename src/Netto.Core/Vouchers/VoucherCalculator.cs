namespace Netto.Core.Vouchers;

public static class VoucherCalculator
{
    public const string MealVoucherDaysNegative = "meal_voucher_days_negative";
    public const string MealVoucherDaysAboveYear = "meal_voucher_days_above_year";
    public const string EcoVouchersNegative = "eco_vouchers_negative";
    public const string EcoVouchersAboveMaximum = "eco_vouchers_above_maximum";
    public const string SportCultureVouchersNegative = "sport_culture_vouchers_negative";
    public const string SportCultureVouchersAboveMaximum = "sport_culture_vouchers_above_maximum";

    // Each meal voucher carries the most the company may pay into it, the only share that earns the
    // deduction of 4 (at 8.90 it falls to 2); the director's share buys the rest of the voucher and
    // moves nothing. Eco and sport/culture vouchers above their yearly maximum are not exempt, which is
    // refused rather than taxed, as is a meal voucher for more days than the year has.
    public static (VoucherResult? Value, string? Error) Compute(VoucherRules rules, VoucherGrants grants)
    {
        if (grants.MealVoucherDays < 0)
            return (null, MealVoucherDaysNegative);
        if (grants.MealVoucherDays > new DateOnly(rules.IncomeYear, 12, 31).DayOfYear)
            return (null, MealVoucherDaysAboveYear);
        if (grants.EcoVouchers < 0m)
            return (null, EcoVouchersNegative);
        if (grants.EcoVouchers > rules.EcoVouchersMaximum.Value)
            return (null, EcoVouchersAboveMaximum);
        if (grants.SportCultureVouchers < 0m)
            return (null, SportCultureVouchersNegative);
        if (grants.SportCultureVouchers > rules.SportCultureVouchersMaximum.Value)
            return (null, SportCultureVouchersAboveMaximum);

        return (new VoucherResult(
            MealVouchers(rules, grants),
            grants.EcoVouchers,
            grants.SportCultureVouchers,
            grants.MealVoucherDays * rules.MealVoucherDeductionAtMaximum.Value), null);
    }

    // What the company pays for the vouchers as given, whether or not the rules accept them.
    public static decimal Cost(VoucherRules rules, VoucherGrants grants) =>
        MealVouchers(rules, grants) + grants.EcoVouchers + grants.SportCultureVouchers;

    private static decimal MealVouchers(VoucherRules rules, VoucherGrants grants) => grants.MealVoucherDays * rules.MealVoucherCompanyShare.Value;
}

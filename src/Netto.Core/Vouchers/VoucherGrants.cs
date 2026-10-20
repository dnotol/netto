namespace Netto.Core.Vouchers;

// What the company grants the director in vouchers for the year, as the user declares it: the days
// with an electronic meal voucher, one per day actually worked, and the total value of the eco and
// the sport/culture vouchers. The meal voucher's shares are not declared: Netto grants the most the
// company may pay (VoucherRules). None unless given.
public sealed record VoucherGrants(int MealVoucherDays, decimal EcoVouchers, decimal SportCultureVouchers)
{
    public static VoucherGrants None { get; } = new(0, 0m, 0m);

    public bool Any => this != None;
}

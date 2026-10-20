namespace Netto.Core.Vouchers;

// What the company pays for each kind of voucher, which reaches the director's pocket untaxed and
// outside INASTI, and the part of it the company deducts. The rest is a disallowed expense, added
// back to its taxable income.
public sealed record VoucherResult(decimal MealVouchers, decimal EcoVouchers, decimal SportCultureVouchers, decimal Deductible)
{
    // What the company pays the director in vouchers, and what reaches the pocket: the same amount.
    public decimal Total => MealVouchers + EcoVouchers + SportCultureVouchers;

    public decimal Disallowed => Total - Deductible;
}

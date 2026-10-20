namespace Netto.Core.Simulation;

// What a step means for the director's pocket. A later track, « muddy in pocket » (company-paid
// items the director also uses privately), joins as another value.
public enum Track
{
    // Money that leaves: corporate tax, social contributions, income tax, municipal surcharge, dividend
    // withholding. What comes back, the withholding refunded on exempt dividends, is a negative amount.
    PaidOut,

    // Only sets the tax base and is never subtracted from net in pocket: the flat-rate costs, the
    // taxable income they leave, what the company cannot deduct of the vouchers.
    TaxBaseOnly,

    // Reaches the pocket outside the pay and the dividend: a voucher's value.
    InPocket,
}

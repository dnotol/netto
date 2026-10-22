namespace Netto.Core.Simulation;

// What a step means for the director's pocket.
public enum Track
{
    // Money that leaves: corporate tax, social contributions, income tax, municipal surcharge, dividend
    // withholding. What comes back, the withholding refunded on exempt dividends, is a negative amount.
    PaidOut,

    // Only sets the tax base and is never subtracted from net in pocket: the flat-rate costs, the
    // taxable income they leave, what the company cannot deduct of the vouchers, the rent's charges
    // and the income it leaves taxable, a company car's benefit and what the company adds back of it.
    TaxBaseOnly,

    // Reaches the pocket outside the pay and the dividend: a voucher's value, an expense allowance, the
    // rent. What leaves it for the company, the director's contribution to a company car, is a
    // negative amount.
    InPocket,

    // What the company spends on something the director also uses privately, a company car: neither
    // cash in the pocket nor lost, it lands in the muddy pocket at that cost.
    Muddy,
}

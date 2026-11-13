using Netto.Core.MovableIncome;

namespace Netto.Core.DirectorPay;

// What the levers hand the director pay to be taxed with it, besides the payslip: another income
// globalised with it, a home office's net rent (OtherIncome, Tax-Calc T13 and T14); a benefit taxed as
// pay and never paid in cash, a company car's and the IT and phone's, summed (BenefitInKind, art. 31 al. 2, 2°); and movable income the
// company pays besides the payslip, a copyright royalty, with its part taxed as pay and its part taxed
// apart (Movable). Nothing unless given. The engine's own rules give them, never below zero.
public sealed record TaxedWithPay
{
    public static TaxedWithPay None { get; } = new();

    public decimal OtherIncome { get; init; }

    public decimal BenefitInKind { get; init; }

    public MovableIncomePaid Movable { get; init; } = MovableIncomePaid.None;

    // What two levers hand the pay together: their other incomes added, their benefits added, and the
    // movable income one of them pays. A lever that hands nothing leaves the other's share as it is. The
    // pay takes one movable income, its part taxed apart at one rate, so two are a bug until it takes more.
    public TaxedWithPay Plus(TaxedWithPay other)
    {
        if (other == None)
            return this;
        if (this == None)
            return other;
        if (Movable != MovableIncomePaid.None && other.Movable != MovableIncomePaid.None)
            throw new InvalidOperationException("Two levers pay movable income, and the pay takes one.");

        return new TaxedWithPay
        {
            OtherIncome = OtherIncome + other.OtherIncome,
            BenefitInKind = BenefitInKind + other.BenefitInKind,
            Movable = Movable == MovableIncomePaid.None ? other.Movable : Movable,
        };
    }
}

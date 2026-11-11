using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// Why a move helps, the line under it on the Optimise view: the engine's figures that differ between
// the year before its line and the year after it (PlannedMove.Before, After), worded by kind, so the line
// says what changes with the move, measured on top of the moves above it. It picks figures and applies no
// rule of its own: which sentence says them follows the figures too, a surcharge left or none, a
// deduction or none, the reduced rate of corporate tax as it was or not. A line a later round topped up
// spans the moves taken between its rounds.
internal static class MoveReasons
{
    public static IReadOnlyList<MoveReason> Of(PlannedMove move)
    {
        var (before, after) = (move.Before, move.After);
        IEnumerable<MoveReason> rate = (before.Company.Condition.AppliesReducedRate(), after.Company.Condition.AppliesReducedRate()) switch
        {
            (true, false) => [new MoveReason(MoveReasonKind.ReducedRateGivenUp, [])],
            (false, true) => [new MoveReason(MoveReasonKind.ReducedRateGained, [])],
            _ => [],
        };

        return [What(move.Move, before, after), .. rate];
    }

    private static MoveReason What(Move move, YearResult before, YearResult after) => move switch
    {
        PayMove => before.Copyright.Royalty == after.Copyright.Royalty
            ? new MoveReason(MoveReasonKind.Pay,
                [before.DirectorPay.GrossPay, after.DirectorPay.GrossPay, before.Dividend.Dividend, after.Dividend.Dividend])
            : new MoveReason(MoveReasonKind.PayWithRoyalty,
            [
                before.DirectorPay.GrossPay, after.DirectorPay.GrossPay, before.Copyright.Royalty, after.Copyright.Royalty,
                before.Dividend.Dividend, after.Dividend.Dividend,
            ]),
        CompanyAdvancePaymentMove => Surcharge(before.AdvancePayments.Surcharge, after.AdvancePayments.Surcharge,
            MoveReasonKind.CompanySurcharge, MoveReasonKind.CompanySurchargeCancelled),
        DirectorAdvancePaymentMove => Surcharge(before.DirectorPay.AdvancePayments.Surcharge, after.DirectorPay.AdvancePayments.Surcharge,
            MoveReasonKind.DirectorSurcharge, MoveReasonKind.DirectorSurchargeCancelled),
        EcoVouchersMove => Vouchers(after.Vouchers.EcoVouchers - before.Vouchers.EcoVouchers, before, after),
        SportCultureVouchersMove => Vouchers(after.Vouchers.SportCultureVouchers - before.Vouchers.SportCultureVouchers, before, after),
        RentMove => new MoveReason(MoveReasonKind.Rent, [before.Rent.Rent, after.Rent.Rent, AfterTax(before), AfterTax(after)]),
        CopyrightFollowsPayMove => new MoveReason(MoveReasonKind.CopyrightFollowsPay,
            [before.Copyright.Royalty, after.Copyright.Royalty, before.DirectorPay.TaxedApartTax.Total, after.DirectorPay.TaxedApartTax.Total]),
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the tab has no reason for."),
    };

    // A payment that leaves no surcharge cancels the one there was; otherwise the surcharge before and after.
    private static MoveReason Surcharge(decimal before, decimal after, MoveReasonKind left, MoveReasonKind cancelled) =>
        after == 0m ? new MoveReason(cancelled, [before]) : new MoveReason(left, [before, after]);

    // What more reaches the pocket in vouchers, and what more the company deducts of what it pays, if any.
    private static MoveReason Vouchers(decimal more, YearResult before, YearResult after) =>
        after.Vouchers.Deductible - before.Vouchers.Deductible is var deducted && deducted == 0m
            ? new MoveReason(MoveReasonKind.VouchersNotDeducted, [more])
            : new MoveReason(MoveReasonKind.Vouchers, [more, deducted]);

    // What reaches the pocket of the rent once the tax it adds is paid, as the Rent card says it.
    private static decimal AfterTax(YearResult year) => RentView.From(year, PlanComposition.None).AfterTax;
}

// One sentence of a move's line: what it says (OptimisationTabText.Why), and its amounts, in euros, in the
// order it names them.
internal sealed record MoveReason(MoveReasonKind Kind, IReadOnlyList<decimal> Amounts);

internal enum MoveReasonKind
{
    // The pay before and after, and the dividend paid out before and after.
    Pay,

    // The pay, the copyright royalty that follows it and the dividend paid out, each before and after.
    PayWithRoyalty,

    // The company no longer has the reduced rate of corporate tax after the move.
    ReducedRateGivenUp,

    // The company has the reduced rate after the move, which it had not before.
    ReducedRateGained,

    // The surcharge on the company's corporate tax before and after.
    CompanySurcharge,

    // The surcharge on the company's corporate tax the move cancels.
    CompanySurchargeCancelled,

    // The surcharge on the director's income tax before and after.
    DirectorSurcharge,

    // The surcharge on the director's income tax the move cancels.
    DirectorSurchargeCancelled,

    // What more reaches the pocket tax-free, and what more the company deducts.
    Vouchers,

    // What more reaches the pocket tax-free, of which the company deducts nothing.
    VouchersNotDeducted,

    // The rent before and after, and what reaches the pocket of it after its tax before and after.
    Rent,

    // The copyright royalty before and after, and the tax on it apart from the pay before and after.
    CopyrightFollowsPay,
}

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

        return [What(move.Move, move.Gain, before, after), .. rate];
    }

    // Whether the kind's sentence says a figure the gain beside it is not: what reaches the pocket in vouchers or
    // rent, or the surcharge a payment takes off, where the gain is what is left of it in the pocket
    // (What, Bridged). The other kinds say no figure of the pocket, so their gain is read by its own label.
    public static bool IsBridged(MoveReasonKind kind) => kind is MoveReasonKind.Vouchers or MoveReasonKind.VouchersNotDeducted
        or MoveReasonKind.Rent or MoveReasonKind.CompanySurcharge or MoveReasonKind.CompanySurchargeCancelled;

    private static MoveReason What(Move move, decimal gain, YearResult before, YearResult after) => move switch
    {
        PayMove => before.Copyright.Royalty == after.Copyright.Royalty
            ? new MoveReason(MoveReasonKind.Pay,
                [before.DirectorPay.GrossPay, after.DirectorPay.GrossPay, before.Dividend.Dividend, after.Dividend.Dividend])
            : new MoveReason(MoveReasonKind.PayWithRoyalty,
            [
                before.DirectorPay.GrossPay, after.DirectorPay.GrossPay, before.Copyright.Royalty, after.Copyright.Royalty,
                before.Dividend.Dividend, after.Dividend.Dividend,
            ]),
        CompanyAdvancePaymentMove => Bridged(Surcharge(before.AdvancePayments.Surcharge, after.AdvancePayments.Surcharge,
            MoveReasonKind.CompanySurcharge, MoveReasonKind.CompanySurchargeCancelled), before.AdvancePayments.Surcharge - after.AdvancePayments.Surcharge, gain),
        DirectorAdvancePaymentMove => Surcharge(before.DirectorPay.AdvancePayments.Surcharge, after.DirectorPay.AdvancePayments.Surcharge,
            MoveReasonKind.DirectorSurcharge, MoveReasonKind.DirectorSurchargeCancelled),
        EcoVouchersMove => Bridged(Vouchers(after.Vouchers.EcoVouchers - before.Vouchers.EcoVouchers, before, after),
            after.Vouchers.EcoVouchers - before.Vouchers.EcoVouchers, gain),
        SportCultureVouchersMove => Bridged(Vouchers(after.Vouchers.SportCultureVouchers - before.Vouchers.SportCultureVouchers, before, after),
            after.Vouchers.SportCultureVouchers - before.Vouchers.SportCultureVouchers, gain),
        RentMove => Bridged(new MoveReason(MoveReasonKind.Rent, [before.Rent.Rent, after.Rent.Rent, AfterTax(before), AfterTax(after)]),
            AfterTax(after) - AfterTax(before), gain),
        CopyrightFollowsPayMove => new MoveReason(MoveReasonKind.CopyrightFollowsPay,
            [before.Copyright.Royalty, after.Copyright.Royalty, before.DirectorPay.TaxedApartTax.Total, after.DirectorPay.TaxedApartTax.Total]),
        _ => throw new ArgumentOutOfRangeException(nameof(move), move, "A move the tab has no reason for."),
    };

    // The sentence with what its gain leaves out of the figure it says: the gain less that figure, the rest, and
    // the gain itself, appended to its amounts, the rest as a size with its direction in Rest. A gain equal to the
    // figure has nothing to bridge. The one difference of two figures the engine gave: no rule of its own.
    private static MoveReason Bridged(MoveReason reason, decimal said, decimal gain) => gain - said is var rest && rest == 0m
        ? reason
        : reason with { Amounts = [.. reason.Amounts, Math.Abs(rest), gain], Rest = rest < 0m ? MoveReasonRest.Less : MoveReasonRest.More };

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
// order it names them; a sentence that says a figure its gain is not ends with the rest and the gain.
internal sealed record MoveReason(MoveReasonKind Kind, IReadOnlyList<decimal> Amounts)
{
    // How the gain stands to the figure the sentence says: none (the same, or a kind that says no pocket figure),
    // the rest less than it or more than it.
    public MoveReasonRest Rest { get; init; } = MoveReasonRest.None;
}

internal enum MoveReasonRest
{
    None,

    // The gain is less than the figure: the rest of the pocket gives up the difference.
    Less,

    // The gain is more than the figure: the rest of the pocket adds the difference.
    More,
}

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

using Netto.Core.Rules;
using Netto.Core.Vouchers;

namespace Netto.Core.Simulation.Levers;

// The vouchers the company grants the director: what it pays for them comes off what it earns, as given,
// and reaches the pocket exempt; what it cannot deduct of them joins its taxable income.
internal sealed record VoucherLever(VoucherRules Rules) : Lever
{
    public override decimal FromProfit(Scenario scenario) => VoucherCalculator.Cost(Rules, scenario.Vouchers);

    public override (LeverYear? Value, Refusal? Error) Compute(Scenario scenario, decimal left)
    {
        var (vouchers, error) = VoucherCalculator.Compute(Rules, scenario.Vouchers);
        return vouchers is null ? (null, error) : (new VoucherYear(Rules, vouchers, scenario.Vouchers.Any), null);
    }
}

// The vouchers as the rules took them, Used once the company grants any.
internal sealed record VoucherYear(VoucherRules Rules, VoucherResult Vouchers, bool Used) : LeverYear
{
    public override decimal ToPocket => Vouchers.Total;

    public override decimal Disallowed => Vouchers.Disallowed;

    // Each kind of voucher reaches the pocket exempt, at what the company pays into it, and what the
    // company cannot deduct of them, the vouchers as the corporate layer taxes them, joins its taxable
    // income.
    public override IReadOnlyList<Step> Steps() => !Used ? [] :
    [
        new Step("meal_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, Vouchers.MealVouchers,
            Rules.Exemption, [Rules.MealVoucherCompanyShare, Rules.MealVoucherHolderShare]),
        new Step("eco_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, Vouchers.EcoVouchers,
            Rules.Exemption, [Rules.EcoVouchersMaximum, Rules.EcoVoucherFaceValueMaximum]),
        new Step("sport_culture_vouchers", Layer.Personal, TaxableEvent.Vouchers, Track.InPocket, Vouchers.SportCultureVouchers,
            Rules.Exemption, [Rules.SportCultureVouchersMaximum]),
        new Step("disallowed_vouchers", Layer.Corporate, TaxableEvent.Vouchers, Track.TaxBaseOnly, Vouchers.Disallowed,
            Rules.NonDeductible, [Rules.MealVoucherCompanyShare, Rules.MealVoucherDeduction, Rules.MealVoucherDeductionAtMaximum]),
    ];
}

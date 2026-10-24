using Netto.Core.Copyright;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;

namespace Netto.Core.Simulation.Levers;

// The copyright royalty the company pays the director, as the engine composes it into the year: here,
// where LeverOrder lists it, YearSimulation reading its result by its type for YearResult.Copyright and
// the split's PayLevel.Royalty alone. It comes off what the company earns, deducted in full, and hands
// the year its movable income
// (MovableIncomePaid): its part within the limits reaches the pocket taxed apart (GrossToNet's
// TaxedApartTax), its part beyond them is taxed as the director's pay, which art. 215's minimum pay
// never counts. A royalty that follows the pay is set first, at what an advance ruling accepts at the
// scenario's own pay, so the year, the split's search and the moves each read one fixed amount at each
// pay. A royalty the user fixes is refused above what the company has before pay and royalty, what it
// earns less what the levers before it take, earlier profits aside as the pay's ceiling leaves them
// (operator, 2026-10-23): it would be paid at a loss. One that follows the pay is held to the pay
// ceiling instead, as the pay is.
internal sealed record CopyrightLever(CopyrightRules Rules) : Lever
{
    public override decimal FromProfit(Scenario scenario) => CopyrightCalculator.AtPay(Rules, scenario.Copyright, scenario.DirectorPay).Royalty;

    public override bool FollowsPay(Scenario scenario) => scenario.Copyright.FollowsPay;

    // A royalty that follows the pay at what it comes to at the scenario's own pay, still following it,
    // so that the year reads one amount and a year run again at another pay follows it there.
    public override Scenario AtPay(Scenario scenario) =>
        scenario.Copyright.FollowsPay ? scenario with { Copyright = CopyrightCalculator.AtPay(Rules, scenario.Copyright, scenario.DirectorPay) } : scenario;

    public override (LeverYear? Value, string? Error) Compute(Scenario scenario, decimal left)
    {
        var (royalty, error) = CopyrightCalculator.Compute(Rules, scenario.Copyright, scenario.DirectorPay);
        if (royalty is null)
            return (null, error);
        if (!scenario.Copyright.FollowsPay && royalty.Royalty > Math.Max(0m, left))
            return (null, CopyrightCalculator.RoyaltyAboveProfit);

        return (new CopyrightYear(Rules, royalty, scenario.Copyright.Any), null);
    }
}

// The royalty as the rules took it, Used once there is one.
internal sealed record CopyrightYear(CopyrightRules Rules, CopyrightResult Copyright, bool Used) : LeverYear
{
    public override decimal ToPocket => Copyright.MovableIncome.PaidApart;

    public override TaxedWithPay TaxedWithPay => new() { Movable = Copyright.MovableIncome };

    // The royalty the company pays and deducts in full, with the amounts that measured what a ruling
    // accepts when it follows the pay; its part within the limits, reaching the pocket, with the limits
    // it used, outside INASTI's base and the advance-payment surcharge's; its flat-rate costs; and its
    // part beyond them, taxed as pay with the pay, a plain zero without one.
    public override IReadOnlyList<Step> Steps() => !Used ? [] :
    [
        new Step("copyright_royalty", Layer.Corporate, TaxableEvent.CopyrightRoyalty, Track.TaxBaseOnly, Copyright.Royalty,
            Rules.Deduction, Copyright.CeilingAmounts),
        new Step("copyright_within_limits", Layer.Personal, TaxableEvent.CopyrightRoyalty, Track.InPocket, Copyright.WithinLimits,
            Rules.Limits, [Rules.RelativeLimit, Rules.Cap]) { AlsoCites = [Rules.PhaseIn, Rules.OutsideContributions, Rules.OutsideAdvancePaymentSurcharge] },
        new Step("copyright_flat_rate_costs", Layer.Personal, TaxableEvent.CopyrightRoyalty, Track.TaxBaseOnly, Copyright.FlatRateCosts,
            Rules.FlatRateCosts.Bands, Rules.FlatRateCosts.Amounts)
        {
            AlsoCites = [Rules.FlatRateCosts.NetIncome, Rules.FlatRateCosts.Who, .. Rules.FlatRateCosts.Attestation is { } attestation ? [attestation] : Array.Empty<Source>()],
        },
        new Step("copyright_taxed_as_pay", Layer.Personal, TaxableEvent.CopyrightRoyalty, Track.TaxBaseOnly, Copyright.TaxedAsPay,
            Rules.TaxedAsPay, [Rules.RelativeLimit, Rules.Cap]) { AlsoCites = [Rules.Limits] },
    ];

    // What the royalty's part within the limits adds to the director's personal tax: its tax apart,
    // joined to the State tax before the split, the municipal surcharge on it, the credit for low earned
    // income it takes away, and what that lost credit adds to the surcharge on the pay's tax. Each is
    // paid out of the pocket, the credit's as a positive amount since it is lost, not refunded. The 15%
    // is on that part less its flat-rate costs, which an attestation holder alone takes from 2026.
    public override IReadOnlyList<Step> StepsAfterPay(GrossToNetResult pay, PersonalTaxRules taxRules, Source municipalSurcharge, RuleAmount municipalRate) =>
        !Used ? [] :
        [
            new Step("copyright_tax", Layer.Personal, TaxableEvent.CopyrightRoyalty, Track.PaidOut, pay.TaxedApartTax.IncomeTax,
                Rules.TaxedApart, [Rules.TaxedApartRate, taxRules.AutonomyFactor, taxRules.RegionalAdditionalRate]),
            new Step("copyright_municipal_surcharge", Layer.Personal, TaxableEvent.CopyrightRoyalty, Track.PaidOut, pay.TaxedApartTax.MunicipalSurcharge,
                municipalSurcharge, [municipalRate]),
            new Step("copyright_low_earned_income_credit_lost", Layer.Personal, TaxableEvent.CopyrightRoyalty, Track.PaidOut, pay.TaxedApartTax.LowEarnedIncomeCreditLost,
                taxRules.LowEarnedIncomeCredit.Source, taxRules.LowEarnedIncomeCredit.Amounts),
            new Step("copyright_income_tax_advance_payments", Layer.Personal, TaxableEvent.CopyrightRoyalty, Track.PaidOut, pay.TaxedApartTax.AdvancePayments,
                taxRules.AdvancePayments.Source, taxRules.AdvancePayments.SurchargeAmounts),
        ];
}

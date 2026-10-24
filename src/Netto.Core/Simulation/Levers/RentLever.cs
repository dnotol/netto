using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Rules;

namespace Netto.Core.Simulation.Levers;

// The rent the company pays for the office in the director's home: it comes off what it earns, as given,
// deducted in full, and reaches the pocket; its net is taxed with the pay, its tax paid out of the pocket
// (GrossToNet's OtherIncomeTax). The office allowance excludes it, which the rules read from the
// allowances given, the allowances having passed their own rules before it.
internal sealed record RentLever(RentRules Rules) : Lever
{
    public override decimal FromProfit(Scenario scenario) => scenario.Rent.Rent;

    public override (LeverYear? Value, string? Error) Compute(Scenario scenario, decimal left)
    {
        var (rent, error) = RentCalculator.Compute(Rules, scenario.Rent, scenario.Allowances.Office);
        return rent is null ? (null, error) : (new RentYear(Rules, rent, scenario.Rent.Any), null);
    }
}

// The rent as the rules took it, Used once the company pays one; without one nothing is let and nothing
// taxed, while its cap stays for its card.
internal sealed record RentYear(RentRules Rules, RentResult Rent, bool Used) : LeverYear
{
    public override decimal ToPocket => Rent.Rent;

    public override TaxedWithPay TaxedWithPay => new() { OtherIncome = Rent.NetRent };

    // The rent reaches the pocket at what the company pays, kept at or below the cap so that none of it
    // is pay; the director is taxed on it, never less than the floor, less its flat charges.
    public override IReadOnlyList<Step> Steps() => !Used ? [] :
    [
        new Step("rent", Layer.Personal, TaxableEvent.Rent, Track.InPocket, Rent.Rent,
            Rules.Cap, [Rules.CapMultiplier, Rules.RevaluationCoefficient]),
        new Step("rent_charges", Layer.Personal, TaxableEvent.Rent, Track.TaxBaseOnly, Rent.Charges,
            Rules.NetRent, [Rules.ChargesRate, Rules.ChargesLimitMultiplier, Rules.RevaluationCoefficient]),
        new Step("rent_taxable_income", Layer.Personal, TaxableEvent.Rent, Track.TaxBaseOnly, Rent.NetRent,
            Rules.RentAsIncome, [Rules.FloorUplift, Rules.IndexationCoefficient]),
    ];

    // What the rent adds to the director's personal tax, taxed with the pay: the income tax and the
    // municipal surcharge on the total less the pay's own, the credit for low earned income it takes
    // away, and what that lost credit adds to the surcharge on the pay's tax, less the bonus. Each is
    // paid out of the pocket, the credit's as a positive amount since it is lost, not refunded.
    public override IReadOnlyList<Step> StepsAfterPay(GrossToNetResult pay, PersonalTaxRules taxRules, Source municipalSurcharge, RuleAmount municipalRate) =>
        !Used ? [] :
        [
            new Step("rent_income_tax", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, pay.OtherIncomeTax.IncomeTax,
                taxRules.IncomeTaxScale, taxRules.IncomeTaxAmounts),
            new Step("rent_municipal_surcharge", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, pay.OtherIncomeTax.MunicipalSurcharge,
                municipalSurcharge, [municipalRate]),
            new Step("rent_low_earned_income_credit_lost", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, pay.OtherIncomeTax.LowEarnedIncomeCreditLost,
                taxRules.LowEarnedIncomeCredit.Source, taxRules.LowEarnedIncomeCredit.Amounts),
            new Step("rent_income_tax_advance_payments", Layer.Personal, TaxableEvent.Rent, Track.PaidOut, pay.OtherIncomeTax.AdvancePayments,
                taxRules.AdvancePayments.Source, taxRules.AdvancePayments.SurchargeAmounts),
        ];
}

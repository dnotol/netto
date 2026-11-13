using Netto.Core.DirectorPay;
using Netto.Core.Equipment;
using Netto.Core.Rules;

namespace Netto.Core.Simulation.Levers;

// The IT and phone the company provides the director for private use, as the engine composes it into the
// year: here, where LeverOrder lists it, YearSimulation reading its result by its type for YearResult.Equipment
// alone. What the set costs the company less what the director pays it for its private use comes off what the
// company earns once one piece is provided, a cost of the company like a car's. It hands the year its benefit in
// kind as plain figures, as the car does: the flat-rate benefit is taxed as pay but never paid, the contribution
// leaves the pocket for the company, and the company deducts the costs in full, adding nothing back. Beside the
// flat allowances for the same costs, which the levers before it pay, it is refused.
internal sealed record EquipmentLever(EquipmentRules Rules) : Lever
{
    public override decimal FromProfit(Scenario scenario) => scenario.Equipment.Any ? scenario.Equipment.YearlyCosts - scenario.Equipment.Contribution : 0m;

    public override (LeverYear? Value, Refusal? Error) Compute(Scenario scenario, decimal left)
    {
        var (set, error) = EquipmentCalculator.Compute(Rules, scenario.Equipment, scenario.Allowances);
        return set is null ? (null, error) : (new EquipmentYear(Rules, set, scenario.Equipment.Any), null);
    }
}

// The set as the rules took it, Used once one piece is provided.
internal sealed record EquipmentYear(EquipmentRules Rules, EquipmentResult Set, bool Used) : LeverYear
{
    public override decimal ToPocket => Money.Minus(0m, Set.BenefitInKind.Contribution);

    public override TaxedWithPay TaxedWithPay => new() { BenefitInKind = Set.BenefitInKind.TaxedAsPay };

    // The set's costs, at what the company spends on them, a cost of the company that only sets its profit and
    // so its tax base, deducted in full; the flat benefit of its pieces, each value it used, read per appliance,
    // a subscription per phone number once one is provided, and for the days provided when that is part of the
    // year; the director's contribution leaving the pocket, a plain zero without one; and the benefit it leaves
    // taxed as pay, in Director pay's figures.
    public override IReadOnlyList<Step> Steps() => !Used ? [] :
    [
        new Step("equipment_costs", Layer.Corporate, TaxableEvent.Equipment, Track.TaxBaseOnly, Set.Costs, Rules.Deduction, []),
        new Step("equipment_benefit", Layer.Personal, TaxableEvent.Equipment, Track.TaxBaseOnly, Set.BenefitBeforeContribution,
            Rules.Benefit, [.. Set.Pieces.Select(piece => piece.Value)])
        {
            AlsoCites =
            [
                Rules.PerAppliance,
                .. Set.Pieces.Any(piece => piece.Kind == EquipmentKind.PhoneSubscription) ? [Rules.SubscriptionPerNumber] : Array.Empty<Source>(),
                .. Set.Days is { AllYear: false } ? [Rules.PartOfYear] : Array.Empty<Source>(),
            ],
        },
        new Step("equipment_contribution", Layer.Personal, TaxableEvent.Equipment, Track.InPocket, Money.Minus(0m, Set.Contribution),
            Rules.Contribution, []) { AlsoCites = [Rules.ContributionPerBenefit] },
        new Step("equipment_benefit_taxed", Layer.Personal, TaxableEvent.Equipment, Track.TaxBaseOnly, Set.Benefit,
            Rules.BenefitAsPay, []) { AlsoCites = [Rules.BenefitInContributions] },
    ];
}

using Netto.Core.BenefitsInKind;
using Netto.Core.Rules;

namespace Netto.Core.Equipment;

// The set as the rules take it. Pieces are its kinds provided, each with how many, its flat yearly value
// and its benefit for the days provided (Days); BenefitBeforeContribution is their sum, Benefit what the
// director is taxed on as pay, less their Contribution, never below zero. Costs are what the set costs the
// company, which it deducts in full. Without a set, all nothing.
public sealed record EquipmentResult(
    IReadOnlyList<EquipmentPiece> Pieces,
    DaysHeld? Days,
    decimal BenefitBeforeContribution,
    decimal Contribution,
    decimal Benefit,
    decimal Costs)
{
    public static EquipmentResult None { get; } = new([], null, 0m, 0m, 0m, 0m);

    // What the set does to the year, as YearSimulation composes it beside the other levers: the benefit
    // taxed as pay, the contribution and the costs, none of which the company cannot deduct.
    public BenefitInKind BenefitInKind => new(Benefit, Contribution, Costs, 0m);
}

// One kind of piece the company provides: how many (Count), the flat yearly value of one (Value), and their
// benefit for the days provided: one piece's value for those days, rounded to the cent, times their count.
public sealed record EquipmentPiece(EquipmentKind Kind, int Count, RuleAmount Value, decimal Benefit);

// The kinds of piece art. 18 §3, 10 AR/CIR 92 values, a tablet apart from a mobile phone though both are
// valued alike, since only a tablet excludes the computer allowance.
public enum EquipmentKind
{
    Pc,
    Tablet,
    MobilePhone,
    Internet,
    PhoneSubscription,
}

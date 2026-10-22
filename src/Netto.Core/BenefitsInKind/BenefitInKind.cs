namespace Netto.Core.BenefitsInKind;

// What something the company puts at the director's disposal for private use does to the year,
// whatever it is: the flat-rate benefit taxed as the director's pay though never paid in cash, what
// the director pays the company for that use, what it costs the company over the year, the part of
// that the company cannot deduct with the share of the benefit it adds back to its taxable income, and
// what lands in the muddy pocket. Plain figures: the module that values it produces them, and
// YearSimulation composes them beside the other levers, so the layers only ever see amounts.
public sealed record BenefitInKind(decimal TaxedAsPay, decimal Contribution, decimal Costs, decimal NotDeductible, decimal MuddyPocket)
{
    public static BenefitInKind None { get; } = new(0m, 0m, 0m, 0m, 0m);
}

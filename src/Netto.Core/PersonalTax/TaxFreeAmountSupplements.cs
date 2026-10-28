namespace Netto.Core.PersonalTax;

// The supplements the dependants add to the tax-free amount (CIR 92 art. 132 and 133): Children, the
// supplement for their number (art. 132 al. 1, 1° to 5°); ChildrenUnder3, those for children under 3 (6°);
// SingleParent, a single parent's (art. 133 al. 1, 1°); DependantsAged66InCare (7°) and OtherDependants
// (8°). ForChildren, 1° to 6°, is the part whose unused tax turns into the refundable credit (art. 134 §3);
// Others is the rest, which counts before it. In a joint return they go to one partner (art. 134 §4, 2°).
public sealed record TaxFreeAmountSupplements(
    decimal Children,
    decimal ChildrenUnder3,
    decimal SingleParent,
    decimal DependantsAged66InCare,
    decimal OtherDependants)
{
    public static TaxFreeAmountSupplements None { get; } = new(0m, 0m, 0m, 0m, 0m);

    public decimal ForChildren => Children + ChildrenUnder3;

    public decimal Others => SingleParent + DependantsAged66InCare + OtherDependants;

    public decimal Total => ForChildren + Others;
}

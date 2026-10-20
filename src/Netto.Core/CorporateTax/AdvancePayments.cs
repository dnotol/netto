namespace Netto.Core.CorporateTax;

// What the company pays in advance on the year's corporate tax, paid or planned by each of the
// income year's four deadlines, in their order. Nothing unless given.
public sealed record AdvancePayments(decimal First, decimal Second, decimal Third, decimal Fourth)
{
    public static AdvancePayments None { get; } = new(0m, 0m, 0m, 0m);

    public IReadOnlyList<decimal> ByDeadline => [First, Second, Third, Fourth];
}

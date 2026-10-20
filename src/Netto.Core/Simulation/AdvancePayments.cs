namespace Netto.Core.Simulation;

// What is paid in advance on a year's tax, the company's corporate tax or the director's income tax,
// paid or planned by each of the income year's four deadlines, in their order. Nothing unless given.
public sealed record AdvancePayments(decimal First, decimal Second, decimal Third, decimal Fourth)
{
    public static AdvancePayments None { get; } = new(0m, 0m, 0m, 0m);

    public IReadOnlyList<decimal> ByDeadline => [First, Second, Third, Fourth];
}

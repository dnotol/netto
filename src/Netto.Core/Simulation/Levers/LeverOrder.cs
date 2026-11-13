using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.Equipment;
using Netto.Core.Rent;
using Netto.Core.Vouchers;

namespace Netto.Core.Simulation.Levers;

// The levers of one income year in the order the law computes the year: what the company pays the
// director besides pay before the split shares what is left, the vouchers, the expense allowances, the
// rent of a home office, which the office allowance excludes, the IT and phone it provides, which the
// computer and internet allowances exclude, and the car it puts at their disposal;
// then the copyright royalty, whose limits are on the pay the split sets. Null when one of them has no
// rules for the year. A new lever takes its place here.
internal static class LeverOrder
{
    public static IReadOnlyList<Lever>? For(int incomeYear) =>
        VoucherRules.For(incomeYear) is { } vouchers && AllowanceRules.For(incomeYear) is { } allowances && RentRules.For(incomeYear) is { } rent
        && EquipmentRules.For(incomeYear) is { } equipment && CarRules.For(incomeYear) is { } car && CopyrightRules.For(incomeYear) is { } copyright
            ? [new VoucherLever(vouchers), new AllowanceLever(allowances), new RentLever(rent), new EquipmentLever(equipment), new CarLever(car), new CopyrightLever(copyright)]
            : null;
}

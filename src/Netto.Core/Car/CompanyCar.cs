namespace Netto.Core.Car;

// The car the company puts at its director's disposal for private use, all year, as the user declares
// it: its catalogue value as sold new to a private buyer, options and VAT included, discounts
// excluded; the date of its first registration; what it costs the company in the year, as the company
// bears it, the VAT it cannot recover included; whether the company pays the energy for private use,
// for an electric car its charging; and what the director pays the company for that use. An electric
// car ordered or leased before 2027. None unless given; there is a car only once its catalogue value
// is more than nothing.
public sealed record CompanyCar(
    decimal CatalogueValue,
    DateOnly FirstRegistration,
    decimal YearlyCosts,
    bool CompanyPaysPrivateEnergy,
    decimal PersonalContribution)
{
    public static CompanyCar None { get; } = new(0m, default, 0m, false, 0m);

    public bool Any => CatalogueValue != 0m;
}

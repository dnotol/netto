namespace Netto.Core.CompanyCar;

// The car the company puts at its director's disposal for private use, all year, as the user declares
// it: its catalogue value as sold new to a private buyer, options and VAT included, discounts
// excluded; the date of its first registration; what it costs the company in the year, as the company
// bears it, the VAT it cannot recover included; whether the company pays the energy for private use,
// for an electric car its charging; and what the director pays the company for that use. None unless
// given; there is a car only once its catalogue value is more than nothing.
public sealed record CarFacts(
    decimal CatalogueValue,
    DateOnly FirstRegistration,
    decimal YearlyCosts,
    bool CompanyPaysPrivateEnergy,
    decimal PersonalContribution)
{
    public static CarFacts None { get; } = new(0m, default, 0m, false, 0m);

    public bool Any => CatalogueValue != 0m;

    // What drives it; an electric car unless said otherwise.
    public CarEngine Engine { get; init; } = CarEngine.Electric;

    // Its CO2 in g/km as the vehicle registration holds it, or NoCo2Data when it holds none.
    public int Co2 { get; init; }

    public bool NoCo2Data { get; init; }

    // When the company ordered it, or signed its lease or rental contract.
    public DateOnly Acquired { get; init; }

    // A plug-in hybrid the user says is a fake one, with the CO2 of its matching car running on its
    // fuel alone, or NoMatchingCar when there is none.
    public bool FakeHybrid { get; init; }

    public int MatchingCarCo2 { get; init; }

    public bool NoMatchingCar { get; init; }

    // A natural-gas car under 12 fiscal horsepower, as the user says.
    public bool SmallNaturalGas { get; init; }
}

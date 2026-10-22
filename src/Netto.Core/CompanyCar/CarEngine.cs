namespace Netto.Core.CompanyCar;

// What drives the car, as the user declares it: an electric motor alone, or an engine running on
// petrol, diesel, LPG or natural gas, alone, with an electric motor (a hybrid), or with a battery the
// car charges from a socket (a plug-in hybrid).
public enum CarEngine
{
    Electric,
    Petrol,
    Diesel,
    Lpg,
    NaturalGas,
    PetrolHybrid,
    DieselHybrid,
    PetrolPlugInHybrid,
    DieselPlugInHybrid,
}

// What the CO2 a car's benefit counts rests on: nothing for an electric car (None), the CO2 the
// vehicle registration holds, the texts' emission when it holds none, a fake hybrid's matching car's,
// or a fake hybrid's own CO2 times the texts' multiplier when it has no matching car.
public enum CarCo2Basis
{
    None,
    Registered,
    WithoutData,
    MatchingCar,
    OwnTimesMultiplier,
}

public static class CarEngines
{
    // Every engine but an electric motor alone burns a fuel, so emits CO2.
    public static bool EmitsCo2(this CarEngine engine) => engine != CarEngine.Electric;

    public static bool IsPlugInHybrid(this CarEngine engine) => engine is CarEngine.PetrolPlugInHybrid or CarEngine.DieselPlugInHybrid;

    // Its engine runs on diesel, alone or beside an electric motor.
    public static bool RunsOnDiesel(this CarEngine engine) => engine is CarEngine.Diesel or CarEngine.DieselHybrid or CarEngine.DieselPlugInHybrid;
}

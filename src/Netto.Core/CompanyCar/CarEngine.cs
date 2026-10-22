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

public static class CarEngines
{
    // Every engine but an electric motor alone burns a fuel, so emits CO2.
    public static bool EmitsCo2(this CarEngine engine) => engine != CarEngine.Electric;

    public static bool IsPlugInHybrid(this CarEngine engine) => engine is CarEngine.PetrolPlugInHybrid or CarEngine.DieselPlugInHybrid;

    // Its engine runs on diesel, alone or beside an electric motor.
    public static bool RunsOnDiesel(this CarEngine engine) => engine is CarEngine.Diesel or CarEngine.DieselHybrid or CarEngine.DieselPlugInHybrid;
}

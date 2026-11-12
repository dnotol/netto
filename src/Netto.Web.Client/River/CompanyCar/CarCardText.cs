using Netto.Core.CompanyCar;

namespace Netto.Web.Client.River.CompanyCar;

// The resource keys the car card builds from values, gone through by the card and its tests alike.
internal static class CarCardText
{
    // The company car against the same car bought privately: what it gains, what it costs more, the
    // same, or the comparison waits for what that car would cost, while it is not given or is refused.
    public static string Private(CarComparison comparison) => comparison.AgainstPrivate switch
    {
        null => "Private_Waiting",
        > 0m => "Private_Gain",
        < 0m => "Private_Loss",
        _ => "Private_Tie",
    };

    // The summary says what the car adds to the pocket or takes from it, the plan with it less the plan
    // without (LeverGain); a card with no car yet says so alone.
    public static string Summary(bool hasCar, decimal gain) => hasCar ? LeverGainText.Summary(gain) : "SummaryNoCar";

    // What drives the car, as the engine select names it.
    public static string Engine(CarEngine engine) => $"Engine_{engine}";

    // The energy for private use the company may pay: an electric car's charging, a plug-in hybrid's
    // fuel or charging (the SPF course's Module 5.3 counts both), any other car's fuel.
    public static string PrivateEnergy(CarEngine engine) => $"PrivateEnergy_{Energy(engine)}";

    public static string PrivateEnergyHint(CarEngine engine) => $"PrivateEnergyHint_{Energy(engine)}";

    // Why 40% is added back when the company pays a car's charging: the administration's reading of
    // « fuel », for an electric car and a plug-in hybrid; none for a car the law's word covers.
    public static string? Reading(CarEngine engine) => engine switch
    {
        CarEngine.Electric => "AddBackReading",
        _ when engine.IsPlugInHybrid() => "AddBackReading_PlugIn",
        _ => null,
    };

    // What the CO2 percentage counts.
    public static string Co2(CarResult car) => $"Co2_{(car.Co2Basis == CarCo2Basis.None ? "Electric" : car.Co2Basis.ToString())}";

    // A car that emits CO2 held to the lowest or the highest percentage, as the engine says it was.
    public static string? Co2Held(CarResult car, CarRules rules) => car.Co2Basis switch
    {
        CarCo2Basis.None => null,
        _ when car.Co2Amounts.Contains(rules.MinimumCo2Percentage) => "Co2_HeldToMinimum",
        _ when car.Co2Amounts.Contains(rules.MaximumCo2Percentage) => "Co2_HeldToMaximum",
        _ => null,
    };

    // The line of a part of the benefit's year by its months, a month begun counting whole: the whole year,
    // one month, or from one month to another.
    public static string Period(DateOnly from, DateOnly to) => (from.Month, to.Month) switch
    {
        (1, 12) => "Figure_Period_Year",
        _ when from.Month == to.Month => "Figure_Period_Month",
        _ => "Figure_Period",
    };

    // How the company deducts costs it does not deduct in full, in the bubble of what it cannot deduct: the
    // part equal to the benefit taxed and the contribution alone, or that part and the rest at its rate;
    // none while it deducts them all, and there is no such line.
    public static string? DeductionHelp(CarResult car) => car switch
    {
        { NotDeductibleCosts: 0m } => null,
        { DeductionRate: 0m } => "Help_Deduction_None",
        _ => "Help_Deduction_Rate",
    };

    private static string Energy(CarEngine engine) => engine switch
    {
        CarEngine.Electric => "Charging",
        _ when engine.IsPlugInHybrid() => "FuelOrCharging",
        _ => "Fuel",
    };
}

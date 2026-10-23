using Netto.Core.CompanyCar;

namespace Netto.Web.Client.River.CompanyCar;

// The resource keys the car card builds from values, gone through by the card and its tests alike.
internal static class CarCardText
{
    // The line of what the car takes in cash from the pocket, against the same plan without it: some,
    // none, or it leaves more cash there, which a benefit that keeps the reduced corporate rate can do.
    public static string Cash(CarComparison comparison) => comparison.CashCost switch
    {
        > 0m => "Figure_Cash_Takes",
        < 0m => "Figure_Cash_Adds",
        _ => "Figure_Cash_None",
    };

    // The company car against the same car bought privately: what it gains, what it costs more, the
    // same, or the comparison waits for what that car would cost, while it is not given or is refused.
    public static string Private(CarComparison comparison) => comparison.AgainstPrivate switch
    {
        null => "Private_Waiting",
        > 0m => "Private_Gain",
        < 0m => "Private_Loss",
        _ => "Private_Tie",
    };

    // The summary ends on what the split shares: « before your pay and royalty » while the royalty is in
    // the plan.
    public static string Summary(bool hasCar, bool royalty) => (hasCar ? "Summary" : "SummaryNoCar") + (royalty ? "_Royalty" : "");

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

    // The line of how the company deducts the car's costs: all of them; all of them, being no more than
    // the benefit taxed and the contribution; that part alone; or that part and the rest at its rate.
    public static string Deduction(CarResult car) => car switch
    {
        { DeductionRate: 1m } => "Figure_Deduction_Full",
        _ when car.CarvedOut == car.Costs => "Figure_Deduction_AllCarvedOut",
        { DeductionRate: 0m } => "Figure_Deduction_None",
        _ => "Figure_Deduction_Rate",
    };

    // The rate the costs are deducted at is in the line's label when it applies to them: all of them, or
    // the rest beyond the part equal to the benefit taxed and the contribution.
    public static bool DeductionTakesItsRate(CarResult car) => Deduction(car) is "Figure_Deduction_Full" or "Figure_Deduction_Rate";

    // How the company deducts costs it does not deduct at one rate, in the line's bubble; none for costs
    // deducted in full.
    public static string? DeductionHelp(CarResult car) => Deduction(car) switch
    {
        "Figure_Deduction_AllCarvedOut" => "Help_Deduction_AllCarvedOut",
        "Figure_Deduction_None" => "Help_Deduction_None",
        "Figure_Deduction_Rate" => "Help_Deduction_Rate",
        _ => null,
    };

    private static string Energy(CarEngine engine) => engine switch
    {
        CarEngine.Electric => "Charging",
        _ when engine.IsPlugInHybrid() => "FuelOrCharging",
        _ => "Fuel",
    };
}

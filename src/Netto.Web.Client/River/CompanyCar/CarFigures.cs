using Netto.Core.CompanyCar;

namespace Netto.Web.Client.River.CompanyCar;

// The company car's figures, line by line, as its card shows them before where its money goes (FigureList),
// keys of the card's texts with their amounts: how the benefit in kind is built and what the company does
// with the car's costs, then the plan with the car weighed in cash against the same plan without it. What a
// label cannot say, a line's bubble does (Help); a rule the card no longer says in a sentence is in its
// details, on the step it is the rule of. The view model selects and orders the engine's figures, and
// computes none.
internal static class CarFigures
{
    // The benefit as the engine took the car: before the contribution, at least the year's minimum, at
    // its CO2 percentage, period by period at the share of its value the car's age leaves; the contribution
    // off it, and the benefit taxed as pay. Then what the company deducts of the car's costs, what it cannot
    // deduct and the share of the benefit it adds back to its taxable income. A line of nothing is left
    // out (no contribution, no costs, every cost deducted), but not the benefit taxed, which is the benefit's
    // result even when a contribution leaves none. None without a car.
    public static IReadOnlyList<FigureLine> Benefit(CarResult car) => car.Periods.Count == 0
        ? []
        :
        [
            new("Figure_Benefit", car.BenefitBeforeContribution),
            new("Figure_Co2", car.Co2Percentage) { Format = FigureFormat.Percentage },
            .. car.Periods.Select(Period),
            .. car.Contribution > 0m ? [new FigureLine("Figure_Contribution", -car.Contribution)] : Array.Empty<FigureLine>(),
            new("Figure_BenefitTaxed", car.Benefit) { Help = "Help_BenefitTaxed" },
            .. Deduction(car),
            new("Figure_AddBack", car.AddBack) { Args = [Formats.WholeShare(car.AddBackRate?.Value ?? 0m)], Help = "Help_AddBack" },
        ];

    // The plan with its car against the same plan without it, at the pay set: the pocket in each, the cash
    // the car takes from it or leaves more in it, and what its benefit adds to the contributions and tax on
    // pay, which Director pay shows.
    public static IReadOnlyList<FigureLine> Cash(CarComparison comparison) =>
    [
        new("Figure_WithCar", comparison.PocketWith),
        new("Figure_WithoutCar", comparison.PocketWithout),
        new(CarCardText.Cash(comparison), Math.Abs(comparison.CashCost)),
        new("Figure_TaxesOnPay", comparison.TaxesOnPayAdded) { Help = "Help_TaxesOnPay" },
    ];

    // A part of the year at one age coefficient, by its months, since a month begun counts whole: the
    // whole year, one month, or from one month to another.
    private static FigureLine Period(CarBenefitPeriod period)
    {
        var (from, to) = (period.Period.From, period.Period.To);
        var coefficient = Formats.WholeShare(period.Period.AgeCoefficient.Value);
        var (key, args) = (from.Month, to.Month) switch
        {
            (1, 12) => ("Figure_Period_Year", new[] { coefficient }),
            _ when from.Month == to.Month => ("Figure_Period_Month", [Month(from), coefficient]),
            _ => ("Figure_Period", [Month(from), Month(to), coefficient]),
        };

        return new FigureLine(key, period.Benefit) { Args = args, Help = "Help_Period" };
    }

    // What the company deducts of the costs, at the rate the label says, how it deducts them when not in
    // full in the bubble, and what it cannot deduct; nothing without costs.
    private static IEnumerable<FigureLine> Deduction(CarResult car)
    {
        if (car.Costs == 0m)
            return [];

        var rate = Formats.Percentage(car.DeductionRate);
        var deducted = new FigureLine(CarCardText.Deduction(car), car.Deductible)
        {
            Args = CarCardText.DeductionTakesItsRate(car) ? [rate] : [],
            Help = CarCardText.DeductionHelp(car),
            HelpArgs = [rate],
        };

        return car.Costs == car.Deductible
            ? [deducted]
            : [deducted, new FigureLine("Figure_NotDeductible", car.Costs - car.Deductible) { Help = "Help_NotDeductible" }];
    }

    private static string Month(DateOnly date) => Formats.Month(date.Year, date.Month);
}

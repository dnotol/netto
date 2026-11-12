using Netto.Core.CompanyCar;

namespace Netto.Web.Client.River.CompanyCar;

// The company car's figures, line by line, as its card shows them before where its money goes (FigureList),
// keys of the card's texts with their amounts: how the benefit in kind is built and what the company cannot
// deduct of the car's costs, then the plan with the car weighed in cash against the same plan without it.
// Each amount is said once: a line equal to the line it builds is left out. What a label cannot say, a
// line's bubble does (Help); a rule the card no longer says in a sentence is in its details, on the step it
// is the rule of. The view model selects and orders the engine's figures, and computes none.
internal static class CarFigures
{
    // The lines the map draws as what the car changes of a tax base (Tabs/MapLinks).
    public const string BenefitTaxed = "Figure_BenefitTaxed";

    public const string NotDeductible = "Figure_NotDeductible";

    public const string AddBack = "Figure_AddBack";

    // The benefit as the engine took the car, then what the company cannot deduct of its costs and the share
    // of the benefit it adds back to its taxable income. On the scale: its CO2 percentage, the periods at the
    // share of its value the car's age leaves while there are several (one is the benefit itself), then the
    // benefit before the contribution and the contribution off it while there is one, and the benefit taxed
    // as pay. Held to the year's minimum: the minimum alone, the value the scale gives in its bubble, no CO2
    // percentage nor period, which no longer set it. The benefit taxed is always a line, the map reading it,
    // even when a contribution leaves none. Each rate the rules set is theirs, as they state it. None without
    // a car.
    public static IReadOnlyList<FigureLine> Benefit(CarResult car) => car.Periods.Count == 0
        ? []
        :
        [
            .. car.AtTheMinimum ? [] : Scale(car),
            .. car.Contribution > 0m
                ? [Minimum(new FigureLine("Figure_Benefit", car.BenefitBeforeContribution), car, CarCardText.BenefitLabel(car)), new FigureLine("Figure_Contribution", -car.Contribution)]
                : Array.Empty<FigureLine>(),
            Minimum(new FigureLine(BenefitTaxed, car.Benefit) { Help = "Help_BenefitTaxed" }, car, CarCardText.BenefitTaxedLabel(car)),
            .. NotDeducted(car),
            new(AddBack, car.AddBack) { Args = car.AddBackRate is { } rate ? [Formats.RuleAmount(rate)] : [], Help = "Help_AddBack" },
        ];

    // The plan with its car against the same plan without it, at the pay the plan with it runs at: the pocket
    // in each, and what its benefit adds to the contributions and tax on pay, which Director pay shows. What
    // the car takes from the pocket is the card's summary.
    public static IReadOnlyList<FigureLine> Cash(CarComparison comparison) =>
    [
        new("Figure_WithCar", comparison.PocketWith),
        new("Figure_WithoutCar", comparison.PocketWithout),
        new("Figure_TaxesOnPay", comparison.TaxesOnPayAdded) { Help = "Help_TaxesOnPay" },
    ];

    // The CO2 percentage, an electric car's named as the lowest the law gives, and the periods while there
    // are several.
    private static IEnumerable<FigureLine> Scale(CarResult car) =>
    [
        new("Figure_Co2", car.Co2Percentage) { Format = FigureFormat.Percentage, Label = CarCardText.Co2Label(car) },
        .. car.Periods.Count > 1 ? car.Periods.Select(Period) : [],
    ];

    // A line worded as the year's minimum (its label, none on the scale), what the scale would give in its
    // bubble.
    private static FigureLine Minimum(FigureLine line, CarResult car, string? label) => label is null
        ? line
        : line with { Label = label, Help = "Help_Minimum", HelpArgs = [Formats.Euros(car.BenefitOnTheScale)] };

    // A part of the year at one age coefficient, by its first and last months, since a month begun counts
    // whole, and its coefficient; its label, one month or from one to another, says those it needs.
    private static FigureLine Period(CarBenefitPeriod period)
    {
        var (from, to) = (period.Period.From, period.Period.To);

        return new FigureLine(CarCardText.Period(from, to), period.Benefit)
        {
            Args = [Month(from), Month(to), Formats.RuleAmount(period.Period.AgeCoefficient)],
            Help = "Help_Period",
        };
    }

    // What the company cannot deduct of the costs, the rate it deducts them at in the label and how in the
    // bubble; nothing while it deducts them all, the costs' own line saying them.
    private static IEnumerable<FigureLine> NotDeducted(CarResult car)
    {
        if (car.NotDeductibleCosts == 0m)
            return [];

        var rate = Formats.Percentage(car.DeductionRate);
        return
        [
            new FigureLine(NotDeductible, car.NotDeductibleCosts)
            {
                Label = CarCardText.NotDeductibleLabel(car), Args = [rate], Help = CarCardText.DeductionHelp(car), HelpArgs = [rate],
            },
        ];
    }

    private static string Month(DateOnly date) => Formats.Month(date.Year, date.Month);
}

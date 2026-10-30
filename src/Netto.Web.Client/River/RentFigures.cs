using Netto.Core.Rent;

namespace Netto.Web.Client.River;

// The rent's figure, line by line (FigureList), from its view: what the company pays, the rent, which
// it takes off what it earns, above the floor and under it alike; then what the rules tax it on, the rent
// or, when the rent is under it, the floor, whose bubble says what it is; less its flat charges, whose
// bubble says what they are; which is what joins the taxable income of the pay, its bubble saying, with a
// partner taxed jointly, that all of it is taxed in the director's column. What that adds to the
// personal tax is the card's drain lines. No line of nothing, so none while no rent is paid.
internal static class RentFigures
{
    public const string CompanyPays = "Figure_CompanyPays";

    public const string TaxedOnTheRent = "Figure_TaxedOnTheRent";

    public const string TaxedOnTheFloor = "Figure_TaxedOnTheFloor";

    public const string FlatCharges = "Figure_FlatCharges";

    public const string JoinsTaxableIncome = "Figure_JoinsTaxableIncome";

    // What the floor is, in the bubble after its line's label.
    public const string FloorHelp = "Help_Floor";

    // What the flat charges are, in the bubble after their line's label.
    public const string FlatChargesHelp = "Help_FlatCharges";

    // What the line the net rent joins the taxable income on says, with a partner taxed jointly: all of it
    // is taxed in the director's column, which is not what the law does for a home owned together.
    public const string JoinsTaxableIncomeHelp = "Help_JoinsTaxableIncome";

    // The bubbles state the rates and coefficients of the view's rules: the floor's indexation and
    // uplift, the flat charges' rate, limit and revaluation.
    public static IReadOnlyList<FigureLine> From(RentView view) => FigureLines.Shown(
    [
        new FigureLine(CompanyPays, view.Rent),
        view.TaxedOnTheFloor
            ? new FigureLine(TaxedOnTheFloor, view.TaxableRent) { Help = FloorHelp, HelpArgs = FloorArgs(view.Rules) }
            : new FigureLine(TaxedOnTheRent, view.TaxableRent),
        new FigureLine(FlatCharges, -view.Charges) { Help = FlatChargesHelp, HelpArgs = FlatChargesArgs(view.Rules) },
        new FigureLine(JoinsTaxableIncome, view.NetRent) { Help = view.TaxedJointly ? JoinsTaxableIncomeHelp : null },
    ]);

    private static string[] FloorArgs(RentRules? rules) =>
        rules is null ? [] : [Formats.RuleAmount(rules.IndexationCoefficient), Formats.RuleAmount(rules.FloorUplift)];

    private static string[] FlatChargesArgs(RentRules? rules) =>
        rules is null ? [] : [Formats.RuleAmount(rules.ChargesRate), Formats.Fraction(rules.ChargesLimitMultiplier), Formats.RuleAmount(rules.RevaluationCoefficient)];
}

namespace Netto.Web.Client.River;

// The rent's figure, line by line (FigureList), from its view: what the company pays, the rent, which
// it takes off what it earns, above the floor and under it alike; then what the rules tax it on, the rent
// or, when the rent is under it, the floor, whose bubble says what it is; less its flat charges, whose
// bubble says what they are; which is what joins the taxable income of the pay. What that adds to the
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

    public static IReadOnlyList<FigureLine> From(RentView view) => FigureLines.Shown(
    [
        new FigureLine(CompanyPays, view.Rent),
        view.TaxedOnTheFloor ? new FigureLine(TaxedOnTheFloor, view.TaxableRent) { Help = FloorHelp } : new FigureLine(TaxedOnTheRent, view.TaxableRent),
        new FigureLine(FlatCharges, -view.Charges) { Help = FlatChargesHelp },
        new FigureLine(JoinsTaxableIncome, view.NetRent),
    ]);
}

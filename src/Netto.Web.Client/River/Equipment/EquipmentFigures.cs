using Netto.Core.Equipment;

namespace Netto.Web.Client.River.Equipment;

// The IT and phone's figures, line by line, as its card shows them before where its money goes (FigureList),
// keys of the card's texts with their amounts: each piece's flat value, then the benefit before and after the
// contribution, then the plan with the set weighed in cash against the same plan without it. Each amount is
// said once. The view model selects and orders the engine's figures, and computes none but a piece's value
// for the days provided, its benefit over how many.
internal static class EquipmentFigures
{
    // The line the map draws as what the set changes of a tax base (Tabs/MapLinks).
    public const string BenefitTaxed = "Figure_BenefitTaxed";

    // Each kind provided with its count and one piece's value, for the days provided when that is not the
    // whole year, with those days; then, while there is a contribution, the benefit before it and the
    // contribution off it; and the benefit taxed as pay, always a line, the map reading it. None without a set.
    public static IReadOnlyList<FigureLine> Benefit(EquipmentResult set) => set.Pieces.Count == 0
        ? []
        :
        [
            .. set.Pieces.Select(piece => Piece(piece, set.Days)),
            .. set.Contribution > 0m
                ? [new FigureLine("Figure_Benefit", set.BenefitBeforeContribution), new FigureLine("Figure_Contribution", -set.Contribution)]
                : Array.Empty<FigureLine>(),
            new(BenefitTaxed, set.Benefit) { Help = "Help_BenefitTaxed" },
        ];

    // The plan with the set against the same plan without it, at the pay the plan with it runs at, and what its
    // benefit adds to the taxes on pay, which Director pay shows. What it takes from the pocket is the summary.
    public static IReadOnlyList<FigureLine> Cash(EquipmentComparison comparison) =>
    [
        new("Figure_WithEquipment", comparison.PocketWith),
        new("Figure_WithoutEquipment", comparison.PocketWithout),
        new("Figure_TaxesOnPay", comparison.TaxesOnPayAdded) { Help = "Help_TaxesOnPay" },
    ];

    private static FigureLine Piece(EquipmentPiece piece, Netto.Core.DaysHeld? days)
    {
        var count = piece.Count.ToString(Formats.Culture);
        var each = Formats.Euros(piece.Benefit / piece.Count);
        return days is { AllYear: false } part
            ? new FigureLine(EquipmentCardText.Piece(piece.Kind, allYear: false), piece.Benefit)
            {
                Args = [count, each, part.Days.ToString(Formats.Culture)], Help = "Help_PieceDays", HelpArgs = [Formats.RuleAmount(piece.Value)],
            }
            : new FigureLine(EquipmentCardText.Piece(piece.Kind, allYear: true), piece.Benefit) { Args = [count, each] };
    }
}

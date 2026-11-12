namespace Netto.Web.Client.River;

// The expense allowances' figure, line by line (FigureList), from their view: what the company pays out for
// them all, which it takes off what it earns and deducts in full, so nothing of it is added back. Where
// each goes is the card's drain lines; while one carries it whole, it says the payout, and no line repeats
// it. No line of nothing, so none while it pays none.
internal static class AllowanceFigures
{
    public const string CompanyPays = "Figure_CompanyPays";

    public static IReadOnlyList<FigureLine> From(AllowanceView view) =>
        DrainLines.Shown(view.Drains).Count > 1 ? FigureLines.Shown([Payout(view)]) : [];

    // What the company pays out for the allowances, the map's share of the company's line, whether the card
    // draws it as a line or its one drain line says it.
    public static FigureLine Payout(AllowanceView view) => new(CompanyPays, view.Total);
}

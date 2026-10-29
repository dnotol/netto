namespace Netto.Web.Client.River;

// The expense allowances' figure, line by line (FigureList), from their view: what the company pays for
// them all, which it takes off what it earns and deducts in full, so nothing of it is added back. Where
// each goes is the card's drain lines. No line of nothing, so none while it pays none.
internal static class AllowanceFigures
{
    public const string CompanyPays = "Figure_CompanyPays";

    public static IReadOnlyList<FigureLine> From(AllowanceView view) => FigureLines.Shown([new FigureLine(CompanyPays, view.Total)]);
}

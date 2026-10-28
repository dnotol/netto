namespace Netto.Web.Client.River;

// The dividend's figure, line by line (FigureList), from its view: this year's profit after tax, once
// profits kept from earlier years join it in what can be paid out, those the disclosure above states;
// the most that can be paid out, once a reserve is kept, which says with the dividend and what is kept
// (the drain lines) whether the reserve is covered; then the dividend paid out of it, the base of each
// drain line's share. No line of nothing.
internal static class DividendFigures
{
    public const string ProfitAfterTax = "Figure_ProfitAfterTax";

    public const string MostPaidOut = "Figure_MostPaidOut";

    public const string Dividend = "Figure_Dividend";

    public static IReadOnlyList<FigureLine> From(DividendView view)
    {
        List<FigureLine> lines = [];
        if (view.EarlierProfits > 0m)
            lines.Add(new FigureLine(ProfitAfterTax, view.ProfitAfterTax));
        if (view.Reserve > 0m)
            lines.Add(new FigureLine(MostPaidOut, view.Maximum));

        lines.Add(new FigureLine(Dividend, view.Dividend));
        return FigureLines.Shown(lines);
    }
}

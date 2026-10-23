namespace Netto.Web.Client.River;

// The dividend's figure, line by line (FigureList), from its view: this year's profit after tax, once
// profits kept from earlier years join it in what can be paid out, those the disclosure above states;
// then the dividend paid out of them, the base of each drain line's share. No line of nothing.
internal static class DividendFigures
{
    public const string ProfitAfterTax = "Figure_ProfitAfterTax";

    public const string Dividend = "Figure_Dividend";

    public static IReadOnlyList<FigureLine> From(DividendView view)
    {
        IEnumerable<FigureLine> lines = view.EarlierProfits > 0m
            ? [new FigureLine(ProfitAfterTax, view.ProfitAfterTax), new FigureLine(Dividend, view.Dividend)]
            : [new FigureLine(Dividend, view.Dividend)];

        return [.. lines.Where(line => Math.Round(line.Amount, 2, MidpointRounding.AwayFromZero) != 0m)];
    }
}

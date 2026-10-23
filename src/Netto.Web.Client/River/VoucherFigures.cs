namespace Netto.Web.Client.River;

// The vouchers' figure, line by line (FigureList), from their view: what the company pays for them, and
// what it cannot deduct of it, added back to its taxable income, which Corporate tax counts. No line of
// nothing, so none while it grants none.
internal static class VoucherFigures
{
    public const string CompanyPays = "Figure_CompanyPays";

    public const string AddedBack = "Figure_AddedBack";

    public static IReadOnlyList<FigureLine> From(VoucherView view) =>
    [
        .. new[] { new FigureLine(CompanyPays, view.Total), new FigureLine(AddedBack, view.Disallowed) }
            .Where(line => Math.Round(line.Amount, 2, MidpointRounding.AwayFromZero) != 0m),
    ];
}

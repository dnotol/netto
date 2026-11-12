namespace Netto.Web.Client.River;

// The vouchers' figure, line by line (FigureList), from their view: what the company pays out for them,
// and what it cannot deduct of it, added back to its taxable income, which Corporate tax counts. The
// payout is a line only while several kinds share it: one kind's drain line to the pocket says it whole.
// No line of nothing, so none while it grants none.
internal static class VoucherFigures
{
    public const string CompanyPays = "Figure_CompanyPays";

    public const string AddedBack = "Figure_AddedBack";

    public static IReadOnlyList<FigureLine> From(VoucherView view) =>
        FigureLines.Shown([.. DrainLines.Shown(view.Drains).Count > 1 ? [Payout(view)] : Array.Empty<FigureLine>(), new FigureLine(AddedBack, view.Disallowed)]);

    // What the company pays out for the vouchers, the map's share of the company's line, whether the card
    // draws it as a line or its one drain line says it.
    public static FigureLine Payout(VoucherView view) => new(CompanyPays, view.Total);
}

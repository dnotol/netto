namespace Netto.Web.Client.River;

// Corporate tax's figures as lines (FigureLine), which its card words from its own texts: what its
// taxable income adds back to the profit the split leaves the company. They select the engine's
// figures; where the profit goes is the card's drain list.
internal static class CorporateTaxFigures
{
    // The taxable income, then each part added back above nothing, explained in its bubble: what the
    // company cannot deduct of the vouchers, the costs of a company car it cannot deduct, and the share
    // of the car's benefit it adds back. None while nothing is added back: the taxable income is then
    // the profit the summary says.
    public static IReadOnlyList<FigureLine> Of(CorporateTaxView view)
    {
        FigureLine[] parts =
        [
            new("Figure_AddedBackVouchers", view.DisallowedVouchers) { Help = "Help_AddedBackVouchers" },
            new("Figure_AddedBackCarCosts", view.CarCostsNotDeductible) { Help = "Help_AddedBackCarCosts" },
            new("Figure_AddedBackCarBenefit", view.CarAddBack) { Help = "Help_AddedBackCarBenefit" },
        ];
        var addedBack = parts.Where(part => part.Amount > 0m).ToList();

        return addedBack.Count == 0 ? [] : [new FigureLine("Figure_TaxableIncome", view.TaxableIncome), .. addedBack];
    }
}

using Netto.Core.DirectorPay;

namespace Netto.Web.Client.River;

// Director pay's figures as lines (FigureLine), which its card words from its own texts: what the pay is
// taxed on besides its gross, with the taxable pay they make (Taxed), and what more by the last
// deadline leaves no surcharge due while one is (Surcharge). They select the engine's figures; where the
// pay goes is the card's drain list.
internal static class DirectorPayFigures
{
    // What the pay is taxed on besides its gross, each part above nothing and explained in its bubble:
    // the contributions the company pays on the director's behalf, which count as pay, beside the gross
    // they are paid with; a company car's benefit in kind, never paid in cash; and a copyright royalty's
    // part beyond its limits, paid besides the payslip. Then the taxable pay the gross and they make,
    // which they add up to. None while the pay is taxed on its gross alone.
    public static IReadOnlyList<FigureLine> Taxed(GrossToNetResult pay)
    {
        FigureLine[] parts =
        [
            new("Figure_CompanyContributions", pay.ContributionsPaidByCompany) { Help = "PaidByHint" },
            new("Figure_BenefitInKind", pay.BenefitInKind) { Help = "Help_BenefitInKind" },
            new("Figure_RequalifiedPay", pay.RequalifiedPay) { Help = "Help_RequalifiedPay" },
        ];
        var taxed = parts.Where(part => part.Amount > 0m).ToList();

        return taxed.Count == 0 ? [] : [.. taxed, new FigureLine("Figure_TaxablePay", pay.TaxablePay)];
    }

    // What more by the last deadline leaves no surcharge due, while one is: its amount is among the
    // pay's drains. None while none is due, the card saying why.
    public static IReadOnlyList<FigureLine> Surcharge(DirectorPayView view) => view.Surcharge == TaxSurchargeState.Due
        ? [new FigureLine(DirectorPayCardText.Surcharge(TaxSurchargeState.Due), view.TopUp) { Args = [Formats.Day(view.LastDeadline)] }]
        : [];
}

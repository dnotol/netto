using Netto.Core.DirectorPay;

namespace Netto.Web.Client.River;

// Director pay's figures as lines (FigureLine), which its card words from its own texts: what the pay is
// taxed on besides its gross, with the taxable pay they make (Taxed), and what more by the first
// deadline still open leaves no surcharge due while one is (Surcharge). They select the engine's
// figures; where the pay goes is the card's drain list.
internal static class DirectorPayFigures
{
    // What the pay is taxed on besides its gross, each part above nothing and explained in its bubble:
    // the contributions the company pays on the director's behalf, which count as pay, beside the gross
    // they are paid with, and the fund's fee it pays with them; a company car's benefit in kind, never
    // paid in cash; and a copyright royalty's part beyond its limits, paid besides the payslip. Then the
    // taxable pay the gross and they make, which they add up to. None while the pay is taxed on its gross
    // alone.
    public static IReadOnlyList<FigureLine> Taxed(GrossToNetResult pay)
    {
        // The company's contributions are never nothing when it pays them: the minimum applies.
        var companyPays = pay.ContributionsPaidByCompany > 0m;
        FigureLine[] parts =
        [
            new("Figure_CompanyContributions", companyPays ? pay.SocialContributions : 0m) { Help = "PaidByHint" },
            new("Figure_CompanyFundFee", companyPays ? pay.FundFee : 0m) { Help = "FundFeeHint" },
            new("Figure_BenefitInKind", pay.BenefitInKind) { Help = "Help_BenefitInKind" },
            new("Figure_RequalifiedPay", pay.RequalifiedPay) { Help = "Help_RequalifiedPay" },
        ];
        var taxed = parts.Where(part => part.Amount > 0m).ToList();

        return taxed.Count == 0 ? [] : [.. taxed, new FigureLine("Figure_TaxablePay", pay.TaxablePay)];
    }

    // What more by the first deadline still open leaves no surcharge due, while one is and a deadline is
    // open: the surcharge's amount is among the pay's drains. None otherwise, the card saying why
    // (DirectorPayCardText.Note).
    public static IReadOnlyList<FigureLine> Surcharge(MoreBy? more) => more is null
        ? []
        : [new FigureLine(DirectorPayCardText.Surcharge(TaxSurchargeState.Due), more.Amount) { Args = [Formats.Day(more.Deadline)] }];
}

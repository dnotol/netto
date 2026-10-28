namespace Netto.Web.Client.River;

// The Household card's figures as lines (FigureLine), which the card words from its own texts: the
// director's net professional income, the marital quotient, taken off it for the partner or added to it from
// theirs, the director's other income taxed with it, a home office's rent, and their taxable income; then the
// partner's net professional income and their taxable income; and what the quotient saves the pay's tax, or
// costs it, since it can lower the credit for children more than the tax. A line of nothing is left out
// (FigureLines). They select the joint assessment's columns, the engine's.
internal static class HouseholdFigures
{
    public const string QuotientToPartner = "Figure_QuotientToPartner";

    public const string QuotientFromPartner = "Figure_QuotientFromPartner";

    public static IReadOnlyList<FigureLine> Of(HouseholdView view) => FigureLines.Shown(
    [
        new FigureLine("Figure_YourIncome", view.Director.ProfessionalIncome),
        .. Quotient(view) is { } quotient ? [quotient] : Array.Empty<FigureLine>(),
        new FigureLine("Figure_YourOtherIncome", view.Director.OtherIncome) { Help = "Help_OtherIncome" },
        new FigureLine("Figure_YourTaxableIncome", view.Director.TaxableIncome),
        new FigureLine("Figure_PartnerIncome", view.Partner.ProfessionalIncome),
        new FigureLine("Figure_PartnerTaxableIncome", view.Partner.TaxableIncome),
        view.QuotientSaves < 0m
            ? new FigureLine("Figure_QuotientCosts", -view.QuotientSaves) { Help = "Help_QuotientSaves" }
            : new FigureLine("Figure_QuotientSaves", view.QuotientSaves) { Help = "Help_QuotientSaves" },
    ]);

    // What declaring the household saves the river's tax altogether, against the director filing alone with no
    // dependants, or what it costs it: the card's last line in a joint year, where it is more than the quotient.
    public static FigureLine Altogether(decimal saves) => saves < 0m
        ? new FigureLine("Figure_HouseholdCosts", -saves) { Help = "Help_Altogether" }
        : new FigureLine("Figure_HouseholdSaves", saves) { Help = "Help_Altogether" };

    // The marital quotient, the one line the map draws of the card, from the director's income to the
    // partner's, taken off it, or from the partner's to the director's, added to it, its bubble saying the
    // rate and the cap that applies; none while it attributes nothing, the partner earning 30% or more of the
    // two incomes together and the director too.
    public static FigureLine? Quotient(HouseholdView view)
    {
        string[] rule = [Formats.RuleAmount(view.Rate), Formats.RuleAmount(view.Cap)];
        return view switch
        {
            { Partner.Quotient: > 0m } => new FigureLine(QuotientToPartner, -view.Partner.Quotient) { Help = "Help_Quotient", HelpArgs = rule },
            { Director.Quotient: > 0m } => new FigureLine(QuotientFromPartner, view.Director.Quotient) { Help = "Help_Quotient", HelpArgs = rule },
            _ => null,
        };
    }
}
